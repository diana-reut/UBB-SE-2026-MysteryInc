using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel; 

namespace HospitalManagement
{
    public sealed partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            this.InitializeComponent();
            //FULL SCREEN
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            _viewModel = new MainWindowViewModel();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentView))
            {
                if (_viewModel.CurrentView?.ToString() == "PharmacistDashboard")
                {
                    var pharmacistWindow = new View.PharmacistView(); 
                    pharmacistWindow.Activate();
                    this.Close(); 
                }
                //AICI VA CONECTATI VOI CU IF ELSE
            }
        }
    }
}
