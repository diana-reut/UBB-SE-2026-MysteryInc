using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Database;
using HospitalManagement.Entity;
using System.Threading.Tasks;

namespace HospitalManagement.View;

internal sealed partial class AdminView : Window, IDisposable
{
    private readonly AdminViewModel _viewModel;
    private readonly HospitalDbContext _dbContext;

    public AdminView()
    {
        InitializeComponent();

        // 1. Maximize Logic
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        // 2. Dependency Injection
        _dbContext = new HospitalDbContext();
        var pRepo = new PatientRepository(_dbContext);
        var hRepo = new MedicalHistoryRepository(_dbContext);
        var rRepo = new MedicalRecordRepository(_dbContext);
        var service = new PatientService(pRepo, hRepo, rRepo);

        // 3. Initialize ViewModel & Bindings
        _viewModel = new AdminViewModel(service);
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += AdminView_Closed;

        // 4. Set DataContext
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _viewModel;

            // YOUR CODE: Alert Logic
            rootElement.Loaded += (s, e) =>
            {
                if (rootElement.DataContext is AdminViewModel vm)
                {
                    // Alert Logic
                    vm.ShowAlertAction = async (message) =>
                    {
                        var alert = new ContentDialog
                        {
                            Title = "System Message",
                            Content = message,
                            CloseButtonText = "OK",
                            XamlRoot = rootElement.XamlRoot,
                        };
                        _ = await alert.ShowAsync();
                    };

                    // Medical History Dialog - Show directly on UI thread
                    vm.ShowMedicalHistoryAction = async (newPatientId) =>
                    {
                        try
                        {
                            // Load all available allergies from database
                            var allergiesList = new List<Allergy>();
                            const string AllergyQuery = "SELECT AllergyId, AllergyName, AllergyType, AllergyCategory FROM Allergy";
                            await using (SqlDataReader reader = _dbContext.ExecuteQuery(AllergyQuery))
                            {
                                while (await reader.ReadAsync())
                                {
                                    allergiesList.Add(new Allergy
                                    {
                                        AllergyId = (int)reader["AllergyId"],
                                        AllergyName = reader["AllergyName"].ToString() ?? "",
                                        AllergyType = reader["AllergyType"]?.ToString(),
                                        AllergyCategory = reader["AllergyCategory"]?.ToString(),
                                    });
                                }
                            }

                            var medicalHistoryDialog = new MedicalHistoryDialog
                            {
                                XamlRoot = rootElement.XamlRoot,
                            };
                            medicalHistoryDialog.Initialize(allergiesList);

                            ContentDialogResult result = await medicalHistoryDialog.ShowAsync();

                            if (result == ContentDialogResult.Primary && medicalHistoryDialog.MedicalHistory is not null)
                            {
                                try
                                {
                                    var hRepo = new MedicalHistoryRepository(_dbContext);
                                    var patientRepo = new PatientRepository(_dbContext);
                                    var recordRepo = new MedicalRecordRepository(_dbContext);
                                    var patientService = new PatientService(patientRepo, hRepo, recordRepo);

                                    medicalHistoryDialog.MedicalHistory.PatientId = newPatientId;

                                    // CreateMedicalHistory will handle saving allergies from MedicalHistory.Allergies
                                    patientService.CreateMedicalHistory(newPatientId, medicalHistoryDialog.MedicalHistory, []);

                                    var successAlert = new ContentDialog
                                    {
                                        Title = "Success",
                                        Content = "Medical history saved successfully!",
                                        CloseButtonText = "OK",
                                        XamlRoot = rootElement.XamlRoot,
                                    };
                                    _ = await successAlert.ShowAsync();
                                }
                                catch (Exception ex)
                                {
                                    var errorAlert = new ContentDialog
                                    {
                                        Title = "Error",
                                        Content = $"Error saving medical history: {ex.Message}",
                                        CloseButtonText = "OK",
                                        XamlRoot = rootElement.XamlRoot,
                                    };
                                    _ = await errorAlert.ShowAsync();
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
                                _ = await skipAlert.ShowAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"ERROR SHOWING MEDICAL HISTORY DIALOG: {ex}");
                        }
                    };

                    vm.ConfirmAction = async (message, title) =>
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

                    vm.RequestDateAction = async (message, title) =>
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
                    vm.OpenOrganDonorDialogAction = async (deceasedPatient) =>
                    {
                        if (deceasedPatient is null)
                        {
                            vm.ShowAlertAction?.Invoke("Patient not selected.");
                            return;
                        }

                        try
                        {
                            // Create Services
                            var prRepo = new PrescriptionRepository(_dbContext);
                            var tRepo = new TransplantRepository(_dbContext);
                            var hRepo = new MedicalHistoryRepository(_dbContext);
                            var transplantService = new TransplantService(tRepo, pRepo, rRepo, new BloodCompatibilityService(pRepo, hRepo), hRepo);

                            // Create ViewModel
                            var organDonorViewModel = new OrganDonorViewModel(transplantService, pRepo, hRepo)
                            {
                                DeceasedPatient = deceasedPatient,
                            };

                            // Create Dialog
                            var dialog = new OrganDonorDialog
                            {
                                XamlRoot = rootElement.XamlRoot,
                            };

                            // Initialize with confirmation callback
                            dialog.Initialize(
                                organDonorViewModel,
                                (transplantId, donorId, score) =>
                                {
                                    try
                                    {
                                        // Perform the assignment
                                        transplantService.AssignDonor(transplantId, donorId, score);

                                        // Defer alert display until dialog is fully closed
                                        _ = rootElement.DispatcherQueue.TryEnqueue(() => vm.ShowAlertAction?.Invoke($"Successfully assigned organ from donor {deceasedPatient.FirstName} {deceasedPatient.LastName}."));
                                    }
                                    catch (Exception ex)
                                    {
                                        // Defer error alert display until dialog is fully closed
                                        _ = rootElement.DispatcherQueue.TryEnqueue(() => vm.ShowAlertAction?.Invoke($"Error assigning organ: {ex.Message}"));
                                    }
                                });

                            // Show the dialog
                            _ = await dialog.ShowAsync();
                        }
                        catch (Exception ex)
                        {
                            vm.ShowAlertAction?.Invoke($"Error opening organ donor dialog: {ex.Message}");
                        }
                    };
                }
            };

            UpdateView(_viewModel.CurrentView);
        }
    }

    // YOUR CODE: Add Patient Dialog Logic
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


    // COLLEAGUE'S CODE: Navigation & Statistics Logic
    private void AdminView_Closed(object sender, WindowEventArgs args)
    {
        Dispose();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
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
        using var statisticsWindow = new StatisticsWindow(_dbContext);
        statisticsWindow.Activate();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
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
            using var patientView = new PatientView(_viewModel.SelectedPatient.Id, () => { });
            patientView.Activate();
        }
    }
}


