using Microsoft.UI.Xaml.Controls;

namespace HospitalManagement.View;

public sealed partial class PoliceAlertDialog : ContentDialog
{
    public PoliceAlertDialog(string reportText)
    {
        InitializeComponent();
        ReportTextBlock.Text = reportText;
    }
}

