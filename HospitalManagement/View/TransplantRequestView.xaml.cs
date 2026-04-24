using System;
using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HospitalManagement.View;

internal sealed partial class TransplantRequestView : Page
{
    public TransplantRequestViewModel ViewModel { get; }

    private readonly Window _parentWindow;

    public TransplantRequestView(int patientId, Window parentWindow)
    {
        Func<int, TransplantRequestViewModel> vmFactory = (Application.Current as App)!.Services.GetRequiredService<Func<int, TransplantRequestViewModel>>();
        ViewModel = vmFactory(patientId);
        _parentWindow = parentWindow;
        InitializeComponent();
    }

    private async void Submit_ClickAsync(object sender, RoutedEventArgs e)
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
                XamlRoot = Content.XamlRoot,
            };
            _ = await dialog.ShowAsync();

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
