using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;


namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for PasswordWindow.xaml
    /// </summary>
    public partial class PasswordWindow : ThemedWindow
    {
 
        public PasswordWindow()
        {
            InitializeComponent();
 
            //DataContext = this;

            myLabel.Visibility= Visibility.Hidden;
        }

        private void SimpleButton_Click_OK(object sender, RoutedEventArgs e)
        {
            bool ret = Global.g_User.Verify();

            if(ret == true)
            {
                this.DialogResult = true;
            }
            else
            {
                myLabel.Visibility = Visibility.Visible;
            }

        }

        private void SimpleButton_Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
