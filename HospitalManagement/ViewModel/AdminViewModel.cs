using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Integration;
using HospitalManagement.Service;
using HospitalManagement.View;
using HospitalManagement.View.DialogServiceAdmin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HospitalManagement.ViewModel;

internal partial class AdminViewModel : ObservableObject, INotifyPropertyChanged
{
    private readonly IPatientService _patientService;
    private readonly IGhostService _ghostService;
    private readonly ITransplantService _transplantService;
    private readonly IDialogService _dialogService;

    private string _currentView = "";
    private bool _isArchivedMode;
    private bool _isExorcismAlertVisible;
    private Patient? _selectedPatient;
    private Patient? _editingPatient;
    private string? _searchQuery;
    private bool _noResultsFound;
    private double? _minAge;
    private double? _maxAge;
    private object? _selectedSexFilter;
    private string? _cnpError;
    private string? _phoneError;
    private string? _dobError;
    private DateTime? _dateOfDeath;
    private bool _isStatisticsVisible;


    // navigation section
    public string CurrentView
    {
        get => _currentView;

        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public bool IsStatisticsVisible
    {
        get => _isStatisticsVisible;

        set
        {
            _isStatisticsVisible = value;
            OnPropertyChanged(nameof(IsStatisticsVisible));
        }
    }

    public Action? CloseAddPatientWindow { get; set; }

    [RelayCommand]
    private static void NavigateHome()
    {
        MainWindow mainWindow = (Application.Current as App)!.Services
            .GetRequiredService<MainWindow>();

        mainWindow.Activate();
    }

    [RelayCommand]
    private void ToggleStatistics()
    {
        IsStatisticsVisible = !IsStatisticsVisible;
    }

    [RelayCommand]
    private void ExecuteSwitchToActive()
    {
        IsArchivedMode = false;
    }

    [RelayCommand]
    private void NavigateToStatistics()
    {
        CurrentView = "Statistics";
    }

    // patients
    public ObservableCollection<Patient> Patients { get; set; }

    public ObservableCollection<Patient> ArchivedPatients { get; set; }

    public Patient NewPatient { get; set; }

    // state

    public bool IsActiveMode => !IsArchivedMode;

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

    public bool IsNotDeceased => SelectedPatient?.IsDeceased == false;

    public bool IsDeceased => SelectedPatient?.IsDeceased == true;

    public bool IsExorcismAlertVisible
    {
        get => _isExorcismAlertVisible;

        set
        {
            _isExorcismAlertVisible = value;
            OnPropertyChanged();
        }
    }

    // selection and editing

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

    public Patient? EditingPatient
    {
        get => _editingPatient;

        set
        {
            _editingPatient = value;
            OnPropertyChanged();
        }
    }

    // search and filter

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

    public bool NoResultsFound
    {
        get => _noResultsFound;

        set
        {
            _noResultsFound = value;
            OnPropertyChanged();
        }
    }

    public double? MinAge
    {
        get => _minAge;

        set
        {
            _minAge = value;
            OnPropertyChanged();
        }
    }

    public double? MaxAge
    {
        get => _maxAge;

        set
        {
            _maxAge = value;
            OnPropertyChanged();
        }
    }

    public object? SelectedSexFilter
    {
        get => _selectedSexFilter;

        set
        {
            _selectedSexFilter = value;
            OnPropertyChanged();
        }
    }

    // validation properties
    public string? CnpError
    {
        get => _cnpError;

        set
        {
            _cnpError = value;
            OnPropertyChanged();
        }
    }

    public string? PhoneError
    {
        get => _phoneError;

        set
        {
            _phoneError = value;
            OnPropertyChanged();
        }
    }

    public string? DobError
    {
        get => _dobError;

        set
        {
            _dobError = value;
            OnPropertyChanged();
        }
    }

    public DateTime? DateOfDeath
    {
        get => _dateOfDeath;

        set
        {
            _dateOfDeath = value;
            OnPropertyChanged();
        }
    }

    // property changed

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    // --- Constructor ---
    public AdminViewModel()
    {
        _ghostService = (Application.Current as App)!.Services.GetRequiredService<IGhostService>();
        _patientService = (Application.Current as App)!.Services.GetRequiredService<IPatientService>();
        _transplantService = (Application.Current as App)!.Services.GetRequiredService<ITransplantService>();
        _dialogService = (Application.Current as App)!.Services.GetRequiredService<IDialogService>();

        Patients = [];
        ArchivedPatients = [];
        NewPatient = new Patient { Dob = DateTime.Today, };


        // Ghost addition
        _ghostService.ExorcismTriggered += (s, e) => IsExorcismAlertVisible = true;
        // GhostSightingCommand = new RelayCommand(() => _ghostService.SawAGhost());
        IsExorcismAlertVisible = _ghostService.IsExorcismTriggered();

        LoadAllPatients();
        CurrentView = "AdminDashboard";
    }

    [RelayCommand]
    public void GhostSighting()
    {
        _ghostService.SawAGhost();
    }

    [RelayCommand]
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


    [RelayCommand]
    public void LoadArchivedPatients()
    {
        IsArchivedMode = true;
        var emptyFilter = new PatientFilter();
        List<Patient> allPatients = _patientService.SearchPatients(emptyFilter);

        ArchivedPatients.Clear();
        IEnumerable<Patient> archivedList = allPatients.Where(p => p.IsArchived);

        foreach (Patient patient in archivedList)
        {
            patient.PhoneNo = FormatPhoneNumber(patient.PhoneNo);
            patient.EmergencyContact = FormatPhoneNumber(patient.EmergencyContact);

            ArchivedPatients.Add(patient);
        }
    }

    [RelayCommand]
    private void OpenPatientDetails()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        int patientId = SelectedPatient.Id;

        IServiceProvider scope = (Application.Current as App)!.Services;
        PatientView patientWindow = scope.GetRequiredService<PatientView>();

        patientWindow.Initialize(patientId, () => { });
        patientWindow.Activate();
    }

