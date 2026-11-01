using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using Newtonsoft.Json;
using OfficeOpenXml;
using WASender.Models;
using WASender.enums;
using WASender.Model;

namespace WASender
{
    public partial class NewUI : UserControl
    {
        Logger logger;
        GeneralSettingsModel generalSettingsModel;
        InitStatusEnum initStatusEnum;
        private List<GMapGlobal> inputs;
        Browser b;
        GmExtractorForm gmExtractorForm;
        bool isStop = false;
        bool isManuallyStopped = false;
        public NewUI(GmExtractorForm _gmExtractorForm)
        {
            InitializeComponent();
            this.gmExtractorForm = _gmExtractorForm;
        }

        private void btnInitWA_Click(object sender, EventArgs e)
        {

            InputDialog id = new InputDialog(this);
            id.ShowDialog();
        }

        private void NewUI_Load(object sender, EventArgs e)
        {
            initLanguage();
            logger = new Logger("GMAPExtractor");
            init();
        }

        public void InputReturnList(List<GMapGlobal> _inputs)
        {
            inputs = _inputs;
            InputReturn(inputs[0].searchQuery);
        }

        public void InputReturn(string searchTurm)
        {
            logger.WriteLog("btnInitWA_Click");
            ChangeInitStatus(InitStatusEnum.Initialising);

            string qry = "https://www.google.com/search?q=Software%20companies%20in%20pune&tbm=lcl&hl=en#rlfi=hd:;si:;mv:[[18.5606616,73.9491173],[18.4697754,73.7824466]];tbs:lrf:!1m4!1u3!2m2!3m1!1e1!1m4!1u2!2m2!2m1!1e1!2m1!1e2!2m1!1e3!3sIAE,lf:1,lf_ui:2";
            
            string _url = "https://www.google.com/maps/search/" + Uri.EscapeDataString(searchTurm) + "/";

            //b = new Browser( this,generalSettingsModel.scollDelay);


            registerBrowser();
        }

        private void registerBrowser()
        {
            if (b == null || b.IsDisposed)
            {
                b = new Browser();
                b.url = "https://www.google.com/maps/";
                b.scollDelay = generalSettingsModel.scollDelay == 0 ? 4 : generalSettingsModel.scollDelay;
                b.isGmap = true;
                b.Show();

                b.browserCloseEvent +=b_browserCloseEvent;
                b.browserWAPI_InjectedEvent +=b_browserWAPI_InjectedEvent;
                b.browserWAPI_do_Counter_increment +=b_browserWAPI_do_Counter_increment;
                b.on_DataItem_GoogleMap+=b_on_DataItem_GoogleMap;

                try { Browser.GmapDebugLog("NewUI registerBrowser: window opened, watchdog 25s"); } catch (Exception) { }
                startInitWatchdog();
            }
            
        }

        private System.Windows.Forms.Timer initWatchdog;
        private void startInitWatchdog()
        {
            try
            {
                if (initWatchdog != null)
                {
                    initWatchdog.Stop();
                    initWatchdog.Dispose();
                }
                initWatchdog = new System.Windows.Forms.Timer();
                initWatchdog.Interval = 25000;
                initWatchdog.Tick += (s, e) =>
                {
                    try
                    {
                        initWatchdog.Stop();
                        if ((initStatusEnum == InitStatusEnum.NotInitialised || initStatusEnum == InitStatusEnum.Initialising)
                            && b != null && !b.IsDisposed)
                        {
                            try { Browser.GmapDebugLog("WATCHDOG: forcing Initialised (browser alive, event tak kunjung datang)"); } catch (Exception) { }
                            DoneInit();
                        }
                    }
                    catch (Exception)
                    {
                    }
                };
                initWatchdog.Start();
            }
            catch (Exception)
            {
            }
        }

        private void b_on_DataItem_GoogleMap(Browser arg1, _DataItem_GoogleMap arg2)
        {
            this.addtoRow(arg2);
        }

