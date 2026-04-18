using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Integration;
using System;
using System.Collections.Generic;

namespace HospitalManagement.Service;
internal interface IPatientService
{
    void ArchiveAsDeceased(int id, DateTime deathDate);
    void ArchivePatient(int id);
    void CreateMedicalHistory(int patientId, MedicalHistory history, List<Allergy> allergies);
    Patient CreatePatient(Patient data);
    void DearchivePatient(int id);
    void DeletePatient(int id);
    bool Exists(string cnp);
    MedicalHistory? GetMedicalHistory(int patientId);
    List<MedicalRecord> GetMedicalRecords(int historyId);
    List<string> GetPatientAllergies(int patientId);
    Patient GetPatientDetails(int id);
    Prescription? GetPrescriptionByRecordId(int recordId);
    bool IsHighRiskPatient(int patientId);
    List<Patient> SearchPatients(PatientFilter filter);
    void UpdatePatient(Patient data);
    bool ValidateCNP(string cnp, Sex sex, DateTime dob);
}