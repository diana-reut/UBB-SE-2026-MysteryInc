using HospitalManagement.Entity;
using System.Collections.Generic;

namespace HospitalManagement.Service;
internal interface IAddictDetectionService
{
    string BuildPoliceReport(Patient patient);
    List<Patient> GetAddictCandidates();
    string GetChronicConditions(int patientId);
}