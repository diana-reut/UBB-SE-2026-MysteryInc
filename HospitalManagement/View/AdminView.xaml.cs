using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Entity;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI;
namespace HospitalManagement.View
{
    internal sealed partial class AdminView : Window
    {
        // NOTE it is very wierd that the view model passes logic onto the view but we will keep it like that because I will go mad fixing it
        private readonly AdminViewModel _viewModel;

        internal AdminView(AdminViewModel adminViewModel)
        {
            InitializeComponent();

            // 1. Maximize Logic
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }


            // 3. Initialize ViewModel & Bindings
            _viewModel = adminViewModel ?? throw new ArgumentNullException(nameof(adminViewModel));
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
                                // FIXME : This needs to be moved in a service, tf are allergies doing here
                                // Load all available allergies from database
                                var allergiesList = new List<Allergy>();
                                const string AllergyQuery = "SELECT AllergyId, AllergyName, AllergyType, AllergyCategory FROM Allergy";
                                using (SqlDataReader reader = _dbContext.ExecuteQuery(AllergyQuery))
                                {
                                    while (reader.Read())
                                    {
                                        allergiesList.Add(new Allergy
                                        {
                                            AllergyId = (int)reader["AllergyId"],
                                            AllergyName = reader["AllergyName"]?.ToString(),
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
                                        // FIXME: make use of dependency injection
                                        var hRepo = new MedicalHistoryRepository(_dbContext);
                                        var patientRepo = new PatientRepository(_dbContext);
                                        var recordRepo = new MedicalRecordRepository(_dbContext);
                                        var patientService = new PatientService(patientRepo, hRepo, recordRepo);

                                        medicalHistoryDialog.MedicalHistory.PatientId = newPatientId;

                                        // CreateMedicalHistory will handle saving allergies from MedicalHistory.Allergies
                                        patientService.CreateMedicalHistory(newPatientId, medicalHistoryDialog.MedicalHistory, new List<Allergy>());

                                        var successAlert = new ContentDialog
                                        {
                                            Title = "Success",
                                            Content = "Medical history saved successfully!",
                                            CloseButtonText = "OK",
                                            XamlRoot = rootElement.XamlRoot,
                                        };
                                        ContentDialogResult contentDialogResult = await successAlert.ShowAsync();
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
                                        ContentDialogResult contentDialogResult = await errorAlert.ShowAsync();
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
                                _viewModel.ShowAlertAction?.Invoke("Patient not selected.");
                                return;
                            }

                            try
                            {

                                // FIXME: make use of dependency injection
                                // Create Services
                                var prRepo = new PrescriptionRepository(_dbContext);
                                var tRepo = new TransplantRepository(_dbContext);
                                var hRepo = new MedicalHistoryRepository(_dbContext);
                                var transplantService = new TransplantService(tRepo, (PatientRepository)pRepo, (MedicalRecordRepository)rRepo, new BloodCompatibilityService(pRepo, hRepo), hRepo);

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
                                            bool showedSuccessMessage = rootElement.DispatcherQueue.TryEnqueue(() => _viewModel.ShowAlertAction?.Invoke($"Successfully assigned organ from donor {deceasedPatient.FirstName} {deceasedPatient.LastName}."));
                                        }
                                        catch (Exception ex)
                                        {
                                            // Defer error alert display until dialog is fully closed
                                            bool showedErrorMessage = rootElement.DispatcherQueue.TryEnqueue(() => _viewModel.ShowAlertAction?.Invoke($"Error assigning organ: {ex.Message}"));
                                        }
                                    });

                                // Show the dialog
                                ContentDialogResult _ = await dialog.ShowAsync();
                            }
                            catch (Exception ex)
                            {
                                _viewModel.ShowAlertAction?.Invoke($"Error opening organ donor dialog: {ex.Message}");
                            }
                        };
                };

                UpdateView(_viewModel.CurrentView);
            }
        }

        private async void OpenAddPatientDialog(object sender, RoutedEventArgs e)
        {
            AddPatientDialog dialog = new AddPatientDialog();
            dialog.XamlRoot = this.Content.XamlRoot;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.NewPatient != null)
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
            // FIXME: Pass statistics via constructor
            var statisticsWindow = new StatisticsWindow(_dbContext);
            statisticsWindow.Activate();
        }

        private void OpenArchive_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
}
