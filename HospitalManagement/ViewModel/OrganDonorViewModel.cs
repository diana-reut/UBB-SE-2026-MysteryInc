using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    internal class OrganDonorViewModel : INotifyPropertyChanged
    {
        private readonly TransplantService _transplantService;
        private readonly IPatientRepository _patientRepo;
        private readonly IMedicalHistoryRepository _historyRepo;

        // Deceased donor being processed
        private Patient _deceasedPatient;
        public Patient DeceasedPatient
        {
            get => _deceasedPatient;
            set { _deceasedPatient = value; OnPropertyChanged(); }
        }

        // UI: Organ selection
        private string _selectedOrgan;
        public string SelectedOrgan
        {
            get => _selectedOrgan;
            set
            {
                _selectedOrgan = value;
                OnPropertyChanged();
                LoadTopMatches();
            }
        }

        public ObservableCollection<string> Organs { get; }

        // UI: Top 5 matches displayed in DataGrid
        private ObservableCollection<TransplantMatch> _topMatches;
        public ObservableCollection<TransplantMatch> TopMatches
        {
            get => _topMatches;
            set { _topMatches = value; OnPropertyChanged(); }
        }

        // UI: Selected recipient from the grid
        private TransplantMatch _selectedMatch;
        public TransplantMatch SelectedMatch
        {
            get => _selectedMatch;
            set { _selectedMatch = value; OnPropertyChanged(); }
        }

        // UI: Loading state
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private string _loadingMessage;
        public string LoadingMessage
        {
            get => _loadingMessage;
            set { _loadingMessage = value; OnPropertyChanged(); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand ConfirmAssignmentCommand { get; }

        // Callback for when assignment is confirmed
        public Action<int, int, float> OnAssignmentConfirmed { get; set; }

        // Constructor
        public OrganDonorViewModel(TransplantService transplantService, IPatientRepository patientRepo, IMedicalHistoryRepository historyRepo)
        {
            _transplantService = transplantService ?? throw new ArgumentNullException(nameof(transplantService));
            _patientRepo = patientRepo ?? throw new ArgumentNullException(nameof(patientRepo));
            _historyRepo = historyRepo ?? throw new ArgumentNullException(nameof(historyRepo));

            // Initialize organs list
            Organs = new ObservableCollection<string>
            {
                "Heart",
                "Kidney",
                "Liver",
                "Pancreas",
                "Lung",
                "Cornea"
            };

            TopMatches = new ObservableCollection<TransplantMatch>();
            ConfirmAssignmentCommand = new RelayCommand(ConfirmAssignment);
        }

        // Load top 5 matches when an organ is selected
        private void LoadTopMatches()
        {
            if (DeceasedPatient == null || string.IsNullOrEmpty(SelectedOrgan))
            {
                TopMatches.Clear();
                return;
            }

            IsLoading = true;
            LoadingMessage = $"Finding compatible recipients for {SelectedOrgan}...";

            try
            {
                var matches = _transplantService.GetTopMatchesForDonor(DeceasedPatient.Id, SelectedOrgan);

                TopMatches.Clear();
                foreach (var transplant in matches)
                {
                    // Fetch the receiver patient and their medical history
                    var receiver = _patientRepo.GetById(transplant.ReceiverId);
                    var receiverHistory = receiver != null ? _historyRepo.GetByPatientId(receiver.Id) : null;
                    
                    var receiverName = receiver != null ? $"{receiver.FirstName} {receiver.LastName}" : "Unknown";
                    var bloodType = receiverHistory?.BloodType?.ToString() ?? "Unknown";
                    
                    var match = new TransplantMatch
                    {
                        TransplantId = transplant.TransplantId,
                        ReceiverId = transplant.ReceiverId,
                        ReceiverName = receiverName,
                        BloodType = bloodType,
                        CompatibilityScore = transplant.CompatibilityScore,
                        RequestDate = transplant.RequestDate,
                        WaitingDays = (DateTime.Now - transplant.RequestDate).Days
                    };
                    TopMatches.Add(match);
                }

                if (TopMatches.Count == 0)
                {
                    LoadingMessage = $"No compatible recipients found for {SelectedOrgan}.";
                }
                else
                {
                    LoadingMessage = "";
                }
            }
            catch (Exception ex)
            {
                LoadingMessage = $"Error loading matches: {ex.Message}";
                TopMatches.Clear();
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Confirm assignment of the selected match
        public void ConfirmAssignment()
        {
            if (SelectedMatch == null)
            {
                throw new InvalidOperationException("Please select a recipient to assign.");
            }

            // Find the full Transplant record to get the final score
            var matches = _transplantService.GetTopMatchesForDonor(DeceasedPatient.Id, SelectedOrgan);
            var selectedTransplant = matches.FirstOrDefault(t => t.ReceiverId == SelectedMatch.ReceiverId);

            if (selectedTransplant == null)
            {
                throw new InvalidOperationException("Selected recipient not found in matches.");
            }

            // Invoke callback
            OnAssignmentConfirmed?.Invoke(selectedTransplant.TransplantId, DeceasedPatient.Id, selectedTransplant.CompatibilityScore);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// View model representation of a transplant match for display in the DataGrid
    /// </summary>
    public class TransplantMatch
    {
        public int TransplantId { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string BloodType { get; set; }
        public float CompatibilityScore { get; set; }
        public DateTime RequestDate { get; set; }
        public int WaitingDays { get; set; }
    }
}
