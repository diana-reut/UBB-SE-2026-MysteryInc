using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Entity;
using HospitalManagement.Repository;

namespace HospitalManagement.Service
{
    internal class TransplantService
    {
        private readonly TransplantRepository _transplantRepo;
        private readonly PatientRepository _patientRepo;
        private readonly BloodCompatibilityService _compatibilityService;

        public TransplantService(
            TransplantRepository transplantRepo,
            PatientRepository patientRepo,
            BloodCompatibilityService compatibilityService)
        {
            _transplantRepo = transplantRepo;
            _patientRepo = patientRepo;
            _compatibilityService = compatibilityService;
        }

        /// <summary>
        /// VM40: Request Transplant (Staff Module)
        /// Handles the initial creation of an organ request.
        /// </summary>
        public void CreateTransplantRequest(int receiverId, string organType)
        {
            var receiver = _patientRepo.GetById(receiverId);
            if (receiver == null)
                throw new ArgumentException("Receiver patient not found.");

            // Create the initial request record
            // RP23: Status starts as 'Waiting' (Pending in this Enum) and DonorID is null
            var request = new Transplant
            {
                ReceiverId = receiverId,
                DonorId = null,
                OrganType = organType,
                RequestDate = DateTime.Now,
                Status = TransplantStatus.Pending,
                CompatibilityScore = 0
            };

            _transplantRepo.Add(request);
        }

        /// <summary>
        /// VM38: Match Donor Command (Admin/Post-Mortem)
        /// Ranks potential recipients for a deceased donor.
        /// </summary>
        public List<(Transplant Request, float Score)> GetPotentialMatchesForDonor(int donorId, string organType)
        {
            var donor = _patientRepo.GetById(donorId);

            // VM38 Condition: Donor must be deceased and a registered donor
            if (donor == null || !donor.IsDeceased || !donor.IsDonor)
            {
                throw new InvalidOperationException("Matching is only allowed for deceased, registered donors.");
            }

            // RP23: Fetch all 'Waiting' records for this organ
            var waitlist = _transplantRepo.GetWaitingByOrgan(organType);
            var results = new List<(Transplant, float)>();

            foreach (var request in waitlist)
            {
                var receiver = _patientRepo.GetById(request.ReceiverId);
                if (receiver == null) continue;

                // Scoring Metric: Score(Blood) + Score(Age) + Score(Sex)
                float score = _compatibilityService.CalculateScore(donor, receiver);

                results.Add((request, score));
            }

            // Return top 5 ranked by score descending
            return results
                .OrderByDescending(r => r.Score)
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// VM38 / RP23: Finalize the match
        /// Updates the request to 'Scheduled' and assigns the donor.
        /// </summary>
        public void ConfirmMatch(int transplantId, int donorId, float finalScore)
        {
            // LinkDonorToRequest implementation
            _transplantRepo.Update(transplantId, donorId, finalScore);
        }

        /// <summary>
        /// Utility: Fetches all transplant history (as donor or receiver) for a patient.
        /// </summary>
        public List<Transplant> GetPatientTransplantHistory(int patientId)
        {
            var history = new List<Transplant>();

            var asReceiver = _transplantRepo.GetByReceiverId(patientId);
            var asDonor = _transplantRepo.GetByDonorId(patientId);

            history.AddRange(asReceiver);
            history.AddRange(asDonor);

            return history.OrderByDescending(t => t.RequestDate).ToList();
        }
    }
}
