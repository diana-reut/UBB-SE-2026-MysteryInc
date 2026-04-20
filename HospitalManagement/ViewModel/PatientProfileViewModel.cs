using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitalManagement.Entity;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel;

internal class PatientProfileViewModel : INotifyPropertyChanged
{
    private Patient? _patient;
    private MedicalRecord? _selectedRecord;
    private readonly IPatientService _patientService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Patient? CurrentPatient
    {
        get => _patient;
        set
        {
            _patient = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FormattedChronicConditions));
            OnPropertyChanged(nameof(FormattedAllergies));
        }
    }

    public MedicalRecord? SelectedRecord
    {
        get => _selectedRecord;
        set
        {
            _selectedRecord = value;
            OnPropertyChanged();
        }
    }

    public string FormattedChronicConditions
    {
        get
        {
            var conditions = CurrentPatient?.MedicalHistory?.ChronicConditions;
            if (conditions is null || conditions.Count == 0) return "None";
            return string.Join(", ", conditions);
        }
    }

    public string FormattedAllergies
    {
        get
        {
            var allergies = CurrentPatient?.MedicalHistory?.Allergies;
            if (allergies is null || allergies.Count == 0) return "None";

            var stringList = new List<string>();
            foreach (var item in allergies)
            {
                stringList.Add($"{item.Allergy.AllergyName} ({item.SeverityLevel})");
            }
            return string.Join(", ", stringList);
        }
    }

    // FIXED: Constructor now only takes the Service via Dependency Injection
    public PatientProfileViewModel(IPatientService patientService)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

        // Initialize with empty state to prevent null reference bindings in XAML
        CurrentPatient = new Patient
        {
            MedicalHistory = new MedicalHistory { MedicalRecords = [] }
        };
    }

    // This is called by the View after it has been created
    public void LoadFullPatientProfile(int id)
    {
        try
        {
            Patient? p = _patientService.GetPatientDetails(id);
            if (p is not null)
            {
                p.MedicalHistory ??= new MedicalHistory();
                p.MedicalHistory.MedicalRecords ??= [];
                CurrentPatient = p;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading patient {id}: {ex.Message}");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}