using HospitalManagement.Database;
using HospitalManagement.Integration.Export;
using HospitalManagement.Integration.External;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.View;
using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

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

        // DB
        _ = services.AddSingleton<IDbContext, HospitalDbContext>();

        // Repositories
        _ = services.AddSingleton<IPatientRepository, PatientRepository>();
        _ = services.AddSingleton<IMedicalHistoryRepository, MedicalHistoryRepository>();
        _ = services.AddSingleton<IMedicalRecordRepository, MedicalRecordRepository>();
        _ = services.AddSingleton<IAllergyRepository, AllergyRepository>();
        _ = services.AddSingleton<ITransplantRepository, TransplantRepository>();
        _ = services.AddSingleton<IPrescriptionRepository, PrescriptionRepository>();

        // Services
        _ = services.AddSingleton<IBloodCompatibilityService, BloodCompatibilityService>();
        _ = services.AddSingleton<IPatientService, PatientService>();
        _ = services.AddSingleton<IAllergyService, AllergyService>();
        _ = services.AddSingleton<ITransplantService, TransplantService>();
        _ = services.AddSingleton<IExportService, ExportService>();
        _ = services.AddSingleton<IImportService, ImportService>();
        _ = services.AddSingleton<IBillingService, BillingService>();
        _ = services.AddTransient<IAddictDetectionService, AddictDetectionService>();
        _ = services.AddTransient<IPrescriptionService, PrescriptionService>();
        _ = services.AddSingleton<IStatisticsService, StatisticsService>();
        _ = services.AddSingleton<IGhostService, GhostService>();

        // ViewModels & Windows
        _ = services.AddTransient<AdminViewModel>();
        _ = services.AddTransient<AdminView>();
        _ = services.AddTransient<PatientViewModel>();
        _ = services.AddTransient<PatientView>();
        _ = services.AddTransient<AddictViewModel>();
        _ = services.AddTransient<AddictView>();
        _ = services.AddTransient<PharmacistViewModel>();
        _ = services.AddTransient<PharmacistView>();
        _ = services.AddTransient<PrescriptionViewModel>();
        _ = services.AddTransient<PrescriptionView>();
        _ = services.AddTransient<OrganDonorViewModel>();
        _ = services.AddTransient<OrganDonorDialog>();
        _ = services.AddTransient<BloodDonorsViewModel>();
        _ = services.AddTransient<BloodDonorsView>();
        _ = services.AddTransient<StatisticsViewModel>();
        _ = services.AddTransient<StatisticsWindow>();
        _ = services.AddTransient<PatientProfileViewModel>();
        _ = services.AddTransient<PatientProfileView>();
        _ = services.AddTransient<MedicalStaffViewModel>();
        


        // MORE
        _ = services.AddSingleton<IExternalProvider, MockERProxy>();
        _ = services.AddSingleton<IExternalProvider, MockStaffProxy>();
        _ = services.AddSingleton<IExternalPatientPublisher, ExternalPatientPublisher>();

        return services.BuildServiceProvider();
    }
}
