using HospitalManagement.Entity;

namespace HospitalManagement.Interfaces.Service;

internal interface IPatientValidator
{
    public void ValidateUpdate(Patient newDetails, Patient existingPatient);

    public ValidationResult ValidatePatient(Patient patient);

    public ValidationResult ValidateMedicalHistory(MedicalHistory history, MedicalHistory? existingHistory = null);
}
