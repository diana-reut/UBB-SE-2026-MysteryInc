using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input; // Required for ICommand
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Integration;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    public class AdminViewModel : INotifyPropertyChanged
    {


        public ICommand NavigateToHomeCommand { get; set; }


        private readonly PatientService _patientService;

        // --- The currently clicked patient in the UI ---
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged();

                if (_selectedPatient != null)
                {
                    // Create the shallow copy for editing
                    EditingPatient = new Patient
                    {
                        Id = _selectedPatient.Id,
                        FirstName = _selectedPatient.FirstName, // Read-only in UI
                        LastName = _selectedPatient.LastName,   // Read-only in UI
                        Cnp = _selectedPatient.Cnp,             // Read-only in UI
                        Sex = _selectedPatient.Sex,             // Editable
                        PhoneNo = _selectedPatient.PhoneNo,     // Editable
                        EmergencyContact = _selectedPatient.EmergencyContact // Editable
                    };
                }
            }
        }

        private Patient _editingPatient;
        public Patient EditingPatient
        {
            get => _editingPatient;
            set { _editingPatient = value; OnPropertyChanged(); }
        }

        public ICommand UpdatePatientCommand { get; }

        // --- VM12: Search Properties ---
        private string _searchQuery;
        public string SearchQuery
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
            set { _noResultsFound = value; OnPropertyChanged(); }
        }

        public ICommand SearchPatientCommand { get; }

        // --- VM13: Filter Properties ---
        private int? _minAge;
        public int? MinAge
        {
            get => _minAge;
            set { _minAge = value; OnPropertyChanged(); }
        }

        private int? _maxAge;
        public int? MaxAge
        {
            get => _maxAge;
            set { _maxAge = value; OnPropertyChanged(); }
        }

        private Sex? _selectedSexFilter;
        public Sex? SelectedSexFilter
        {
            get => _selectedSexFilter;
            set { _selectedSexFilter = value; OnPropertyChanged(); }
        }

        public ICommand FilterPatientCommand { get; }
        public ICommand ClearFilterCommand { get; }

        // --- Properties bound to the View ---
        public ObservableCollection<Patient> Patients { get; set; }

        public ObservableCollection<Patient> ArchivedPatients { get; set; }

        public Patient NewPatient { get; set; }

        // --- Validation Errors (For the red labels) ---
        public ObservableCollection<string> ValidationErrors { get; set; }

        private string _cnpError;
        public string CnpError { get => _cnpError; set { _cnpError = value; OnPropertyChanged(); } }

        private string _phoneError;
        public string PhoneError { get => _phoneError; set { _phoneError = value; OnPropertyChanged(); } }

        private string _dobError;
        public string DobError { get => _dobError; set { _dobError = value; OnPropertyChanged(); } }

        // --- The Close Window Notification ---
        public Action CloseAddPatientWindow { get; set; }

        // --- VM15: Deceased Logic ---
        private DateTime? _dateOfDeath;
        public DateTime? DateOfDeath
        {
            get => _dateOfDeath;
            set { _dateOfDeath = value; OnPropertyChanged(); }
        }

        // This property will be used in XAML to disable buttons: IsEnabled="{Binding IsNotDeceased}"
        public Func<string, string, DateTime?> RequestDateAction { get; set; }
        public bool IsNotDeceased => SelectedPatient != null && !SelectedPatient.IsDeceased;

        public ICommand MarkAsDeceasedCommand { get; }

        // --- UI Callbacks ---
        public Func<string, string, bool> ConfirmAction { get; set; }
        public Action<string> ShowAlertAction { get; set; } // For the deceased warning


        // --- Commands bound to the View Buttons ---
        public ICommand LoadAllPatientsCommand { get; }

        public ICommand LoadArchivedPatientsCommand { get; }

        public ICommand AddPatientCommand { get; }

        public ICommand ArchivePatientCommand { get; }
        public ICommand DearchivePatientCommand { get; }

        // --- Constructor ---
        public AdminViewModel(PatientService patientService)
        {
            _patientService = patientService;

            Patients = new ObservableCollection<Patient>();
            LoadAllPatientsCommand = new RelayCommand(LoadAllPatients);

            ArchivedPatients = new ObservableCollection<Patient>();
            LoadArchivedPatientsCommand = new RelayCommand(LoadArchivedPatients);

            NewPatient = new Patient { Dob = DateTime.Today };
            ValidationErrors = new ObservableCollection<string>();
            AddPatientCommand = new RelayCommand(AddPatient);

            ArchivePatientCommand = new RelayCommand(ArchivePatient);
            DearchivePatientCommand = new RelayCommand(DearchivePatient);

            UpdatePatientCommand = new RelayCommand(UpdatePatient);

            SearchPatientCommand = new RelayCommand(SearchPatient);

            FilterPatientCommand = new RelayCommand(ExecuteFilter);
            ClearFilterCommand = new RelayCommand(ClearFilters);

            MarkAsDeceasedCommand = new RelayCommand(MarkAsDeceased);
            NavigateToHomeCommand = new RelayCommand(() => { /* This gets overwritten by MainWindow */ });


        }

        // --- VM6: Load All Patients (The Method) ---
        public void LoadAllPatients()
        {
            var emptyFilter = new PatientFilter();
            var allPatients = _patientService.SearchPatients(emptyFilter);

            Patients.Clear();

            var activePatients = allPatients.Where(p => p.IsArchived == false);

            foreach (var patient in activePatients)
            {
                patient.PhoneNo = FormatPhoneNumber(patient.PhoneNo);
                patient.EmergencyContact = FormatPhoneNumber(patient.EmergencyContact);
                Patients.Add(patient);
            }
        }

        // --- Helper: Phone Number Formatter ---
        private string FormatPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            phone = phone.Replace(" ", "").Replace("-", "");
            if (phone.StartsWith("0") && phone.Length == 10)
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
            var allPatients = _patientService.SearchPatients(emptyFilter);

            // 2. Clear the UI list to prevent duplicates
            ArchivedPatients.Clear();

            // 3. Filter archived patients in-memory using LINQ
            var archivedList = allPatients.Where(p => p.IsArchived == true);

            // 4. Format phone numbers and add to the ObservableCollection
            foreach (var patient in archivedList)
            {
                patient.PhoneNo = FormatPhoneNumber(patient.PhoneNo);
                patient.EmergencyContact = FormatPhoneNumber(patient.EmergencyContact);

                ArchivedPatients.Add(patient);
            }
        }

        // --- VM8: Add Patient Logic ---
        private void AddPatient()
        {
            // 1. Reset previous errors
            ValidationErrors.Clear();
            CnpError = string.Empty;
            PhoneError = string.Empty;
            DobError = string.Empty;
            bool isValid = true;

            // 2. Validate CNP (13 digits + Secondary Service Check)
            if (string.IsNullOrWhiteSpace(NewPatient.Cnp) || NewPatient.Cnp.Length != 13 || !NewPatient.Cnp.All(char.IsDigit))
            {
                CnpError = "CNP must be exactly 13 digits.";
                ValidationErrors.Add(CnpError);
                isValid = false;
            }
            else if (!_patientService.ValidateCNP(NewPatient.Cnp, NewPatient.Sex, NewPatient.Dob)) // Secondary check
            {
                CnpError = "CNP does not match the selected sex or date of birth.";
                ValidationErrors.Add(CnpError);
                isValid = false;
            }

            // 3. Validate Phone (10 digits)
            if (string.IsNullOrWhiteSpace(NewPatient.PhoneNo) || NewPatient.PhoneNo.Length != 10 || !NewPatient.PhoneNo.All(char.IsDigit))
            {
                PhoneError = "Phone must be exactly 10 digits.";
                ValidationErrors.Add(PhoneError);
                isValid = false;
            }

            // 4. Validate Date of Birth (Must be in the past)
            if (NewPatient.Dob >= DateTime.Today)
            {
                DobError = "Date of Birth must be in the past.";
                ValidationErrors.Add(DobError);
                isValid = false;
            }

            // 5. Check if we should stop
            if (!isValid)
            {
                // We update this property to force the UI to refresh the error list
                OnPropertyChanged(nameof(ValidationErrors));
                return;
            }

            // --- SUCCESS PATH ---

            // 6. Map and Create (Assumes your teammate named the method CreatePatient)
            _patientService.CreatePatient(NewPatient);

            // 7. Refresh the main patient list so the new patient appears instantly
            LoadAllPatients();

            // 8. Reset the form for the next time it opens
            NewPatient = new Patient { Dob = DateTime.Today };
            OnPropertyChanged(nameof(NewPatient));

            // 9. Trigger the CloseWindow notification
            CloseAddPatientWindow?.Invoke();
        }

        private void ArchivePatient()
        {
            if (SelectedPatient == null) return; // Nobody is selected!

            // 1. Trigger the mandatory confirmation layer
            // If the View isn't hooked up yet, or they click 'No', we abort.
            bool isConfirmed = ConfirmAction?.Invoke(
                $"Are you sure you want to archive {SelectedPatient.FirstName} {SelectedPatient.LastName}?",
                "Confirm Archive") ?? false;

            if (!isConfirmed) return;

            // 2. Call the Service 
            _patientService.ArchivePatient(SelectedPatient.Id);

            // 3. Refresh both lists so the patient instantly moves from one grid to the other!
            LoadAllPatients();
            LoadArchivedPatients();
        }

        private void DearchivePatient()
        {
            if (SelectedPatient == null) return; // Nobody is selected!

            // 1. Strict Validation: Cannot dearchive deceased patients
            if (SelectedPatient.IsDeceased)
            {
                // Tell the UI to show a warning popup
                ShowAlertAction?.Invoke("Cannot dearchive this patient. The record indicates the patient is deceased.");
                return; // Abort the command
            }

            // 2. Call the Service
             _patientService.DearchivePatient(SelectedPatient.Id);

            // 3. Refresh both lists so the patient moves back to the active grid!
            LoadAllPatients();
            LoadArchivedPatients();
        }

        // --- VM10: Update Patient ---
        private void UpdatePatient()
        {
            if (EditingPatient == null || SelectedPatient == null) return;

            // 1. (Optional) Re-run phone formatting before saving
            EditingPatient.PhoneNo = FormatPhoneNumber(EditingPatient.PhoneNo);
            EditingPatient.EmergencyContact = FormatPhoneNumber(EditingPatient.EmergencyContact);

            // 2. Send the updated copy to the Service
            try
            {
                _patientService.UpdatePatient(EditingPatient);

                // 3. Sync: Refresh the main list to show the new data
                LoadAllPatients();

                // 4. Clear the selection/edit form
                EditingPatient = null;
                SelectedPatient = null;

                ShowAlertAction?.Invoke("Patient updated successfully.");
            }
            catch (Exception ex)
            {
                ShowAlertAction?.Invoke($"Update failed: {ex.Message}");
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
                    filter.namePart = SearchQuery;
                }
            }

            // 3. Call the Service
            // Note: Since PatientFilter doesn't have IsArchived, 
            // the service likely returns everyone. We filter for active only here.
            var results = _patientService.SearchPatients(filter);

            // 4. UI Collection Sync
            Patients.Clear();
            var activeResults = results.Where(p => p.IsArchived == false);

            foreach (var p in activeResults)
            {
                p.PhoneNo = FormatPhoneNumber(p.PhoneNo);
                p.EmergencyContact = FormatPhoneNumber(p.EmergencyContact);
                Patients.Add(p);
            }

            // 5. Update Visual State for "No Results"
            NoResultsFound = (Patients.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery));
        }

        // --- VM13: Execute High-Precision Filter ---
        private void ExecuteFilter()
        {
            try
            {
                // 1. Map Age and Sex to the filter
                var filter = new PatientFilter
                {
                    minAge = MinAge,
                    maxAge = MaxAge,
                    sex = SelectedSexFilter
                };

                // 2. Re-apply SearchQuery with the "13-digit shield"
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    // Only use CNP field if it satisfies the Service's 13-digit rule
                    if (SearchQuery.All(char.IsDigit) && SearchQuery.Length == 13)
                    {
                        filter.CNP = SearchQuery;
                    }
                    else
                    {
                        // If it's partial numbers or text, treat as namePart
                        filter.namePart = SearchQuery;
                    }
                }

                // 3. Fetch from Service (Service validates Min/Max Age here)
                var results = _patientService.SearchPatients(filter);

                // 4. Sync Collection (Active only)
                Patients.Clear();
                // Use a basic filter to ensure we only show active patients in this view
                var activeResults = results.Where(x => x.IsArchived == false);

                foreach (var p in activeResults)
                {
                    // Ensure formatting is applied to the results
                    p.PhoneNo = FormatPhoneNumber(p.PhoneNo);
                    p.EmergencyContact = FormatPhoneNumber(p.EmergencyContact);
                    Patients.Add(p);
                }

                // 5. Update Visual State
                NoResultsFound = (Patients.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery));
            }
            catch (ArgumentException ex)
            {
                // This catches the "Min age > Max age" or "Negative age" errors 
                // thrown by the PatientService and shows them to the user.
                ShowAlertAction?.Invoke(ex.Message);
            }
        }

        // --- VM13: Clear/Reset ---
        private void ClearFilters()
        {
            // Reset the specific filter fields
            MinAge = null;
            MaxAge = null;
            SelectedSexFilter = null;

            // Optional: Also clear the search query to return to a 100% clean list
            SearchQuery = string.Empty;

            LoadAllPatients(); // Returns to the full active list
            NoResultsFound = false;
        }


        // --- VM15: Mark As Deceased ---
        private void MarkAsDeceased()
        {
            if (SelectedPatient == null) return;

            // 1. Trigger the Specialized Dialog (UI Callback)
            // We'll reuse our ShowDialog pattern to get the Date from the View
            DateTime? chosenDate = RequestDateAction?.Invoke("Enter Date of Death:", "Mark as Deceased");

            if (chosenDate == null) return; // User cancelled

            // 2. Validation: Cannot be in the future
            if (chosenDate > DateTime.Now)
            {
                ShowAlertAction?.Invoke("Date of death cannot be in the future.");
                return;
            }

            // 3. Validation: Cannot be before Date of Birth
            if (chosenDate < SelectedPatient.Dob)
            {
                ShowAlertAction?.Invoke("Date of death cannot be earlier than the Date of Birth.");
                return;
            }

            // 4. Update the Record
            SelectedPatient.Dod = chosenDate; // Setting the Date of Death
            SelectedPatient.IsArchived = true; // Securely move to locked archive state

            // 5. Call Service to Save
            _patientService.UpdatePatient(SelectedPatient);

            // 6. Refresh and Notify
            LoadAllPatients();
            LoadArchivedPatients();

            // This forces the "Edit" buttons to re-check if they should be disabled
            OnPropertyChanged(nameof(IsNotDeceased));

            ShowAlertAction?.Invoke($"{SelectedPatient.FirstName} has been marked as deceased. The record is now locked.");
        }       


        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}