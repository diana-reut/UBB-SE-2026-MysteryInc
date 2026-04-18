using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitalManagement.Database;
using HospitalManagement.Entity;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel;

internal class PatientProfileViewModel : INotifyPropertyChanged
{
    private Patient? _patient;
    private MedicalRecord? _selectedRecord;
    private readonly IPatientService _patientService;

    public Patient? CurrentPatient
    {
        get => _patient;

        set
        {
            _patient = value;
            OnPropertyChanged();
            // Notify UI that the formatted lists have updated
            OnPropertyChanged(nameof(FormattedChronicConditions));
            OnPropertyChanged(nameof(FormattedAllergies));
        }
    }

    // Holds the record selected when double-clicking the list
    public MedicalRecord? SelectedRecord
    {
        get => _selectedRecord;

        set
        {
            _selectedRecord = value;
            OnPropertyChanged();
        }
    }



    // Formats the List<string> into readable text
    public string FormattedChronicConditions
    {
        get
        {
            if (CurrentPatient?.MedicalHistory?.ChronicConditions is null || CurrentPatient.MedicalHistory.ChronicConditions.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", CurrentPatient.MedicalHistory.ChronicConditions);
        }
    }

    // Formats the Tuple List<(Allergy, Severity)> into readable text
    public string FormattedAllergies
    {
        get
        {
            if (CurrentPatient?.MedicalHistory?.Allergies is null || CurrentPatient.MedicalHistory.Allergies.Count == 0)
            {
                return "None";
            }

            var stringList = new List<string>();
            foreach ((Allergy Allergy, string SeverityLevel) item in CurrentPatient.MedicalHistory.Allergies)
            {
                // Changed item.Allergy.Name to item.Allergy.AllergyName
                stringList.Add($"{item.Allergy.AllergyName} ({item.SeverityLevel})");
            }

            return string.Join(", ", stringList);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PatientProfileViewModel(int patientId)
    {
        using var dbContext = new HospitalDbContext();
        var patientRepo = new PatientRepository(dbContext);
        var historyRepo = new MedicalHistoryRepository(dbContext);
        var recordRepo = new MedicalRecordRepository(dbContext);

        _patientService = new PatientService(patientRepo, historyRepo, recordRepo);

        // Dummy payload to prevent XAML crash
        CurrentPatient = new Patient
        {
            MedicalHistory = new MedicalHistory
            {
                MedicalRecords = [],
            },
        };

        LoadFullPatientProfile(patientId);
    }

    public void LoadFullPatientProfile(int id)
    {
        try
        {
            Patient? p = _patientService.GetPatientDetails(id);
            if (p is not null)
            {
                p.MedicalHistory ??= new MedicalHistory();
                if (p.MedicalHistory.MedicalRecords is null)
                {
                    p.MedicalHistory.MedicalRecords = [];
                }

                CurrentPatient = p;
            }
        }
        catch (Exception ex)
        {
            // Keep the dummy data if the database completely fails
            Console.WriteLine(ex);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
