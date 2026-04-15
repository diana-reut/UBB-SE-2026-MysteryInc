using HospitalManagement.Entity.DTOs;

namespace HospitalManagement.Integration.PatientObserver;

public interface IPatientObserver
{
    public void OnNewExternalPatient(ExternalPatientDTO newPatientData);
}
