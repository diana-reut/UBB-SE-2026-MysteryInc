using System;
using System.Collections.Generic;
using System.Linq;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Repository;

namespace HospitalManagement.Service
{
    public class TransplantService
    {
        private readonly TransplantRepository _transplantRepo;
        private readonly PatientRepository _patientRepo;
        private readonly MedicalRecordRepository _recordRepo;
        private readonly BloodCompatibilityService _compatibilityService;

        public TransplantService(
            TransplantRepository transplantRepo,
            PatientRepository patientRepo,
            MedicalRecordRepository recordRepo,
            BloodCompatibilityService compatibilityService)
        {
            _transplantRepo = transplantRepo;
            _patientRepo = patientRepo;
            _recordRepo = recordRepo;
            _compatibilityService = compatibilityService;
        }

        // VM40: Create the initial 'Waiting' request
        public void CreateWaitlistRequest(int receiverId, string organType)
        {
            var receiver = _patientRepo.GetById(receiverId);
            if (receiver == null) throw new ArgumentException("Receiver not found.");

            var request = new Transplant
            {
                ReceiverId = receiverId,
                DonorId = null, //null at first till we find the donor we need
                OrganType = organType,
                RequestDate = DateTime.Now,
                Status = TransplantStatus.Pending, // Maps to 'Waiting' in the DB
                CompatibilityScore = 0
            };

            _transplantRepo.Add(request);
        }

        // VM38: Logic to find the top 5 matches for a deceased donor
        public List<Transplant> GetTopMatchesForDonor(int donorId, string organType)
        {
            var donor = _patientRepo.GetById(donorId);
            if (donor == null || !donor.IsDeceased || !donor.IsDonor)
                throw new InvalidOperationException("Donor must be deceased and registered.");

            var waitlist = _transplantRepo.GetWaitingByOrgan(organType);
            var scoredMatches = new List<Transplant>();

            foreach (var request in waitlist)
            {
                var receiver = _patientRepo.GetById(request.ReceiverId);
                if (receiver == null) continue;
                if (receiver?.MedicalHistory?.BloodType == null || receiver.MedicalHistory.Rh == null)
                    continue;

                // 1. Apply Hard Filters (Blood & Rh compatibility)
                if (!_compatibilityService.IsBloodMatch(donor.MedicalHistory?.BloodType, receiver.MedicalHistory.BloodType.Value)) continue;
                if (!_compatibilityService.IsRhMatch(donor.MedicalHistory?.Rh, receiver.MedicalHistory.Rh.Value)) continue;

                // 2. Exclude if recipient has chronic conditions affecting success
                if (receiver.MedicalHistory.ChronicConditions != null && receiver.MedicalHistory.ChronicConditions.Any()) continue;

                // 3. Calculate Score (Blood + Age + Sex + Urgency)
                request.CompatibilityScore = CalculatePostMortemScore(donor, receiver);
                scoredMatches.Add(request);
            }

            return scoredMatches
                .OrderByDescending(t => t.CompatibilityScore)
                .ThenBy(t => t.RequestDate) // Fairness rule: longest waiting first
                .Take(5)
                .ToList();
        }

        // VM38: Admin confirms the match from the list a.k.a assigns the donor
        public void AssignDonor(int transplantId, int donorId, float finalScore)
        {
            _transplantRepo.Update(transplantId, donorId, finalScore);
        }

        private float CalculatePostMortemScore(Patient donor, Patient receiver)
        {
            // Base compatibility (Blood/Age/Sex) - Max 100
            float score = _compatibilityService.CalculateScore(donor, receiver);

            // Add Medical Urgency points (SV20 / RP5)
            // 10+ ER visits in last 3 months = 20 points, otherwise 5
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);
            int erVisits = _recordRepo.GetERVisitCount(receiver.Id, threeMonthsAgo);

            score += (erVisits >= 10) ? 20 : 5;

            return score;
        }


        //added these functions to make MVVM easier, if needed, move them to model-view logic

        // Logic for VM40: Triggers the red "URGENT PRIORITY" label in VW24
        public bool IsUrgent(int patientId)
        {
            // Threshold: More than 10 ER visits in the last 3 months
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);
            int erVisits = _recordRepo.GetERVisitCount(patientId, threeMonthsAgo);

            return erVisits > 10;
        }

        // Logic for VM40: Triggers the yellow "Warning Box" in VW24
        public string? GetChronicWarning(int patientId)
        {
            var patient = _patientRepo.GetById(patientId);

            // Check if the patient has any chronic conditions listed in their history
            if (patient?.MedicalHistory?.ChronicConditions != null &&
                patient.MedicalHistory.ChronicConditions.Any())
            {
                return "Patient has underlying conditions that may affect transplant success.";
            }

            return null;
        }

    }
}