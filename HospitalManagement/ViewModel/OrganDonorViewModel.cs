using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Entity;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel;

internal partial class OrganDonorViewModel : INotifyPropertyChanged
{
    internal class OrganDonorViewModel : INotifyPropertyChanged
    {
        private readonly ITransplantService _transplantService;
        private readonly IPatientRepository _patientRepo;
        private readonly IMedicalHistoryRepository _historyRepo;

    // Deceased donor being processed
    private Patient? _deceasedPatient;

    public Patient? DeceasedPatient
    {
        get => _deceasedPatient;

        set
        {
            _deceasedPatient = value;
            OnPropertyChanged();
        }
    }

    // UI: Organ selection
    private string? _selectedOrgan;

    public string? SelectedOrgan
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
    private ObservableCollection<TransplantMatch>? _topMatches;

    public ObservableCollection<TransplantMatch>? TopMatches
    {
        get => _topMatches;

        set
        {
            _topMatches = value;
            OnPropertyChanged();
        }
    }

    // UI: Selected recipient from the grid
    private TransplantMatch? _selectedMatch;

    public TransplantMatch? SelectedMatch
    {
        get => _selectedMatch;

        set
        {
            _selectedMatch = value;
            OnPropertyChanged();
        }
    }

    // UI: Loading state
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;

        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    private string? _loadingMessage;

    public string? LoadingMessage
    {
        get => _loadingMessage;

        set
        {
            _loadingMessage = value;
            OnPropertyChanged();
        }
    }

    private string? _errorMessage;

    public string? ErrorMessage
    {
        get => _errorMessage;

        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    // Commands
    public ICommand ConfirmAssignmentCommand { get; }

    // Callback for when assignment is confirmed
    public Action<int, int, float>? OnAssignmentConfirmed { get; set; }

        // Constructor
        public OrganDonorViewModel(ITransplantService transplantService, IPatientRepository patientRepo, IMedicalHistoryRepository historyRepo)
        {
            _transplantService = transplantService ?? throw new ArgumentNullException(nameof(transplantService));
        _patientRepo = patientRepo ?? throw new ArgumentNullException(nameof(patientRepo));
        _historyRepo = historyRepo ?? throw new ArgumentNullException(nameof(historyRepo));

        // Initialize organs list
        Organs =
        [
            "Heart",
            "Kidney",
            "Liver",
            "Pancreas",
            "Lung",
            "Cornea"
        ];

        TopMatches = [];
        ConfirmAssignmentCommand = new RelayCommand(ConfirmAssignment);
    }

    // Load top 5 matches when an organ is selected
    private void LoadTopMatches()
    {
        if (DeceasedPatient is null || string.IsNullOrEmpty(SelectedOrgan))
        {
            TopMatches?.Clear();
            return;
        }

        IsLoading = true;
        LoadingMessage = $"Finding compatible recipients for {SelectedOrgan}...";

        try
        {
            System.Collections.Generic.List<Transplant> matches = _transplantService.GetTopMatchesForDonor(DeceasedPatient.Id, SelectedOrgan);

            TopMatches?.Clear();
            foreach (Transplant transplant in matches)
            {
                // Fetch the receiver patient and their medical history
                Patient? receiver = _patientRepo.GetById(transplant.ReceiverId);
                MedicalHistory? receiverHistory = receiver is not null ? _historyRepo.GetByPatientId(receiver.Id) : null;
                string receiverName = receiver is not null ? $"{receiver.FirstName} {receiver.LastName}" : "Unknown";
                string bloodType = receiverHistory?.BloodType?.ToString() ?? "Unknown";

                var match = new TransplantMatch
                {
                    TransplantId = transplant.TransplantId,
                    ReceiverId = transplant.ReceiverId,
                    ReceiverName = receiverName,
                    BloodType = bloodType,
                    CompatibilityScore = transplant.CompatibilityScore,
                    RequestDate = transplant.RequestDate,
                    WaitingDays = (DateTime.Now - transplant.RequestDate).Days,
                };
                TopMatches?.Add(match);
            }

            if (TopMatches?.Count == 0)
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
            TopMatches?.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Confirm assignment of the selected match
    public void ConfirmAssignment()
    {
        if (SelectedMatch is null)
        {
            throw new InvalidOperationException("Please select a recipient to assign.");
        }

        // Find the full Transplant record to get the final score
        System.Collections.Generic.List<Transplant> matches = _transplantService.GetTopMatchesForDonor(DeceasedPatient!.Id, SelectedOrgan!);
        Transplant? selectedTransplant = matches.FirstOrDefault(t => t.ReceiverId == SelectedMatch.ReceiverId) ?? throw new InvalidOperationException("Selected recipient not found in matches.");

        // Invoke callback
        OnAssignmentConfirmed?.Invoke(selectedTransplant.TransplantId, DeceasedPatient.Id, selectedTransplant.CompatibilityScore);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
