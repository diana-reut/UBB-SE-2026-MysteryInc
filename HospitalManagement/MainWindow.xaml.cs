using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.View;
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
                else if (_viewModel.CurrentView?.ToString() == "AdminDashboard")
                {

                    var adminWindow = new View.AdminView();

                    // 1. Get the ViewModel from the window we just created
                    if (adminWindow.Content is FrameworkElement root && root.DataContext is AdminViewModel adminVM)
                    {
                        // 2. Inject the "Close & Re-open" logic into the Command
                        adminVM.NavigateToHomeCommand = new RelayCommand(() =>
                        {
                            var roleSelection = new MainWindow();
                            roleSelection.Activate();
                            adminWindow.Close();
                        });

                        // 3. Force the UI to refresh its binding (Just in case)
                        root.DataContext = null;
                        root.DataContext = adminVM;
                    }

                    adminWindow.Activate();
                    this.Close();
                }

            }
        }
            
    }
}
