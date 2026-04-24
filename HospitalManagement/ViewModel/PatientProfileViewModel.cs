using HospitalManagement.Entity;
using HospitalManagement.Integration.Export;
using HospitalManagement.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HospitalManagement.ViewModel;

internal class PatientProfileViewModel : INotifyPropertyChanged
{
    private Patient? _patient;
    private MedicalRecord? _selectedRecord;
    private readonly IPatientService _patientService;
    private readonly IImportService _importService;
    private readonly IExportService _exportService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Action<string, string>? ShowAlertAction { get; set; }

    public Action<string>? OpenFileAction { get; set; }

    public Func<int, Task>? ShowPrescriptionAction { get; set; }

    public Patient? CurrentPatient
    {
        get => _patient;

        set
        {
            if (_patient == value)
            {
                return;
            }

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
            if (_selectedRecord == value)
            {
                return;
            }

            _selectedRecord = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRecord));
        }
    }

    public string FormattedChronicConditions
    {
        get
        {
            List<string>? conditions = CurrentPatient?.MedicalHistory?.ChronicConditions;

            if (conditions is null || conditions.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", conditions);
        }
    }

    public string FormattedAllergies
    {
        get
        {
            List<(Allergy Allergy, string SeverityLevel)>? allergies = CurrentPatient?.MedicalHistory?.Allergies;
            if (allergies is null || allergies.Count == 0)
            {
                return "None";
            }

            var stringList = new List<string>();
            foreach ((Allergy Allergy, string SeverityLevel) item in allergies)
            {
                stringList.Add($"{item.Allergy.AllergyName} ({item.SeverityLevel})");
            }

            return string.Join(", ", stringList);
        }
    }

    public PatientProfileViewModel()
    {
        _patientService = (Application.Current as App)!.Services.GetRequiredService<IPatientService>();
        _exportService = (Application.Current as App)!.Services.GetRequiredService<IExportService>();
        _importService = (Application.Current as App)!.Services.GetRequiredService<IImportService>();

        // Initialize with empty state to prevent null reference bindings in XAML
        CurrentPatient = new Patient
        {
            MedicalHistory = new MedicalHistory { MedicalRecords = [], },
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

    public void CheckHighRiskStatus()
    {
        if (CurrentPatient is not null && _patientService.IsHighRiskPatient(CurrentPatient.Id))
        {
            ShowAlertAction?.Invoke("High Risk Patient Alert", "Warning: This patient is flagged as High Risk.");
        }
    }

    public void ExportSelectedRecord()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        try
        {
            string path = _exportService.ExportRecordToPDF(SelectedRecord.Id);
            OpenFileAction?.Invoke(path);
        }
        catch (Exception ex)
        {
            ShowAlertAction?.Invoke("Export Failed", ex.Message);
        }
    }

    public async Task ViewPrescriptionAsync()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        Prescription? prescription = _patientService.GetPrescriptionByRecordId(SelectedRecord.Id);

        if (prescription is not null)
        {
            if (ShowPrescriptionAction is null)
            {
                ShowAlertAction?.Invoke("Not Implemented", "The action to show prescriptions is not implemented.");
                return;
            }

            await ShowPrescriptionAction.Invoke(prescription.Id);
        }
        else
        {
            ShowAlertAction?.Invoke("No Prescription", "This consultation does not have an associated prescription.");
        }
    }

    public void ImportRecords(bool isER)
    {
        if (CurrentPatient is null)
        {
            return;
        }

        try
        {
            if (isER)
            {
                _importService.ImportFromER(CurrentPatient.Id, 1);
            }
            else
            {
                _importService.ImportFromAppointment(CurrentPatient.Id, 1);
            }

            LoadFullPatientProfile(CurrentPatient.Id);
            ShowAlertAction?.Invoke("Import Successful", "Records imported correctly.");
        }
        catch (Exception ex)
        {
            ShowAlertAction?.Invoke("Import Failed", ex.Message);
        }
    }
}
