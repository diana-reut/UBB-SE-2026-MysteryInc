using HospitalManagement.Entity;
using HospitalManagement.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.View;

internal sealed partial class PatientProfileView : Page
{
    private PatientProfileViewModel _viewModel;

    public PatientProfileView(PatientProfileViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = (Application.Current as App)!.Services.GetRequiredService<PatientProfileViewModel>();


        DataContext = _viewModel;

        _viewModel.ShowAlertAction = ShowAlertAsync;
        _viewModel.OpenFileAction = OpenFile;
        _viewModel.ShowPrescriptionAction = ShowPrescriptionAsync;

        Loaded += Page_Loaded;
    }

    public void Initialize(int patientId)
    {
        _viewModel.LoadFullPatientProfile(patientId);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.CheckHighRiskStatus();
    }

    private async void ViewPrescription_ClickAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.ViewPrescriptionAsync();
    }

    private void ExportPDF_ClickAsync(object sender, RoutedEventArgs e)
    {
        _viewModel.ExportSelectedRecord();
    }

    private void ImportER_ClickAsync(object sender, RoutedEventArgs e)
    {
        _viewModel.ImportRecords(isER: true);
    }

    private void ImportStaff_ClickAsync(object sender, RoutedEventArgs e)
    {
        _viewModel.ImportRecords(isER: false);
    }

    private void RecordList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is MedicalRecord clickedRecord)
        {
            _viewModel.SelectedRecord = clickedRecord;
        }
    }

    private void OpenFile(string path)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        };
        System.Diagnostics.Process.Start(psi);
    }

    private async Task ShowPrescriptionAsync(int prescriptionId)
    {
        var prescriptionWindow = new Window { Title = "Prescription Details" };
        var prescriptionPage = (Application.Current as App)!.Services.GetRequiredService<PrescriptionView>();
        prescriptionPage.ViewModel.ApplyFilterCommand(prescriptionId, null, null, null, null, null);
        prescriptionWindow.Content = prescriptionPage;
        prescriptionWindow.Activate();
    }

    private async void ShowAlertAsync(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot,
        };
        _ = await dialog.ShowAsync();
    }
}
