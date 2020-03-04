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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Toolbox_DB31.Classes;

namespace Toolbox_DB31
{
	/// <summary>
	/// Interaction logic for MaintenanceMenu.xaml
	/// </summary>
	public partial class MaintenanceMenu : Page
	{
        public MaintenanceMenu()
		{
			InitializeComponent();
            DataContext = this;
		}

    }
}
