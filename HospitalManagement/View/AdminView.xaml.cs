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
    internal sealed partial class AdminView : Window, IDisposable
    {
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
            Closed += AdminView_Closed;

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
                                // Load all available allergies from database
                                var allergiesList = new List<Allergy>();
                                string allergyQuery = "SELECT AllergyId, AllergyName, AllergyType, AllergyCategory FROM Allergy";
                                using (SqlDataReader reader = _dbContext.ExecuteQuery(allergyQuery))
                                {
                                    while (reader.Read())
                                    {
                                        allergiesList.Add(new Allergy
                                        {
                                            AllergyId = (int)reader["AllergyId"],
                                            AllergyName = reader["AllergyName"].ToString(),
                                            AllergyType = reader["AllergyType"]?.ToString(),
                                            AllergyCategory = reader["AllergyCategory"]?.ToString()
                                        });
                                    }
                                }

                                var medicalHistoryDialog = new MedicalHistoryDialog();
                                medicalHistoryDialog.XamlRoot = rootElement.XamlRoot;
                                medicalHistoryDialog.Initialize(allergiesList);

                                var result = await medicalHistoryDialog.ShowAsync();

                                if (result == ContentDialogResult.Primary && medicalHistoryDialog.MedicalHistory != null)
                                {
                                    try
                                    {
                                        var hRepo = new MedicalHistoryRepository(_dbContext);
                                        var patientRepo = new PatientRepository(_dbContext);
                                        var recordRepo = new MedicalRecordRepository(_dbContext);
                                        var patientService = new PatientService(patientRepo, hRepo, recordRepo);

                                        medicalHistoryDialog.MedicalHistory.PatientId = newPatientId;

                                        // CreateMedicalHistory will handle saving allergies from MedicalHistory.Allergies
                                        patientService.CreateMedicalHistory(newPatientId, medicalHistoryDialog.MedicalHistory, new List<Allergy>());

                                        ContentDialog successAlert = new ContentDialog
                                        {
                                            Title = "Success",
                                            Content = "Medical history saved successfully!",
                                            CloseButtonText = "OK",
                                            XamlRoot = rootElement.XamlRoot
                                        };
                                        await successAlert.ShowAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        ContentDialog errorAlert = new ContentDialog
                                        {
                                            Title = "Error",
                                            Content = $"Error saving medical history: {ex.Message}",
                                            CloseButtonText = "OK",
                                            XamlRoot = rootElement.XamlRoot
                                        };
                                        await errorAlert.ShowAsync();
                                    }
                                }
                                else if (medicalHistoryDialog.WasSkipped)
                                {
                                    ContentDialog skipAlert = new ContentDialog
                                    {
                                        Title = "Skipped",
                                        Content = "You can add medical history later from the patient profile.",
                                        CloseButtonText = "OK",
                                        XamlRoot = rootElement.XamlRoot
                                    };
                                    await skipAlert.ShowAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"ERROR SHOWING MEDICAL HISTORY DIALOG: {ex}");
                            }
                        };

                    _viewModel.ConfirmAction = async (message, title) =>
                        {
                            ContentDialog confirmDialog = new ContentDialog
                            {
                                Title = title,
                                Content = message,
                                PrimaryButtonText = "Yes, Archive",
                                CloseButtonText = "Cancel",
                                DefaultButton = ContentDialogButton.Close,
                                XamlRoot = rootElement.XamlRoot
                            };

                            var result = await confirmDialog.ShowAsync();
                            return result == ContentDialogResult.Primary;
                        };

                    _viewModel.RequestDateAction = async (message, title) =>
                        {
                            DatePicker datePicker = new DatePicker
                            {
                                Header = message,
                                HorizontalAlignment = HorizontalAlignment.Stretch
                            };

                            ContentDialog dialog = new ContentDialog
                            {
                                Title = title,
                                Content = datePicker,
                                PrimaryButtonText = "Confirm",
                                CloseButtonText = "Cancel",
                                XamlRoot = rootElement.XamlRoot
                            };

                            var result = await dialog.ShowAsync();

                            if (result == ContentDialogResult.Primary)
                            {
                                return datePicker.Date.DateTime;
                            }

                            return null;
                        };

                    // Organ Donor Dialog Logic
                    _viewModel.OpenOrganDonorDialogAction = async (deceasedPatient) =>
                        {
                            if (deceasedPatient == null)
                            {
                                _viewModel.ShowAlertAction?.Invoke("Patient not selected.");
                                return;
                            }

                            try
                            {
                                // Create Services
                                var prRepo = new PrescriptionRepository(_dbContext);
                                var tRepo = new TransplantRepository(_dbContext);
                                var hRepo = new MedicalHistoryRepository(_dbContext);
                                var transplantService = new TransplantService(tRepo, (PatientRepository)pRepo, (MedicalRecordRepository)rRepo, new BloodCompatibilityService(pRepo, hRepo), hRepo);

                                // Create ViewModel
                                var organDonorViewModel = new OrganDonorViewModel(transplantService, pRepo, hRepo);
                                organDonorViewModel?.DeceasedPatient = deceasedPatient;

                                // Create Dialog
                                var dialog = new OrganDonorDialog();
                                dialog.XamlRoot = rootElement.XamlRoot;

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

        private void AdminView_Closed(object sender, WindowEventArgs args) 
        {
            Dispose();
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
            var statisticsWindow = new StatisticsWindow(_dbContext);
            statisticsWindow.Activate();
        }

        private void OpenArchive_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (this.Content is Microsoft.UI.Xaml.FrameworkElement fe && fe.DataContext is AdminViewModel vm)
            {
                vm.IsArchivedMode = true;
            }
            // Alternatively, if the Window itself holds the DataContext:
            else if (this is Microsoft.UI.Xaml.Window w && w.Content is Microsoft.UI.Xaml.FrameworkElement feWindow && feWindow.DataContext is AdminViewModel vmWin)
            {
                vmWin.IsArchivedMode = true;
            }
            
        }

        private void BackToActive_Click(object sender, RoutedEventArgs e)
        {
            if (this.Content is FrameworkElement root && root.DataContext is AdminViewModel vm)
            {
                vm.IsArchivedMode = false;
            }
        }

        private void OpenPage_Click(object sender, RoutedEventArgs e) => OpenStatisticsWindow();

        private void PatientListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (_viewModel?.SelectedPatient != null)
            {
                var patientView = new PatientView(_viewModel.SelectedPatient.Id, () => { });
                patientView.Activate();
            }
        }

        public void Dispose() => _dbContext?.Dispose();
    }
}