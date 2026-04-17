using HospitalManagement.Entity;
using System.Collections.Generic;

namespace HospitalManagement.Service;
internal interface ITransplantService
{
    void AssignDonor(int transplantId, int donorId, float finalScore);
    void CreateWaitlistRequest(int receiverId, string organType);
    string? GetChronicWarning(int patientId);
    List<Transplant> GetTopMatchesForDonor(int donorId, string organType);
    bool IsUrgent(int patientId);
}