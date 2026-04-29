using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.View;

internal sealed partial class PatientView : Window
{
    private readonly PatientViewModel _viewModel;
    private Action _goBackCallback;

    public PatientView()
    {
        _goBackCallback = null!;
        InitializeComponent();

        _viewModel = (Application.Current as App)!.Services.GetRequiredService<PatientViewModel>();

        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = _viewModel;
        }

        MaximizeWindow();
        SetupViewModelActions();
    }

    public void Initialize(int patientId, Action goBackCallback)
    {
        _goBackCallback = goBackCallback;
        _viewModel.GoBackAction = GoBack;
        _viewModel.LoadFullPatientProfile(patientId);
    }

    private void SetupViewModelActions()
    {
        _viewModel.OpenRouletteAction = async (basePrice) =>
        {
            var rouletteDialog = new DiscountRouletteDialog
            {
                XamlRoot = Content.XamlRoot,
            };
            rouletteDialog.Initialize(basePrice);
            rouletteDialog.OnSpinComplete = _viewModel.HandleRouletteResult;
            _ = await rouletteDialog.ShowAsync();
        };

        _viewModel.OpenPrescriptionDialogAction = async (prescription) =>
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
