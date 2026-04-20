using HospitalManagement.Entity;
using HospitalManagement.Integration;
using HospitalManagement.Service;
using HospitalManagement.View;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HospitalManagement.ViewModel;

internal class MedicalStaffViewModel : INotifyPropertyChanged
{
    private string _searchQuery = "";
    private string _errorMessage = "";
    private ObservableCollection<Patient> _searchResults = [];
    private readonly IPatientService _patientService;
    private Patient? _selectedPatient;

    private readonly IGhostService _ghostService;

    private bool _isExorcismAlertVisible;

    public bool IsExorcismAlertVisible
    {
        get => _isExorcismAlertVisible;

        set
        {
            _isExorcismAlertVisible = value;
            OnPropertyChanged();
        }
    }

    public Patient? SelectedPatient
    {
        get => _selectedPatient;

        set
        {
            _selectedPatient = value;
            OnPropertyChanged();
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;

        set
        {
            _searchQuery = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;

        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Patient> SearchResults
    {
        get => _searchResults;

        set
        {
            _searchResults = value;
            OnPropertyChanged();
        }
    }

    public ICommand SearchCommand { get; set; }

    public ICommand? BackToMainCommand { get; set; }

    public ICommand GhostSightingCommand { get; set; }


    // NEW COMMANDS FOR MEDICAL STAFF ACTIONS
    public ICommand FindBloodDonorsCommand { get; set; }

    public ICommand RequestTransplantCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MedicalStaffViewModel()
    {

        _patientService = (App.Current as App).Services.GetService<IPatientService>();

        _ghostService = (App.Current as App).Services.GetService<IGhostService>();
        _ghostService.ExorcismTriggered += (s, e) => IsExorcismAlertVisible = true;
        IsExorcismAlertVisible = _ghostService.IsExorcismTriggered();

        SearchCommand = new RelayCommand(ExecuteSearch);

        // INITIALIZE THE NEW COMMANDS
        FindBloodDonorsCommand = new RelayCommand(FindBloodDonors);
        RequestTransplantCommand = new RelayCommand(RequestTransplant);
        GhostSightingCommand = new RelayCommand(() => _ghostService.SawAGhost());
    }

    private void ExecuteSearch()
    {
        ErrorMessage = "";
        SearchResults.Clear();

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        var filter = new PatientFilter();

        if (SearchQuery.Length == 13 && SearchQuery.All(char.IsDigit))
        {
            filter.CNP = SearchQuery;
        }
        else
        {
            filter.NamePart = SearchQuery;
        }

        try
        {
            System.Collections.Generic.List<Patient> results = _patientService.SearchPatients(filter);

            if (results is null || results.Count == 0)
            {
                ErrorMessage = "There are no patients with this name or CNP.";
            }
            else
            {
                foreach (Patient p in results)
                {
                    SearchResults.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Database connection error: " + ex.Message;
        }
    }

    // --- EMPTY METHODS READY FOR FEATURE IMPLEMENTATION ---
    private void FindBloodDonors()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        // Create the new Window
        var donorsWindow = new Window
        {
            Title = $"Compatible Donors - {SelectedPatient.FirstName} {SelectedPatient.LastName}",
        };

        // Launch your brand new page!
        IServiceProvider scope = (Application.Current as App).Services;
        BloodDonorsView donorsPage = scope.GetRequiredService<BloodDonorsView>();
        donorsPage.Initialize(SelectedPatient.Id);
        donorsWindow.Content = donorsPage;
        donorsWindow.Activate();
    }

    private void RequestTransplant()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        var requestWindow = new Window
        {
            Title = $"Organ Transplant Request - {SelectedPatient.FirstName} {SelectedPatient.LastName}",
        };

        // Launch the correctly named page!
        var requestPage = new View.TransplantRequestView(SelectedPatient.Id, requestWindow);

        requestWindow.Content = requestPage;
        requestWindow.Activate();
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
