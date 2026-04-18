using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using System.Collections.Generic;

namespace HospitalManagement.Service;
internal interface IBloodCompatibilityService
{
    int CalculateScore(Patient donor, Patient recipient);
    List<Patient> GetTopCompatibleDonors(int recipientId);
    bool IsBloodMatch(BloodType? donor, BloodType receiver);
    bool IsRhMatch(RhEnum? donor, RhEnum receiver);
}