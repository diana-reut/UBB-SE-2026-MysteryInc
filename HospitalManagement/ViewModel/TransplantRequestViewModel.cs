using HospitalManagement.Service;
using Microsoft.Extensions.DependencyInjection;
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


    public Visibility UrgentVisibility => IsUrgent ? Visibility.Visible : Visibility.Collapsed;

    public Visibility WarningVisibility => !string.IsNullOrEmpty(WarningMessage) ? Visibility.Visible : Visibility.Collapsed;

    private string? _selectedOrgan;

    public string? SelectedOrgan
    {
        get => _selectedOrgan;

        set
        {
            if (_selectedOrgan != value)
            {
                _selectedOrgan = value;
                OnPropertyChanged();
            }
        }
    }

    public TransplantRequestViewModel()
    {
        _transplantService = (Application.Current as App)!.Services.GetRequiredService<ITransplantService>();
        _patientService = (Application.Current as App)!.Services.GetRequiredService<IPatientService>();
    }

    public void Initialize(int patientId)
    {
        _patientId = patientId;
        Entity.Patient patient = _patientService.GetById(patientId);
        if (patient is not null)
        {
            PatientName = $"{patient.FirstName} {patient.LastName}";
        }

        IsUrgent = _transplantService.IsUrgent(patientId);
        WarningMessage = _transplantService.GetChronicWarning(patientId);

        OnPropertyChanged(nameof(PatientName));
        OnPropertyChanged(nameof(IsUrgent));
        OnPropertyChanged(nameof(UrgentVisibility));
        OnPropertyChanged(nameof(WarningMessage));
        OnPropertyChanged(nameof(WarningVisibility));
    }

    public void SubmitRequest()
    {
        if (string.IsNullOrEmpty(SelectedOrgan))
        {
            throw new MyNotImplementedException("Please select an organ type from the dropdown.");
        }

        _transplantService.CreateWaitlistRequest(_patientId, SelectedOrgan);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