        private void b_browserWAPI_do_Counter_increment(object sender, EventArgs e)
        {
            doCounerIncremet();
        }

        private void b_browserWAPI_InjectedEvent(object sender, EventArgs e)
        {
            DoneInit();
        }

        private void b_browserCloseEvent(object sender, EventArgs e)
        {
            b = null;
            ChangeInitStatus(InitStatusEnum.Stopped);
        }

        public void closeBroser()
        {
            if (b != null)
            {
                b.Close();
            }
        }

        public void DoneInit()
        {
            try { Browser.GmapDebugLog("NewUI DoneInit -> Initialised"); } catch (Exception) { }
            ChangeInitStatus(InitStatusEnum.Initialised);
        }

        private void initLanguage()
        {


            getSelectedLanguage();
            //SetLanguagesDropdown();




            this.materialLabel2.Text = Strings.Clickbellowbuttontoopenbrowser;
            this.label5.Text = Strings.Status;
            this.materialButton1.Text = Strings.StartGrabbing;
            this.btnInitWA.Text = Strings.Start;
            this.materialButton2.Text = Strings.Stop;
            materialButton3.Text = Strings.Export;

            label2.Text = Strings.Count;
            dataGridView1.Columns[0].HeaderText = "#";
            dataGridView1.Columns[1].HeaderText = Strings.BusinessName;
            dataGridView1.Columns[2].HeaderText = Strings.MobileNumber;
            dataGridView1.Columns[3].HeaderText = Strings.ReviewCount;
            dataGridView1.Columns[4].HeaderText = Strings.RatingCount;
            dataGridView1.Columns[5].HeaderText = Strings.Catagory;
            dataGridView1.Columns[6].HeaderText = Strings.Address;
            dataGridView1.Columns[7].HeaderText = Strings.Website;
            dataGridView1.Columns[8].HeaderText = Strings.EmailId;
            //dataGridView1.Columns[9].HeaderText = Strings.PostalCode;
            dataGridView1.Columns[9].HeaderText = Strings.latitude;
            dataGridView1.Columns[10].HeaderText = Strings.longitude;
            dataGridView1.Columns[11].HeaderText = Strings.State;
            dataGridView1.Columns[12].HeaderText = Strings.City;
            //dataGridView1.Columns[13].HeaderText = Strings.facebookprofile;
            //dataGridView1.Columns[14].HeaderText = Strings.linkedinprofile;
            //dataGridView1.Columns[15].HeaderText = Strings.twitterprofile;

            //dataGridView1.Columns[16].HeaderText = Strings.ImagesFolder;

            materialCheckbox1.Text = Strings.GrabEmailIdaftercompletinfetchingalldata;
            materialButton4.Text = Strings.ImportNumbers;
            materialButton5.Text = Strings.ExportNumbersOnly;

        }

        List<_DataItem_GoogleMap> allData=new List<_DataItem_GoogleMap>();
        public void addtoRow(_DataItem_GoogleMap data)
        {
            if (data == null)
            {
                return;
            }
            // Hindari baris ganda (data live + harvest bisa overlap)
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                try
                {
                    string rName = row.Cells[1].Value == null ? "" : row.Cells[1].Value.ToString();
                    string rPhone = row.Cells[2].Value == null ? "" : row.Cells[2].Value.ToString();
                    if (rName == (data.Business_name ?? "") && rPhone == (data.Phone ?? ""))
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                }
            }

            dataGridView1.Rows.Add(
                new object[] {
                dataGridView1.Rows.Count ,
                data.Business_name,
                data.Phone,
                data.Review,
                data.Rating,
                data.Category,
                data.Address,
                data.Website_url,
                "",
                data.Lat,
                data.Long,
                data.State,
                data.City
                });

            allData.Add(data);

            label1.Text = (dataGridView1.Rows.Count - 1).ToString();

            dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;

        }
        private void getSelectedLanguage()
        {
            string settingPath = Config.GetGeneralSettingsFilePath();

            if (!File.Exists(settingPath))
            {
                File.Create(settingPath).Close();
            }
            generalSettingsModel = new GeneralSettingsModel();
            generalSettingsModel.selectedLanguage = "English";
            try
            {
                string GeneralSettingJson = "";
                using (StreamReader r = new StreamReader(settingPath))
                {
                    GeneralSettingJson = r.ReadToEnd();
                }
                var dict = JsonConvert.DeserializeObject<GeneralSettingsModel>(GeneralSettingJson);
                if (dict != null)
                {
                    generalSettingsModel = dict;
                }
                if (generalSettingsModel.selectedLanguage == null || generalSettingsModel.selectedLanguage == "")
                {
                    generalSettingsModel.selectedLanguage = "English";
                }
                Strings.selectedLanguage = generalSettingsModel.selectedLanguage;
            }
            catch (Exception ex)
            {

            }

        }

        private void init()
        {
            ChangeInitStatus(InitStatusEnum.NotInitialised);
            ChangeCampStatus(CampaignStatusEnum.NotStarted);
            try { GridTheme.Apply(dataGridView1); } catch (Exception) { }
        }

        private void ChangeCampStatus(CampaignStatusEnum _campaignStatus)
        {

            AutomationCommon.ChangeCampStatus(_campaignStatus, lblRunStatus);
        }

        private void ChangeInitStatus(InitStatusEnum _initStatus)
        {
            logger.WriteLog("ChangeInitStatus = " + _initStatus.ToString());
            this.initStatusEnum = _initStatus;
            AutomationCommon.ChangeInitStatus(_initStatus, lblInitStatus);
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {

            try { Browser.GmapDebugLog("StartGrabbing clicked, initStatus=" + initStatusEnum.ToString()); } catch (Exception) { }
            // Step 1 = jendela browser dibuka. Kalau jendelanya masih hidup,
            // anggap init selesai (jangan nag user yg sudah klik Start).
            if (initStatusEnum != InitStatusEnum.Initialised && b != null && !b.IsDisposed)
            {
                try { Browser.GmapDebugLog("StartGrabbing: browser alive -> auto DoneInit"); } catch (Exception) { }
                DoneInit();
            }
            if (initStatusEnum != InitStatusEnum.Initialised)
            {
                Utils.showAlert(Strings.PleasefollowStepNo1FirstInitialiseWhatsapp, Alerts.Alert.enmType.Error);
                return;
            }
            if (inputs == null || inputs.Count == 0)
            {
                Utils.showAlert(Strings.PleasefollowStepNo1FirstInitialiseWhatsapp, Alerts.Alert.enmType.Error);
                return;
            }

            dataGridView1.Rows.Clear();
            allData = new List<_DataItem_GoogleMap>();
            label1.Text = "0";


            StartCapturing();
            isManuallyStopped = false;
        }

        int totalCounter = 0;
        private void StartCapturing()
        {
            totalCounter = 0;
            b.start(inputs[totalCounter].searchQuery);
            ChangeCampStatus(CampaignStatusEnum.Running);
        }

        public void doCounerIncremet()
        {
            totalCounter = totalCounter + 1;
            if (inputs.Count() > totalCounter)
            {
                b.start(inputs[totalCounter].searchQuery);
            }
            else
            {
                if (b !=null)
                {
                    b.Close();
                }
                

                if (materialCheckbox1.Checked && isManuallyStopped==false)
                {
                    isGebaEamilIEnd = false;
                    startcheckingEMailIds();
                    ChangeCampStatus(CampaignStatusEnum.GettingEmailIds);
                }
                else
                {
                    ChangeCampStatus(CampaignStatusEnum.Finish);
                }
            }
        }
        bool isGebaEamilIEnd = false;
        private async void startcheckingEMailIds()
        {
            int _counter = 0;
            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                if (isManuallyStopped == false)
                {
                    if (isGebaEamilIEnd == false)
                    {
                        if (item.Cells[7].Value != null)
                        {
                            dataGridView1.CurrentCell = item.Cells[8];
                            string emailId = await EmailExtractor.GetEmailAsync(item.Cells[7].Value.ToString());
                            allData[_counter].Email = emailId;
                            item.Cells[8].Value = emailId;
                        }
                        _counter++;
                        label1.Text = _counter.ToString() + " / " + dataGridView1.Rows.Count.ToString();
                    }
                }
                
                
            }
            ChangeCampStatus(CampaignStatusEnum.Finish);
        }

