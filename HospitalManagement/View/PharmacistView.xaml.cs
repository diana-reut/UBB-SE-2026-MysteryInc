using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;

namespace HospitalManagement.View;

internal sealed partial class PharmacistView : Window
{
    public PharmacistViewModel ViewModel { get; }

    private readonly IServiceProvider _services;

    public PharmacistView()
    {
        ViewModel = (App.Current as App).Services.GetService<PharmacistViewModel>();
        InitializeComponent();
        _services = (App.Current as App).Services;

        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        if (ViewModel is null)
        {
            throw new InvalidOperationException("PharmacistViewModel not found in service provider.");
        }

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = ViewModel;
        }

        if (ViewModel.CurrentView is null)
        {
            throw new InvalidOperationException("PharmacistViewModel not found in service provider.");
        }

        UpdateView(ViewModel.CurrentView);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PharmacistViewModel.CurrentView))
        {
            if (ViewModel.CurrentView is null)
            {
                throw new InvalidOperationException("PharmacistViewModel not found in service provider.");
            }

            UpdateView(ViewModel.CurrentView);
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
