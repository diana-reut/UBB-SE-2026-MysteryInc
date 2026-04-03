using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HospitalManagement.View
{
    public sealed partial class TransplantRequestView : Page
    {
        public HospitalManagement.ViewModel.TransplantRequestViewModel ViewModel { get; }
        private Window _parentWindow;

        public TransplantRequestView(int patientId, Window parentWindow)
        {
            ViewModel = new HospitalManagement.ViewModel.TransplantRequestViewModel(patientId);
            _parentWindow = parentWindow;
            this.InitializeComponent();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ErrorText.Visibility = Visibility.Collapsed;
                ViewModel.SubmitRequest();

                var dialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "The patient has been successfully added to the Organ Transplant Waitlist.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();

                _parentWindow.Close();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.Close();
        }
    }
}