        private void materialButton2_Click(object sender, EventArgs e)
        {
            isManuallyStopped = true;
            isStop = true;
            if (b != null)
            {
                b.stop();
                b.Close();
            }
            isGebaEamilIEnd = true;
        }
        public void b_Closed()
        {
            isStop = true;
            ChangeInitStatus(InitStatusEnum.Stopped);
            ChangeCampStatus(CampaignStatusEnum.Stopped);
        }

        private void materialButton4_Click(object sender, EventArgs e)
        {
            String FolderPath = Config.GetTempFolderPath();
            String file = Path.Combine(FolderPath, "GMapData" + Guid.NewGuid().ToString() + ".json");
            string NewFileName = file.ToString();
            string json = JsonConvert.SerializeObject(allData.ToArray(), Formatting.Indented);
            File.WriteAllText(NewFileName, json);

            savesampleExceldialog.FileName = "GMapData.json";
            savesampleExceldialog.Filter = "JSON file (*.json)|*.json";
            if (savesampleExceldialog.ShowDialog() == DialogResult.OK)
            {
                File.Copy(NewFileName, savesampleExceldialog.FileName.EndsWith(".json") ? savesampleExceldialog.FileName : savesampleExceldialog.FileName + ".json", true);
                Utils.showAlert(Strings.Filedownloadedsuccessfully, Alerts.Alert.enmType.Success);
            }
        }

