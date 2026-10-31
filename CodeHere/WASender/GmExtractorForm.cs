
using MaterialSkin.Controls;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;
using WASender.Models;

namespace WASender
{
    public partial class GmExtractorForm : MyMaterialForm
    {
        MaterialSkin.MaterialSkinManager materialSkinManager;
        GeneralSettingsModel generalSettingsModel;
        OldClassic c;
        NewUI u;
        WaSenderForm waSenderForm;
        public GmExtractorForm(WaSenderForm _WASender)
        {
            this.waSenderForm = _WASender;
            InitializeComponent();
            this.Icon = Strings.AppIcon;
            generalSettingsModel = Config.GetSettings();
        }

        private void GmExtractorForm_Load(object sender, EventArgs e)
        {
            initLanguage();
            int _crowlMethod = generalSettingsModel.crowlingMethod == 0 ? 1 : generalSettingsModel.crowlingMethod;

            if (_crowlMethod == 1)
            {
                materialCard1.Controls.Clear();
                u = new NewUI(this);
                u.Dock = DockStyle.Fill;
                materialCard1.Controls.Add(u);
            }
            else
            {
                materialCard1.Controls.Clear();
                c = new OldClassic(this);
                c.Dock = DockStyle.Fill;
                materialCard1.Controls.Add(c);
            }
            
        }


        public void checkBrowserType()
        {
            try
            {
                GeneralSettingsModel settings = Config.GetSettings();
                if (settings.crowlingMethod == 1)
                {
                    materialCard1.Controls.Clear();
                    u = new NewUI(this);
                    u.Dock = DockStyle.Fill;
                    materialCard1.Controls.Add(u);
                }
                else
                {
                    materialCard1.Controls.Clear();
                    c = new OldClassic(this);
                    c.Dock = DockStyle.Fill;
                    materialCard1.Controls.Add(c);
                }
            }
            catch (Exception ex)
            {

            }
        }

      
        private void initLanguage()
        {
            this.Text = Strings.GoogleMapDataEExtractor;
            
            
        }



        private void materialCard1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            String GetGeneralSettingsFilePath = Config.GetGeneralSettingsFilePath();
            if (!File.Exists(GetGeneralSettingsFilePath))
            {
                File.Create(GetGeneralSettingsFilePath).Close();
            }

            string Json = JsonConvert.SerializeObject(generalSettingsModel, Formatting.Indented);
            File.WriteAllText(GetGeneralSettingsFilePath, Json);

            MaterialSnackBar SnackBarMessage = new MaterialSnackBar(Strings.LanguageIsSet, Strings.OK, true);
            SnackBarMessage.Show(this);
        }

        private void GmExtractorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (c != null)
                {
                    c.driver.Quit();
                }
            }
            catch (Exception ex)
            {
                
            }

            //List<string> allsites = new List<string>();
            //WebScrapper form = new WebScrapper(this.waSenderForm, allsites);
            //form.Show();
            if (this.u != null)
            {
                u.closeBroser();
            }
            waSenderForm.formReturn(true);
        }

        public void importNumbers(List<GMapModel> gMapModelList)
        {
            this.Close();
            this.waSenderForm.gmapDataReturn(gMapModelList);
        }

       
    }
}
