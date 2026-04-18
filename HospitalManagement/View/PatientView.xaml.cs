using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using HospitalManagement.ViewModel;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Database;
using HospitalManagement.Integration.Export;

namespace HospitalManagement.View;

internal sealed partial class PatientView : Window, IDisposable
{
    private readonly PatientViewModel _viewModel;
    private readonly HospitalDbContext _dbContext;
    private readonly Action _goBackCallback;

    public PatientView(int patientId, Action goBackCallback)
    {
        InitializeComponent();

        // 1. Maximize Logic
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
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
        var prescRepo = new PrescriptionRepository(_dbContext);
        var tRepo = new TransplantRepository(_dbContext);
        var service = new PatientService(pRepo, hRepo, rRepo, prescRepo);
        var exportService = new ExportService(rRepo, prescRepo, pRepo, hRepo);
        var billingService = new BillingService(hRepo, rRepo, prescRepo, tRepo);

        // 3. Initialize ViewModel
        _viewModel = new PatientViewModel(service, exportService, billingService)
        {
            GoBackAction = GoBack,
            // 4. Set up Roulette Dialog handler
            OpenRouletteAction = async (basePrice, onComplete) =>
                {
                    var rouletteDialog = new DiscountRouletteDialog
                    {
                        XamlRoot = Content.XamlRoot,
                    };
                    rouletteDialog.Initialize(basePrice);
                    rouletteDialog.OnSpinComplete = onComplete;
                    _ = await rouletteDialog.ShowAsync();
                },

            // 4b. Set up Prescription Dialog handler
            OpenPrescriptionDialogAction = async (prescription) =>
                {
                    var prescriptionDialog = new PrescriptionDialog
                    {
                        XamlRoot = Content.XamlRoot,
                    };
                    prescriptionDialog.Initialize(prescription);
                    _ = await prescriptionDialog.ShowAsync();
                },
        };
        // 5. Set DataContext
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _viewModel;
        }

        // 6. Load patient data using ID
        _viewModel.LoadFullPatientProfile(patientId);
    }

    private void GoBack()
    {
        _goBackCallback?.Invoke();
        Close();
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
