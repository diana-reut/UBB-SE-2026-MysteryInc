using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Database;
using Microsoft.UI.Xaml.Markup;
using CommunityToolkit.WinUI;

namespace HospitalManagement.View
{
    public sealed partial class AdminView : Window, IDisposable
    {
        private AdminViewModel _viewModel;
        private HospitalDbContext _dbContext;

        public AdminView()
        {
            this.InitializeComponent();

            // 1. Maximize Logic
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
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
            this.Closed += AdminView_Closed;

            // 4. Set DataContext
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;

                // YOUR CODE: Alert Logic
                rootElement.Loaded += (s, e) =>
                {
                    if (rootElement.DataContext is AdminViewModel vm)
                    {
                        vm.ShowAlertAction = async (message) =>
                        {
                            ContentDialog alert = new ContentDialog
                            {
                                Title = "System Message",
                                Content = message,
                                CloseButtonText = "OK",
                                XamlRoot = rootElement.XamlRoot
                            };
                            await alert.ShowAsync();
                        };

                        vm.ConfirmAction = async (message, title) =>
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

                        vm.RequestDateAction = async (message, title) =>
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
                        vm.OpenOrganDonorDialogAction = async (deceasedPatient) =>
                        {
                            if (deceasedPatient == null)
                            {
                                vm.ShowAlertAction?.Invoke("Patient not selected.");
                                return;
                            }

                            try
                            {
                                // Create Services
                                var prRepo = new PrescriptionRepository(_dbContext);
                                var tRepo = new TransplantRepository(_dbContext);
                                var transplantService = new TransplantService(tRepo, pRepo, rRepo, new BloodCompatibilityService(pRepo));

                                // Create ViewModel
                                var organDonorViewModel = new OrganDonorViewModel(transplantService);
                                organDonorViewModel.DeceasedPatient = deceasedPatient;

                                // Create Dialog
                                var dialog = new OrganDonorDialog();
                                dialog.XamlRoot = rootElement.XamlRoot;

                                // Initialize with confirmation callback
                                dialog.Initialize(organDonorViewModel, (transplantId, donorId, score) =>
                                {
                                    try
                                    {
                                        // Perform the assignment
                                        transplantService.AssignDonor(transplantId, donorId, score);
                                        vm.ShowAlertAction?.Invoke($"Successfully assigned organ from donor {deceasedPatient.FirstName} {deceasedPatient.LastName}.");
                                    }
                                    catch (Exception ex)
                                    {
                                        vm.ShowAlertAction?.Invoke($"Error assigning organ: {ex.Message}");
                                    }
                                });

                                // Show the dialog
                                await dialog.ShowAsync();
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

        // COLLEAGUE'S CODE: Navigation & Statistics Logic
        private void AdminView_Closed(object sender, WindowEventArgs args) => Dispose();

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
            var statisticsWindow = new StatisticsWindow(_dbContext);
            statisticsWindow.Activate();
        }

        private void OpenPage_Click(object sender, RoutedEventArgs e) => OpenStatisticsWindow();

        private void PatientListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (_viewModel?.SelectedPatient != null)
            {
                var patientView = new PatientView(_viewModel.SelectedPatient, () => { });
                patientView.Activate();
            }
        }

        public void Dispose() => _dbContext?.Dispose();
    }
}