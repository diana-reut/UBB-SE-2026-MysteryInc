using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace HospitalManagement.View
{
    public sealed partial class PoliceAlertDialog : ContentDialog
    {
        public PoliceAlertDialog(string reportText)
        {
            this.InitializeComponent();
            ReportTextBlock.Text = reportText;
        }
    }

}