        private string ResolveTemplateXlsx(string fileName)
        {
            try
            {
                string[] cands = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), fileName),
                    fileName
                };
                foreach (string p in cands) { try { if (!string.IsNullOrEmpty(p) && File.Exists(p)) return p; } catch (Exception) { } }
            }
            catch (Exception) { }
            return fileName;
        }

        private string PrepareExportFile(string prefix)
        {
            // Export wajib jalan: pakai template bila ada, kalau tidak bikin workbook kosong.
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            String FolderPath = Config.GetTempFolderPath();
            try { if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath); } catch (Exception) { }
            string NewFileName = Path.Combine(FolderPath, prefix + Guid.NewGuid().ToString() + ".xlsx");
            string tpl = ResolveTemplateXlsx("ChatListTemplate.xlsx");
            if (File.Exists(tpl))
            {
                File.Copy(tpl, NewFileName, true);
                return NewFileName;
            }
            using (var pkg = new ExcelPackage())
            {
                pkg.Workbook.Worksheets.Add("Data");
                pkg.SaveAs(new FileInfo(NewFileName));
            }
            return NewFileName;
        }

        private void materialButton3_Click(object sender, EventArgs e)
        {
            try
            {
                if (allData == null || allData.Count() == 0)
                {
                    MessageBox.Show("Belum ada data untuk di-export.", Strings.Info, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string NewFileName = PrepareExportFile("GMapData");
            var newFile = new FileInfo(NewFileName);
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (ExcelPackage xlPackage = new ExcelPackage(newFile))
            {
                var ws = xlPackage.Workbook.Worksheets[0];

                ws.Cells[1, 1].Value = Strings.Name;
                ws.Cells[1, 2].Value = Strings.MobileNumber;
                ws.Cells[1, 3].Value = Strings.ReviewCount;
                ws.Cells[1, 4].Value = Strings.RatingCount;
                ws.Cells[1, 5].Value = Strings.Catagory;
                ws.Cells[1, 6].Value = Strings.Address;
                ws.Cells[1, 7].Value = Strings.Website;
                ws.Cells[1, 8].Value = Strings.EmailId;
                ws.Cells[1, 9].Value = Strings.latitude;
                ws.Cells[1, 10].Value = Strings.longitude;
                ws.Cells[1, 11].Value = Strings.State;
                ws.Cells[1, 12].Value = Strings.City;
                


                for (int i = 0; i < allData.Count(); i++)
                {
                    ws.Cells[i + 2, 1].Value = allData[i].Business_name;
                    ws.Cells[i + 2, 2].Value = allData[i].Phone;
                    ws.Cells[i + 2, 3].Value = allData[i].Review;
                    ws.Cells[i + 2, 4].Value = allData[i].Rating;
                    ws.Cells[i + 2, 5].Value = allData[i].Category;
                    ws.Cells[i + 2, 6].Value = allData[i].Address;
                    ws.Cells[i + 2, 7].Value = allData[i].Website_url;
                    ws.Cells[i + 2, 8].Value = allData[i].Email;
                    ws.Cells[i + 2, 9].Value = allData[i].Lat;
                    ws.Cells[i + 2, 10].Value = allData[i].Long;
                    ws.Cells[i + 2, 11].Value = allData[i].State;
                    ws.Cells[i + 2, 12].Value = allData[i].City;
                }
                xlPackage.Save();
            }


            savesampleExceldialog.FileName = "GMapData.xlsx";
            savesampleExceldialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
            if (savesampleExceldialog.ShowDialog() == DialogResult.OK)
            {
                File.Copy(NewFileName, savesampleExceldialog.FileName.EndsWith(".xlsx") ? savesampleExceldialog.FileName : savesampleExceldialog.FileName + ".xlsx", true);
                Utils.showAlert(Strings.Filedownloadedsuccessfully, Alerts.Alert.enmType.Success);
            }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export gagal: " + ex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void materialButton5_Click(object sender, EventArgs e)
        {
            try
            {
                if (allData == null || allData.Count() == 0)
                {
                    MessageBox.Show("Belum ada data untuk di-export.", Strings.Info, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string NewFileName = PrepareExportFile("GMapData_Numbers");
            var newFile = new FileInfo(NewFileName);
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (ExcelPackage xlPackage = new ExcelPackage(newFile))
            {
                var ws = xlPackage.Workbook.Worksheets[0];

                ws.Cells[1, 1].Value = Strings.MobileNumber;
                int ActualCounter = 0;
                for (int i = 0; i < allData.Count(); i++)
                {
                    if (allData[i].Phone != "")
                    {
                        ws.Cells[ActualCounter + 2, 1].Value = allData[i].Phone;
                        ActualCounter++;
                    }

                }
                xlPackage.Save();
            }


            savesampleExceldialog.FileName = "GMapData.xlsx";
            savesampleExceldialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
            if (savesampleExceldialog.ShowDialog() == DialogResult.OK)
            {
                File.Copy(NewFileName, savesampleExceldialog.FileName.EndsWith(".xlsx") ? savesampleExceldialog.FileName : savesampleExceldialog.FileName + ".xlsx", true);
                Utils.showAlert(Strings.Filedownloadedsuccessfully, Alerts.Alert.enmType.Success);
            }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export gagal: " + ex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void materialButton4_Click_1(object sender, EventArgs e)
        {
            List<GMapModel> ls = new List<GMapModel>();


            foreach (_DataItem_GoogleMap item in allData)
            {
                ls.Add(new GMapModel()
                {
                    mobilenumber = item.Phone
                });
            }

            this.gmExtractorForm.importNumbers(ls);
        }

       
    }
}
