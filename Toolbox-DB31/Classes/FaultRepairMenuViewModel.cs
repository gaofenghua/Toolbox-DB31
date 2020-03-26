using System;
using DevExpress.Mvvm;

namespace Toolbox_DB31.Classes
{
    public class FaultRepairMenuViewModel
    {
        public bool IsFrontImageLossEnabled { get; set; }
        public bool IsFrontImageAbnormalityEnabled { get; set; }
        public bool IsFrontTransmissionFailureEnabled { get; set; }
        public bool IsFrontOtherFailureEnabled { get; set; }
        public bool IsMatrixVideoFailureEnabled { get; set; }
        public bool IsMatrixOperationFailureEnabled { get; set; }
        public bool IsMatrixCommunicationFailureEnabled { get; set; }
        public bool IsMatrixOtherFailureEnabled { get; set; }
        public bool IsRecorderMonitoringFailureEnabled { get; set; }
        public bool IsRecorderRecordingFailureEnabled { get; set; }
        public bool IsRecorderPlaybackFailureEnabled { get; set; }
        public bool IsRecorderOtherFailureEnabled { get; set; }
        public string VideoMonitorRecords { get; set; }

        public FaultRepairMenuViewModel()
        {
            IsFrontImageLossEnabled = false;
            IsFrontImageAbnormalityEnabled = false;
            IsFrontTransmissionFailureEnabled = false;
            IsFrontOtherFailureEnabled = false;
            IsMatrixVideoFailureEnabled = false;
            IsMatrixOperationFailureEnabled = false;
            IsMatrixCommunicationFailureEnabled = false;
            IsMatrixOtherFailureEnabled = false;
            IsRecorderMonitoringFailureEnabled = false;
            IsRecorderRecordingFailureEnabled = false;
            IsRecorderPlaybackFailureEnabled = false;
            IsRecorderOtherFailureEnabled = false;
            VideoMonitorRecords = string.Empty;
        }
    }
}
