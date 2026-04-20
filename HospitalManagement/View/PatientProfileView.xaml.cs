using HospitalManagement.Entity;
using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace HospitalManagement.View;

internal sealed partial class PatientProfileView : Page
{
    public PatientProfileViewModel ViewModel { get; }
    private readonly IServiceProvider _services;

    public PatientProfileView()
    {
        InitializeComponent();

        _services = (Application.Current as App)!.Services;
        ViewModel = _services.GetRequiredService<PatientProfileViewModel>();
        DataContext = ViewModel;

        // Initialize Callbacks
        ViewModel.ShowAlertAction = OnShowAlert;
        ViewModel.OpenFileAction = OnOpenFile;
        ViewModel.ShowPrescriptionAction = OnShowPrescriptionAsync;

        Loaded += Page_Loaded;
    }

    public void Initialize(int patientId)
    {
        ViewModel.LoadFullPatientProfile(patientId);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.CheckHighRiskStatus();
    }

    private async void ViewPrescription_ClickAsync(object sender, RoutedEventArgs e)
    {
        await ViewModel.ViewPrescriptionAsync();
    }

    private void ExportPDF_ClickAsync(object sender, RoutedEventArgs e)
    {
        ViewModel.ExportSelectedRecord();
    }

    private void ImportER_ClickAsync(object sender, RoutedEventArgs e)
    {
        ViewModel.ImportRecords(isER: true);
    }

    private void ImportStaff_ClickAsync(object sender, RoutedEventArgs e)
    {
        ViewModel.ImportRecords(isER: false);
    }

    private void RecordList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is MedicalRecord clickedRecord)
        {
            ViewModel.SelectedRecord = clickedRecord;
        }
    }

    // --- ViewModel Callback Implementations ---

    private void OnShowAlert(string title, string content)
    {
        ShowAlert(title, content);
    }

    private void OnOpenFile(string path)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private async Task OnShowPrescriptionAsync(int prescriptionId)
    {
        var prescriptionWindow = new Window { Title = "Prescription Details" };
        var prescriptionPage = _services.GetRequiredService<PrescriptionView>();

        prescriptionPage.ViewModel.ApplyFilterCommand(prescriptionId, null, null, null, null, null);

        prescriptionWindow.Content = prescriptionPage;
        prescriptionWindow.Activate();
    }

    private async void ShowAlert(string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot,
        };
        await dialog.ShowAsync();
    }
}