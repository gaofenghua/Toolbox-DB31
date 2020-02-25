using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Toolbox_DB31.DB31_Adapter;

namespace Toolbox_DB31.Classes
{
    class User_ViewModel : ViewModelBase
    {
        public User_ViewModel()
        {
        }
        public string NameInput
        {
            get { return Global.g_User.Input_UserName; }
            set { Global.g_User.Input_UserName = value; }
        }
        public string PasswordInput
        {
            get { return Global.g_User.Input_Password; }
            set { Global.g_User.Input_Password = value; }
        }
    }
}
