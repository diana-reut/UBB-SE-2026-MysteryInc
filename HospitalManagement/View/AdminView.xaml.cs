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
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Database;
using Microsoft.UI.Windowing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HospitalManagement.View
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AdminView : Window
    {
        private AdminViewModel _viewModel;

        public AdminView()
        {
            this.InitializeComponent();

            (this.Content as FrameworkElement).Loaded += (s, e) =>
            {
                if ((this.Content as FrameworkElement).DataContext is AdminViewModel vm)
                {
                    vm.ShowAlertAction = async (message) =>
                    {
                        ContentDialog alert = new ContentDialog
                        {
                            Title = "System Message",
                            Content = message,
                            CloseButtonText = "OK",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await alert.ShowAsync();
                    };
                }
            };

            // 1. Full Screen / Maximize Logic (Same as PharmacistView)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            // 2. Manual Dependency Injection Chain
            var context = new HospitalDbContext();
            var patientRepo = new PatientRepository(context);
            var historyRepo = new MedicalHistoryRepository(context);
            var recordRepo = new MedicalRecordRepository(context);

            var patientService = new PatientService(patientRepo, historyRepo, recordRepo);

            // 3. Initialize ViewModel
            _viewModel = new AdminViewModel(patientService);

            // 4. Set DataContext on the root element (Grid/StackPanel)
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }
        }

        // This method will be called by your "Add Patient" button in XAML
        private async void OpenAddPatientDialog(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            AddPatientDialog dialog = new AddPatientDialog();

            // This tells the dialog to anchor itself to the current window
            dialog.XamlRoot = this.Content.XamlRoot;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.NewPatient != null)
            {
                // Pass the new patient to your ViewModel
                // Use the XAML root element's DataContext (usually the first Grid)
                if (this.Content is FrameworkElement rootElement &&
             rootElement.DataContext is AdminViewModel vm)
                {
                    vm.NewPatient = dialog.NewPatient;
                    vm.AddPatientCommand.Execute(null);
                }
            }
        }
    }

}
