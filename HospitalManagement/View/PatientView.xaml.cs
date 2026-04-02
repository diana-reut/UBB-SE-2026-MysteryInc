using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Database;
using HospitalManagement.Entity;

namespace HospitalManagement.View
{
    public sealed partial class PatientView : Window, IDisposable
    {
        private PatientViewModel _viewModel;
        private HospitalDbContext _dbContext;
        private Action _goBackCallback;

        public PatientView(Patient patient, Action goBackCallback)
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

            _goBackCallback = goBackCallback;

            // 2. Dependency Injection
            _dbContext = new HospitalDbContext();
            var pRepo = new PatientRepository(_dbContext);
            var hRepo = new MedicalHistoryRepository(_dbContext);
            var rRepo = new MedicalRecordRepository(_dbContext);
            var service = new PatientService(pRepo, hRepo, rRepo);

            // 3. Initialize ViewModel
            _viewModel = new PatientViewModel(service);
            _viewModel.GoBackAction = GoBack;
            _viewModel.SelectedPatient = patient;

            // 4. Set DataContext
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }
        }

        private void GoBack()
        {
            _goBackCallback?.Invoke();
            this.Close();
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            Dispose();
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