    public async Task AssignOrganDonorAsync(int transplantId, int donorId, float score, string donorName)
    {
        try
        {
            _transplantService.AssignDonor(transplantId, donorId, score);

            await _dialogService.ShowAlertAsync($"Successfully assigned organ from donor {donorName}.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync($"Error assigning organ: {ex.Message}");
        }
    }

    public async Task ProcessMedicalHistoryResultAsync(int patientId, MedicalHistory history, bool wasSkipped)
    {
        if (wasSkipped)
        {
            await _dialogService.ShowAlertAsync("You can add medical history later from the patient profile.");

            return;
        }

        if (history is null)
        {
            return;
        }

        try
        {
            history.PatientId = patientId;
            _patientService.CreateMedicalHistory(patientId, history);
            await _dialogService.ShowAlertAsync("Medical history saved successfully!");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync($"Error saving medical history: {ex.Message}");
        }
    }

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

    [RelayCommand]
    private async void AddPatientAsync()
    {
        Patient? patient = await _dialogService.ShowAddPatientDialogAsync();

        if (patient is null)
        {
            return;
        }

        try
        {
            Patient createdPatient = _patientService.CreatePatient(patient);

            LoadAllPatients();

            (MedicalHistory? history, bool skipped) = await _dialogService.ShowMedicalHistoryAsync();

            await ProcessMedicalHistoryResultAsync(
                createdPatient.Id,
                history,
                skipped);

            await _dialogService.ShowAlertAsync("Patient added successfully.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async void ArchivePatientAsync()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        Task<bool>? confirmTask = _dialogService.ShowConfirmAsync(
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

    [RelayCommand]
    private async void DearchivePatientAsync()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        if (SelectedPatient.IsDeceased)
        {
            await _dialogService.ShowAlertAsync("Cannot dearchive this patient. The record indicates the patient is deceased.");

            return;
        }

        _patientService.DearchivePatient(SelectedPatient.Id);

        LoadAllPatients();
        LoadArchivedPatients();
    }

    [RelayCommand]
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

            await _dialogService.ShowAlertAsync("Patient updated successfully.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync($"Update failed: {ex.Message}");
        }
    }

    [RelayCommand]
    public void SearchPatient()
    {
        var filter = new PatientFilter();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            if (SearchQuery.All(char.IsDigit))
            {
                if (SearchQuery.All(char.IsDigit) && SearchQuery.Length == 13)
                {
                    filter.CNP = SearchQuery;
                }
            }
            else
            {
                filter.NamePart = SearchQuery;
            }
        }

        // Note: Since PatientFilter doesn't have IsArchived,
        // the service likely returns everyone. We filter for active only here.
        List<Patient> results = _patientService.SearchPatients(filter);

        Patients.Clear();
        IEnumerable<Patient> activeResults = results.Where(p => !p.IsArchived);

        foreach (Patient p in activeResults)
        {
            p.PhoneNo = FormatPhoneNumber(p.PhoneNo);
            p.EmergencyContact = FormatPhoneNumber(p.EmergencyContact);
            Patients.Add(p);
        }

        NoResultsFound = Patients.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery);
    }

