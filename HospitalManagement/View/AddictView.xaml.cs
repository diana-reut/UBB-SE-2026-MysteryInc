using HospitalManagement.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;


namespace HospitalManagement.View
{
    internal sealed partial class AddictView : UserControl
    {
        public ViewModel.AddictViewModel ViewModel { get; }

        [DllImport("user32.dll")]
        public static extern bool MessageBeep(uint uType);

        public AddictView()
        {
            ViewModel = (App.Current as App).Services.GetService<ViewModel.AddictViewModel>();
            this.InitializeComponent();
        }

        private async void OnNotifyPoliceClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && sender is Button btn && btn.CommandParameter is int patientId)
            {
                string reportText = ViewModel.GetPoliceReportMessage(patientId);

               
                var dialog = new PoliceAlertDialog(reportText)
                {
                    XamlRoot = this.XamlRoot 
                };

                var result = await dialog.ShowAsync();

               
                if (result == ContentDialogResult.Primary)
                {
                    ViewModel.ConfirmPoliceAlert(patientId);
                }
            }
        }
    }
}
