using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using Microsoft.UI.Xaml;

namespace HospitalManagement.ViewModel;

internal class TransplantRequestViewModel : INotifyPropertyChanged
{
    private readonly TransplantService _transplantService;
    private readonly int _patientId;

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

    public TransplantRequestViewModel(int patientId)
    {
        _patientId = patientId;

        using var dbContext = new HospitalDbContext();
        var patientRepo = new PatientRepository(dbContext);
        var historyRepo = new MedicalHistoryRepository(dbContext);
        var recordRepo = new MedicalRecordRepository(dbContext);
        var transplantRepo = new TransplantRepository(dbContext);

        var bloodService = new BloodCompatibilityService(patientRepo, historyRepo);
        _transplantService = new TransplantService(transplantRepo, patientRepo, recordRepo, bloodService, historyRepo);

        Entity.Patient? patient = patientRepo.GetById(patientId);
        if (patient is not null)
        {
            PatientName = $"{patient.FirstName} {patient.LastName}";
        }

        IsUrgent = _transplantService.IsUrgent(patientId);
        WarningMessage = _transplantService.GetChronicWarning(patientId);
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
