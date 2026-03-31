using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.View
{
    public sealed partial class PharmacistView : Window
    {
        private PharmacistViewModel _viewModel;

        public PharmacistView()
        {
            this.InitializeComponent();

            // Deschiderea Full Screen (Maximizatã)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            // Ini?ializãm ViewModel ?i abonãm evenimentul
            _viewModel = new PharmacistViewModel();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Setãm DataContext-ul pentru bindings
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }

            // Încãrcãm View-ul default manual la pornire (dacã dorim sã ne asigurãm)
            UpdateView(_viewModel.CurrentView);
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PharmacistViewModel.CurrentView))
            {
                UpdateView(_viewModel.CurrentView);
            }
        }

        private void UpdateView(string viewName)
        {
            switch (viewName)
            {
                case "Prescriptions":
                    var prescriptionView = new PrescriptionView();
                    
                    // 1. Instan?iem DbContext-ul folosind namespace-ul corect
                    var dbContext = new HospitalDbContext();
                    
                    // 2. Instan?iem Repository-urile necesare
                    var prescriptRepo = new PrescriptionRepository(dbContext);
                    var medHistoryRepo = new MedicalHistoryRepository(dbContext);
                    
                    // 3. Instan?iem Serviciile
                    var pService = new PrescriptionService(prescriptRepo); // Presupunând cã ia doar repository-ul
                    var aService = new AddictDetectionService(prescriptRepo, medHistoryRepo);
                    
                    // 4. Instan?iem ViewModel-ul
                    var prescriptionVM = new PrescriptionViewModel(pService, aService);
                    
                    prescriptionView.ViewModel = prescriptionVM;
                    prescriptionView.DataContext = prescriptionVM;

                    MainContentArea.Content = prescriptionView;
                    break;

                case "Addicts":
                    // MainContentArea.Content = new AddictView();
                    MainContentArea.Content = new TextBlock 
                    { 
                        Text = "Addict Monitor View Coming Soon...", 
                        FontSize = 24, 
                        HorizontalAlignment = HorizontalAlignment.Center, 
                        VerticalAlignment = VerticalAlignment.Center 
                    };
                    break;
            }
        }

        private void BackToHomeButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Activate();
            this.Close();
        }
    }
}
