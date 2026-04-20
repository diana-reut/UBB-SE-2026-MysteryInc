using HospitalManagement.Entity;
using HospitalManagement.ViewModel;
using HospitalManagement.Service;
using HospitalManagement.Integration.Export;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace HospitalManagement.View;

internal sealed partial class PatientProfileView : Page
{
    public PatientProfileViewModel ViewModel { get; }

    private readonly IPatientService _patientService;
    private readonly IImportService _importService;
    private readonly IExportService _exportService;
    private readonly IServiceProvider _services;

    public PatientProfileView(
        PatientProfileViewModel viewModel,
        IPatientService patientService,
        IImportService importService,
        IExportService exportService,
        IServiceProvider services)
    {
        InitializeComponent();

        ViewModel = viewModel;
        _patientService = patientService;
        _importService = importService;
        _exportService = exportService;
        _services = services;

        DataContext = ViewModel;
        Loaded += Page_LoadedAsync;
    }

    public void Initialize(int patientId)
    {
        ViewModel.LoadFullPatientProfile(patientId);
    }

    private async void Page_LoadedAsync(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentPatient is not null && _patientService.IsHighRiskPatient(ViewModel.CurrentPatient.Id))
        {
            var dialog = new ContentDialog
            {
                Title = "High Risk Patient Alert",
                Content = "Warning: This patient is flagged as High Risk (10+ ER visits recently).",
                CloseButtonText = "Acknowledge",
                XamlRoot = Content.XamlRoot,
            };
            await dialog.ShowAsync();
        }
    }

    private void RecordList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is MedicalRecord clickedRecord)
        {
            ViewModel.SelectedRecord = clickedRecord;
        }
    }

    private async void ViewPrescription_ClickAsync(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRecord is null)
        {
            return;
        }

        Prescription? actualPrescription = _patientService.GetPrescriptionByRecordId(ViewModel.SelectedRecord.Id);

        if (actualPrescription is not null)
        {
            var prescriptionWindow = new Window { Title = "Prescription Details" };

            PrescriptionView prescriptionPage = _services.GetRequiredService<PrescriptionView>();

            // Note: If your PrescriptionView doesn't have an Initialize method, 
            // you might need to set the DataContext or apply filters to its internal ViewModel here
            prescriptionPage.ViewModel.ApplyFilterCommand(actualPrescription.Id, null, null, null, null, null);

            prescriptionWindow.Content = prescriptionPage;
            prescriptionWindow.Activate();
        }
        else
        {
            ShowAlert("No Prescription", "This consultation does not have an associated prescription.");
        }
    }

    private async void ExportPDF_ClickAsync(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedRecord is null)
        {
            return;
        }

        try
        {
            // Just one line of code thanks to DI!
            string savedFilePath = _exportService.ExportRecordToPDF(ViewModel.SelectedRecord.Id);

            using (System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = savedFilePath,
                UseShellExecute = true,
            }))
            {
            }
        }
        catch (Exception ex)
        {
            ShowAlert("Export Failed", ex.Message);
        }
    }

    private async void ImportER_ClickAsync(object sender, RoutedEventArgs e) => await HandleImportAsync(isER: true);

    private async void ImportStaff_ClickAsync(object sender, RoutedEventArgs e) => await HandleImportAsync(isER: false);

    private async System.Threading.Tasks.Task HandleImportAsync(bool isER)
    {
        if (ViewModel.CurrentPatient is null)
        {
            return;
        }

        try
        {
            int patientId = ViewModel.CurrentPatient.Id;

            if (isER)
            {
                _importService.ImportFromER(patientId, 1);
            }
            else
            {
                _importService.ImportFromAppointment(patientId, 1);
            }

            ViewModel.LoadFullPatientProfile(patientId);
            ShowAlert("Import Successful", "Records imported correctly.");
        }
        catch (Exception ex)
        {
            ShowAlert("Import Failed", ex.Message);
        }
    }

    private async void ShowAlert(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}
