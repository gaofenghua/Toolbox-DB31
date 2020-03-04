using System;
using DevExpress.Mvvm;

namespace Toolbox_DB31.Classes
{
    public enum MaintenanceType
    {
        REGULAR_MAINTENANCE = 0,
        SPECIAL_MAINTENANCE
    }

    public class MaintenanceMenuViewModel : ViewModelBase
	{
        public MaintenanceType m_Type { get; set; }
        public bool IsVideoMonitorEnabled { get; set; }
        public bool IsPerimeterAlarmEnabled { get; set; }
        public bool IsIntruderAlarmEnabled { get; set; }
        public bool IsBuildingIntercomEnabled { get; set; }
        public bool IsAccessManagementEnabled { get; set; }
        public bool IsEntranceControlEnabled { get; set; }
        public bool IsGuardTourEnabled { get; set; }
        public bool IsOthersEnabled { get; set; }
        public string MaintenanceRecords { get; set; }

        public MaintenanceMenuViewModel()
        {
            m_Type = MaintenanceType.REGULAR_MAINTENANCE;
            IsVideoMonitorEnabled = false;
            IsPerimeterAlarmEnabled = false;
            IsIntruderAlarmEnabled = false;
            IsBuildingIntercomEnabled = false;
            IsAccessManagementEnabled = false;
            IsEntranceControlEnabled = false;
            IsGuardTourEnabled = false;
            IsOthersEnabled = false;
            MaintenanceRecords = string.Empty;
        }

	}
}