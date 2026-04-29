using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Entity;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel;

internal partial class OrganDonorDialogViewModel : INotifyPropertyChanged
{
    private readonly ITransplantService _transplantService;

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

    public Action<int, int, float>? OnAssignmentConfirmed { get; set; }

    public OrganDonorDialogViewModel(ITransplantService transplantService)
    {
        _transplantService = transplantService ?? throw new ArgumentNullException(nameof(transplantService));

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
    }

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
            System.Collections.Generic.List<TransplantMatch> matches =
                _transplantService.GetTopMatchesAsDisplayModels(DeceasedPatient.Id, SelectedOrgan);

            TopMatches?.Clear();
            foreach (TransplantMatch match in matches)
            {
                TopMatches?.Add(match);
            }

            LoadingMessage = TopMatches?.Count == 0
                ? $"No compatible recipients found for {SelectedOrgan}."
                : "";
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

    public bool TryConfirmAssignment(out string? error)
    {
        error = null;

        if (SelectedMatch is null)
        {
            error = "Please select a recipient from the list before confirming.";
            return false;
        }

        if (string.IsNullOrEmpty(SelectedOrgan))
        {
            error = "Please select an organ before confirming.";
            return false;
        }

        try
        {
            _transplantService.AssignDonor(SelectedMatch.TransplantId, DeceasedPatient!.Id, SelectedMatch.CompatibilityScore);
            OnAssignmentConfirmed?.Invoke(SelectedMatch.TransplantId, DeceasedPatient.Id, SelectedMatch.CompatibilityScore);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Error: {ex.Message}";
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}