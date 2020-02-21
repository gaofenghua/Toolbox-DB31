using System;
using DevExpress.Mvvm;

namespace Toolbox_DB31.Classes
{
	public class MaintenanceMenuViewModel : ViewModelBase
	{
		public bool IsVideoMonitorEnabled = false;
		public bool IsPerimeterAlarmEnabled = false;
		public bool IsIntruderAlarmEnabled = false;
		public bool IsBuildingIntercomEnabled = false;
		public bool IsAccessManagementEnabled = false;
		public bool IsEntranceControlEnabled = false;
		public bool IsGuardTourEnabled = false;
		public bool IsOthersEnabled = false;
		public string MaintenanceRecords = string.Empty;

		public enum MaintenanceStatus
		{
			REGULAR_MAINTENANCE = 0,
			SPECIAL_MAINTENANCE
		}
	}
}