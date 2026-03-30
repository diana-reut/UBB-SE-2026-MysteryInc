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

        public BloodCompatibilityService(PatientRepository patientRepo)
        {
            _patientRepo = patientRepo;
        }

        /// <summary>
        /// SV20: Implements the top compatible donors search.
        /// Orchestrates hard filters and point scoring to rank donors.
        /// </summary>
        public List<Patient> GetTopCompatibleDonors(int recipientId)
        {

            var recipient = _patientRepo.GetById(recipientId);

            //THE NULL CHECK: 
            // If we don't know who the recipient is, or they don't have blood info, 
            // we cannot find a match.
            if (recipient == null ||
                recipient.MedicalHistory == null ||
                recipient.MedicalHistory.BloodType == null ||
                recipient.MedicalHistory.Rh == null)
            {
                // Return an empty list as required for "No matches" scenarios
                return new List<Patient>();
            }

            //Fetch all potential donors (Active & Non-Deceased)
            var allPatients = _patientRepo.GetAll(include_archived: false);

            var rankedDonors = new List<(Patient Donor, int Score)>();

            foreach (var donor in allPatients)
            {
                //Basic exclusions
                if (donor.Id == recipientId) continue;
                if (donor.MedicalHistory == null) continue;
                if (donor.IsDonor == false) continue;


                // --- HARD FILTERS (RP5 / SV20) ---

                //check if we have information about their bloodtype
                if (donor.MedicalHistory.BloodType == null || donor.MedicalHistory.Rh == null) continue;

                // Blood Type Compatibility
                if (!IsBloodMatch(donor.MedicalHistory.BloodType, recipient.MedicalHistory.BloodType!.Value)) continue;

                // Rh Factor Compatibility
                if (!IsRhMatch(donor.MedicalHistory.Rh, recipient.MedicalHistory.Rh!.Value)) continue;

                // Chronic Conditions (Exclude immediately if "True")
                if (donor.MedicalHistory.ChronicConditions != null && donor.MedicalHistory.ChronicConditions.Any()) continue;

                // Severe Allergies (Exclude if Anaphylactic)
                if (donor.MedicalHistory.Allergies != null &&
                    donor.MedicalHistory.Allergies.Any(a => a.SeverityLevel.Equals("Anaphylactic", StringComparison.OrdinalIgnoreCase)))
                    continue;

                // --- POINT SCORING (SV20) ---
                int currentScore = CalculateScore(donor, recipient);
                rankedDonors.Add((donor, currentScore));
            }

            // 3. Return Top 20 ranked by score
            return rankedDonors
                .OrderByDescending(x => x.Score)
                .Select(x => x.Donor)
                .Take(20)
                .ToList();
        }

        private int CalculateScore(Patient donor, Patient recipient)
        {
            int total = 0;

            // Blood Type Match (50 points identical / 25 points compatible)
            if (donor.MedicalHistory.BloodType == recipient.MedicalHistory.BloodType &&
                donor.MedicalHistory.Rh == recipient.MedicalHistory.Rh)
                total += 50;
            else
                total += 25;

            // Age Proximity (30 points max)
            // Decrease by 5 for every 5-year gap
            int ageGap = Math.Abs(donor.Dob.Year - recipient.Dob.Year);
            int agePoints = 30 - ((ageGap / 5) * 5);
            total += Math.Max(0, agePoints);

            // Sex Match (20 points same / 10 points different)
            total += (donor.Sex == recipient.Sex) ? 20 : 10;

            return total;
        }

        private bool IsBloodMatch(BloodType? donor, BloodType receiver)
        {
            if (donor == null) return false;
            if (donor == BloodType.O) return true;
            if (donor == BloodType.A && (receiver == BloodType.A || receiver == BloodType.AB)) return true;
            if (donor == BloodType.B && (receiver == BloodType.B || receiver == BloodType.AB)) return true;
            if (donor == BloodType.AB && receiver == BloodType.AB) return true;
            return false;
        }

        private bool IsRhMatch(RhEnum? donor, RhEnum receiver)
        {
            if (donor == null) return false;
            // Rh- can only receive from Rh-
            if (receiver == RhEnum.Negative) return donor == RhEnum.Negative;
            // Rh+ can receive from anyone
            return true;
        }
    }
}