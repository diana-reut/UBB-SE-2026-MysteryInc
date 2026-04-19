using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using HospitalManagement.ViewModel;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.View;

internal sealed partial class PharmacistView : Window
{
    public PharmacistViewModel ViewModel { get; }

    public PharmacistView()
    {
        ViewModel = (App.Current as App).Services.GetService<PharmacistViewModel>();
        InitializeComponent();

        // Deschiderea Full Screen (Maximizatã)
        nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Setãm DataContext-ul pentru bindings
        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = ViewModel;
        }

        // Încãrcãm View-ul default manual la pornire (dacã dorim sã ne asigurãm)
        UpdateView(ViewModel.CurrentView);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PharmacistViewModel.CurrentView))
        {
            UpdateView(ViewModel.CurrentView);
        }
    }

    private void UpdateView(string viewName)
    {
        switch (viewName)
        {
            case "Prescriptions":
            {
                MainContentArea.Content = new PrescriptionView();
                    break;
            }
            case "Addicts":
            {
                    MainContentArea.Content = new AddictView();

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
