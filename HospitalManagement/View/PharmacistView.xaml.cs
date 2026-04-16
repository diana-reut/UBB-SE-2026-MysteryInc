using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using HospitalManagement.ViewModel;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.View;

internal sealed partial class PharmacistView : Window
{
    private readonly PharmacistViewModel _viewModel;

    public PharmacistView()
    {
        InitializeComponent();

        // Deschiderea Full Screen (Maximizatã)
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        // Ini?ializãm ViewModel ?i abonãm evenimentul
        _viewModel = new PharmacistViewModel();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Setãm DataContext-ul pentru bindings
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _viewModel;
        }

        // Încãrcãm View-ul default manual la pornire (dacã dorim sã ne asigurãm)
        UpdateView(_viewModel.CurrentView);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
            {
                var prescriptionView = new PrescriptionView();

                using var dbContext = new HospitalDbContext();
                var prescriptRepo = new PrescriptionRepository(dbContext);
                var medHistoryRepo = new MedicalHistoryRepository(dbContext);

                var pService = new PrescriptionService(prescriptRepo);
                var aService = new AddictDetectionService(prescriptRepo, medHistoryRepo);

                var prescriptionVM = new PrescriptionViewModel(pService, aService);

                prescriptionView.ViewModel = prescriptionVM;
                prescriptionView.DataContext = prescriptionVM;

                MainContentArea.Content = prescriptionView;
                break;
            }
            case "Addicts":
            {
                // 1. Instan?iem View-ul de Addicts (în locul The Placeholder-ului de tip TextBlock)
                var addictView = new AddictView();

                // 2. Re-generãm conexiunile la BD ca la Presciptions
                using var dbContextAddict = new HospitalDbContext();
                var prescriptRepoAddict = new PrescriptionRepository(dbContextAddict);
                var medHistoryRepoAddict = new MedicalHistoryRepository(dbContextAddict);

                // 3. Creãm Serviciul pentru adic?ii
                var addictDetectionService = new AddictDetectionService(prescriptRepoAddict, medHistoryRepoAddict);

                // 4. Instan?iem automat ViewModel-ul pt AddictView
                var addictViewModel = new AddictViewModel(addictDetectionService);

                // 5. Legãm model-ul de XAML
                addictView.ViewModel = addictViewModel;
                addictView.DataContext = addictViewModel;

                // 6. Afi?eazã efectiv controlul pe ecran
                MainContentArea.Content = addictView;
                break;
            }
            default:
                break;
        }
    }

    private void BackToHomeButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        mainWindow.Activate();
        Close();
    }
}
