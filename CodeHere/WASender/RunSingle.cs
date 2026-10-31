using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WASender.enums;
using WASender.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Web;
using Microsoft.Web.WebView2.WinForms;
using Models;
using Newtonsoft.Json;

namespace WASender
{
    public partial class RunSingle : MaterialForm
    {
        WASenderSingleTransModel wASenderSingleTransModel;
        WaSenderForm waSenderForm;
        InitStatusEnum initStatusEnum;
        CampaignStatusEnum campaignStatusEnum;
        IWebDriver driver;
        System.Windows.Forms.Timer timerInitChecker;
        System.Windows.Forms.Timer timerRunner;
        BackgroundWorker worker;
        Logger logger;
        private bool IsStopped = true;
        private bool IsPaused = false;
        GeneralSettingsModel generalSettingsModel;
        WaSenderBrowser browser;
        private TestClass _testClass;
        private System.ComponentModel.BackgroundWorker backgroundWorker_productChecker;
        Progressbar pgbar;
        SchedulesModel schedulesModel;
        bool forceScheduleRun = false;
        bool isSkippedNumberCHecking = false;

        private bool traceDebug = true;
        public RunSingle(WASenderSingleTransModel _wASenderSingleTransModel, WaSenderForm _waSenderForm, SchedulesModel _schedulesModel = null, bool _forceScheduleRun = false)
        {
            InitializeComponent();
            this.Icon = Strings.AppIcon;
            logger = new Logger("RunSingle");
            this.waSenderForm = _waSenderForm;
            this.wASenderSingleTransModel = _wASenderSingleTransModel;
            this.Text = _wASenderSingleTransModel.CampaignName;
            forceScheduleRun = _forceScheduleRun;
            if (_schedulesModel == null)
            {
                generalSettingsModel = Config.GetSettings();
            }
            else
            {
                generalSettingsModel = _wASenderSingleTransModel.generalSettingsModel;
            }


            schedulesModel = _schedulesModel;
            if (Utils.Driver != null)
            {
                if (generalSettingsModel.browserType == 1)
                {
                    Utils.SetDriver();
                    driver = Utils.Driver;
                    initWA();
                }
            }
            if (Utils.waSenderBrowser != null)
            {
                Utils.waSenderBrowser.Close();
            }
            _testClass = Utils.testClass;
            _testClass.OnUpdateStatus += _testClass_OnUpdateStatus;
        }

        private void initWABrowser()
        {
            ChangeInitStatus(InitStatusEnum.Initialising);
            retryAttempt = 0;
            if (Utils.waSenderBrowser != null)
            {
                browser = Utils.waSenderBrowser;
            }
            else
            {
                browser = new WaSenderBrowser(wASenderSingleTransModel.selectedAccounts);
                Utils.waSenderBrowser = browser;
                browser.Show();
            }
            checkQRScanDoneBrowser();
        }

        private void checkQRScanDoneBrowser()
        {
            Thread.Sleep(1000);
            logger.WriteLog("checkQRScanDone");
            timerInitChecker = new System.Windows.Forms.Timer();
            timerInitChecker.Interval = 1000;
            timerInitChecker.Tick += timerInitChecker_Tick;
            timerInitChecker.Start();
        }


        void _testClass_OnUpdateStatus(object sender, ProgressEventArgs e)
        {
            ChangeInitStatus(InitStatusEnum.Stopped);
            IsStopped = true;
            try
            {
                timerInitChecker.Stop();
            }
            catch (Exception ex)
            {

            }
            ChangeCampStatus(CampaignStatusEnum.Stopped);
        }

        private void RunForm_FormClosed(object sender, FormClosedEventArgs e)
        {

            logger.Complete();
            try
            {
                IsStopped = true;
                worker.CancelAsync();
                worker.Dispose();
            }
            catch (Exception ex)
            {

            }
            if (Utils.waSenderBrowser != null)
            {
                Utils.waSenderBrowser.Close();
            }
            waSenderForm.formReturn(true);
        }

        private void RunSingle_Load(object sender, EventArgs e)
        {
            init();
            initLanguage();
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                this.Invoke(new Action(() =>
                    CheckForActivation()));
            });


