using System;
using DevExpress.Mvvm;

namespace Toolbox_DB31.Classes
{
	public class RepairMenuViewModel : ViewModelBase
	{
		public bool IsVideoMonitorEnabled = false;
		public bool IsPerimeterAlarmEnabled = false;
		public bool IsIntruderAlarmEnabled = false;
		public bool IsBuildingIntercomEnabled = false;
		public bool IsAccessManagementEnabled = false;
		public bool IsEntranceControlEnabled = false;
		public bool IsGuardTourEnabled = false;
		public bool IsOthersEnabled = false;
		public string RepairRecords = string.Empty;

		public enum RepairStatus
		{
			NOT_REPAIR = 0,
			PART_REPAIR,
			TOTAL_REPAIR
		}

	}
}