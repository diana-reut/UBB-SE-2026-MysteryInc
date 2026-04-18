using Microsoft.UI.Xaml;
using System;
using Microsoft.Extensions.DependencyInjection;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HospitalManagement;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public IServiceProvider Services { get; }
    private Window? _window;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
        Configuration.Config.Load();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDbContext, HospitalDbContext>();

        services.AddSingleton<IPatientRepository, PatientRepository>();
        services.AddSingleton<IMedicalHistoryRepository, MedicalHistoryRepository>();
        services.AddSingleton<IMedicalRecordRepository, MedicalRecordRepository>();


        services.AddSingleton<IPatientService, PatientService>();


        return services.BuildServiceProvider();
    }
}
