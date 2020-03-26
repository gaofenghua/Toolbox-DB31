using System;
using DevExpress.Mvvm;

namespace Toolbox_DB31.Classes
{
    public enum RepairStatus
    {
        NOT_REPAIR = 0,
        PART_REPAIR,
        TOTAL_REPAIR
    }

    public class RepairMenuViewModel : ViewModelBase
	{
		public bool IsVideoMonitorEnabled { get; set; }
        public bool IsPerimeterAlarmEnabled { get; set; }
        public bool IsIntruderAlarmEnabled { get; set; }
        public bool IsBuildingIntercomEnabled { get; set; }
        public bool IsAccessManagementEnabled { get; set; }
        public bool IsEntranceControlEnabled { get; set; }
        public bool IsGuardTourEnabled { get; set; }
        public bool IsOthersEnabled { get; set; }
        public string RepairRecords { get; set; }
        public RepairStatus m_Status { get; set; }

        public RepairMenuViewModel()
        {
            IsVideoMonitorEnabled = false;
            IsPerimeterAlarmEnabled = false;
            IsIntruderAlarmEnabled = false;
            IsBuildingIntercomEnabled = false;
            IsAccessManagementEnabled = false;
            IsEntranceControlEnabled = false;
            IsGuardTourEnabled = false;
            IsOthersEnabled = false;
            RepairRecords = string.Empty;
            m_Status = RepairStatus.NOT_REPAIR;
        }

    }
}