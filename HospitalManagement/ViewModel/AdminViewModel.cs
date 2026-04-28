using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Integration;
using HospitalManagement.Service;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace HospitalManagement.ViewModel;

internal class AdminViewModel : INotifyPropertyChanged
{
    private string _currentView = "";

    public string CurrentView
    {
        get => _currentView;

        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    private bool _isArchivedMode;

    public bool IsActiveMode => !IsArchivedMode;

    // Remember to update this whenever IsArchivedMode changes
    public bool IsArchivedMode
    {
        get => _isArchivedMode;

        set
        {
            _isArchivedMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsActiveMode)); // Notify the UI to flip the other one!
        }
    }

    public ICommand NavigateToHomeCommand { get; set; }

    public ICommand NavigateToStatisticsCommand { get; }

    private readonly IPatientService _patientService;
    private readonly IGhostService _ghostService;
    private readonly ITransplantService _transplantService;

    // --- Ghost logic ---
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

    public ICommand GhostSightingCommand { get; }

    // --- The currently clicked patient in the UI ---
    private Patient? _selectedPatient;

    public Patient? SelectedPatient
    {
        get => _selectedPatient;

        set
        {
            _selectedPatient = value;
            OnPropertyChanged();

            OnPropertyChanged(nameof(IsNotDeceased));
            OnPropertyChanged(nameof(IsDeceased));

            if (_selectedPatient is not null)
            {
                // Create the shallow copy for editing
                EditingPatient = new Patient
                {
                    Id = _selectedPatient.Id,
                    FirstName = _selectedPatient.FirstName, // Read-only in UI
                    LastName = _selectedPatient.LastName,   // Read-only in UI
                    Cnp = _selectedPatient.Cnp,
                    Dob = _selectedPatient.Dob,              // Read-only in UI
                    Sex = _selectedPatient.Sex,             // Editable
                    PhoneNo = _selectedPatient.PhoneNo,     // Editable
                    EmergencyContact = _selectedPatient.EmergencyContact, // Editable
                    Dod = _selectedPatient.Dod,
                };
            }
        }
    }

    private Patient? _editingPatient;

    public Patient? EditingPatient
    {
        get => _editingPatient;

        set
        {
            _editingPatient = value;
            OnPropertyChanged();
        }
    }

    public ICommand UpdatePatientCommand { get; }

    // --- VM12: Search Properties ---
    private string? _searchQuery;

    public string? SearchQuery
    {
        get => _searchQuery;

        set
        {
            _searchQuery = value;
            OnPropertyChanged();
            // We trigger the search automatically as they type!
            SearchPatient();
        }
    }

    private bool _noResultsFound;

    public bool NoResultsFound
    {
        get => _noResultsFound;

        set
        {
            _noResultsFound = value;
            OnPropertyChanged();
        }
    }

    public ICommand SearchPatientCommand { get; }

    // --- VM13: Filter Properties ---
    private double? _minAge;

    public double? MinAge
    {
        get => _minAge;

        set
        {
            _minAge = value;
            OnPropertyChanged();
        }
    }

    private double? _maxAge;

    public double? MaxAge
    {
        get => _maxAge;

        set
        {
            _maxAge = value;
            OnPropertyChanged();
        }
    }

    private object? _selectedSexFilter;

    public object? SelectedSexFilter
    {
        get => _selectedSexFilter;

        set
        {
            _selectedSexFilter = value;
            OnPropertyChanged();
        }
    }

    public ICommand FilterPatientCommand { get; }

    public ICommand ClearFilterCommand { get; }

    // --- Properties bound to the View ---
    public ObservableCollection<Patient> Patients { get; set; }

    public ObservableCollection<Patient> ArchivedPatients { get; set; }

    public Patient NewPatient { get; set; }

    private string? _cnpError;

    public string? CnpError
    {
        get => _cnpError;

        set
        {
            _cnpError = value;
            OnPropertyChanged();
        }
    }

    private string? _phoneError;

    public string? PhoneError
    {
        get => _phoneError;

        set
        {
            _phoneError = value;
            OnPropertyChanged();
        }
    }

    private string? _dobError;

    public string? DobError
    {
        get => _dobError;

        set
        {
            _dobError = value;
            OnPropertyChanged();
        }
    }

    // --- The Close Window Notification ---
    public Action? CloseAddPatientWindow { get; set; }

    // --- VM15: Deceased Logic ---
    private DateTime? _dateOfDeath;

    public DateTime? DateOfDeath
    {
        get => _dateOfDeath;

        set
        {
            _dateOfDeath = value;
            OnPropertyChanged();
        }
    }

    // This property will be used in XAML to disable buttons: IsEnabled="{Binding IsNotDeceased}"
    public Func<string, string, Task<DateTime?>>? RequestDateAction { get; set; }

    public bool IsNotDeceased => SelectedPatient?.IsDeceased == false;

    public bool IsDeceased => SelectedPatient?.IsDeceased == true;

    public ICommand MarkAsDeceasedCommand { get; }

    public ICommand MarkAsOrganDonorCommand { get; }

    // --- UI Callbacks ---
    public Func<string, string, Task<bool>>? ConfirmAction { get; set; }

    public Func<string, Task>? ShowAlertAction { get; set; } // For the deceased warning

    public Func<Patient, Task>? OpenOrganDonorDialogAction { get; set; } // For opening organ donor dialog

    // For showing medical history - just pass the patient ID
    public Func<int, Task>? ShowMedicalHistoryAction { get; set; }

    // --- Commands bound to the View Buttons ---
    public ICommand LoadAllPatientsCommand { get; }

    public ICommand LoadArchivedPatientsCommand { get; }

    public ICommand AddPatientCommand { get; }

    public ICommand ArchivePatientCommand { get; }

    public ICommand DearchivePatientCommand { get; }

    public ICommand OpenOrganDonorCommand { get; }

    public ICommand ReportGhostCommand { get; }

    // --- Constructor ---
    public AdminViewModel()
    {
        _ghostService = (Application.Current as App)!.Services.GetRequiredService<IGhostService>();
        _patientService = (Application.Current as App)!.Services.GetRequiredService<IPatientService>();
        _transplantService = (Application.Current as App)!.Services.GetRequiredService<ITransplantService>();

        NavigateToStatisticsCommand = new RelayCommand(NavigateToStatistics);

        Patients = [];
        LoadAllPatientsCommand = new RelayCommand(LoadAllPatients);

        ArchivedPatients = [];
        LoadArchivedPatientsCommand = new RelayCommand(LoadArchivedPatients);

        NewPatient = new Patient { Dob = DateTime.Today, };

        AddPatientCommand = new RelayCommand(AddPatientAsync);
        ArchivePatientCommand = new RelayCommand(ArchivePatientAsync);
        DearchivePatientCommand = new RelayCommand(DearchivePatientAsync);

        UpdatePatientCommand = new RelayCommand(UpdatePatientAsync);

        SearchPatientCommand = new RelayCommand(SearchPatient);

        FilterPatientCommand = new RelayCommand(ExecuteFilterAsync);
        ClearFilterCommand = new RelayCommand(ClearFilters);

        MarkAsDeceasedCommand = new RelayCommand(MarkAsDeceasedAsync);
        MarkAsOrganDonorCommand = new RelayCommand(MarkAsOrganDonorAsync);
        OpenOrganDonorCommand = new RelayCommand(OpenOrganDonorDialogAsync);
        ReportGhostCommand = new RelayCommand(ReportGhostAsync);
        NavigateToHomeCommand = new RelayCommand(() => { /* This gets overwritten by MainWindow */ });

        // Ghost addition
        _ghostService.ExorcismTriggered += (s, e) => IsExorcismAlertVisible = true;
        GhostSightingCommand = new RelayCommand(() => _ghostService.SawAGhost());
        IsExorcismAlertVisible = _ghostService.IsExorcismTriggered();

        LoadAllPatients();


        CurrentView = "AdminDashboard";
    }

    public void LoadAllPatients()
    {
        var emptyFilter = new PatientFilter();
        List<Patient> allPatients = _patientService.SearchPatients(emptyFilter);

        Patients.Clear();

        IEnumerable<Patient> activePatients = allPatients.Where(p => !p.IsArchived);

        foreach (Patient patient in activePatients)
        {
            patient.PhoneNo = FormatPhoneNumber(patient.PhoneNo);
            patient.EmergencyContact = FormatPhoneNumber(patient.EmergencyContact);
            Patients.Add(patient);
        }
    }

    // --- Helper: Phone Number Formatter ---
    private static string FormatPhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return phone;
        }

        phone = phone.Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal);

        if (!(!phone.StartsWith('0') || phone.Length != 10))
        {
            return $"+40 {phone.Substring(1, 3)} {phone.Substring(4, 3)} {phone.Substring(7, 3)}";
        }

        return phone;
    }

    // --- VM7: Load Archived Patients ---
    public void LoadArchivedPatients()
    {
        // 1. Fetch ALL patients using an empty filter
        var emptyFilter = new PatientFilter();
        List<Patient> allPatients = _patientService.SearchPatients(emptyFilter);

        // 2. Clear the UI list to prevent duplicates
        ArchivedPatients.Clear();

        // 3. Filter archived patients in-memory using LINQ
        IEnumerable<Patient> archivedList = allPatients.Where(p => p.IsArchived);

        // 4. Format phone numbers and add to the ObservableCollection
        foreach (Patient patient in archivedList)
        {
            patient.PhoneNo = FormatPhoneNumber(patient.PhoneNo);
            patient.EmergencyContact = FormatPhoneNumber(patient.EmergencyContact);

            ArchivedPatients.Add(patient);
        }
    }

    private async void AddPatientAsync()
    {
        if (NewPatient is null)
        {
            return;
        }

        try
        {
            Patient createdPatient = _patientService.CreatePatient(NewPatient);
            LoadAllPatients();
            _ = ShowMedicalHistoryAction?.Invoke(createdPatient.Id);

            NewPatient = new Patient { Dob = DateTime.Today.AddYears(-20), };
            OnPropertyChanged(nameof(NewPatient));

            CloseAddPatientWindow?.Invoke();
        }
        catch (ArgumentException ex)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction(ex.Message);
            }
        }
        catch (Exception ex)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction($"Database Error: {ex.Message}");
            }
        }
    }

    private async void ArchivePatientAsync()
    {
        if (SelectedPatient is null)
        {
            return; // Nobody is selected!
        }

        Task<bool>? confirmTask = ConfirmAction?.Invoke(
            $"Are you sure you want to archive {SelectedPatient.FirstName} {SelectedPatient.LastName}?",
            "Confirm Archive");

        bool isConfirmed = confirmTask is not null && await confirmTask;

        if (!isConfirmed)
        {
            return;
        }

        _patientService.ArchivePatient(SelectedPatient.Id);

        LoadAllPatients();
        LoadArchivedPatients();
    }

    private async void DearchivePatientAsync()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        // 1. Strict Validation: Cannot dearchive deceased patients
        if (SelectedPatient.IsDeceased)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("Cannot dearchive this patient. The record indicates the patient is deceased.");
            }

            return;
        }

        _patientService.DearchivePatient(SelectedPatient.Id);

        LoadAllPatients();
        LoadArchivedPatients();
    }

    // --- VM10: Update Patient ---
    private async void UpdatePatientAsync()
    {
        if (EditingPatient is null || SelectedPatient is null)
        {
            return;
        }

        // 1. (Optional) Re-run phone formatting before saving
        // EditingPatient.PhoneNo = FormatPhoneNumber(EditingPatient.PhoneNo);
        // EditingPatient.EmergencyContact = FormatPhoneNumber(EditingPatient.EmergencyContact);

        // 2. Send the updated copy to the Service
        try
        {
            _patientService.UpdatePatient(EditingPatient);

            EditingPatient.PhoneNo = FormatPhoneNumber(EditingPatient.PhoneNo);
            EditingPatient.EmergencyContact = FormatPhoneNumber(EditingPatient.EmergencyContact);

            // 3. Sync: Refresh the main list to show the new data
            LoadAllPatients();

            // 4. Clear the selection/edit form
            EditingPatient = null!;
            SelectedPatient = null!;

            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("Patient updated successfully.");
            }
        }
        catch (Exception ex)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction($"Update failed: {ex.Message}");
            }
        }
    }

    // --- VM12: Search Patient (Fuzzy Logic) ---
    // --- VM12: Search Patient (Fuzzy Logic Updated) ---
    public void SearchPatient()
    {
        // 1. Initialize the filter from your colleague's class
        var filter = new PatientFilter();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            // 2. Fuzzy Logic: Identify if Query is CNP or Name
            if (SearchQuery.All(char.IsDigit))
            {
                if (SearchQuery.All(char.IsDigit) && SearchQuery.Length == 13)
                {
                    filter.CNP = SearchQuery; // Safe to send to Service
                }
            }
            else
            {
                // If it contains letters, map to the namePart field
                filter.NamePart = SearchQuery;
            }
        }

        // 3. Call the Service
        // Note: Since PatientFilter doesn't have IsArchived,
        // the service likely returns everyone. We filter for active only here.
        List<Patient> results = _patientService.SearchPatients(filter);

        // 4. UI Collection Sync
        Patients.Clear();
        IEnumerable<Patient> activeResults = results.Where(p => !p.IsArchived);

        foreach (Patient p in activeResults)
        {
            p.PhoneNo = FormatPhoneNumber(p.PhoneNo);
            p.EmergencyContact = FormatPhoneNumber(p.EmergencyContact);
            Patients.Add(p);
        }

        // 5. Update Visual State for "No Results"
        NoResultsFound = Patients.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery);
    }

    // --- VM13: Execute High-Precision Filter ---
    private async void ExecuteFilterAsync()
    {
        try
        {
            // 1. Extract and Convert the Sex value
            Sex? finalSexEnum = null; // Start as null (no filter)

            if (SelectedSexFilter is Microsoft.UI.Xaml.Controls.ComboBoxItem item)
            {
                string? content = item.Content.ToString();

                // Try to convert "M" or "F" string to the Sex Enum
                if (Enum.TryParse(content, out Sex result))
                {
                    finalSexEnum = result;
                }
            }

            // 2. Map values to the filter
            var filter = new PatientFilter
            {
                MinAge = (int?)MinAge,
                MaxAge = (int?)MaxAge,
                Sex = finalSexEnum, // Now the types match perfectly!
            };

            // 3. Re-apply SearchQuery with the "13-digit shield"
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                if (SearchQuery.All(char.IsDigit) && SearchQuery.Length == 13)
                {
                    filter.CNP = SearchQuery;
                }
                else
                {
                    filter.NamePart = SearchQuery;
                }
            }

            // 4. Fetch from Service
            List<Patient> results = _patientService.SearchPatients(filter);

            // 5. Sync Collection (Active only)
            Patients.Clear();
            IEnumerable<Patient> activeResults = results.Where(x => !x.IsArchived);

            foreach (Patient p in activeResults)
            {
                p.PhoneNo = FormatPhoneNumber(p.PhoneNo);
                p.EmergencyContact = FormatPhoneNumber(p.EmergencyContact);
                Patients.Add(p);
            }

            // 6. Update Visual State
            NoResultsFound = Patients.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery);
        }
        catch (ArgumentException ex)
        {
            // This catches "Min > Max" errors from your Service logic
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction(ex.Message);
            }
        }
    }

    // --- VM13: Clear/Reset ---
    private void ClearFilters()
    {
        // Reset the specific filter fields
        MinAge = null;
        MaxAge = null;
        SelectedSexFilter = null!;

        // Optional: Also clear the search query to return to a 100% clean list
        SearchQuery = "";

        LoadAllPatients(); // Returns to the full active list
        NoResultsFound = false;
    }


    // --- VM15: Mark As Deceased ---
    private async void MarkAsDeceasedAsync()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        DateTime? chosenDate = await (RequestDateAction?.Invoke("Enter Date of Death:", "Mark as Deceased") ?? Task.FromResult<DateTime?>(null));
        if (chosenDate is null)
        {
            return; // User cancelled
        }

        // 2. Validation: Cannot be in the future
        if (chosenDate > DateTime.Now)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("Date of death cannot be in the future.");
            }

            return;
        }

        // 3. Validation: Cannot be before Date of Birth
        if (chosenDate < SelectedPatient.Dob)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("Date of death cannot be earlier than the Date of Birth.");
            }

            return;
        }

        string cleanPhone = SelectedPatient.PhoneNo
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("+40", "0", StringComparison.Ordinal);

        string cleanEmergency = SelectedPatient.EmergencyContact
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("+40", "0", StringComparison.Ordinal);
        SelectedPatient.PhoneNo = cleanPhone;
        SelectedPatient.EmergencyContact = cleanEmergency;

        // 4. Update the Record
        SelectedPatient.Dod = chosenDate; // Setting the Date of Death
        SelectedPatient.IsArchived = true; // Securely move to locked archive state

        try
        {
            // 5. Call Service to Save
            _patientService.UpdatePatient(SelectedPatient);

            // 6. Refresh and Notify
            LoadAllPatients();
            LoadArchivedPatients();

            OnPropertyChanged(nameof(IsNotDeceased));
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("This patient has now become a ghost. Beware!!!");
            }
        }
        catch (Exception ex)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction($"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Open the Organ Donor assignment dialog.
    /// </summary>
    private async void OpenOrganDonorDialogAsync()
    {
        if (SelectedPatient?.IsDeceased != true || !SelectedPatient.IsDonor)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("Patient must be deceased and registered as a donor.");
            }

            return;
        }

        OpenOrganDonorDialogAction?.Invoke(SelectedPatient);
    }

    /// <summary>
    /// Mark the selected patient as an organ donor and open the organ donor assignment dialog.
    /// </summary>
    private async void MarkAsOrganDonorAsync()
    {
        if (SelectedPatient is null)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("Please select a patient first.");
            }

            return;
        }

        if (!SelectedPatient.IsDeceased)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("Patient must be marked as deceased before registering as an organ donor.");
            }

            return;
        }

        try
        {
            SelectedPatient.PhoneNo = SelectedPatient.PhoneNo
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("+40", "0", StringComparison.Ordinal);

            SelectedPatient.EmergencyContact = SelectedPatient.EmergencyContact
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("+40", "0", StringComparison.Ordinal);
            // Mark as organ donor
            SelectedPatient.IsDonor = true;

            // Update the database
            _patientService.UpdatePatient(SelectedPatient);

            // Open the Organ Donor Dialog BEFORE refreshing lists (to avoid stale data)
            OpenOrganDonorDialogAsync();

            // Refresh the lists after dialog completes
            LoadAllPatients();
            LoadArchivedPatients();
        }
        catch (Exception ex)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction($"Error marking patient as organ donor: {ex.Message}");
            }
        }
    }

    // Report ghost sighting (paranormal activity logging)
    private async void ReportGhostAsync()
    {
        // part of this was deleting during merge so idk what comes here
        System.Diagnostics.Debug.WriteLine($">> GHOST REPORTED FROM ADMIN AT {DateTime.Now} <<");

        try
        {
            // This forces the "Edit" buttons to re-check if they should be disabled
            OnPropertyChanged(nameof(IsNotDeceased));
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction("The patient has been marked as deceased. The record is now locked and moved to the archive.");
            }
        }
        catch (Exception ex)
        {
            if (ShowAlertAction is not null)
            {
                await ShowAlertAction($"Error: {ex.Message}");
            }
        }
    }

    // --- INotifyPropertyChanged Implementation ---
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void NavigateToStatistics()
    {
        CurrentView = "Statistics";
    }
}
