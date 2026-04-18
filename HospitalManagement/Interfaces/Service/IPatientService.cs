using HospitalManagement.Entity;
using HospitalManagement.Integration;
using System;
using System.Collections.Generic;

namespace HospitalManagement.Interfaces.Service;

internal interface IPatientService
{
    public Patient CreatePatient(Patient data);

    public void UpdatePatient(Patient data);

    public void ArchivePatient(int id);

    public void DearchivePatient(int id);

    public void ArchiveAsDeceased(int id, DateTime deathDate);

    public List<Patient> SearchPatients(PatientFilter filter);

    public void CreateMedicalHistory(int patientId, MedicalHistory history);

    public Patient GetPatientDetails(int id);

    public bool IsHighRiskPatient(int patientId);

    public void DeletePatient(int id);

    public bool Exists(string cnp);

    public MedicalHistory? GetMedicalHistory(int patientId);

    public List<MedicalRecord> GetMedicalRecords(int historyId);

    public List<string> GetPatientAllergies(int patientId);

    public Prescription? GetPrescriptionByRecordId(int recordId);
}
