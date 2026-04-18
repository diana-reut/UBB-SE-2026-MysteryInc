using System;
using System.Collections.Generic;
using System.Linq;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Repository;

namespace HospitalManagement.Service;

internal class TransplantService
{
    private readonly TransplantRepository _transplantRepo;
    private readonly PatientRepository _patientRepo;
    private readonly MedicalRecordRepository _recordRepo;
    private readonly BloodCompatibilityService _compatibilityService;

    // 1. ADDED THE MISSING REPOSITORY TO FIX THE BUG
    private readonly MedicalHistoryRepository _historyRepo;

    public TransplantService(
        TransplantRepository transplantRepo,
        PatientRepository patientRepo,
        MedicalRecordRepository recordRepo,
        BloodCompatibilityService compatibilityService,
        MedicalHistoryRepository historyRepo)
    {
        _transplantRepo = transplantRepo;
        _patientRepo = patientRepo;
        _recordRepo = recordRepo;
        _compatibilityService = compatibilityService;
        _historyRepo = historyRepo;
    }

    public void CreateWaitlistRequest(int receiverId, string organType)
    {
        var receiver = _patientRepo.GetById(receiverId);
        if (receiver == null) throw new ArgumentException("Receiver not found.");

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

    public List<Transplant> GetTopMatchesForDonor(int donorId, string organType)
    {
        var donor = _patientRepo.GetById(donorId);
        if (donor == null || !donor.IsDeceased || !donor.IsDonor)
            throw new InvalidOperationException("Donor must be deceased and registered.");

        // FIX: Prevent null crashes by eager loading the history
        donor.MedicalHistory = _historyRepo.GetByPatientId(donor.Id);

        var waitlist = _transplantRepo.GetWaitingByOrgan(organType);
        var scoredMatches = new List<Transplant>();

        foreach (var request in waitlist)
        {
            var receiver = _patientRepo.GetById(request.ReceiverId);
            if (receiver == null) continue;

            // FIX: Eagerly load the receiver's history
            receiver.MedicalHistory = _historyRepo.GetByPatientId(receiver.Id);

            if (receiver.MedicalHistory?.BloodType == null || receiver.MedicalHistory.Rh == null) continue;

            if (!BloodCompatibilityService.IsBloodMatch(donor.MedicalHistory?.BloodType, receiver.MedicalHistory.BloodType.Value)) continue;
            if (!BloodCompatibilityService.IsRhMatch(donor.MedicalHistory?.Rh, receiver.MedicalHistory.Rh.Value)) continue;

            if (receiver.MedicalHistory.ChronicConditions != null && receiver.MedicalHistory.ChronicConditions.Any()) continue;

            request.CompatibilityScore = CalculatePostMortemScore(donor, receiver);
            scoredMatches.Add(request);
        }

        return scoredMatches
            .OrderByDescending(t => t.CompatibilityScore)
            .ThenBy(t => t.RequestDate)
            .Take(5)
            .ToList();
    }

    public void AssignDonor(int transplantId, int donorId, float finalScore)
    {
        _transplantRepo.Update(transplantId, donorId, finalScore);
    }

    private float CalculatePostMortemScore(Patient donor, Patient receiver)
    {
        float score = BloodCompatibilityService.CalculateScore(donor, receiver);
        var threeMonthsAgo = DateTime.Now.AddMonths(-3);
        int erVisits = _recordRepo.GetERVisitCount(receiver.Id, threeMonthsAgo);
        score += (erVisits >= 10) ? 20 : 5;
        return score;
    }

    public bool IsUrgent(int patientId)
    {
        var threeMonthsAgo = DateTime.Now.AddMonths(-3);
        int erVisits = _recordRepo.GetERVisitCount(patientId, threeMonthsAgo);
        return erVisits >= 10;
    }

    public string? GetChronicWarning(int patientId)
    {
        var patient = _patientRepo.GetById(patientId);

        // FIX: Actually fetch the conditions from the database!
        if (patient != null)
        {
            patient.MedicalHistory = _historyRepo.GetByPatientId(patientId);
        }

        if (patient?.MedicalHistory?.ChronicConditions != null &&
            patient.MedicalHistory.ChronicConditions.Any())
        {
            return "Patient has underlying conditions that may affect transplant success.";
        }

        return null;
    }
}