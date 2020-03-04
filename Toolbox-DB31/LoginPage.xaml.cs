using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Toolbox_DB31
{
    /// <summary>
    /// LoginPage.xaml 的交互逻辑
    /// </summary>
    public partial class LoginPage : Page
    {
        public event Action<object, string> Event_Login_Finished;

        public LoginPage()
        {
            InitializeComponent();
        }

        private void SimpleButton_Click_Inspector(object sender, RoutedEventArgs e)
        {
            Global.g_User.Input_Department = DB31_Adapter.DB31_User.Enum_Department.Inspector;

            Invoke_PasswordWindow();
        }

        private void SimpleButton_Click_Operator(object sender, RoutedEventArgs e)
        {
            Global.g_User.Input_Department = DB31_Adapter.DB31_User.Enum_Department.Operator;

            Invoke_PasswordWindow();
        }

        private void SimpleButton_Click_Maintainer(object sender, RoutedEventArgs e)
        {
            Global.g_User.Input_Department = DB31_Adapter.DB31_User.Enum_Department.Maintainer;

            Invoke_PasswordWindow();
        }

        private void Invoke_PasswordWindow()
        {
            PasswordWindow thePassword = new PasswordWindow();
            thePassword.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            thePassword.Owner = Window.GetWindow(this);
            bool? bSuccess = thePassword.ShowDialog();

            if (bSuccess.GetValueOrDefault())
            {
                if (Event_Login_Finished != null)
                {
                    Event_Login_Finished(this, "s");
                }
            }
        }
    }
}
