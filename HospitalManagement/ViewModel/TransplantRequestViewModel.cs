using HospitalManagement.Service;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HospitalManagement.ViewModel;

internal class TransplantRequestViewModel : INotifyPropertyChanged
{
    private readonly ITransplantService _transplantService;
    private readonly IPatientService _patientService;

    private int _patientId;

    public string PatientName { get; set; } = null!;

    public bool IsUrgent { get; set; }

    public string? WarningMessage { get; set; }

    public Visibility UrgentVisibility
        => IsUrgent ? Visibility.Visible : Visibility.Collapsed;

    public Visibility WarningVisibility
        => !string.IsNullOrEmpty(WarningMessage)
            ? Visibility.Visible
            : Visibility.Collapsed;

    private string? _selectedOrgan;

    public string? SelectedOrgan
    {
        get => _selectedOrgan;

        set
        {
            if (_selectedOrgan == value)
                return;

            _selectedOrgan = value;
            OnPropertyChanged();
        }
    }


    private string? _errorMessage;

    public string? ErrorMessage
    {
        get => _errorMessage;

        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private bool _requestSucceeded;

    public bool RequestSucceeded
    {
        get => _requestSucceeded;

        private set
        {
            _requestSucceeded = value;
            OnPropertyChanged();
        }
    }

    public TransplantRequestViewModel(
        ITransplantService transplantService,
        IPatientService patientService)
    {
        _transplantService = transplantService;
        _patientService = patientService;
    }

    public void Initialize(int patientId)
    {
        _patientId = patientId;

        var patient = _patientService.GetById(patientId);

        if (patient != null)
            PatientName = $"{patient.FirstName} {patient.LastName}";

        IsUrgent = _transplantService.IsUrgent(patientId);
        WarningMessage = _transplantService.GetChronicWarning(patientId);

        OnPropertyChanged(nameof(PatientName));
    }

    public void SubmitRequest()
    {
        ErrorMessage = null;
        RequestSucceeded = false;

        try
        {
            if (string.IsNullOrEmpty(SelectedOrgan))
            {
                ErrorMessage = "Please select an organ type from the dropdown.";
                return;
            }

            _transplantService.CreateWaitlistRequest(_patientId, SelectedOrgan);

            RequestSucceeded = true;
        }
        catch (MyNotImplementedException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
