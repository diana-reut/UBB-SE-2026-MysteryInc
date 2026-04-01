using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using HospitalManagement.ViewModel;
using Microsoft.UI.Windowing;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HospitalManagement.View
{
    /// <summary>
    /// Admin view window for managing patients and accessing statistics
    /// </summary>
    public sealed partial class AdminView : Window, IDisposable
    {
        private AdminViewModel _viewModel;
        private HospitalDbContext _dbContext;

        public AdminView()
        {
            this.InitializeComponent();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            _dbContext = new HospitalDbContext();
            var pRepo = new PatientRepository(_dbContext);
            var hRepo = new MedicalHistoryRepository(_dbContext);
            var rRepo = new MedicalRecordRepository(_dbContext);

            var service = new PatientService(pRepo, hRepo, rRepo);

            this._viewModel = new AdminViewModel(service);
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            this.Closed += AdminView_Closed;
            UpdateView(_viewModel.CurrentView);
        }

        private void AdminView_Closed(object sender, WindowEventArgs args)
        {
            Dispose();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdminViewModel.CurrentView))
            {
                UpdateView(_viewModel.CurrentView);
            }
        }

        private void UpdateView(string viewName)
        {
            switch (viewName)
            {
                case "AdminDashboard":
                    break;

                case "Statistics":
                    OpenStatisticsWindow();
                    break;
            }
        }

        /// <summary>
        /// Opens the Statistics window as a separate window
        /// </summary>
        private void OpenStatisticsWindow()
        {
            var statisticsWindow = new StatisticsWindow(_dbContext);
            statisticsWindow.Activate();
        }

        public Page GetCurrentPage()
        {
            return _viewModel.CurrentView switch
            {
                _ => new Page() // Return a default Page instance instead of null to avoid CS8603
            };
        }

        private void OpenPage_Click(object sender, RoutedEventArgs e)
        {
            OpenStatisticsWindow();
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
