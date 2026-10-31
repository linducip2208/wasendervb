using Microsoft.Web.WebView2.WinForms;
using Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;
using WASender.enums;
using WASender.Models;

namespace WASender
{
    public partial class RunGroupWarmer : MyMaterialPopOp
    {
        WarmerModel warmerModel;
        private BackgroundWorker backgroundWorker1;
        WaSenderBrowser waMultiInstance;
        public static bool IsRunning = false;
        InitStatusEnum initStatusEnum;
        CampaignStatusEnum campaignStatusEnum;
        System.Windows.Forms.Timer timerInitChecker;
        int retryAttempt = 0;
        private TestClass _testClass;
        private System.ComponentModel.BackgroundWorker backgroundWorker_productChecker;
        Progressbar pgbar;
        WAPI_GroupModel selectedGroup;

        public RunGroupWarmer(WarmerModel _warmerModel)
        {
            InitializeComponent();
            this.warmerModel = _warmerModel;
            _testClass = Utils.testClass;
            _testClass.OnUpdateStatus += _testClass_OnUpdateStatus;
            this.Icon = Strings.AppIcon;
        }
        void _testClass_OnUpdateStatus(object sender, ProgressEventArgs e)
        {
            ChangeInitStatus(InitStatusEnum.Stopped);
            ChangeCampStatus(CampaignStatusEnum.Stopped);
        }
        private void ChangeCampStatus(CampaignStatusEnum _campaignStatus)
        {
            this.campaignStatusEnum = _campaignStatus;
            AutomationCommon.ChangeCampStatus(_campaignStatus, lblRunStatus);
        }

        private void ChangeInitStatus(InitStatusEnum _initStatus)
        {
            this.initStatusEnum = _initStatus;
            AutomationCommon.ChangeInitStatus(_initStatus, lblInitStatus);
        }

