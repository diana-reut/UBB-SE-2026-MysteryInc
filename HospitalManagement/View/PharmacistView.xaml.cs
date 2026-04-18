using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;

namespace HospitalManagement.View;

internal sealed partial class PharmacistView : Window
{
    private readonly PharmacistViewModel _viewModel;

    private readonly IServiceProvider _services;

    public PharmacistView(PharmacistViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        _viewModel = viewModel;

        // Deschiderea Full Screen (Maximizatã)
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

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
                PrescriptionView prescriptionView = _services.GetRequiredService<PrescriptionView>();
                MainContentArea.Content = prescriptionView;
                break;
            }
            case "Addicts":
            {
                AddictView addictView = _services.GetRequiredService<AddictView>();
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
