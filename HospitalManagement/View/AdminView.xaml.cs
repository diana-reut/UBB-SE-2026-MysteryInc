using HospitalManagement.Entity;
using HospitalManagement.Service;
using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace HospitalManagement.View;

internal sealed partial class AdminView : Window
{
    // NOTE it is very wierd that the view model passes logic onto the view but we will keep it like that because I will go mad fixing it
    private readonly AdminViewModel _viewModel;
    private readonly IPatientService _patientService;
    private readonly ITransplantService _transplantService;
    private StatisticsWindow? _statisticsWindow;

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

        // 2. Dependency Injection
        _patientService = (Application.Current as App)!.Services.GetRequiredService<IPatientService>();
        _transplantService = (Application.Current as App)!.Services.GetRequiredService<ITransplantService>();

        // 3. Initialize ViewModel & Bindings
        _viewModel = (Application.Current as App)!.Services.GetRequiredService<AdminViewModel>();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // 4. Set DataContext
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _viewModel;
            rootElement.Loaded += (s, e) =>
            {
                // Alert Logic
                _viewModel.ShowAlertAction = async (message) =>
                    {
                        var alert = new ContentDialog
                        {
                            Title = "System Message",
                            Content = message,
                            CloseButtonText = "OK",
                            XamlRoot = rootElement.XamlRoot,
                        };
                        ContentDialogResult _ = await alert?.ShowAsync();
                    };

                // Medical History Dialog - Show directly on UI thread
                _viewModel.ShowMedicalHistoryAction = async (newPatientId) =>
                    {
                        try
                        {
                            var medicalHistoryDialog = new MedicalHistoryDialog
                            {
                                XamlRoot = rootElement.XamlRoot,
                            };
                            medicalHistoryDialog.Initialize();

                            ContentDialogResult result = await medicalHistoryDialog.ShowAsync();

                            if (result == ContentDialogResult.Primary && medicalHistoryDialog.MedicalHistory is not null)
                            {
                                try
                                {
                                    medicalHistoryDialog.MedicalHistory.PatientId = newPatientId;
                                    _patientService.CreateMedicalHistory(newPatientId, medicalHistoryDialog.MedicalHistory);

                                    _viewModel.ShowAlertAction?.Invoke("Medical history saved successfully!");
                                }
                                catch (Exception ex)
                                {
                                    _viewModel.ShowAlertAction?.Invoke($"Error saving medical history: {ex.Message}");
                                }
                            }
                            else if (medicalHistoryDialog.WasSkipped)
                            {
                                var skipAlert = new ContentDialog
                                {
                                    Title = "Skipped",
                                    Content = "You can add medical history later from the patient profile.",
                                    CloseButtonText = "OK",
                                    XamlRoot = rootElement.XamlRoot,
                                };
                                ContentDialogResult contentDialogResult = await skipAlert.ShowAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR SHOWING MEDICAL HISTORY DIALOG: {ex}");
                        }
                    };

                _viewModel.ConfirmAction = async (message, title) =>
                    {
                        var confirmDialog = new ContentDialog
                        {
                            Title = title,
                            Content = message,
                            PrimaryButtonText = "Yes, Archive",
                            CloseButtonText = "Cancel",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = rootElement.XamlRoot,
                        };

                        ContentDialogResult result = await confirmDialog.ShowAsync();
                        return result == ContentDialogResult.Primary;
                    };

                _viewModel.RequestDateAction = async (message, title) =>
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
                            XamlRoot = rootElement.XamlRoot,
                        };

                        ContentDialogResult result = await dialog.ShowAsync();

                        if (result == ContentDialogResult.Primary)
                        {
                            return datePicker.Date.DateTime;
                        }

                        return null;
                    };

                // Organ Donor Dialog Logic
                _viewModel.OpenOrganDonorDialogAction = async (deceasedPatient) =>
                    {
                        if (deceasedPatient is null)
                        {
                            return;
                        }

                        OrganDonorDialog dialog = (Application.Current as App)!.Services.GetRequiredService<OrganDonorDialog>();
                        dialog.XamlRoot = rootElement.XamlRoot;


                        dialog.Initialize(
                            (transplantId, donorId, score) =>
                            {
                                try
                                {
                                    _transplantService.AssignDonor(transplantId, donorId, score);
                                    bool showedSuccessMessage = rootElement.DispatcherQueue.TryEnqueue(() => _viewModel.ShowAlertAction?.Invoke($"Successfully assigned organ from donor {deceasedPatient.FirstName} {deceasedPatient.LastName}."));
                                }
                                catch (Exception ex)
                                {
                                    bool showedErrorMessage = rootElement.DispatcherQueue.TryEnqueue(() => _viewModel.ShowAlertAction?.Invoke($"Error assigning organ: {ex.Message}"));
                                }
                            });
                        ContentDialogResult _ = await dialog.ShowAsync();
                    };
            };

            UpdateView(_viewModel.CurrentView);
        }
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

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs? e)
    {
        if (e is null)
        {
            return;
        }

        if (e.PropertyName == nameof(AdminViewModel.CurrentView))
        {
            UpdateView(_viewModel.CurrentView);
        }
    }

    private void UpdateView(string viewName)
    {
        if (viewName == "Statistics")
        {
            OpenStatisticsWindow();
        }
    }

    private void OpenStatisticsWindow()
    {
        _statisticsWindow = (Application.Current as App)!.Services.GetRequiredService<StatisticsWindow>();
        _statisticsWindow.Activate();
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
        OpenStatisticsWindow();
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
}
