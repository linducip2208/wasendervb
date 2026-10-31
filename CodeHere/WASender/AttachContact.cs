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
    public partial class AttachContact : MyMaterialPopOp
    {
        WaSenderForm waSenderForm;
        public AttachContact(WaSenderForm _waSenderForm)
        {
            InitializeComponent();
            this.waSenderForm = _waSenderForm;
            this.Icon = Strings.AppIcon;
        }

        private void AttachContact_Load(object sender, EventArgs e)
        {
            initLanguages();
        }

        private void initLanguages()
        {
            this.Text = Strings.AttachContact;
            materialTextBox21.Hint = Strings.ContactName;
            materialTextBox22.Hint = Strings.ContactNumberwithcountrycode;
            materialButton1.Text = Strings.Save;
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            if (materialTextBox21.Text == "" || materialTextBox22.Text == "")
            { 
                Utils.showError(Strings.Pleaseprovidemandatorydetails);
                return;
            }

            ContactMessageModel model = new ContactMessageModel();
            model.name = materialTextBox21.Text;
            model.number = materialTextBox22.Text.Replace("+","").Replace("-","").Replace(" ","");
            this.waSenderForm.receiveContactMessage(model);
            this.Close();

        }
    }
}