    [RelayCommand]
    private async void ExecuteFilterAsync()
    {
        try
        {
            Sex? finalSexEnum = null;

            if (SelectedSexFilter is Microsoft.UI.Xaml.Controls.ComboBoxItem item)
            {
                string? content = item.Content.ToString();

                if (Enum.TryParse(content, out Sex result))
                {
                    finalSexEnum = result;
                }
            }

            var filter = new PatientFilter
            {
                MinAge = (int?)MinAge,
                MaxAge = (int?)MaxAge,
                Sex = finalSexEnum,
            };

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

            List<Patient> results = _patientService.SearchPatients(filter);

            Patients.Clear();
            IEnumerable<Patient> activeResults = results.Where(x => !x.IsArchived);

            foreach (Patient p in activeResults)
            {
                p.PhoneNo = FormatPhoneNumber(p.PhoneNo);
                p.EmergencyContact = FormatPhoneNumber(p.EmergencyContact);
                Patients.Add(p);
            }

            NoResultsFound = Patients.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery);
        }
        catch (ArgumentException ex)
        {
            await _dialogService.ShowAlertAsync(ex.Message);
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        MinAge = null;
        MaxAge = null;
        SelectedSexFilter = null!;

        SearchQuery = "";

        LoadAllPatients();
        NoResultsFound = false;
    }

    [RelayCommand]
    private async void MarkAsDeceasedAsync()
    {
        if (SelectedPatient is null)
        {
            return;
        }

        DateTime? chosenDate = await (_dialogService.ShowDatePickerAsync("Enter Date of Death:", "Mark as Deceased") ?? Task.FromResult<DateTime?>(null));
        if (chosenDate is null)
        {
            return;
        }

        if (chosenDate > DateTime.Now)
        {
            await _dialogService.ShowAlertAsync("Date of death cannot be in the future.");

            return;
        }

        if (chosenDate < SelectedPatient.Dob)
        {
            await _dialogService.ShowAlertAsync("Date of death cannot be earlier than the Date of Birth.");

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

        SelectedPatient.Dod = chosenDate;
        SelectedPatient.IsArchived = true;

        try
        {
            _patientService.UpdatePatient(SelectedPatient);

            LoadAllPatients();
            LoadArchivedPatients();

            OnPropertyChanged(nameof(IsNotDeceased));
            await _dialogService.ShowAlertAsync("This patient has now become a ghost. Beware!!!");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync($"Error: {ex.Message}");
        }
    }


    // idk where this is used...
    [RelayCommand]
    private async void OpenOrganDonorDialogAsync()
    {
        if (SelectedPatient?.IsDeceased != true || !SelectedPatient.IsDonor)
        {
            await _dialogService.ShowAlertAsync("Patient must be deceased and registered as a donor.");

            return;
        }

        await _dialogService.ShowOrganDonorDialogAsync(SelectedPatient);
    }

    [RelayCommand]
    private async void MarkAsOrganDonorAsync()
    {
        if (SelectedPatient is null)
        {
            await _dialogService.ShowAlertAsync("Please select a patient first.");

            return;
        }

        if (!SelectedPatient.IsDeceased)
        {
            await _dialogService.ShowAlertAsync("Patient must be marked as deceased before registering as an organ donor.");

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
            SelectedPatient.IsDonor = true;

            _patientService.UpdatePatient(SelectedPatient);
            OpenOrganDonorDialogAsync();
            LoadAllPatients();
            LoadArchivedPatients();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync($"Error marking patient as organ donor: {ex.Message}");
        }
    }

    [RelayCommand]
    private async void ReportGhostAsync()
    {
        // part of this was deleting during merge so idk what comes here
        System.Diagnostics.Debug.WriteLine($">> GHOST REPORTED FROM ADMIN AT {DateTime.Now} <<");

        try
        {
            // This forces the "Edit" buttons to re-check if they should be disabled
            OnPropertyChanged(nameof(IsNotDeceased));
            await _dialogService.ShowAlertAsync("The patient has been marked as deceased. The record is now locked and moved to the archive.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync($"Error: {ex.Message}");
        }
    }
}
