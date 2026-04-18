using HospitalManagement.Entity;
using System.Collections.ObjectModel;

namespace HospitalManagement.Service;

internal interface IAddictDetectionService
{
    string BuildPoliceReport(Patient patient);

    Collection<Patient> GetAddictCandidates();

    string GetChronicConditions(int patientId);
}