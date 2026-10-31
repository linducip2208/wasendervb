using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;
using WASender.Models;


namespace WASender
{
    public partial class AddLocation : MyMaterialPopOp
    {
        WaSenderForm waSenderForm;

        public AddLocation(WaSenderForm _waSenderForm)
        {
            InitializeComponent();
            this.waSenderForm = _waSenderForm;
            this.Icon = Strings.AppIcon;
        }

        private void AddLocation_Load(object sender, EventArgs e)
        {
            initLanguages();
        }

        private void initLanguages()
        {
            this.Name = Strings.AtachLocation;
            materialTextBox21.Hint = Strings.Latitute + "*";
            materialTextBox22.Hint = Strings.Longitude + "*";
            materialTextBox23.Hint = Strings.NameofLocationoptional;
            materialTextBox24.Hint = Strings.Addressoptional;
            materialTextBox25.Hint = Strings.URLoptional;
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            if (materialTextBox21.Text == "" || materialTextBox22.Text == "")
            {
                Utils.showError(Strings.Pleaseprovidemandatorydetails);
                return;
            }

            LocationMessageModel locationMessageModel = new LocationMessageModel();
            locationMessageModel.lat = materialTextBox21.Text;
            locationMessageModel.lng = materialTextBox22.Text;
            locationMessageModel.name = materialTextBox23.Text;
            locationMessageModel.address = materialTextBox24.Text;
            locationMessageModel.url = materialTextBox25.Text;

            //this.newBroadcast.receiveLocationMessage(locationMessageModel);
            this.waSenderForm.RecievLocationData(locationMessageModel);
            this.Close();

        }
    }
}
