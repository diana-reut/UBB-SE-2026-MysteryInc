using HospitalManagement.Entity;
using HospitalManagement.Service;
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
    private readonly IPatientService _patientService;
    private readonly ITransplantService _transplantService;
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

        _patientService = (Application.Current as App)!.Services.GetRequiredService<IPatientService>();
        _transplantService = (Application.Current as App)!.Services.GetRequiredService<ITransplantService>();
        _statisticsControl = (Application.Current as App)!.Services.GetRequiredService<StatisticsView>();
        StatisticsContainer.Child = _statisticsControl;

        _viewModel = (Application.Current as App)!.Services.GetRequiredService<AdminViewModel>();

        // 4. Set DataContext
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _viewModel;
            rootElement.Loaded += (s, e) =>
            {
                _viewModel.ShowAlertAction = ShowAlertDialogAsync;
                _viewModel.ShowMedicalHistoryAction = ShowMedicalHistoryDialogAsync;
                _viewModel.ConfirmAction = ShowConfirmDialogAsync;
                _viewModel.RequestDateAction = ShowRequestDateDialogAsync;
                _viewModel.OpenOrganDonorDialogAction = ShowOpenOrganDonorDialogAsync;
            };
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

            if (result == ContentDialogResult.Primary && medicalHistoryDialog.MedicalHistory is not null)
            {
                try
                {
                    medicalHistoryDialog.MedicalHistory.PatientId = newPatientId;
                    _patientService.CreateMedicalHistory(newPatientId, medicalHistoryDialog.MedicalHistory);

                    if (_viewModel.ShowAlertAction is not null)
                    {
                        await _viewModel.ShowAlertAction("Medical history saved successfully!");
                    }
                }
                catch (Exception ex)
                {
                    if (_viewModel.ShowAlertAction is not null)
                    {
                        await _viewModel.ShowAlertAction($"Error saving medical history: {ex.Message}");
                    }
                }
            }
            else if (medicalHistoryDialog.WasSkipped)
            {
                var skipAlert = new ContentDialog
                {
                    Title = "Skipped",
                    Content = "You can add medical history later from the patient profile.",
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot,
                };
                ContentDialogResult contentDialogResult = await skipAlert.ShowAsync();
            }
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


        dialog.Initialize(
            deceasedPatient,
            (transplantId, donorId, score) =>
            {
                try
                {
                    _transplantService.AssignDonor(transplantId, donorId, score);
                    bool showedSuccessMessage = Content.DispatcherQueue.TryEnqueue(() => _viewModel.ShowAlertAction?.Invoke($"Successfully assigned organ from donor {deceasedPatient.FirstName} {deceasedPatient.LastName}."));
                }
                catch (Exception ex)
                {
                    bool showedErrorMessage = Content.DispatcherQueue.TryEnqueue(() => _viewModel.ShowAlertAction?.Invoke($"Error assigning organ: {ex.Message}"));
                }
            });
        ContentDialogResult _ = await dialog.ShowAsync();
    }

    private async void OpenAddPatientDialogAsync(object sender, RoutedEventArgs e)
    {
        var dialog = new AddPatientDialog
        {
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.NewPatient is not null)
        {
            _viewModel.NewPatient = dialog.NewPatient;
            _viewModel.AddPatientCommand.Execute(null);
        }
    }


    private void ToggleStatisticsWindow()
    {
        StatisticsContainer.Visibility = StatisticsContainer.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OpenArchive_Click(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement fe && fe.DataContext is AdminViewModel vm)
        {
            vm.IsArchivedMode = true;
        }
        // Alternatively, if the Window itself holds the DataContext:
        else if (Content is FrameworkElement feWindow && feWindow.DataContext is AdminViewModel vmWin)
        {
            vmWin.IsArchivedMode = true;
        }
    }

    private void BackToActive_Click(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement root && root.DataContext is AdminViewModel vm)
        {
            vm.IsArchivedMode = false;
        }
    }

    private void OpenPage_Click(object sender, RoutedEventArgs e)
    {
        ToggleStatisticsWindow();
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