        private void RunGroupWarmer_Load(object sender, EventArgs e)
        {
            initLanguages();
            if (Utils.waSenderBrowser != null)
            {
                Utils.waSenderBrowser.Close();
            }
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                this.Invoke(new Action(() =>
                    CheckForActivation()));
            });
        }


        private void CheckForActivation()
        {
            pgbar = new Progressbar();
            this.backgroundWorker_productChecker = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker_productChecker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_productChecker_DoWork);
            this.backgroundWorker_productChecker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker2_RunWorkerCompleted); ;
            this.backgroundWorker_productChecker.RunWorkerAsync();
        }


        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pgbar.Close();
            if (e.Cancelled)
            {
                MessageBox.Show("Operation was canceled");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Operation was canceled");
            }
            else
            {
                try
                {
                    bool mode = (bool)e.Result;
                    if (mode == false)
                    {

                        MessageBox.Show(Strings.ProductIsNotActivated, Strings.ProductIsNotActivated, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                catch (Exception ex)
                {

                }

            }
        }

        private void backgroundWorker_productChecker_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = CheckForActivationInternal();
        }

        private bool CheckForActivationInternal()
        {
            try
            {
                WPPHelper.CheckExecutingAssembly();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void initLanguages()
        {
            this.Text = Strings.Run + " " + Strings.Group + " " + Strings.Warmer;
            materialButton1.Text = Strings.Start;
            materialButton2.Text = Strings.Stop;
            btnInitWA.Text = Strings.ClicktoInitiate;
            label5.Text = Strings.Status;

            dataGridView1.Columns[0].HeaderText = Strings.From;
            dataGridView1.Columns[1].HeaderText = Strings.to;
            dataGridView1.Columns[2].HeaderText = Strings.Time;
            dataGridView1.Columns[3].HeaderText = Strings.Message;
            lblInitStatus.Text = Strings.NotInitialised;
            lblRunStatus.Text = Strings.NotStarted;
            label7.Text = Strings.Status;
        }

        private void btnInitWA_Click(object sender, EventArgs e)
        {
            initWABrowser();
        }

        private void initWABrowser()
        {
            ChangeInitStatus(InitStatusEnum.Initialising);
            retryAttempt = 0;
            if (Utils.waSenderBrowser != null)
            {
                waMultiInstance = Utils.waSenderBrowser;
            }
            else
            {

            }

            List<ConnectedAccountModel> selectedAccounts = new List<ConnectedAccountModel>();
            ConnectedAccountModel selectedAccount;
            foreach (var item in warmerModel.SelectedAccountNames)
            {
                selectedAccount = new ConnectedAccountModel();
                selectedAccount.sessionName = item.Name;
                selectedAccount.ID = item.ID;
                selectedAccounts.Add(selectedAccount);
            }

            waMultiInstance = new WaSenderBrowser(selectedAccounts);
            Utils.waSenderBrowser = waMultiInstance;
            waMultiInstance.Show();
            checkQRScanDoneBrowser();
        }

        private void checkQRScanDoneBrowser()
        {
            Thread.Sleep(1000);
            timerInitChecker = new System.Windows.Forms.Timer();
            timerInitChecker.Interval = 1000;
            timerInitChecker.Tick += timerInitChecker_Tick;
            timerInitChecker.Start();
        }

        public async void timerInitChecker_Tick(object sender, EventArgs e)
        {
            try
            {
                bool IsAllinitiated = true;
                foreach (TabPage tab in waMultiInstance.tabControl1.TabPages)
                {
                    WebView2 vw = (WebView2)tab.Controls.Find("webView21", true)[0];

                    MainUC mainUC = (MainUC)tab.Controls.Find("MainUC", true)[0];
                    if (!await WPPHelper.isWPPinjected(vw))
                    {
                        await WPPHelper.InjectWapiSync(vw, Config.GetSysFolderPath());
                        mainUC._isWPPIJected = true;
                    }

                    string name = tab.Text;
                    bool isInitiated = await WPPHelper.isWaInited(vw);
                    IsAllinitiated = isInitiated;
                }
                if (IsAllinitiated)
                {
                    ChangeInitStatus(InitStatusEnum.Initialised);
                    timerInitChecker.Stop();
                }
            }
            catch (Exception ex)
            {
                if (retryAttempt == 5)
                {
                    retryAttempt = 0;
                    timerInitChecker.Stop();
                }
                else
                {
                    retryAttempt++;
                    Thread.Sleep(1000);
                }
            }
        }
        private void RunGroupWarmer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Utils.waSenderBrowser != null)
            {
                Utils.waSenderBrowser.Close();
            }
        }
        static IEnumerable<T> GetCommonItems<T>(IEnumerable<T>[] lists)
        {
            HashSet<T> hs = new HashSet<T>(lists.First());
            for (int i = 1; i < lists.Length; i++)
                hs.IntersectWith(lists[i]);
            return hs;
        }

        public void ReturnBack(WAPI_GroupModel _selectedGroup)
        {
            this.selectedGroup = _selectedGroup;

            ChangeCampStatus(CampaignStatusEnum.Running);
            IsRunning = true;
            doStartRun();

        }

        private async void doStartRun()
        {
            List<string> MessagesList = warmerModel.selectedText.Split('\n').ToList();

            while (IsRunning)
            {
                foreach (WarmerContactModel fromAccount in warmerModel.SelectedAccountNames)
                {
                   
                    if (IsRunning)
                    {
                        {
                            Random r = new Random();
                            int rInt = r.Next(warmerModel.delayFrom * 1000, warmerModel.delayTo * 1000);
                            r = new Random();
                            int randomMessageIndex = r.Next(0, MessagesList.Count() - 1);
                            string randomMessage = MessagesList[randomMessageIndex];
                            randomMessage = randomMessage.Replace("\r", "");

                            waMultiInstance.tabControl1.SelectedTab = fromAccount.tabPage;

                            bool _result = await WPPHelper.openChatAtBottomLongGroupSync(fromAccount.webview, selectedGroup.GroupId);

                            if (_result == false)
                            {
                                _result = await WPPHelper.openChatAtBottomLongGroupSync(fromAccount.webview, selectedGroup.GroupId);
                            }
                            if (IsRunning)
                            {
                                bool list = await WPPHelper.SendMessageFullGroupRT(fromAccount.webview, selectedGroup.GroupId, rInt, randomMessage, "");
                            }
                            
                            DateTime dt = DateTime.Now;
                            dataGridView1.Rows.Add(new object[]{
                                    fromAccount.Name,
                                    selectedGroup.GroupName,
                                    dt.Hour + ":" + dt.Minute + ":" + dt.Second,
                                    randomMessage
                                    });

                            dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;
                        }

                        {
                            Random r = new Random();
                            int rInt = r.Next(warmerModel.delayFrom * 1000, warmerModel.delayTo * 1000);
                            r = new Random();
                            await Task.Delay(rInt);
                        }

                    }
                }
            }
        }

        List<List<T>> ChunkBy<T>(List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
        private async void materialButton1_Click(object sender, EventArgs e)
        {
            if (initStatusEnum != InitStatusEnum.Initialised)
            {
                Utils.showAlert(Strings.PleasefollowStepNo1FirstInitialiseWhatsapp, Alerts.Alert.enmType.Error);
                return;
            }
            if (campaignStatusEnum != CampaignStatusEnum.Running && campaignStatusEnum != CampaignStatusEnum.Paused)
            {
                ChangeCampStatus(CampaignStatusEnum.Starting);
                lblRunStatus.Text = Strings.GettingyourGroups;
                var Accounts = warmerModel.SelectedAccountNames;
                warmerModel.SelectedAccountNames = new List<WarmerContactModel>();
                Accounts.Shuffle();
                warmerModel.SelectedAccountNames = Accounts;

                foreach (var account in warmerModel.SelectedAccountNames)
                {
                    foreach (TabPage tab in waMultiInstance.tabControl1.TabPages)
                    {
                        if (tab.Text == account.Name)
                        {
                            WebView2 vw = (WebView2)tab.Controls.Find("webView21", true)[0];
                            if (!await WPPHelper.isWPPinjected(vw))
                            {
                                string _result = await WPPHelper.InjectWapi(vw, Config.GetSysFolderPath());
                                await Task.Delay(500);
                            }
                            string Number = await WPPHelper.getMyUserId(vw);
                            account.Number = Number;
                            account.webview = vw;
                            account.tabPage = tab;
                        }
                    }
                }

                var chunks = ChunkBy<WarmerContactModel>(warmerModel.SelectedAccountNames, 2).ToList();

                foreach (List<WarmerContactModel> chunk in chunks)
                {
                    if (chunk.Count() == 1)
                    {
                        chunk.Add(warmerModel.SelectedAccountNames.FirstOrDefault());
                    }
                }

                foreach (List<WarmerContactModel> chunk in chunks)
                {

                    foreach (WarmerContactModel _selectedAccount in chunk)
                    {
                        _selectedAccount.toAccountId = chunk.Where(x => x.ID != _selectedAccount.ID).FirstOrDefault().ID;
                    }
                }


                List<WAPI_GroupModel_Holder> holderList = new List<WAPI_GroupModel_Holder>();
                WAPI_GroupModel_Holder holder;

                foreach (TabPage tab in waMultiInstance.tabControl1.TabPages)
                {
                    WebView2 vw = (WebView2)tab.Controls.Find("webView21", true)[0];
                    holder = new WAPI_GroupModel_Holder();
                    List<WAPI_GroupModel> Groups = await WPPHelper.getMyGroupsShort(vw);
                    List<string> ss = Groups.Select(x => x.GroupId).ToList();
                    holder.groups = Groups;
                    holder.groupIds = ss;
                    holderList.Add(holder);
                }
                IEnumerable<string>[] lists = new IEnumerable<string>[holderList.Count()];
                for (int i = 0; i < holderList.Count(); i++)
                {
                    lists[i] = holderList[i].groupIds;
                }

                var commons = GetCommonItems(lists);

                if (commons.Count() > 0)
                {
                    List<WAPI_GroupModel> commpnGroups = new List<WAPI_GroupModel>();
                    foreach (string item in commons)
                    {
                        WAPI_GroupModel selectedGroup = holderList.FirstOrDefault().groups.Where(x => x.GroupId == item).FirstOrDefault();
                        commpnGroups.Add(selectedGroup);
                    }
                    if (commpnGroups.Count() > 0)
                    {
                        lblRunStatus.Text = Strings.SelectGrouptowarmingup;
                        ChooseGroup c = new ChooseGroup(this, commpnGroups);
                        c.ShowDialog();
                    }
                    else
                    {
                        ChangeCampStatus(CampaignStatusEnum.Error);
                        Utils.showError(Strings.SelectedAccountsDonthaveanyGroupInCommon + " !!!");
                    }
                }
                else
                {
                    ChangeCampStatus(CampaignStatusEnum.Error);
                    Utils.showError(Strings.SelectedAccountsDonthaveanyGroupInCommon);
                }

            }


        }

        private void materialButton2_Click(object sender, EventArgs e)
        {
            IsRunning = false;
            ChangeCampStatus(CampaignStatusEnum.Stopped);
        }

    }
    class WAPI_GroupModel_Holder
    {
        public List<string> groupIds { get; set; }

        public List<WAPI_GroupModel> groups { get; set; }
    }
}
