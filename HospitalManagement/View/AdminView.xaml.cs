using HospitalManagement.Entity;
using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace HospitalManagement.View;

internal sealed partial class AdminView : Window
{
    private readonly AdminViewModel _viewModel;
    private readonly StatisticsView _statisticsControl;

    private void SetupWindow()
    {
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    public AdminView()
    {
        InitializeComponent();
        SetupWindow();

        _statisticsControl = (Application.Current as App)!.Services.GetRequiredService<StatisticsView>();
        StatisticsContainer.Child = _statisticsControl;

        _viewModel = (Application.Current as App)!.Services.GetRequiredService<AdminViewModel>();

        // 4. Set DataContext
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _viewModel;

            _viewModel.ShowAlertAction = ShowAlertDialogAsync;
            _viewModel.ShowMedicalHistoryAction = ShowMedicalHistoryDialogAsync;
            _viewModel.ConfirmAction = ShowConfirmDialogAsync;
            _viewModel.RequestDateAction = ShowRequestDateDialogAsync;
            _viewModel.OpenOrganDonorDialogAction = ShowOpenOrganDonorDialogAsync;
        }
    }

    private async Task ShowAlertDialogAsync(string message)
    {
        var alert = new ContentDialog
        {
            Title = "System Message",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        ContentDialogResult _ = await alert.ShowAsync();
    }

    private async Task ShowMedicalHistoryDialogAsync(int newPatientId)
    {
        try
        {
            var medicalHistoryDialog = new MedicalHistoryDialog
            {
                XamlRoot = Content.XamlRoot,
            };
            medicalHistoryDialog.Initialize();

            ContentDialogResult result = await medicalHistoryDialog.ShowAsync();
            await _viewModel.ProcessMedicalHistoryResultAsync(
                newPatientId,
                medicalHistoryDialog.MedicalHistory,
                medicalHistoryDialog.WasSkipped
        );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR SHOWING MEDICAL HISTORY DIALOG: {ex}");
        }
    }

    private async Task<bool> ShowConfirmDialogAsync(string message, string title)
    {
        var confirmDialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Confirm",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };
        ContentDialogResult result = await confirmDialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async Task<DateTime?> ShowRequestDateDialogAsync(string message, string title)
    {
        var datePicker = new DatePicker
        {
            Header = message,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = datePicker,
            PrimaryButtonText = "Confirm",
            CloseButtonText = "Cancel",
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            return datePicker.Date.DateTime;
        }

        return null;
    }

    private async Task ShowOpenOrganDonorDialogAsync(Patient deceasedPatient)
    {
        if (deceasedPatient is null)
        {
            return;
        }

        OrganDonorDialog dialog = (Application.Current as App)!.Services.GetRequiredService<OrganDonorDialog>();
        dialog.XamlRoot = Content.XamlRoot;

        string donorFullName = $"{deceasedPatient.FirstName} {deceasedPatient.LastName}";
        dialog.Initialize(
            deceasedPatient,
            async (transplantId, donorId, score) => await _viewModel.AssignOrganDonorAsync(transplantId, donorId, score, donorFullName)
        );
        ContentDialogResult _ = await dialog.ShowAsync();
    }

    private async void OpenAddPatientDialogAsync(object sender, RoutedEventArgs e)
    {
        var dialog = new AddPatientDialog
        {
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await _viewModel.AddPatientFlowAsync(dialog.NewPatient);
        }
    }

    private void PatientListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (_viewModel?.SelectedPatient is not null)
        {
            int patientId = _viewModel.SelectedPatient.Id;
            IServiceProvider scope = (Application.Current as App)!.Services;
            PatientView patientWindow = scope.GetRequiredService<PatientView>();

            patientWindow.Initialize(patientId, () => { });
            patientWindow.Activate();
        }
    }

    private void Home_Click(object sender, RoutedEventArgs e)
    {
        MainWindow mainWindow = (Application.Current as App)!.Services.GetRequiredService<MainWindow>();
        mainWindow.Activate();
        Close();
    }
}
