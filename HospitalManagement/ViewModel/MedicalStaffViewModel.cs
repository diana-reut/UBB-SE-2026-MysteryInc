using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Entity;
using HospitalManagement.Integration;
using HospitalManagement.Service;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using Microsoft.UI.Xaml;

namespace HospitalManagement.ViewModel
{
    public class MedicalStaffViewModel : INotifyPropertyChanged
    {
        private string _searchQuery = string.Empty;
        private string _errorMessage = string.Empty;
        private ObservableCollection<Patient> _searchResults = new ObservableCollection<Patient>();
        private readonly PatientService _patientService;
        private Patient _selectedPatient;

        public Patient SelectedPatient
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
            set { _searchQuery = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Patient> SearchResults
        {
            get => _searchResults;
            set { _searchResults = value; OnPropertyChanged(); }
        }

        public ICommand SearchCommand { get; set; }
        public ICommand BackToMainCommand { get; set; }
        public ICommand GhostSightingCommand { get; set; }

        // NEW COMMANDS FOR MEDICAL STAFF ACTIONS
        public ICommand FindBloodDonorsCommand { get; set; }
        public ICommand RequestTransplantCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MedicalStaffViewModel()
        {
            var dbContext = new HospitalDbContext();
            var patientRepo = new PatientRepository(dbContext);
            var historyRepo = new MedicalHistoryRepository(dbContext);
            var recordRepo = new MedicalRecordRepository(dbContext);

            _patientService = new PatientService(patientRepo, historyRepo, recordRepo);

            SearchCommand = new RelayCommand(ExecuteSearch);

            // INITIALIZE THE NEW COMMANDS
            FindBloodDonorsCommand = new RelayCommand(FindBloodDonors);
            RequestTransplantCommand = new RelayCommand(RequestTransplant);
        }

        private void ExecuteSearch()
        {
            ErrorMessage = string.Empty;
            SearchResults.Clear();

            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            var filter = new PatientFilter();

            if (SearchQuery.Length == 13 && SearchQuery.All(char.IsDigit))
            {
                filter.CNP = SearchQuery;
            }
            else
            {
                filter.namePart = SearchQuery;
            }

            try
            {
                var results = _patientService.SearchPatients(filter);

                if (results == null || results.Count == 0)
                {
                    ErrorMessage = "There are no patients with this name or CNP.";
                }
                else
                {
                    foreach (var p in results)
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
            if (SelectedPatient == null) return;

            // Create the new Window
            var donorsWindow = new Window();
            donorsWindow.Title = $"Compatible Donors - {SelectedPatient.FirstName} {SelectedPatient.LastName}";

            // Launch your brand new page!
            var donorsPage = new HospitalManagement.View.BloodDonorsView(SelectedPatient.Id);
            donorsWindow.Content = donorsPage;
            donorsWindow.Activate();
        }

        private void RequestTransplant()
        {
            if (SelectedPatient == null) return;

            var requestWindow = new Window();
            requestWindow.Title = $"Organ Transplant Request - {SelectedPatient.FirstName} {SelectedPatient.LastName}";

            // Launch the correctly named page!
            var requestPage = new HospitalManagement.View.TransplantRequestView(SelectedPatient.Id, requestWindow);

            requestWindow.Content = requestPage;
            requestWindow.Activate();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}