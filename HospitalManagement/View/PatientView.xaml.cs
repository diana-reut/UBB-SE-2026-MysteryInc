using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using HospitalManagement.ViewModel;

namespace HospitalManagement.View;

internal sealed partial class PatientView : Window
{
    public PatientViewModel ViewModel { get; }

    private Action _goBackCallback;

    public PatientView(PatientViewModel viewModel)
    {
        _goBackCallback = null!;
        InitializeComponent();
        ViewModel = viewModel;

        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = ViewModel;
        }

        MaximizeWindow();

        SetupViewModelActions();
    }

    public void Initialize(int patientId, Action goBackCallback)
    {
        _goBackCallback = goBackCallback;
        ViewModel.GoBackAction = GoBack;

        ViewModel.LoadFullPatientProfile(patientId);
    }

    private void SetupViewModelActions()
    {
        ViewModel.OpenRouletteAction = async (basePrice, onComplete) =>
        {
            var rouletteDialog = new DiscountRouletteDialog
            {
                XamlRoot = Content.XamlRoot,
            };
            rouletteDialog.OnSpinComplete = onComplete;
            rouletteDialog.ViewModel.Initialize(basePrice);
            _ = await rouletteDialog.ShowAsync();
        };

        ViewModel.OpenPrescriptionDialogAction = async (prescription) =>
        {
            var prescriptionDialog = new PrescriptionDialog
            {
                XamlRoot = Content.XamlRoot,
            };
            prescriptionDialog.Initialize(prescription);
            _ = await prescriptionDialog.ShowAsync();
        };
    }

    private void MaximizeWindow()
    {
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    private void GoBack()
    {
        _goBackCallback?.Invoke();
        Close();
    }
}