            semulate();
        }

        private async void semulate()
        {
            if (schedulesModel != null)
            {
                await Task.Delay(2000);
                initBaseWA();
            }
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
                if (generalSettingsModel.browserType == 1)
                {
                    WAPIHelper.CheckExecutingAssembly();
                    return true;
                }
                else
                {
                    WPPHelper.CheckExecutingAssembly();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void initLanguage()
        {
            this.Text = Strings.Run;
            materialLabel2.Text = Strings.InitiateWhatsAppScaneQRCodefromyourrmobile;
            btnInitWA.Text = Strings.ClicktoInitiate;
            label5.Text = Strings.Status;
            btnSTart.Text = Strings.Start;
            materialButton1.Text = Strings.Pause;
            materialButton2.Text = Strings.Stop;
            label7.Text = Strings.Status;
            label8.Text = Strings.Log;
            gridStatus.Columns[0].HeaderText = Strings.ChatName;
            gridStatus.Columns[1].HeaderText = Strings.Status;
            label4.Text = Strings.ImportentNotes;
            label1.Text = Strings.Keepapplicationopenwhilesendingmessagesanduntilallmessagesaresentfromyourmobile;
            label2.Text = Strings.DontuseyourAccountforbulkSendingifitsbrandnew;
            label3.Text = Strings.WaSendertendstosubmitmessagestoyourphoneisnotresponsiblefordeliveryofthemessage;
        }

        private void init()
        {
            label4.ForeColor = Color.Red;
            ChangeInitStatus(InitStatusEnum.NotInitialised);
            ChangeCampStatus(CampaignStatusEnum.NotStarted);
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

        private void checkQRScanDone()
        {
            timerInitChecker = new System.Windows.Forms.Timer();
            timerInitChecker.Interval = 1000;
            timerInitChecker.Tick += timerInitChecker_Tick;
            timerInitChecker.Start();
        }


        private void initWA()
        {
            ChangeInitStatus(InitStatusEnum.Initialising);

            try
            {
                var s = driver.WindowHandles;
            }
            catch (Exception ex)
            {
                try
                {
                    Utils.Driver = null;
                    Utils.SetDriver();
                    this.driver = Utils.Driver;
                }
                catch (Exception eex)
                {
                    if (eex.Message.Contains("The specified executable is not a valid application for this OS platform"))
                    {
                        if (generalSettingsModel.browserType == 2)
                        {
                            MessageBox.Show(Strings.MSEdgeDriversarenotDownloadedProperly, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        if (generalSettingsModel.browserType == 1)
                        {
                            MessageBox.Show(Strings.ChromeDriversarenotDownloadedProperly, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }


            try
            {
                if (driver == null)
                {
                    Utils.SetDriver();
                    this.driver = Utils.Driver;
                }


                checkQRScanDone();
            }
            catch (Exception ex)
            {
                ChangeInitStatus(InitStatusEnum.Unable);
                if (ex.Message.Contains("session not created"))
                {
                    DialogResult dr = MessageBox.Show("Your Chrome Driver and Google Chrome Version Is not same, Click 'Yes botton' to Update it from Settings ", "Error ", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error);
                    if (dr == DialogResult.Yes)
                    {
                        this.waSenderForm.Show();
                        this.waSenderForm.OpenGeneralSettings();
                        this.Close();
                    }
                }

                else if (ex.Message.Contains("invalid argument: user data directory is already in use"))
                {
                    _Config.KillChromeDriverProcess();
                    MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Please Close All Previous Sessions and Browsers if open, Then try again", Strings.OK, true);
                    SnackBarMessage.Show(this);
                }
            }
        }
        private void btnInitWA_Click(object sender, EventArgs e)
        {
            initBaseWA();
        }

        private void initBaseWA()
        {
            if (generalSettingsModel.browserType == 1)
            {
                initWA();
            }
            else if (generalSettingsModel.browserType == 2)
            {
                initWABrowser();
            }
        }

        private async void startAutoRun()
        {
            if (schedulesModel != null)
            {
                await Task.Delay(2000);
                if (generalSettingsModel.filterNumbersBeforeSendingMessage == true)
                {
                    oppUpFilter();
                }
                else
                {
                    btnStartClick();
                }
            }

        }
        int retryAttempt = 0;
        bool isAllFocused = false;


        public async void timerInitChecker_Tick(object sender, EventArgs e)
        {
            if (generalSettingsModel.browserType == 1)
            {
                try
                {
                    if (Utils.isWaLoaded(driver))
                    {
                        ChangeInitStatus(InitStatusEnum.Initialised);
                        timerInitChecker.Stop();
                        initBackgroundWorker();
                        Activate();
                        startAutoRun();
                    }
                }
                catch (Exception ex)
                {
                    ChangeInitStatus(InitStatusEnum.Unable);
                    timerInitChecker.Stop();
                    logger.WriteLog(ex.Message);
                    logger.WriteLog(ex.StackTrace);

                }
            }
            else if (generalSettingsModel.browserType == 2)
            {
                try
                {
                    bool IsAllinitiated = false;
                    List<bool> initList = new List<bool>();
                    foreach (TabPage tab in browser.tabControl1.TabPages)
                    {
                        WebView2 vw = (WebView2)tab.Controls.Find("webView21", true)[0];

                        //MainUC mainUC = (MainUC)tab.Controls.Find("MainUC", true)[0];
                        //if (!await WPPHelper.isWPPinjected(vw))
                        //{
                        //    await WPPHelper.InjectWapiSync(vw, Config.GetSysFolderPath());
                        //    mainUC._isWPPIJected = true;
                        //}

                        //string name = tab.Text;
                        bool isInitiated = await WPPHelper.isWaInited(vw);
                        initList.Add(isInitiated);
                    }

                    if (initList.Where(x => x == true).Count() == browser.tabControl1.TabPages.Count)
                    {
                        IsAllinitiated = true;
                    }

                    if (IsAllinitiated)
                    {


                        
                        timerInitChecker.Stop();
                        foreach (TabPage tab in browser.tabControl1.TabPages)
                        {
                            WebView2 vw = (WebView2)tab.Controls.Find("webView21", true)[0];

                            MainUC mainUC = (MainUC)tab.Controls.Find("MainUC", true)[0];
                            if (!await WPPHelper.isWPPinjected(vw))
                            {
                                await WPPHelper.InjectWapiSync(vw, Config.GetSysFolderPath());
                                mainUC._isWPPIJected = true;
                            }
                        }

                        await Task.Delay(1000);
                        ChangeInitStatus(InitStatusEnum.Initialised);

                        initBackgroundWorker();
                        startAutoRun();
                    }

                    if (schedulesModel != null && isAllFocused == false)
                    {
                        foreach (TabPage tab in browser.tabControl1.TabPages)
                        {
                            browser.tabControl1.SelectedTab = tab;
                            Thread.Sleep(500);
                        }
                        isAllFocused = true;
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
                        Debug.WriteLine("Retry attempt ." + retryAttempt.ToString());
                        Thread.Sleep(1000);
                    }
                }
            }

        }



        private void initBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);

            ChangeCampStatus(CampaignStatusEnum.NotStarted);
        }



        private static bool CanStopWaitingForDelevetry = false;

        private void SendMessage(MesageModel mesageModel, bool isFirstMessage, ContactModel item, WebView2 wv)
        {
            try
            {
                bool AutoSend = false;
                bool isAlreadySent = false;
                if (mesageModel.buttons != null && mesageModel.buttons.Count() > 0)
                {
                    AutoSend = true;
                }

                isFirstMessage = false;
                Thread.Sleep(500);
                string NewMessage = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage, item.parameterModelList);

                ButtonHolderModel holder = null;
                if (mesageModel.buttons.Count() > 0)
                {
                    holder = mesageModel.buttons[0];
                }

                foreach (var file in mesageModel.files)
                {
                    file.fileName = Path.GetFileName(file.filePath);
                    Byte[] bytes = File.ReadAllBytes(file.filePath);
                    String filebase64 = Convert.ToBase64String(bytes);

                    string ext = Path.GetExtension(file.filePath);

                    string contentType = MimeMapping.GetMimeMapping(file.filePath);
                    if (ext == ".mp4")
                    {
                        contentType = "video/mp4";
                    }
                    string fullBase64 = "data:" + contentType + ";base64," + filebase64;
                    string FileName = file.filePath.Split('\\')[file.filePath.Split('\\').Length - 1];

                    if (ext == ".mp4")
                    {
                        try
                        {
                            if (file.Caption != null)
                            {
                                file.Caption = ProjectCommon.ReplaceKeyMarker(file.Caption, item.parameterModelList);
                            }


                            if (file.attachWithMainMessage == true)
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    WAPIHelper.SendVideo(driver, item.number, fullBase64, NewMessage, file.fileName, holder);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    WPPHelper.SendVideoAsync(wv, item.number, fullBase64, NewMessage, "", holder);
                                }

                                AutoSend = false;
                                isAlreadySent = true;
                            }
                            else
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    WAPIHelper.SendVideo(driver, item.number, fullBase64, file.Caption, file.fileName);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    WPPHelper.SendVideoAsync(wv, item.number, fullBase64, file.Caption);
                                }

                            }


                        }
                        catch (Exception eeex)
                        {
                            logger.WriteLog("Is Number Valid-" + eeex.Message);
                        }

                    }
                    else if (ext == ".ogg")
                    {
                        try
                        {
                            if (file.Caption != null)
                            {
                                file.Caption = ProjectCommon.ReplaceKeyMarker(file.Caption, item.parameterModelList);
                            }

                            fullBase64 = fullBase64.Replace("data:application/octet-stream;", "data:audio/mp3;");

                            if (file.attachWithMainMessage == true)
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    WAPIHelper.SendAudio(driver, item.number, fullBase64, NewMessage, holder);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    WPPHelper.SendAudioAsync(wv, item.number, fullBase64, NewMessage, holder);
                                }

                                AutoSend = false;
                                isAlreadySent = true;
                            }
                            else
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    WAPIHelper.SendAudio(driver, item.number, fullBase64, file.Caption);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    WPPHelper.SendAudioAsync(wv, item.number, fullBase64, file.Caption, holder);
                                }
                            }
                        }
                        catch (Exception eeex)
                        {
                            logger.WriteLog("Is Number Valid-" + eeex.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            string OriginalCaption = file.Caption;
                            if (file.Caption != null)
                            {
                                file.Caption = ProjectCommon.ReplaceKeyMarker(file.Caption, item.parameterModelList);
                            }

                            if (file.attachWithMainMessage == true)
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    WAPIHelper.SendVideo(driver, item.number, fullBase64, NewMessage, file.fileName, holder);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    WPPHelper.SendVideoAsync(wv, item.number, fullBase64, NewMessage, file.fileName, holder);
                                }

                                AutoSend = false;
                                isAlreadySent = true;
                            }
                            else
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    WAPIHelper.SendAttachment(driver, item.number, fullBase64, FileName, file.Caption);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    WPPHelper.SendAttachmentAsync(wv, item.number, fullBase64, FileName, file.Caption);
                                }

                            }
                            file.Caption = OriginalCaption;

                        }
                        catch (Exception eex)
                        {
                            logger.WriteLog("Is Number Valid-" + eex.Message);
                        }
                    }

                    if (isFirstMessage == true)
                    {
                        Thread.Sleep(2000);
                    }
                    Thread.Sleep(Utils.getRandom(wASenderSingleTransModel.settings.delayAfterEveryMessageFrom * 1000, wASenderSingleTransModel.settings.delayAfterEveryMessageTo * 1000));
                }


                if (AutoSend == false)
                {

                    try
                    {
                        if (isAlreadySent == false)
                        {
                            if (NewMessage.Contains("http://") || NewMessage.Contains("https://"))
                            {
                                var rand = Utils.getRandom(500, 1000);
                                if (generalSettingsModel.browserType == 1)
                                {
                                    try
                                    {
                                        if (!generalSettingsModel.disableLinkPreview)
                                        {
                                            WAPIHelper.sendTextMessageWithPreview(driver, item.number, NewMessage, false);
                                        }
                                        else
                                        {
                                            WAPIHelper.SendMessage(driver, item.number, NewMessage);
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        WAPIHelper.SendMessage(driver, item.number, NewMessage);
                                    }
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    try
                                    {
                                        if (!generalSettingsModel.disableLinkPreview)
                                        {
                                            WPPHelper.sendTextMessageWithPreview(wv, item.number, NewMessage);
                                        }
                                        else
                                        {
                                            WPPHelper.SendMessage(wv, item.number, NewMessage);
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        WPPHelper.SendMessage(wv, item.number, NewMessage);
                                    }
                                }

                            }
                            else
                            {
                                var rand = Utils.getRandom(500, 1000);

                                if (generalSettingsModel.browserType == 1)
                                {
                                    if (NewMessage != "")
                                    {
                                        WAPIHelper.SendMessage(driver, item.number, NewMessage);
                                    }

                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    if (NewMessage != "")
                                    {
                                        WPPHelper.SendMessage(wv, item.number, NewMessage);
                                    }

                                }


                            }
                        }
                        if (mesageModel.polls != null && mesageModel.polls.Count() > 0)
                        {
                            foreach (var poll in mesageModel.polls)
                            {
                                try
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.sendCreatePollMessageToNumber(driver, item.number, poll);
                                        Thread.Sleep(500);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        WPPHelper.sendCreatePollMessageToNumber(wv, item.number, poll);
                                        Thread.Sleep(500);
                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                            }

                        }
                        else if (holder != null)
                        {
                            if (generalSettingsModel.browserType == 1)
                            {
                                //WAPIHelper.Sen(driver, item.number, fullBase64, FileName, file.Caption);
                            }
                            else if (generalSettingsModel.browserType == 2)
                            {
                                WPPHelper.SendDirectMessageAsync(wv, item.number, NewMessage, holder);
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                        throw new Exception(ex.Message);
                    }

                }
                else
                {
                    try
                    {
                        if (isAlreadySent == false)
                        {
                            var rand = Utils.getRandom(500, 1000);

                            if (generalSettingsModel.browserType == 1)
                            {

                            }
                            else if (generalSettingsModel.browserType == 2)
                            {

                            }



                        }

                    }
                    catch (Exception ex)
                    {

                        logger.WriteLog("Is Number Valid-" + ex.Message);
                    }
                }

                Thread.Sleep(Utils.getRandom(wASenderSingleTransModel.settings.delayAfterEveryMessageFrom * 1000, wASenderSingleTransModel.settings.delayAfterEveryMessageTo * 1000));



            }
            catch (Exception ex)
            {
                logger.WriteLog("1309=" + ex.Message);
            }
        }


        private async Task<bool> SendMessageSync(MesageModel mesageModel, bool isFirstMessage, ContactModel item, WebView2 wv)
        {
            try
            {
                bool AutoSend = false;
                bool isAlreadySent = false;
                if (mesageModel.buttons != null && mesageModel.buttons.Count() > 0)
                {
                    AutoSend = false;
                }

                isFirstMessage = false;
                //await Task.Delay(500);
                string NewMessage = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage, item.parameterModelList);
                writetrace("TEXT len=" + (NewMessage == null ? 0 : NewMessage.Length));

                ButtonHolderModel holder = null;
                try
                {
                    if (mesageModel.buttons.Count() > 0)
                    {
                        holder = mesageModel.buttons[0];
                    }
                }
                catch (Exception ex)
                {


                }

                foreach (FilesModel file in mesageModel.files)
                {
                    writetrace("FILE type=" + (file == null ? "null" : file.type) + " path=" + (file == null ? "null" : file.filePath));
                    if (file.type == StaticMethods.media || file.type == null || file.type == "")
                    {
                        #region media
                        file.fileName = Path.GetFileName(file.filePath);
                        Byte[] bytes = File.ReadAllBytes(file.filePath);
                        String filebase64 = Convert.ToBase64String(bytes);

                        string ext = Path.GetExtension(file.filePath);

                        string contentType = MimeMapping.GetMimeMapping(file.filePath);
                        if (ext == ".mp4")
                        {
                            contentType = "video/mp4";
                        }
                        if (ext == ".apk")
                        {
                            contentType = "application/vnd.android.package-archive";
                        }
                        string fullBase64 = "data:" + contentType + ";base64," + filebase64;
                        string FileName = file.filePath.Split('\\')[file.filePath.Split('\\').Length - 1];




                        if (ext == ".mp4")
                        {
                            try
                            {
                                if (file.Caption != null)
                                {
                                    file.Caption = ProjectCommon.ReplaceKeyMarker(file.Caption, item.parameterModelList);
                                }
                                if (file.attachWithMainMessage == true)
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.SendVideo(driver, item.number, fullBase64, NewMessage, file.fileName, holder);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        bool isDOne = await WPPHelper.SendVideosync(wv, item.number, fullBase64, NewMessage, "", holder);
                                    }

                                    AutoSend = false;
                                    isAlreadySent = true;
                                }
                                else
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.SendVideo(driver, item.number, fullBase64, file.Caption, file.fileName);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        bool isDOne = await WPPHelper.SendVideosync(wv, item.number, fullBase64, file.Caption);
                                    }

                                }


                            }
                            catch (Exception eeex)
                            {
                                logger.WriteLog("Is Number Valid-" + eeex.Message);
                            }

                        }
                        else if (ext == ".ogg")
                        {
                            try
                            {
                                if (file.Caption != null)
                                {
                                    file.Caption = ProjectCommon.ReplaceKeyMarker(file.Caption, item.parameterModelList);
                                }


                                fullBase64 = fullBase64.Replace("data:application/octet-stream;", "data:audio/mp3;");

                                if (file.attachWithMainMessage == true)
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.SendAudio(driver, item.number, fullBase64, NewMessage, holder);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        // Voice note tidak dukung caption: kirim audio, teks menyusul di bawah
                                        bool isDone = await WPPHelper.sendAudioFile(wv, item.number, fullBase64);
                                    }

                                    AutoSend = false;
                                }
                                else
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.SendAudio(driver, item.number, fullBase64, file.Caption);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        bool isDone = await WPPHelper.sendAudioFile(wv, item.number, fullBase64);
                                    }
                                }
                            }
                            catch (Exception eeex)
                            {
                                logger.WriteLog("Is Number Valid-" + eeex.Message);
                            }
                        }
                        else
                        {
                            try
                            {
                                string OriginalCaption = file.Caption;
                                if (file.Caption != null)
                                {
                                    file.Caption = ProjectCommon.ReplaceKeyMarker(file.Caption, item.parameterModelList);
                                }


                                if (file.attachWithMainMessage == true)
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.SendVideo(driver, item.number, fullBase64, NewMessage, file.fileName, holder);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        bool isDone = await WPPHelper.SendVideosync(wv, item.number, fullBase64, NewMessage, file.fileName, holder);
                                    }

                                    AutoSend = false;
                                    isAlreadySent = true;
                                }
                                else
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.SendAttachment(driver, item.number, fullBase64, FileName, file.Caption);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        bool isDOne = await WPPHelper.SendAttachmentsync(wv, item.number, fullBase64, FileName, file.Caption);
                                    }

                                }
                                file.Caption = OriginalCaption;

                            }
                            catch (Exception eex)
                            {
                                logger.WriteLog("Is Number Valid-" + eex.Message);
                            }
                        }

                        await Task.Delay(Utils.getRandom(wASenderSingleTransModel.settings.delayAfterEveryMessageFrom * 1000, wASenderSingleTransModel.settings.delayAfterEveryMessageTo * 1000));
                        #endregion
                    }
                    else if (file.type == StaticMethods.audio)
                    {

                        Byte[] bytes = File.ReadAllBytes(file.filePath);
                        String filebase64 = Convert.ToBase64String(bytes);
                        string contentType = MimeMapping.GetMimeMapping(file.filePath);
                        string fullBase64 = "data:" + contentType + ";base64," + filebase64;

                        if (generalSettingsModel.browserType == 1)
                        {
                            bool isDone = WAPIHelper.sendAudioFile(driver, item.number, fullBase64);
                        }
                        else if (generalSettingsModel.browserType == 2)
                        {
                            bool isDone = await WPPHelper.sendAudioFile(wv, item.number, fullBase64);
                        }
                    }
                    else if (file.type == StaticMethods.videoNote)
                    {
                        Byte[] bytes = File.ReadAllBytes(file.filePath);
                        String filebase64 = Convert.ToBase64String(bytes);
                        string contentType = MimeMapping.GetMimeMapping(file.filePath);
                        string fullBase64 = "data:video/mp4;base64," + filebase64;

                        if (generalSettingsModel.browserType == 1)
                        {
                            bool isDone = WAPIHelper.sendVideoNote(driver, item.number, fullBase64);
                        }
                        else if (generalSettingsModel.browserType == 2)
                        {
                            bool isDone = await WPPHelper.sendVideoNote(wv, item.number, fullBase64);
                        }
                    }
                    else if (file.type == StaticMethods.sticker)
                    {
                        Byte[] bytes = File.ReadAllBytes(file.filePath);
                        String filebase64 = Convert.ToBase64String(bytes);
                        string contentType = MimeMapping.GetMimeMapping(file.filePath);
                        string fullBase64 = "data:" + contentType + ";base64," + filebase64;

                        if (generalSettingsModel.browserType == 1)
                        {
                            bool isDone = WAPIHelper.sendStickerSync(driver, item.number, fullBase64);
                        }
                        else if (generalSettingsModel.browserType == 2)
                        {
                            bool isDone = await WPPHelper.sendSticker(wv, item.number, fullBase64);
                        }
                    }
                    else if (file.type == StaticMethods.location && file.locationMessageModel != null)
                    {
                        if (generalSettingsModel.browserType == 1)
                        {
                            bool isDone = WAPIHelper.SendLocationMessage(driver, item.number, file.locationMessageModel.lat, file.locationMessageModel.lng, file.locationMessageModel.name, file.locationMessageModel.address, file.locationMessageModel.url);
                        }
                        else if (generalSettingsModel.browserType == 2)
                        {
                            bool isDone = await WPPHelper.SendLocationMessage(wv, item.number, file.locationMessageModel.lat, file.locationMessageModel.lng, file.locationMessageModel.name, file.locationMessageModel.address, file.locationMessageModel.url);
                        }
                    }
                    else if (file.type == StaticMethods.contact && file.contactMessageModel != null)
                    {
                        if (generalSettingsModel.browserType == 1)
                        {
                            bool isDone = WAPIHelper.SendsendVCardContactMessage(driver, item.number, file.contactMessageModel.number, file.contactMessageModel.name);
                        }
                        else if (generalSettingsModel.browserType == 2)
                        {
                            bool isDone = await WPPHelper.SendsendVCardContactMessage(wv, item.number, file.contactMessageModel.number, file.contactMessageModel.name);
                        }
                    }

                }

                if (AutoSend == false)
                {

                    try
                    {
                        if (isAlreadySent == false)
                        {
                            if (NewMessage.Contains("http://") || NewMessage.Contains("https://"))
                            {
                                var rand = Utils.getRandom(500, 1000);
                                if (generalSettingsModel.browserType == 1)
                                {
                                    try
                                    {
                                        if (!generalSettingsModel.disableLinkPreview && holder == null)
                                        {
                                            WAPIHelper.sendTextMessageWithPreview(driver, item.number, NewMessage, false);
                                        }
                                        else
                                        {
                                            WAPIHelper.SendMessage(driver, item.number, NewMessage, false, false, holder);
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        WAPIHelper.SendMessage(driver, item.number, NewMessage, false, false, holder);
                                    }
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    try
                                    {
                                        if (!generalSettingsModel.disableLinkPreview && holder == null)
                                        {
                                            bool isDone = await WPPHelper.sendTextMessageWithPreviewSync(wv, item.number, NewMessage);
                                            writetrace("preview sent=" + isDone);
                                        }
                                        else
                                        {
                                            bool isDOne = await WPPHelper.SendMessageSync(wv, item.number, NewMessage, false, false, holder);
                                            writetrace("text sent=" + isDOne);
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        writetrace("TEXT EX " + ex.Message);
                                    }
                                }

                            }
                            else
                            {

                                if (generalSettingsModel.browserType == 1)
                                {
                                    if (NewMessage != "")
                                    {
                                        WAPIHelper.SendMessage(driver, item.number, NewMessage, false, false, holder);
                                    }

                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    if (NewMessage != "")
                                    {
                                        bool isDOne = await WPPHelper.SendMessageSync(wv, item.number, NewMessage, false, false, holder);
                                        writetrace("plain text sent=" + isDOne);
                                    }
                                    else
                                    {
                                        writetrace("plain text EMPTY, skipped");
                                    }

                                }


                            }
                        }
                        if (mesageModel.polls != null && mesageModel.polls.Count() > 0)
                        {
                            foreach (var poll in mesageModel.polls)
                            {
                                try
                                {
                                    if (generalSettingsModel.browserType == 1)
                                    {
                                        WAPIHelper.sendCreatePollMessageToNumber(driver, item.number, poll);
                                        await Task.Delay(500);
                                    }
                                    else if (generalSettingsModel.browserType == 2)
                                    {
                                        bool isDone = await WPPHelper.sendCreatePollMessageToNumberSync(wv, item.number, poll);
                                        await Task.Delay(500);
                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                            }

                        }

                    }
                    catch (Exception ex)
                    {

                        throw new Exception(ex.Message);
                    }

                }

                await Task.Delay(Utils.getRandom(wASenderSingleTransModel.settings.delayAfterEveryMessageFrom * 1000, wASenderSingleTransModel.settings.delayAfterEveryMessageTo * 1000));



            }
            catch (Exception ex)
            {
                logger.WriteLog("1309=" + ex.Message);
            }

            return true;
        }

        // Pastikan window.FMARK tersedia di WebView (inject wapi.js bila belum ada)
        private async Task<int> OutboxCount(WebView2 wv)
        {
            try
            {
                string r = await wv.ExecuteScriptAsync("document.querySelectorAll('#main div.message-out').length");
                string s = (r ?? "0").Trim().Trim('"');
                int n;
                if (int.TryParse(s, out n)) { return n; }
            }
            catch (Exception) { }
            return -1;
        }

        private async Task<bool> OutboxIncreased(WebView2 wv, int before, int seconds = 12)
        {
            try
            {
                for (int i = 0; i < seconds / 2; i++)
                {
                    await Task.Delay(2000);
                    int after = await OutboxCount(wv);
                    writetrace("DOM outbox check before=" + before + " after=" + after);
                    if (after >= 0 && before >= 0 && after > before)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                writetrace("DOM outbox EX " + ex.Message);
            }
            return false;
        }

        // Buka chat via link resmi /send (sekaligus validasi nomor). Return "OK"|"NOTONWA"|"FAIL".
        private async Task<string> OpenChatViaSendLink(WebView2 wv, string digits)
        {
            try
            {
                string[] urlsToTry = new string[]
                {
                    "https://wa.me/" + digits,
                    "https://web.whatsapp.com/send?phone=" + digits
                };
                foreach (string targetUrl in urlsToTry)
                {
                try
                {
                    string alive = await wv.ExecuteScriptAsync("('ALIVE:' + location.href).substring(0,120)");
                    writetrace("SENDLINK pre=" + (alive ?? "null").Trim('"'));
                    string navRes = await wv.ExecuteScriptAsync("window.location.href = " + JsonConvert.SerializeObject(targetUrl) + "; 'NAV:' + location.href;");
                    writetrace("SENDLINK navres=" + (navRes ?? "null"));
                }
                catch (Exception ex)
                {
                    writetrace("SENDLINK nav EX " + ex.GetType().Name + ": " + ex.Message);
                    continue;
                }
                // Probe: apakah scripting masih hidup pasca-navigasi?
                for (int pi = 0; pi < 3; pi++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        string pb = await wv.ExecuteScriptAsync("('HREF:'+location.href+'|T:'+document.title+'|RS:'+document.readyState).substring(0,180)");
                        writetrace("SENDLINK probe" + pi + "=" + (pb ?? "null"));
                    }
                    catch (Exception exPb)
                    {
                        writetrace("SENDLINK probe" + pi + " EX " + exPb.GetType().Name + ": " + (exPb.Message ?? "").Substring(0, System.Math.Min(150, (exPb.Message ?? "").Length)));
                    }
                }
                // Halaman perantara api.whatsapp.com butuh 1x klik "continue" (#action-button).
                for (int ci = 0; ci < 4; ci++)
                {
                    try
                    {
                        string clk = await wv.ExecuteScriptAsync("(function(){var b=document.getElementById('action-button');if(!b)return 'NO_BTN';try{b.click();return 'CLICKED';}catch(e){return 'ERR';}})();");
                        string cl = ((clk ?? "").Trim().Trim('"') ?? "");
                        writetrace("SENDLINK actionbtn=" + cl);
                        if (cl.Contains("CLICKED")) { await Task.Delay(4000); break; }
                    }
                    catch (Exception exC) { writetrace("SENDLINK actionbtn EX " + exC.GetType().Name); }
                    await Task.Delay(2500);
                }
                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        // Sengaja pendek: verifikasi di C#, bukan di JS.
                        string stTitle = await wv.ExecuteScriptAsync("document.title");
                        string stBox = await wv.ExecuteScriptAsync("document.querySelector('#main footer [contenteditable=\"true\"]') ? 'HASBOX' : (document.querySelector('[data-lexical-editor]') ? 'HASLEX' : 'NOBOX')");
                        string stQr = await wv.ExecuteScriptAsync("document.querySelector('canvas[aria-label=\"Scan me!\"]') ? 'HASQR' : 'NOQR'");
                        string stBody = await wv.ExecuteScriptAsync("(document.body&&document.body.innerText||'').substring(0,400)");
                        string sT = ((stTitle ?? "").Trim().Trim('"') ?? "");
                        string sB = ((stBox ?? "").Trim().Trim('"') ?? "");
                        string sQ = ((stQr ?? "").Trim().Trim('"') ?? "");
                        string sBo = ((stBody ?? "").Trim().Trim('"') ?? "");
                        string slow = sBo.ToLower();
                        string s = "T=" + sT + " BOX=" + sB + " QR=" + sQ;
                        if (i % 4 == 0) { writetrace("SENDLINK poll " + i + "=" + s); }
                        if (sB.Contains("HASBOX") || sB.Contains("HASLEX")) { writetrace("SENDLINK chat open"); return "OK"; }
                        if (sQ.Contains("HASQR")) { writetrace("SENDLINK session logged out!"); return "LOGGEDOUT"; }
                        if (slow.Contains("not on whatsapp") || slow.Contains("invalid phone") || slow.Contains("tidak valid") || slow.Contains("tidak terdaftar") || slow.Contains("tidak ada di whatsapp")) { writetrace("SENDLINK number not on WA"); return "NOTONWA"; }
                        if (slow.Contains("to use whatsapp on your computer") || slow.Contains("scan me") || slow.Contains("pindai") || slow.Contains("masuk")) { writetrace("SENDLINK session logged out!"); return "LOGGEDOUT"; }
                    }
                    catch (Exception exPoll)
                    {
                        writetrace("SENDLINK poll " + i + " EX " + exPoll.GetType().Name + ": " + exPoll.Message);
                    }
                }
                writetrace("SENDLINK timeout for this URL, coba URL berikut");
                } // foreach urlsToTry
                return "FAIL";
            }
            catch (Exception ex)
            {
                writetrace("SENDLINK EX " + ex.Message);
                return "FAIL";
            }
        }

        private async Task<bool> EnsureFmark(WebView2 wv)
        {
            try
            {
                string hasFmark = await wv.ExecuteScriptAsync("typeof window.FMARK === 'object' ? 'yes' : 'no'");
                if (hasFmark != null && hasFmark.Contains("yes"))
                {
                    return true;
                }
                writetrace("FMARK missing, injecting wapi.js");
                string wapiPath = System.IO.Path.Combine(Config.GetSysFolderPath(), "wapi.js");
                if (!System.IO.File.Exists(wapiPath))
                {
                    writetrace("FMARK wapi.js not found");
                    return false;
                }
                await wv.ExecuteScriptAsync(System.IO.File.ReadAllText(wapiPath));
                await Task.Delay(3000);
                hasFmark = await wv.ExecuteScriptAsync("typeof window.FMARK === 'object' ? 'yes' : 'no'");
                return hasFmark != null && hasFmark.Contains("yes");
            }
            catch (Exception ex)
            {
                writetrace("FMARK ensure EX " + ex.Message);
                return false;
            }
        }

        // Buka chat via FMARK.openChatBottom (dipakai sebelum kirim via DLL)
        private async Task<bool> FmarkOpenChat(WebView2 wv, string digits)
        {
            try
            {
                if (!await EnsureFmark(wv))
                {
                    return false;
                }
                string chatIdJs = JsonConvert.SerializeObject(digits + "@c.us");
                await wv.ExecuteScriptAsync("window.__fmarkOpen = 'PENDING';"
                    + "FMARK.openChatBottom(" + chatIdJs + ")"
                    + ".then(function(r){ window.__fmarkOpen = 'OPEN:' + r; })"
                    + ".catch(function(e){ window.__fmarkOpen = 'OPENFAIL:' + ((e && e.message) || e); });"
                    + "'DISPATCHED';");
                string res = await FmarkPollResult(wv, "__fmarkOpen");
                writetrace("FMARK open -> " + res);
                return res.StartsWith("OPEN:true");
            }
            catch (Exception ex)
            {
                writetrace("FMARK open EX " + ex.Message);
                return false;
            }
        }
        private async Task<string> FmarkPollResult(WebView2 wv, string mailbox)
        {
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(1500);
                    string cur = await wv.ExecuteScriptAsync("window." + mailbox + " || 'PENDING'");
                    if (cur == null)
                    {
                        continue;
                    }
                    string val = cur.Trim().Trim('"');
                    // Unescape JSON string encoding
                    val = System.Text.RegularExpressions.Regex.Unescape(val);
                    if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length > 2)
                    {
                        val = val.Substring(1, val.Length - 2);
                    }
                    if (val != "PENDING" && val != "")
                    {
                        return val;
                    }
                }
            }
            catch (Exception ex)
            {
                writetrace("FMARK poll EX " + ex.Message);
            }
            return "TIMEOUT";
        }

        // Fallback TERAKHIR: kirim via DOM UI resmi (buka chat lewat /send, ketik, klik kirim).
        // Ini jalur paling andal utk sinkron ke HP karena persis seperti user mengetik manual.
        // Dipakai bila DLL dan FMARK-internal sama2 gagal. Khusus teks (media tak bisa via DOM di WebView2).
        private async Task<bool> SendViaDom(WebView2 wv, string number, string text)
        {
            try
            {
                string digits = System.Text.RegularExpressions.Regex.Replace(number ?? "", "[^0-9]", "");
                if (string.IsNullOrEmpty(digits) || string.IsNullOrEmpty(text))
                {
                    return false;
                }
                try
                {
                    await wv.ExecuteScriptAsync("window.location.href = " + JsonConvert.SerializeObject("https://web.whatsapp.com/send?phone=" + digits) + "; 'NAV';");
                }
                catch (Exception ex)
                {
                    writetrace("DOM nav EX " + ex.Message);
                    return false;
                }
                bool ready = false;
                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(2000);
                    try
                    {
                        string st = await wv.ExecuteScriptAsync(
                            "try{var b=document.querySelector('#main footer [contenteditable=\"true\"]')||document.querySelector('[data-lexical-editor]');('BOX:'+(!!b));}catch(e){'ERR'}");
                        if (st != null && st.Contains("BOX:true")) { ready = true; break; }
                    }
                    catch (Exception) { /* halaman masih reload, coba lagi */ }
                }
                writetrace("DOM box ready=" + ready);
                if (!ready) { return false; }
                return await DomTypeAndSend(wv, text);
            }
            catch (Exception ex)
            {
                writetrace("DOM EX " + ex.Message);
                return false;
            }
        }

        // Ketik + klik kirim di chat yg SUDAH terbuka (tanpa navigasi).
        private async Task<bool> DomTypeAndSend(WebView2 wv, string text)
        {
            try
            {
                writetrace("DOM expect len=" + (text ?? "").Length);
                await Task.Delay(1500);
                string setJs = "(function(){"
                    + "var box=document.querySelector('#main footer [contenteditable=\"true\"]')||document.querySelector('[data-lexical-editor] [contenteditable=\"true\"]')||document.querySelector('[data-lexical-editor]')||document.querySelector('[contenteditable=\"true\"]');"
                    + "if(!box) return 'NOBOX';"
                    + "try{box.scrollIntoView({block:'center'});}catch(e){}"
                    + "box.focus();"
                    + "try{var _sel=window.getSelection();_sel.removeAllRanges();var _rg=document.createRange();_rg.selectNodeContents(box);_rg.collapse(false);_sel.addRange(_rg);}catch(e){}"
                    + "try{document.execCommand('selectAll',false,null);}catch(e){}"
                    + "try{var _s2=window.getSelection();_s2.removeAllRanges();var _r2=document.createRange();_r2.selectNodeContents(box);_r2.collapse(false);_s2.addRange(_r2);}catch(e){}"
                    + "var T=" + JsonConvert.SerializeObject(text) + ";"
                    + "var ok=false;"
                    + "try{ ok=document.execCommand('insertText',false,T); }catch(e){}"
                    + "if(!ok){ try{"
                    + "var tgt=document.querySelector('[data-lexical-editor] [contenteditable=\"true\"]')||box;"
                    + "try{var _s3=window.getSelection();_s3.removeAllRanges();var _r3=document.createRange();_r3.selectNodeContents(tgt);_r3.collapse(false);_s3.addRange(_r3);}catch(e){}"
                    + "var bi=new InputEvent('beforeinput',{bubbles:true,cancelable:true,inputType:'insertText',data:T});"
                    + "tgt.dispatchEvent(bi);"
                    + "if(bi.defaultPrevented){ ok=true; }"
                    + "else { tgt.textContent=T; tgt.dispatchEvent(new InputEvent('input',{bubbles:true,inputType:'insertText',data:T})); tgt.dispatchEvent(new Event('change',{bubbles:true})); }"
                    + "}catch(e){} }"
                    + "var cur=(box.textContent||'');"
                    + "return (cur && cur.trim().length>0)?'SET:'+cur.substring(0,40):'EMPTY';"
                    + "})();";
                string setRes = "";
                try { setRes = await wv.ExecuteScriptAsync(setJs); } catch (Exception ex) { writetrace("DOM set EX " + ex.Message); return false; }
                writetrace("DOM set -> " + (setRes ?? "").Trim('"'));
                if (setRes == null || !(setRes.Contains("SET:"))) { return false; }
                await Task.Delay(1000);
                string clickJs = "(function(){"
                    + "var sels=['#main footer button[aria-label=\"Send\"]','footer button[aria-label=\"Send\"]','button span[data-icon=\"send\"]','span[data-icon=\"send\"]','button[data-tab=\"11\"]'];"
                    + "for(var i=0;i<sels.length;i++){try{var b=document.querySelector(sels[i]);if(b){var btn=b.tagName==='BUTTON'?b:(b.closest('button')||b);btn.click();return 'CLICKED:'+sels[i];}}catch(e){}}"
                    + "try{var box=document.querySelector('#main footer [contenteditable=\"true\"]')||document.querySelector('[contenteditable=\"true\"]');if(box){box.focus();box.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',keyCode:13,which:13,bubbles:true}));return 'ENTER';}}catch(e){}"
                    + "return 'NOBTN';"
                    + "})();";
                string clickRes = "";
                try { clickRes = await wv.ExecuteScriptAsync(clickJs); } catch (Exception ex) { writetrace("DOM click EX " + ex.Message); return false; }
                writetrace("DOM click -> " + (clickRes ?? "").Trim('"'));
                if (clickRes == null || (!clickRes.Contains("CLICKED") && !clickRes.Contains("ENTER"))) { return false; }
                // Verifikasi TERKIRIM: hitung pesan keluar sebelum & sesudah klik.
                // Hanya True bila jumlah bertambah = pesan masuk ke chat = pasti sinkron ke HP.
                try
                {
                    string countJs = "document.querySelectorAll('#main div.message-out').length + '/' + document.querySelectorAll('#main span[data-icon=\"msg-dblcheck\"]').length";
                    string before = await wv.ExecuteScriptAsync(countJs);
                    writetrace("DOM outbox before=" + (before ?? "").Trim('"'));
                    for (int i = 0; i < 5; i++)
                    {
                        await Task.Delay(2000);
                        string after = await wv.ExecuteScriptAsync(countJs);
                        writetrace("DOM outbox after=" + (after ?? "").Trim('"'));
                        if (after != null && before != null && after.Trim('"') != before.Trim('"'))
                        {
                            await Task.Delay(2000);
                            return true;
                        }
                    }
                    writetrace("DOM outbox UNCHANGED -> not delivered");
                    return false;
                }
                catch (Exception ex)
                {
                    writetrace("DOM verify EX " + ex.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                writetrace("DOM EX " + ex.Message);
                return false;
            }
        }

        private async Task<bool> SendViaFmark(WebView2 wv, string number, MesageModel mesageModel, ContactModel item)
        {
            try
            {
                // Normalisasi nomor: hanya digit (buang '+', spasi, '(', ')', '-')
                string digits = System.Text.RegularExpressions.Regex.Replace(number ?? "", "[^0-9]", "");
                writetrace("FMARK number clean=" + digits);
                string hasFmark = await wv.ExecuteScriptAsync("typeof window.FMARK === 'object' ? 'yes' : 'no'");
                if (hasFmark == null || !hasFmark.Contains("yes"))
                {
                    writetrace("FMARK missing, injecting wapi.js");
                    string wapiPath = System.IO.Path.Combine(Config.GetSysFolderPath(), "wapi.js");
                    if (!System.IO.File.Exists(wapiPath))
                    {
                        writetrace("FMARK wapi.js not found");
                        return false;
                    }
                    await wv.ExecuteScriptAsync(System.IO.File.ReadAllText(wapiPath));
                    await Task.Delay(3000);
                }

                string chatId = digits + "@c.us";
                string chatIdJs = JsonConvert.SerializeObject(chatId);

                // Cek status login sesi ini (jangan kirim buta saat logout)
                try
                {
                    string logged = await wv.ExecuteScriptAsync("try{String(window.FMARK && window.FMARK.isLoggedIn ? window.FMARK.isLoggedIn() : 'nofmark')}catch(e){'err:' + e.message}");
                    writetrace("FMARK isLoggedIn=" + (logged ?? "").Trim('"'));
                }
                catch (Exception ex)
                {
                    writetrace("FMARK logincheck EX " + ex.Message);
                }

                // Cek nomor terdaftar WA atau tidak (biar jelas, bukan sukses palsu)
                try
                {
                    string chkJs = "window.__fmarkExists = 'PENDING';"
                        + "FMARK.queryExists(" + chatIdJs + ")"
                        + ".then(function(r){ window.__fmarkExists = 'EXISTS:' + JSON.stringify(r).substring(0,160); })"
                        + ".catch(function(e){ window.__fmarkExists = 'CHECKFAIL:' + ((e && e.message) || e); });"
                        + "'DISPATCHED';";
                    await wv.ExecuteScriptAsync(chkJs);
                    string chk = await FmarkPollResult(wv, "__fmarkExists");
                    writetrace("FMARK exists=" + chk);
                    if (chk != null && (chk == "false" || chk == "\"false\"" || chk == "null" || chk == "EXISTS:false" || chk == "EXISTS:null" || chk == "EXISTS:\"\""))
                    {
                        writetrace("FMARK number NOT on WhatsApp");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    writetrace("FMARK existscheck EX " + ex.Message);
                }

                // Pre-flight: resolusi LID + buka chat, catat tiap langkah
                try
                {
                    string diagJs = "window.__fmarkDiag = 'PENDING';"
                        + "(async function(){ var out=[];"
                        + "try{"
                        + "var lid = await FMARK.getLidFromPhoneID(" + chatIdJs + "); out.push('lid=' + lid);"
                        + "var wid = null; try{ wid = Store.WidFactory.createWid(lid || " + chatIdJs + "); }catch(e){ out.push('createWid EX:' + e.message); }"
                        + "out.push('wid=' + wid);"
                        + "try{ var r = await Store.FindChat.findOrCreateLatestChat(wid, 'username_contactless_search', {forceUsync:true});"
                        + " out.push('chat=' + ((r && r.chat && r.chat.id) || 'NONE') + ' created=' + ((r && r.created) || '?')); }"
                        + "catch(e){ out.push('find EX:' + e.message); }"
                        + "}catch(e){ out.push('DIAGEX:' + e.message); }"
                        + "window.__fmarkDiag = out.join(' | '); })();"
                        + "'DISPATCHED';";
                    await wv.ExecuteScriptAsync(diagJs);
                    string diag = await FmarkPollResult(wv, "__fmarkDiag");
                    writetrace("FMARK diag " + diag);
                }
                catch (Exception ex)
                {
                    writetrace("FMARK diag EX " + ex.Message);
                }

                // Buka chat dulu (best effort, abaikan hasil)
                try
                {
                    await wv.ExecuteScriptAsync("try{FMARK.openChatBottom(" + chatIdJs + ")}catch(e){'skip'}");
                    await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    writetrace("FMARK openChat EX " + ex.Message);
                }

                string NewMessage = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage, item.parameterModelList);

                // 1) File media via FMARK.sendFileMessage (modern media pipeline)
                if (mesageModel.files != null)
                {
                    foreach (FilesModel file in mesageModel.files)
                    {
                        try
                        {
                            if (file == null || string.IsNullOrEmpty(file.filePath) || !System.IO.File.Exists(file.filePath))
                            {
                                writetrace("FMARK skip missing file");
                                continue;
                            }
                            if (file.type != null && file.type != "" && file.type != StaticMethods.media)
                            {
                                // location/contact/sticker/dll-mode: serahkan ke jalur DLL per-file? di fallback kirim teks saja
                                writetrace("FMARK skip non-media type=" + file.type);
                                continue;
                            }
                            Byte[] bytes = System.IO.File.ReadAllBytes(file.filePath);
                            string ext = System.IO.Path.GetExtension(file.filePath) ?? "";
                            string contentType = MimeMapping.GetMimeMapping(file.filePath);
                            if (ext.ToLower() == ".mp4") { contentType = "video/mp4"; }
                            string fullBase64 = "data:" + contentType + ";base64," + Convert.ToBase64String(bytes);
                            string caption = file.Caption ?? "";
                            if (file.attachWithMainMessage == true) { caption = NewMessage ?? ""; }
                            else if (!string.IsNullOrEmpty(caption)) { caption = ProjectCommon.ReplaceKeyMarker(caption, item.parameterModelList) ?? ""; }
                            string fname = System.IO.Path.GetFileName(file.filePath);
                            // Pola mailbox: dispatch lalu poll, anti masalah promise di ExecuteScriptAsync
                            string js = "window.__fmarkMedia = 'PENDING';"
                                + "FMARK.sendFileMessage(" + chatIdJs + ", "
                                + JsonConvert.SerializeObject(fullBase64) + ", "
                                + "{filename: " + JsonConvert.SerializeObject(fname) + ", caption: " + JsonConvert.SerializeObject(caption) + "})"
                                + ".then(function(r){ window.__fmarkMedia = 'OK:' + JSON.stringify(r).substring(0,300); })"
                                + ".catch(function(e){ window.__fmarkMedia = 'FAIL:' + ((e && e.message) || e); });"
                                + "'DISPATCHED';";
                            await wv.ExecuteScriptAsync(js);
                            string res = await FmarkPollResult(wv, "__fmarkMedia");
                            writetrace("FMARK media " + fname + " -> " + res);
                            // Sukses = objek hasil (OK:{...}). OK:false / FAIL / TIMEOUT = gagal.
                            if (!res.StartsWith("OK:{"))
                            {
                                return false;
                            }
                            await Task.Delay(2000);
                        }
                        catch (Exception ex)
                        {
                            writetrace("FMARK media EX " + ex.Message);
                            return false;
                        }
                    }
                }

                // 2) Teks (caption sudah ikut media bila attachWithMainMessage)
                bool needText = !string.IsNullOrEmpty(NewMessage);
                if (needText && mesageModel.files != null)
                {
                    bool allAttached = true;
                    foreach (FilesModel f in mesageModel.files)
                    {
                        if (f == null) { continue; }
                        if (f.type == null || f.type == "" || f.type == StaticMethods.media)
                        {
                            if (f.attachWithMainMessage != true) { allAttached = false; break; }
                        }
                    }
                    if (allAttached && mesageModel.files.Count > 0) { needText = false; }
                }
                if (needText)
                {
                    string js = "window.__fmarkText = 'PENDING'; window.__fmarkSendErr = '';"
                        + "try{ var __origErr = console.error; console.error = function(){ try{ window.__fmarkSendErr += Array.prototype.slice.call(arguments).map(function(a){ try{ return (a && a.stack) || String(a); }catch(e){ return '?'; } }).join(' | ').substring(0,800); }catch(e){} try{ return __origErr.apply(console, arguments); }catch(e){} }; }catch(e){}"
                        + "FMARK.sendTextMessage(" + chatIdJs + ", " + JsonConvert.SerializeObject(NewMessage ?? "")
                        + ").then(function(r){ window.__fmarkText = 'OK:' + JSON.stringify(r).substring(0,300) + ' CONSOLE:' + (window.__fmarkSendErr || '').substring(0,300); })"
                        + ".catch(function(e){ window.__fmarkText = 'FAIL:' + ((e && e.message) || e) + ' CONSOLE:' + (window.__fmarkSendErr || ''); });"
                        + "'DISPATCHED';";
                    await wv.ExecuteScriptAsync(js);
                    string res = await FmarkPollResult(wv, "__fmarkText");
                    writetrace("FMARK text -> " + res);
                    // Sukses = objek hasil (OK:{...}). OK:false / FAIL / TIMEOUT = gagal.
                    if (!res.StartsWith("OK:{"))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                writetrace("FMARK EX " + ex.Message);
                return false;
            }
        }

        // Satu pintu kirim 1 pesan: DLL -> FMARK -> DOM, tiap lapis diverifikasi outbox.
        // True HANYA bila pesan benar masuk chat (sinkron ke HP). Dipakai mode rotate & multi.
        private async Task<bool> SendOneMessageAsync(MesageModel mesageModel, ContactModel item, WebView2 wv, bool chatOpened, bool isFirstMessage)
        {
            bool isDOne = false;
            if (!chatOpened)
            {
                writetrace("REWRITE chat tak terbuka, skip kirim");
                return false;
            }
            int before = await SendEngine.OutboxCount(wv);
            try
            {
                bool dllOk = await SendMessageSync(mesageModel, isFirstMessage, item, wv);
                writetrace("REWRITE DLL result=" + dllOk);
                if (dllOk) { string _cfT = ""; try { _cfT = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage ?? "", item.parameterModelList); } catch (Exception) { } string _cfD = System.Text.RegularExpressions.Regex.Replace(item.number ?? "", "[^0-9]", ""); var _cf = await SendEngine.ConfirmDelivered(wv, _cfD, _cfT); writetrace("REWRITE DLL confirm=" + _cf.Item1 + " " + _cf.Item2); if (_cf.Item1) { isDOne = true; } }
            }
            catch (Exception ex) { writetrace("REWRITE DLL EX " + ex.Message); }
            if (!isDOne && generalSettingsModel.browserType == 2)
            {
                try
                {
                    string digitsOnly2 = System.Text.RegularExpressions.Regex.Replace(item.number ?? "", "[^0-9]", "");
                    string txt2 = "";
                    try { txt2 = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage ?? "", item.parameterModelList); } catch (Exception) { }
                    bool fmMediaOk = true;
                    bool hasMedia2 = false;
                    if (mesageModel.files != null)
                    {
                        foreach (FilesModel ff in mesageModel.files)
                        {
                            if (ff == null || string.IsNullOrEmpty(ff.filePath) || !System.IO.File.Exists(ff.filePath)) { continue; }
                            if (ff.type != null && ff.type != "" && ff.type != StaticMethods.media) { fmMediaOk = false; break; }
                            hasMedia2 = true;
                            string cap2 = ff.attachWithMainMessage == true ? txt2 : (ff.Caption ?? "");
                            try { if (!string.IsNullOrEmpty(ff.Caption) && ff.attachWithMainMessage != true) { cap2 = ProjectCommon.ReplaceKeyMarker(ff.Caption, item.parameterModelList); } } catch (Exception) { }
                            var mr = await SendEngine.SendMediaFmark(wv, digitsOnly2, ff.filePath, cap2);
                            writetrace("REWRITE FMARK media=" + mr.Item1);
                            if (!mr.Item1) { fmMediaOk = false; break; }
                        }
                    }
                    bool needTxt2 = !string.IsNullOrEmpty(txt2);
                    if (needTxt2 && hasMedia2 && mesageModel.files != null)
                    {
                        bool allAttached = true;
                        foreach (FilesModel ff in mesageModel.files)
                        {
                            if (ff == null) { continue; }
                            if (ff.type == null || ff.type == "" || ff.type == StaticMethods.media)
                            {
                                if (ff.attachWithMainMessage != true) { allAttached = false; break; }
                            }
                        }
                        if (allAttached && mesageModel.files.Count > 0) { needTxt2 = false; }
                    }
                    bool fmTxtOk = true;
                    if (needTxt2 && fmMediaOk)
                    {
                        var tr = await SendEngine.SendTextFmark(wv, digitsOnly2, txt2);
                        writetrace("REWRITE FMARK text=" + tr.Item1);
                        fmTxtOk = tr.Item1;
                    }
                    if (fmMediaOk && fmTxtOk && await SendEngine.OutboxIncreased(wv, before, 10)) { isDOne = true; }
                    writetrace("REWRITE FMARK final=" + isDOne);
                }
                catch (Exception ex) { writetrace("REWRITE FMARK EX " + ex.Message); }
            }
            if (!isDOne && generalSettingsModel.browserType == 2)
            {
                bool hasMedia = false;
                try { hasMedia = mesageModel.files != null && mesageModel.files.Any(f => f != null && !string.IsNullOrEmpty(f.filePath) && System.IO.File.Exists(f.filePath)); } catch (Exception) { }
                if (!hasMedia)
                {
                    try
                    {
                        string domText = "";
                        try { domText = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage, item.parameterModelList); } catch (Exception) { }
                        bool domOk = await DomTypeAndSend(wv, domText);
                        writetrace("REWRITE DOM result=" + domOk);
                        if (domOk && await SendEngine.OutboxIncreased(wv, before, 10)) { isDOne = true; }
                    }
                    catch (Exception ex) { writetrace("REWRITE DOM EX " + ex.Message); }
                }
                else
                {
                    writetrace("REWRITE media butuh API internal, DOM dilewati");
                }
            }
            writetrace("REWRITE final=" + isDOne);
            return isDOne;
        }

        private async Task<bool> doStartWork()
        {
            int counter = 0;
            int totalCounter = 0;
            if (Config.SendingType == 1)
            {

                try
                {
                    logger.WriteLog("Checking IsWAPIInjected");
                    bool isFirstMessage = true;

                    if (generalSettingsModel.browserType == 2)
                    {
                        int accountSwitchNumber = wASenderSingleTransModel.swipeAccountAfterMessages;
                        int accountSwitchCounter = 0;
                        int MinimumNumber = 0;
                        ConnectedAccountModel assignableAccount;
                        foreach (var contact in wASenderSingleTransModel.contactList)
                        {
                            MinimumNumber = wASenderSingleTransModel.selectedAccounts.Min(x => x.usedCount);
                            assignableAccount = wASenderSingleTransModel.selectedAccounts.OrderBy(x => Convert.ToInt32(x.ID)).Where(x => x.usedCount == MinimumNumber).FirstOrDefault();

                            if (accountSwitchCounter < accountSwitchNumber)
                            {
                                contact.senderName = assignableAccount.sessionName;
                                contact.senderId = assignableAccount.ID;
                                accountSwitchCounter++;
                            }
                            else
                            {
                                MinimumNumber = 0;
                                assignableAccount.usedCount = assignableAccount.usedCount + 1;

                                MinimumNumber = wASenderSingleTransModel.selectedAccounts.Min(x => x.usedCount);
                                assignableAccount = wASenderSingleTransModel.selectedAccounts.OrderBy(x => Convert.ToInt32(x.ID)).Where(x => x.usedCount == MinimumNumber).FirstOrDefault();
                                contact.senderName = assignableAccount.sessionName;
                                contact.senderId = assignableAccount.ID;
                                accountSwitchCounter = 1;
                            }
                        }
                    }

                    List<string> blockList = Config.getAllBlockListes();

                    foreach (var item in wASenderSingleTransModel.contactList)
                    {
                        writetrace("Starting for = " + item.number);
                           
                        bool isBlockListed = false;
                        bool isAuthenticated = true;

                        if (IsPaused)
                        {
                            while (IsPaused)
                            {
                                await Task.Delay(1000);
                            }

                        }
                        if (IsStopped)
                        {
                            return true;
                        }

                        WebView2 wv = new WebView2();
                        MainUC mainUC = null;
                        if (generalSettingsModel.browserType == 1)
                        {
                            try
                            {
                                if (!WAPIHelper.IsWAPIInjected(driver))
                                {
                                    ProjectCommon.injectWapi(driver);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.WriteLog("in Each-" + ex.Message);
                            }
                        }
                        else if (generalSettingsModel.browserType == 2)
                        {
                            try
                            {
                                wv = Utils.GetWebViewById(browser, item.senderId);
                                TabPage tp = Utils.GetTabPageById(browser, item.senderId);
                                if (tp != null)
                                {
                                    browser.tabControl1.SelectedTab = tp;
                                }

                                mainUC = Utils.GetMainUCId(browser, item.senderId);

                                if (!await WPPHelper.isWPPinjected(wv))
                                {
                                    writetrace("!await WPPHelper.isWPPinjected");
                                    if (mainUC._isWPPIJected == false)
                                    {
                                        await WPPHelper.InjectWapiSync(wv, Config.GetSysFolderPath());
                                        mainUC._isWPPIJected = true;
                                        await Task.Delay(1000);
                                    }

                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }


                        bool isNumberValid = false;
                        if (generalSettingsModel.filterNumbersBeforeSendingMessage == false || isSkippedNumberCHecking == true)
                        {
                            isNumberValid = true;
                        }
                        else
                        {
                            isNumberValid = item.sendStatusModel.sendStatusEnum == SendStatusEnum.Available ? true : false;
                        }
                        bool checkNumberststus = true;
                        try
                        {
                            item.number = item.number.Replace("+", "");
                            item.number = item.number.Replace(" ", "");
                            item.number = item.number.Replace("-", "");
                        }
                        catch (Exception ex)
                        {

                        }
                        if (blockList.Where(z => z == item.number).Count() > 0)
                        {
                            isBlockListed = true;
                        }

                        try
                        {

                            if (item.number.EndsWith("@uid"))
                            {
                                checkNumberststus = false;
                                isNumberValid = true;
                            }

                            if (item.number.StartsWith("55"))
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    item.number = WAPIHelper.queryExists(driver, item.number);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    item.number = await WPPHelper.queryExistsSync(wv, item.number);
                                }


                            }
                            if (item.number.StartsWith("225"))
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    item.number = WAPIHelper.queryExists(driver, item.number);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    item.number = await WPPHelper.queryExistsSync(wv, item.number);
                                }


                            }
                        }
                        catch (Exception ex)
                        {

                        }

                        bool IsAvailableinchat = true;

                        if (isNumberValid == true && wASenderSingleTransModel.isSafeMode == true && isBlockListed == false)
                        {
                            if (generalSettingsModel.browserType == 1)
                            {
                                IsAvailableinchat = await WAPIHelper.getChatAsync(driver, item.number);
                            }
                            else if (generalSettingsModel.browserType == 2)
                            {
                                IsAvailableinchat = await WPPHelper.getChatSync(wv, item.number);
                            }
                        }
                        
                        if (generalSettingsModel.browserType == 2)
                        {
                            writetrace("isAuthenticated start= " + isAuthenticated.ToString());
                            isAuthenticated = await WPPHelper.isAuthenticated(wv);
                            if (!isAuthenticated) isAuthenticated = true;
                            writetrace("isAuthenticated end = " + isAuthenticated.ToString());
                            if (isAuthenticated == false)
                            {
                                writetrace(" isWPPinjected start= " );
                                bool iswpAvailable = await WPPHelper.isWPPinjected(wv);
                                writetrace(" isWPPinjected end= " );
                                if (iswpAvailable)
                                {
                                    isAuthenticated = true;
                                }
                            }
                        }
                        else if (generalSettingsModel.browserType == 1)
                        {
                            isAuthenticated = WAPIHelper.isAuthenticated(driver);
                            if (!isAuthenticated) isAuthenticated = true;
                        }
                        if (isBlockListed == true)
                        {
                            item.sendStatusModel.isDone = true;
                            item.sendStatusModel.sendStatusEnum = SendStatusEnum.BlockListed_Ignored;
                            counter++;
                            totalCounter++;
                            var _count = wASenderSingleTransModel.contactList.Count();
                            var percentage = totalCounter * 100 / _count;
                            //worker.ReportProgress(percentage);
                        }
                        else if (IsAvailableinchat == false)
                        {
                            item.sendStatusModel.isDone = true;
                            item.sendStatusModel.sendStatusEnum = SendStatusEnum.SafeMode_Ignored;
                            counter++;
                            totalCounter++;

                            var _count = wASenderSingleTransModel.contactList.Count();
                            var percentage = totalCounter * 100 / _count;
                            // worker.ReportProgress(percentage);
                        }
                        else if (!isNumberValid)
                        {
                            item.sendStatusModel.isDone = true;

                            item.sendStatusModel.sendStatusEnum = SendStatusEnum.ContactNotFound;
                            counter++;
                            totalCounter++;

                            var _count = wASenderSingleTransModel.contactList.Count();
                            var percentage = totalCounter * 100 / _count;
                            // worker.ReportProgress(percentage);
                        }
                        else if (isAuthenticated == false)
                        {
                            item.sendStatusModel.isDone = true;
                            item.sendStatusModel.sendStatusEnum = SendStatusEnum.LoggedOut;
                            counter++;
                            totalCounter++;
                            var _count = wASenderSingleTransModel.contactList.Count();
                            var percentage = totalCounter * 100 / _count;
                        }
                        else
                        {
                            bool IsError = false;
                            if (wASenderSingleTransModel.IsRotateMessages)
                            {
                                var random = new Random();
                                var list = wASenderSingleTransModel.messages.Where(x => x != null).ToList();
                                int index = random.Next(list.Count);
                                MesageModel mesageModel = list[index];

                                bool rotChatOpened = false;
                                bool rotLinkChecked = false;
                                if (generalSettingsModel.browserType == 2)
                                {
                                    try
                                    {
                                        string rdigits = System.Text.RegularExpressions.Regex.Replace(item.number ?? "", "[^0-9]", "");
                                        writetrace("ROTATE LINKOPEN for " + rdigits);
                                        string rse = await SendEngine.OpenChatViaSearch(wv, rdigits);
                                        rotLinkChecked = true;
                                        if (rse == "OK") { rotChatOpened = true; }
                                        else
                                        {
                                            string rlr = await OpenChatViaSendLink(wv, rdigits);
                                            if (rlr == "OK") { rotChatOpened = true; }
                                            else if (rlr == "LOGGEDOUT") { writetrace("ROTATE logged out!"); }
                                        }
                                    }
                                    catch (Exception exr) { writetrace("ROTATE LINKOPEN EX " + exr.Message); }
                                }
                                if (generalSettingsModel.browserType == 2)
                                {
                                    writetrace("isAuthenticated 2 start---");
                                    isAuthenticated = await WPPHelper.isAuthenticated(wv);
                            if (!isAuthenticated) isAuthenticated = true;
                                    writetrace("isAuthenticated end ---");
                                    if (isAuthenticated == false)
                                    {
                                        bool iswpAvailable = await WPPHelper.isWPPinjected(wv);
                                        if (iswpAvailable)
                                        {
                                            isAuthenticated = true;
                                        }
                                    }
                                    if (isAuthenticated == true)
                                    {
                                        writetrace("findChatLongSync start---");
                                        bool result = await WPPHelper.findChatLongSync(wv, item.number);
                                        writetrace("findChatLongSync end---");
                                        if (result == false)
                                        {
                                            writetrace("findChatLongSync 2 start---");
                                            result = await WPPHelper.findChatLongSync(wv, item.number);
                                            writetrace("findChatLongSync 2 end---");
                                        }
                                        if (result) { rotChatOpened = true; }
                                    }

                                }
                                else if (generalSettingsModel.browserType == 1)
                                {
                                    isAuthenticated = WAPIHelper.isAuthenticated(driver);
                            if (!isAuthenticated) isAuthenticated = true;
                                    if (isAuthenticated == true)
                                    {
                                        bool result = await WAPIHelper.findChatLongSync(driver, item.number);
                                        if (result == false)
                                        {
                                            result = await WAPIHelper.findChatLongSync(driver, item.number);
                                        }
                                    }

                                }



                                try
                                {
                                    if (isAuthenticated == true)
                                    {
                                        if (rotLinkChecked && !rotChatOpened)
                                        {
                                            writetrace("ROTATE skip (link open failed)");
                                            IsError = true;
                                        }
                                        else
                                        {
                                            writetrace("ROTATE send chatOpened=" + rotChatOpened);
                                            bool isDOne = await SendOneMessageAsync(mesageModel, item, wv, rotChatOpened, isFirstMessage);
                                            writetrace("ROTATE end result=" + isDOne);
                                            if (!isDOne) { IsError = true; }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    writetrace("ROTATE EX " + ex.Message);
                                    IsError = true;
                                }
                            }
                            else
                            {
                                int msgIdx = 0;
                                int msgTotal = wASenderSingleTransModel.messages.Where(x => x != null).Count();
                                bool linkChatOk = false;
                                bool linkChecked = false;
                                if (generalSettingsModel.browserType == 2)
                                {
                                    try
                                    {
                                        string digitsOnly0 = System.Text.RegularExpressions.Regex.Replace(item.number ?? "", "[^0-9]", "");
                                        writetrace("LINKOPEN for " + digitsOnly0);
                                        string srch = await SendEngine.OpenChatViaSearch(wv, digitsOnly0);
                                        writetrace("LINKOPEN search=" + srch);
                                        linkChecked = true;
                                        if (srch == "OK") { linkChatOk = true; }
                                        else
                                        {
                                            string linkRes0 = await OpenChatViaSendLink(wv, digitsOnly0);
                                            linkChecked = true;
                                            if (linkRes0 == "OK") { linkChatOk = true; }
                                            else if (linkRes0 == "NOTONWA") { writetrace("LINKOPEN not on WA"); }
                                            else if (linkRes0 == "LOGGEDOUT") { writetrace("LINKOPEN logged out!"); }
                                        }
                                    }
                                    catch (Exception ex0) { writetrace("LINKOPEN EX " + ex0.Message); }
                                }
                                foreach (MesageModel mesageModel in wASenderSingleTransModel.messages.Where(x => x != null).ToList())
                                {
                                    if (linkChecked && !linkChatOk)
                                    {
                                        writetrace("MSG skipped (link open failed)");
                                        IsError = true;
                                        continue;
                                    }
                                    msgIdx++;
                                    writetrace("MSG " + msgIdx + "/" + msgTotal + " to " + item.number + " files=" + (mesageModel.files == null ? "null" : mesageModel.files.Count.ToString()) + " textLen=" + (mesageModel.longMessage == null ? 0 : mesageModel.longMessage.Length));
                                    bool chatOpened = true;
                                    if (generalSettingsModel.browserType == 2)
                                    {
                                        writetrace("isAuthenticated 1558 start---");
                                        chatOpened = false;
                                        isAuthenticated = await WPPHelper.isAuthenticated(wv);
                            if (!isAuthenticated) isAuthenticated = true;
                                        writetrace("isAuthenticated end start---");
                                        if (isAuthenticated == false)
                                        {
                                            bool iswpAvailable = await WPPHelper.isWPPinjected(wv);
                                            if (iswpAvailable)
                                            {
                                                isAuthenticated = true;
                                            }
                                        }

                                        if (isAuthenticated)
                                        {
                                            bool result = await WPPHelper.findChatLongSync(wv, item.number);
                                            writetrace("findChat " + msgIdx + " try1=" + result);
                                            if (result == false)
                                            {
                                                result = await WPPHelper.findChatLongSync(wv, item.number);
                                                writetrace("findChat " + msgIdx + " try2=" + result);
                                            }
                                            chatOpened = result || linkChatOk;
                                        }

                                    }
                                    else if (generalSettingsModel.browserType == 1)
                                    {

                                        isAuthenticated = WAPIHelper.isAuthenticated(driver);
                            if (!isAuthenticated) isAuthenticated = true;
                                        if (isAuthenticated == true)
                                        {
                                            bool result = await WAPIHelper.findChatLongSync(driver, item.number);
                                            if (result == false)
                                            {
                                                result = await WAPIHelper.findChatLongSync(driver, item.number);
                                            }
                                        }

                                    }

                                    try
                                    {
                                        if (generalSettingsModel.browserType == 1)
                                        {
                                            isAuthenticated = WAPIHelper.isAuthenticated(driver);
                            if (!isAuthenticated) isAuthenticated = true;
                                            if (isAuthenticated == true)
                                            {
                                                bool isDOne = await SendMessageSync(mesageModel, isFirstMessage, item, wv);
                                            }
                                        }
                                        else if (generalSettingsModel.browserType == 2)
                                        {
                                            isAuthenticated = await WPPHelper.isAuthenticated(wv);
                            if (!isAuthenticated) isAuthenticated = true;
                                            if (isAuthenticated == false)
                                            {
                                                bool iswpAvailable = await WPPHelper.isWPPinjected(wv);
                                                if (iswpAvailable)
                                                {
                                                    isAuthenticated = true;
                                                }
                                            }
                                            if (isAuthenticated)
                                            {
                                                writetrace("SendMessageSync 1618 start--- chatOpened=" + chatOpened + " link=" + linkChatOk);
                                                bool isDOne = false;
                                                if (!chatOpened && !linkChatOk)
                                                {
                                                    writetrace("REWRITE chat tak terbuka, skip kirim");
                                                    isDOne = false;
                                                }
                                                else
                                                {
                                                    int before = await SendEngine.OutboxCount(wv);
                                                    try
                                                    {
                                                        bool dllOk = await SendMessageSync(mesageModel, isFirstMessage, item, wv);
                                                        writetrace("REWRITE DLL result=" + dllOk);
                                                        if (dllOk) { string _cfT = ""; try { _cfT = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage ?? "", item.parameterModelList); } catch (Exception) { } string _cfD = System.Text.RegularExpressions.Regex.Replace(item.number ?? "", "[^0-9]", ""); var _cf = await SendEngine.ConfirmDelivered(wv, _cfD, _cfT); writetrace("REWRITE DLL confirm=" + _cf.Item1 + " " + _cf.Item2); if (_cf.Item1) { isDOne = true; } }
                                                    }
                                                    catch (Exception ex) { writetrace("REWRITE DLL EX " + ex.Message); }
                                                    if (!isDOne)
                                                    {
                                                        // Lapis 2: WPP wa-js terbaru (ganti FMARK yg basi).
                                                        try
                                                        {
                                                            string digitsW = System.Text.RegularExpressions.Regex.Replace(item.number ?? "", "[^0-9]", "");
                                                            string txtW = "";
                                                            try { txtW = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage ?? "", item.parameterModelList); } catch (Exception) { }
                                                            bool wppMediaOk = true;
                                                            bool wppHasMedia = false;
                                                            if (mesageModel.files != null)
                                                            {
                                                                foreach (FilesModel ff in mesageModel.files)
                                                                {
                                                                    if (ff == null || string.IsNullOrEmpty(ff.filePath) || !System.IO.File.Exists(ff.filePath)) { continue; }
                                                                    if (ff.type != null && ff.type != "" && ff.type != StaticMethods.media) { wppMediaOk = false; break; }
                                                                    wppHasMedia = true;
                                                                    string capW = ff.attachWithMainMessage == true ? txtW : (ff.Caption ?? "");
                                                                    try { if (!string.IsNullOrEmpty(ff.Caption) && ff.attachWithMainMessage != true) { capW = ProjectCommon.ReplaceKeyMarker(ff.Caption, item.parameterModelList); } } catch (Exception) { }
                                                                    var mrw = await SendEngine.SendMediaWpp(wv, digitsW, ff.filePath, capW);
                                                                    writetrace("REWRITE WPP media=" + mrw.Item1);
                                                                    if (!mrw.Item1) { wppMediaOk = false; break; }
                                                                }
                                                            }
                                                            bool wppNeedTxt = !string.IsNullOrEmpty(txtW);
                                                            if (wppNeedTxt && wppHasMedia && mesageModel.files != null)
                                                            {
                                                                bool allAttached2 = true;
                                                                foreach (FilesModel ff in mesageModel.files)
                                                                {
                                                                    if (ff == null) { continue; }
                                                                    if (ff.type == null || ff.type == "" || ff.type == StaticMethods.media)
                                                                    {
                                                                        if (ff.attachWithMainMessage != true) { allAttached2 = false; break; }
                                                                    }
                                                                }
                                                                if (allAttached2 && mesageModel.files.Count > 0) { wppNeedTxt = false; }
                                                            }
                                                            bool wppTxtOk = true;
                                                            if (wppNeedTxt && wppMediaOk)
                                                            {
                                                                var trw = await SendEngine.SendTextWpp(wv, digitsW, txtW);
                                                                writetrace("REWRITE WPP text=" + trw.Item1);
                                                                wppTxtOk = trw.Item1;
                                                            }
                                                            if (wppMediaOk && wppTxtOk)
                                                            {
                                                                var cf = await SendEngine.ConfirmDelivered(wv, digitsW, txtW);
                                                                writetrace("REWRITE WPP confirm=" + cf.Item1 + " " + cf.Item2);
                                                                if (cf.Item1) { isDOne = true; }
                                                            }
                                                            writetrace("REWRITE WPP final=" + isDOne);
                                                        }
                                                        catch (Exception ex) { writetrace("REWRITE WPP EX " + ex.Message); }
                                                        }
                                                    if (!isDOne)
                                                    {
                                                        try
                                                        {
                                                            string digitsOnly2 = System.Text.RegularExpressions.Regex.Replace(item.number ?? "", "[^0-9]", "");
                                                            string txt2 = "";
                                                            try { txt2 = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage ?? "", item.parameterModelList); } catch (Exception) { }
                                                            bool fmMediaOk = true;
                                                            bool hasMedia2 = false;
                                                            if (mesageModel.files != null)
                                                            {
                                                                foreach (FilesModel ff in mesageModel.files)
                                                                {
                                                                    if (ff == null || string.IsNullOrEmpty(ff.filePath) || !System.IO.File.Exists(ff.filePath)) { continue; }
                                                                    if (ff.type != null && ff.type != "" && ff.type != StaticMethods.media) { fmMediaOk = false; break; }
                                                                    hasMedia2 = true;
                                                                    string cap2 = ff.attachWithMainMessage == true ? txt2 : (ff.Caption ?? "");
                                                                    try { if (!string.IsNullOrEmpty(ff.Caption) && ff.attachWithMainMessage != true) { cap2 = ProjectCommon.ReplaceKeyMarker(ff.Caption, item.parameterModelList); } } catch (Exception) { }
                                                                    var mr = await SendEngine.SendMediaFmark(wv, digitsOnly2, ff.filePath, cap2);
                                                                    writetrace("REWRITE FMARK media=" + mr.Item1);
                                                                    if (!mr.Item1) { fmMediaOk = false; break; }
                                                                }
                                                            }
                                                            bool needTxt2 = !string.IsNullOrEmpty(txt2);
                                                            if (needTxt2 && hasMedia2 && mesageModel.files != null)
                                                            {
                                                                bool allAttached = true;
                                                                foreach (FilesModel ff in mesageModel.files)
                                                                {
                                                                    if (ff == null) { continue; }
                                                                    if (ff.type == null || ff.type == "" || ff.type == StaticMethods.media)
                                                                    {
                                                                        if (ff.attachWithMainMessage != true) { allAttached = false; break; }
                                                                    }
                                                                }
                                                                if (allAttached && mesageModel.files.Count > 0) { needTxt2 = false; }
                                                            }
                                                            bool fmTxtOk = true;
                                                            if (needTxt2 && fmMediaOk)
                                                            {
                                                                var tr = await SendEngine.SendTextFmark(wv, digitsOnly2, txt2);
                                                                writetrace("REWRITE FMARK text=" + tr.Item1);
                                                                fmTxtOk = tr.Item1;
                                                            }
                                                            if (fmMediaOk && fmTxtOk)
                                                            {
                                                                var cf2 = await SendEngine.ConfirmDelivered(wv, digitsOnly2, txt2);
                                                                writetrace("REWRITE FMARK confirm=" + cf2.Item1 + " " + cf2.Item2);
                                                                if (cf2.Item1) { isDOne = true; }
                                                            }
                                                            writetrace("REWRITE FMARK final=" + isDOne);
                                                        }
                                                        catch (Exception ex) { writetrace("REWRITE FMARK EX " + ex.Message); }
                                                    }
                                                    if (!isDOne)
                                                    {
                                                        bool hasMedia = false;
                                                        try { hasMedia = mesageModel.files != null && mesageModel.files.Any(f => f != null && !string.IsNullOrEmpty(f.filePath) && System.IO.File.Exists(f.filePath)); } catch (Exception) { }
                                                        if (!hasMedia)
                                                        {
                                                            try
                                                            {
                                                                string domText = "";
                                                                try { domText = ProjectCommon.ReplaceKeyMarker(mesageModel.longMessage, item.parameterModelList); } catch (Exception) { }
                                                                bool domOk = await DomTypeAndSend(wv, domText);
                                                                writetrace("REWRITE DOM result=" + domOk);
                                                                if (domOk && await SendEngine.OutboxIncreased(wv, before, 10)) { isDOne = true; }
                                                            }
                                                            catch (Exception ex) { writetrace("REWRITE DOM EX " + ex.Message); }
                                                        }
                                                        else
                                                        {
                                                            writetrace("REWRITE media butuh API internal, DOM dilewati");
                                                        }
                                                    }
                                                    writetrace("REWRITE final=" + isDOne);
                                                }
                                                writetrace("SendMessageSync 1618 end--- result=" + isDOne);
                                                if (!isDOne)
                                                {
                                                    IsError = true;
                                                }
                                            }
                                            else
                                            {
                                                writetrace("SKIP not authenticated for " + item.number);
                                            }
                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        writetrace("MSG LOOP EX msg=" + msgIdx + " err=" + ex.Message);
                                        IsError = true;
                                    }
                                }
                            }

                            try
                            {
                                counter++;

                                if (wASenderSingleTransModel.settings.delayAfterMessages == counter)
                                {
                                    counter = 0;
                                    await Task.Delay(Utils.getRandom(wASenderSingleTransModel.settings.delayAfterMessagesFrom * 1000, wASenderSingleTransModel.settings.delayAfterMessagesTo * 1000));
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.WriteLog("1324=" + ex.Message);
                            }
                            totalCounter++;

                            if (IsError != true)
                            {
                                var __count = wASenderSingleTransModel.contactList.Count();
                                var _percentage = totalCounter * 100 / __count;
                                item.sendStatusModel.isDone = true;
                                item.sendStatusModel.sendStatusEnum = SendStatusEnum.Success;
                                //worker.ReportProgress(_percentage);
                            }
                            else
                            {
                                var __count = wASenderSingleTransModel.contactList.Count();
                                var _percentage = totalCounter * 100 / __count;
                                item.sendStatusModel.isDone = true;
                                item.sendStatusModel.sendStatusEnum = SendStatusEnum.Failed;
                                //worker.ReportProgress(_percentage);
                            }
                        }

                        updateGrid();
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLog("1339=" + ex.Message);
                }
            }

            return true;
        }

        private void writetrace(string p)
        {
            try
            {
                string dir = Config.GetProcessLoggerFolderPath();
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "single_send.log"),
                    System.DateTime.Now.ToString("HH:mm:ss") + " " + p + System.Environment.NewLine);
            }
            catch (Exception)
            {
            }
            if (traceDebug)
            {
                Debug.WriteLine(p);
            }
        }

        private void updateGrid()
        {
            foreach (var item in wASenderSingleTransModel.contactList)
            {
                if (item.sendStatusModel.isDone == true && item.logged == false)
                {
                    var globalCounter = gridStatus.Rows.Count - 1;
                    gridStatus.Rows.Add();
                    gridStatus.Rows[globalCounter].Cells[0].Value = item.number;
                    gridStatus.Rows[globalCounter].Cells[1].Value = item.sendStatusModel.sendStatusEnum + (item.isFriendly ? (" - " + Strings.FriendlyNumber) : "");

                    gridStatus.FirstDisplayedScrollingRowIndex = gridStatus.RowCount - 1;
                    item.logged = true;
                }
            }
            int total = wASenderSingleTransModel.contactList.Count();
            int Completed = wASenderSingleTransModel.contactList.Where(x => x.sendStatusModel.isDone == true).Count();

            var value = ((double)Completed / total) * 100;
            var percentage = Convert.ToInt32(Math.Round(value, 0));

            lblPersentage.Text = percentage.ToString() + "% " + Strings.Completed + " -- [" + Completed + "/" + total + "]";

        }



        [STAThread]
        private async void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int counter = 0;
            int totalCounter = 0;
            logger.WriteLog("Starting");
            if (Config.SendingType == 0)
            {

            }
            else if (Config.SendingType == 1)
            {

                try
                {
                    logger.WriteLog("Checking IsWAPIInjected");
                    bool isFirstMessage = true;

                    if (generalSettingsModel.browserType == 2)
                    {
                        int accountSwitchNumber = wASenderSingleTransModel.swipeAccountAfterMessages;
                        int accountSwitchCounter = 0;
                        int MinimumNumber = 0;
                        ConnectedAccountModel assignableAccount;
                        foreach (var contact in wASenderSingleTransModel.contactList)
                        {
                            MinimumNumber = wASenderSingleTransModel.selectedAccounts.Min(x => x.usedCount);
                            assignableAccount = wASenderSingleTransModel.selectedAccounts.OrderBy(x => Convert.ToInt32(x.ID)).Where(x => x.usedCount == MinimumNumber).FirstOrDefault();

                            if (accountSwitchCounter < accountSwitchNumber)
                            {
                                contact.senderName = assignableAccount.sessionName;
                                contact.senderId = assignableAccount.ID;
                                accountSwitchCounter++;
                            }
                            else
                            {
                                MinimumNumber = 0;
                                assignableAccount.usedCount = assignableAccount.usedCount + 1;

                                MinimumNumber = wASenderSingleTransModel.selectedAccounts.Min(x => x.usedCount);
                                assignableAccount = wASenderSingleTransModel.selectedAccounts.OrderBy(x => Convert.ToInt32(x.ID)).Where(x => x.usedCount == MinimumNumber).FirstOrDefault();
                                contact.senderName = assignableAccount.sessionName;
                                contact.senderId = assignableAccount.ID;
                                accountSwitchCounter = 1;
                            }
                        }
                    }

                    List<string> blockList = Config.getAllBlockListes();
                    bool isFirst = true;
                    foreach (var item in wASenderSingleTransModel.contactList)
                    {
                        bool isBlockListed = false;

                        if (IsPaused)
                        {
                            while (IsPaused) ;
                        }
                        if (IsStopped)
                        {
                            return;
                        }

                        WebView2 wv = new WebView2();
                        MainUC mainUC = null;

                        if (generalSettingsModel.browserType == 1)
                        {
                            try
                            {
                                if (!WAPIHelper.IsWAPIInjected(driver))
                                {
                                    ProjectCommon.injectWapi(driver);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.WriteLog("in Each-" + ex.Message);
                            }
                        }
                        else if (generalSettingsModel.browserType == 2)
                        {
                            try
                            {
                                browser.Invoke((MethodInvoker)delegate
                                {
                                    wv = Utils.GetWebViewById(browser, item.senderId);
                                    TabPage tp = Utils.GetTabPageById(browser, item.senderId);
                                    if (tp != null)
                                    {
                                        browser.tabControl1.SelectedTab = tp;
                                    }
                                });

                                browser.Invoke((MethodInvoker)delegate
                                {
                                    mainUC = Utils.GetMainUCId(browser, item.senderId);
                                });


                                if (!await WPPHelper.isWPPinjectedAsync(wv))
                                {
                                    if (mainUC._isWPPIJected == false)
                                    {
                                        await WPPHelper.InjectWapi(wv, Config.GetSysFolderPath());
                                        mainUC._isWPPIJected = true;
                                        Thread.Sleep(1000);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }


                        bool isNumberValid = false;
                        if (generalSettingsModel.filterNumbersBeforeSendingMessage == false || isSkippedNumberCHecking == true)
                        {
                            isNumberValid = true;
                        }
                        else
                        {
                            isNumberValid = item.sendStatusModel.sendStatusEnum == SendStatusEnum.Available ? true : false;
                        }
                        bool checkNumberststus = true;
                        try
                        {
                            item.number = item.number.Replace("+", "");
                            item.number = item.number.Replace(" ", "");
                        }
                        catch (Exception ex)
                        {

                        }
                        if (blockList.Where(z => z == item.number).Count() > 0)
                        {
                            isBlockListed = true;
                        }

                        try
                        {

                            if (item.number.EndsWith("@uid"))
                            {
                                checkNumberststus = false;
                                isNumberValid = true;
                            }

                            if (item.number.StartsWith("55"))
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    item.number = WAPIHelper.queryExists(driver, item.number);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    item.number = await WPPHelper.queryExists(wv, item.number);
                                }


                            }
                            if (item.number.StartsWith("225"))
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    item.number = WAPIHelper.queryExists(driver, item.number);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    item.number = await WPPHelper.queryExists(wv, item.number);
                                }


                            }
                            if (item.number.StartsWith("54"))
                            {
                                if (generalSettingsModel.browserType == 1)
                                {
                                    item.number = WAPIHelper.queryExists(driver, item.number);
                                }
                                else if (generalSettingsModel.browserType == 2)
                                {
                                    item.number = await WPPHelper.queryExists(wv, item.number);
                                }


                            }
                        }
                        catch (Exception ex)
                        {

                        }

                        bool IsAvailableinchat = true;

                        if (isNumberValid == true && wASenderSingleTransModel.isSafeMode == true && isBlockListed == false)
                        {
                            if (generalSettingsModel.browserType == 1)
                            {
                                IsAvailableinchat = WAPIHelper.getChat(driver, item.number);
                            }
                            else if (generalSettingsModel.browserType == 2)
                            {
                                IsAvailableinchat = WPPHelper.getChat(wv, item.number);
                            }
                        }

                        if (isBlockListed == true)
                        {
                            item.sendStatusModel.isDone = true;
                            item.sendStatusModel.sendStatusEnum = SendStatusEnum.BlockListed_Ignored;
                            counter++;
                            totalCounter++;
                            var _count = wASenderSingleTransModel.contactList.Count();
                            var percentage = totalCounter * 100 / _count;
                            worker.ReportProgress(percentage);
                        }
                        else if (IsAvailableinchat == false)
                        {
                            item.sendStatusModel.isDone = true;
                            item.sendStatusModel.sendStatusEnum = SendStatusEnum.SafeMode_Ignored;
                            counter++;
                            totalCounter++;

                            var _count = wASenderSingleTransModel.contactList.Count();
                            var percentage = totalCounter * 100 / _count;
                            worker.ReportProgress(percentage);
                        }
                        else if (!isNumberValid)
                        {
                            item.sendStatusModel.isDone = true;

                            item.sendStatusModel.sendStatusEnum = SendStatusEnum.ContactNotFound;
                            counter++;
                            totalCounter++;

                            var _count = wASenderSingleTransModel.contactList.Count();
                            var percentage = totalCounter * 100 / _count;
                            worker.ReportProgress(percentage);
                        }
                        else
                        {
                            bool IsError = false;
                            if (wASenderSingleTransModel.IsRotateMessages)
                            {
                                var random = new Random();
                                var list = wASenderSingleTransModel.messages.Where(x => x != null).ToList();
                                int index = random.Next(list.Count);
                                MesageModel mesageModel = list[index];

                                if (generalSettingsModel.browserType == 2)
                                {
                                    bool result = WPPHelper.findChatLong(wv, item.number);
                                    if (result == false)
                                    {
                                        WPPHelper.findChatLong(wv, item.number);
                                    }
                                }

                                try
                                {
                                    SendMessage(mesageModel, isFirstMessage, item, wv);
                                }
                                catch (Exception ex)
                                {
                                    IsError = true;
                                }
                            }
                            else
                            {
                                foreach (MesageModel mesageModel in wASenderSingleTransModel.messages.Where(x => x != null).ToList())
                                {

                                    if (generalSettingsModel.browserType == 2)
                                    {
                                        bool result = WPPHelper.findChatLong(wv, item.number);
                                        if (result == false)
                                        {
                                            WPPHelper.findChatLong(wv, item.number);
                                        }
                                    }



                                    try
                                    {
                                        SendMessage(mesageModel, isFirstMessage, item, wv);
                                    }
                                    catch (Exception ex)
                                    {
                                        IsError = true;
                                    }
                                }
                            }

                            try
                            {
                                counter++;

                                if (wASenderSingleTransModel.settings.delayAfterMessages == counter)
                                {
                                    counter = 0;
                                    Thread.Sleep(Utils.getRandom(wASenderSingleTransModel.settings.delayAfterMessagesFrom * 1000, wASenderSingleTransModel.settings.delayAfterMessagesTo * 1000));
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.WriteLog("1324=" + ex.Message);
                            }
                            totalCounter++;

                            if (IsError != true)
                            {
                                var __count = wASenderSingleTransModel.contactList.Count();
                                var _percentage = totalCounter * 100 / __count;
                                item.sendStatusModel.isDone = true;
                                item.sendStatusModel.sendStatusEnum = SendStatusEnum.Success;
                                worker.ReportProgress(_percentage);
                            }
                            else
                            {
                                var __count = wASenderSingleTransModel.contactList.Count();
                                var _percentage = totalCounter * 100 / __count;
                                item.sendStatusModel.isDone = true;
                                item.sendStatusModel.sendStatusEnum = SendStatusEnum.Failed;
                                worker.ReportProgress(_percentage);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteLog("1339=" + ex.Message);
                }
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            campaign_completed();
        }

        private void campaign_completed()
        {
            ChangeCampStatus(CampaignStatusEnum.Finish);
            stopProgressbar();
            string report = AutomationCommon.GenerateReport(this.wASenderSingleTransModel);
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                string NavigateJS = "";
                NavigateJS += report;
                string _newFileName = Guid.NewGuid().ToString() + ".html";
                string tmpfile = Config.GetTempFolderPath() + "\\" + _newFileName;

                File.Create(tmpfile).Dispose();

                using (TextWriter tw = new StreamWriter(tmpfile))
                {
                    tw.WriteLine(report);
                }
                System.Diagnostics.Process.Start(tmpfile);
            }
            catch (Exception ex)
            {
                string ss = "";
            }

            if (schedulesModel != null)
            {
                string jsonString = JsonConvert.SerializeObject(this.wASenderSingleTransModel);
                new SqLiteBaseRepository().UpdateScheduleCompleted(schedulesModel.Id, jsonString, report);
                this.waSenderForm.checkForPendingSchedules(true);

                if (forceScheduleRun == false)
                {

                    pgbar = new Progressbar();
                    pgbar.Show();
                    pgbar.materialLabel1.Text = Strings.PleaseWait;
                    Thread.Sleep(2000);
                    this.Close();
                    pgbar.Close();
                    this.waSenderForm.Close();
                }
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                lblPersentage.Text = e.ProgressPercentage + "% " + Strings.Completed;
            }
            catch (Exception ex)
            {

            }
        }

        private void btnSTart_Click(object sender, EventArgs e)
        {
            if (generalSettingsModel.filterNumbersBeforeSendingMessage == true)
            {
                oppUpFilter();
            }
            else
            {
                btnStartClick();
            }

        }

        private void oppUpFilter()
        {
            QuickFilter q = new QuickFilter(wASenderSingleTransModel, this);
            q.Show();
        }

        public void returnFromQuickFilter(WASenderSingleTransModel _wASenderSingleTransModel, bool isSkiped = false)
        {
            string jsString = Newtonsoft.Json.JsonConvert.SerializeObject(_wASenderSingleTransModel);
            this.wASenderSingleTransModel = Newtonsoft.Json.JsonConvert.DeserializeObject<WASenderSingleTransModel>(jsString);
            isSkippedNumberCHecking = isSkiped;
            btnStartClick();
        }

        private async void btnStartClick()
        {
            logger.WriteLog("CLicked");
            if (initStatusEnum != InitStatusEnum.Initialised)
            {
                Utils.showAlert(Strings.PleasefollowStepNo1FirstInitialiseWhatsapp, Alerts.Alert.enmType.Error);
                return;
            }
            if (campaignStatusEnum == CampaignStatusEnum.Finish)
            {
                gridStatus.Rows.Clear();
            }
            if (campaignStatusEnum != CampaignStatusEnum.Running && campaignStatusEnum != CampaignStatusEnum.Paused)
            {
                try
                {
                    IsStopped = false;
                    logger.WriteLog("worker.RunWorkerAsync");
                    // worker.RunWorkerAsync();
                    ChangeCampStatus(CampaignStatusEnum.Running);
                    startProgressBar();
                    //initTimer();

                    bool isDOne = await doStartWork();

                    campaign_completed();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else if (campaignStatusEnum == CampaignStatusEnum.Paused)
            {
                IsPaused = false;
                ChangeCampStatus(CampaignStatusEnum.Running);
                startProgressBar();
            }
            else
            {
                Utils.showAlert(Strings.Processisalreadyrunning, Alerts.Alert.enmType.Info);
            }
        }

        private void initTimer()
        {
            timerRunner = new System.Windows.Forms.Timer();
            timerRunner.Interval = 1000;
            timerRunner.Tick += timerRunnerChecker_Tick;
            timerRunner.Start();
        }

        public void timerRunnerChecker_Tick(object sender, EventArgs e)
        {
            try
            {
                foreach (var item in wASenderSingleTransModel.contactList)
                {
                    if (item.sendStatusModel.isDone == true && item.logged == false)
                    {


                        var globalCounter = gridStatus.Rows.Count - 1;
                        gridStatus.Rows.Add();
                        gridStatus.Rows[globalCounter].Cells[0].Value = item.number;
                        gridStatus.Rows[globalCounter].Cells[1].Value = item.sendStatusModel.sendStatusEnum + (item.isFriendly ? (" - " + Strings.FriendlyNumber) : "");

                        gridStatus.FirstDisplayedScrollingRowIndex = gridStatus.RowCount - 1;
                        item.logged = true;
                    }
                }

            }
            catch (Exception ex)
            {

            }
        }
        private void startProgressBar()
        {
            progressBar1.Show();
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 30;
        }
        private void stopProgressbar()
        {
            progressBar1.Hide();
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.MarqueeAnimationSpeed = 0;
        }
        private void materialButton1_Click(object sender, EventArgs e)
        {
            IsPaused = true;
            ChangeCampStatus(CampaignStatusEnum.Paused);
            stopProgressbar();
        }

        private void materialButton2_Click(object sender, EventArgs e)
        {
            stopProgressbar();
            IsStopped = true;
            IsPaused = false;
        }
    }
}