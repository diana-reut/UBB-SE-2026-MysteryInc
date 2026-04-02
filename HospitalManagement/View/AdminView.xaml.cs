using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Database;
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
                        vm.ConfirmAction = async (message, title) => // Added 'async' here
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

                            // This returns a bool, but because the method is 'async', 
                            // C# automatically wraps it in a Task<bool> for you!
                            return result == ContentDialogResult.Primary;
                        };
                    }
                };
            }

            UpdateView(_viewModel.CurrentView);
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

        public void Dispose() => _dbContext?.Dispose();
    }
}