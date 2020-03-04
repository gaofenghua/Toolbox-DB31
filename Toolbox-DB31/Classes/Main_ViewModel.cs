using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm;

namespace Toolbox_DB31.Classes
{
    public class Main_ViewModel : ViewModelBase
    {
        //private string _BottemLabel = "Label_Summary——ViewModel";
        //public string BottomLabel { get { return _BottemLabel; } set { _BottemLabel = value; RaisePropertyChanged("BottomLabel"); ; } }

        private string _LabelStatus = "Label_Summary——ViewModel";
        public string LabelStatus { get { return _LabelStatus; } set { _LabelStatus = value; RaisePropertyChanged("LabelStatus"); ; } }

        private string _LabelMessage = "Label_Summary——ViewModel";
        public string LabelMessage { get { return _LabelMessage; } set { _LabelMessage = value; RaisePropertyChanged("LabelMessage"); ; } }
    }
}
