using System;
using System.Collections.Generic;
using System.Linq;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Repository;

namespace HospitalManagement.Service
{
    public class BloodCompatibilityService
    {
        private readonly PatientRepository _patientRepo;
        // 1. Add the History Repository
        private readonly MedicalHistoryRepository _historyRepo;

        public BloodCompatibilityService(PatientRepository patientRepo, MedicalHistoryRepository historyRepo)
        {
            _patientRepo = patientRepo;
            _historyRepo = historyRepo;
        }

        public List<Patient> GetTopCompatibleDonors(int recipientId)
        {
            var recipient = _patientRepo.GetById(recipientId);

            // 2. Actually fetch the Recipient's Medical History from the Database
            if (recipient != null)
            {
                recipient.MedicalHistory = _historyRepo.GetByPatientId(recipientId);
            }

            if (recipient == null ||
                recipient.MedicalHistory == null ||
                recipient.MedicalHistory.BloodType == null ||
                recipient.MedicalHistory.Rh == null)
            {
                return new List<Patient>();
            }

            var allPatients = _patientRepo.GetAll(include_archived: false);
            var rankedDonors = new List<(Patient Donor, int Score)>();

            foreach (var donor in allPatients)
            {
                if (donor.Id == recipientId) continue;
                if (donor.IsDonor == false) continue;

                // 3. Actually fetch the Donor's Medical History from the Database
                donor.MedicalHistory = _historyRepo.GetByPatientId(donor.Id);

                // Now their strict rules will actually work!
                if (donor.MedicalHistory == null) continue;
                if (donor.MedicalHistory.BloodType == null || donor.MedicalHistory.Rh == null) continue;

                if (!IsBloodMatch(donor.MedicalHistory.BloodType, recipient.MedicalHistory.BloodType!.Value)) continue;
                if (!IsRhMatch(donor.MedicalHistory.Rh, recipient.MedicalHistory.Rh!.Value)) continue;

                if (donor.MedicalHistory.ChronicConditions != null && donor.MedicalHistory.ChronicConditions.Any()) continue;

                if (donor.MedicalHistory.Allergies != null &&
                    donor.MedicalHistory.Allergies.Any(a => a.SeverityLevel.Equals("Anaphylactic", StringComparison.OrdinalIgnoreCase)))
                    continue;

                int currentScore = CalculateScore(donor, recipient);
                rankedDonors.Add((donor, currentScore));
            }

            return rankedDonors
                .OrderByDescending(x => x.Score)
                .Select(x => x.Donor)
                .Take(20)
                .ToList();
        }

        public int CalculateScore(Patient donor, Patient recipient)
        {
            int total = 0;

            if (donor.MedicalHistory.BloodType == recipient.MedicalHistory.BloodType &&
                donor.MedicalHistory.Rh == recipient.MedicalHistory.Rh)
                total += 50;
            else
                total += 25;

            int ageGap = Math.Abs(donor.Dob.Year - recipient.Dob.Year);
            int agePoints = 30 - ((ageGap / 5) * 5);
            total += Math.Max(0, agePoints);

            total += (donor.Sex == recipient.Sex) ? 20 : 10;

            return total;
        }

        public bool IsBloodMatch(BloodType? donor, BloodType receiver)
        {
            if (donor == null) return false;
            if (donor == BloodType.O) return true;
            if (donor == BloodType.A && (receiver == BloodType.A || receiver == BloodType.AB)) return true;
            if (donor == BloodType.B && (receiver == BloodType.B || receiver == BloodType.AB)) return true;
            if (donor == BloodType.AB && receiver == BloodType.AB) return true;
            return false;
        }

        public bool IsRhMatch(RhEnum? donor, RhEnum receiver)
        {
            if (donor == null) return false;
            if (receiver == RhEnum.Negative) return donor == RhEnum.Negative;
            return true;
        }
    }
}