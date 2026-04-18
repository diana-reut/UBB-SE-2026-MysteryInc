using HospitalManagement.Entity;
using HospitalManagement.Repository;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HospitalManagement.Service;

internal class AddictDetectionService
{
    private readonly PrescriptionRepository _prescriptionRepository;
    private readonly MedicalHistoryRepository _medicalHistoryRepository;

    public AddictDetectionService(PrescriptionRepository prescriptionRepository, MedicalHistoryRepository medicalHistoryRepository)
    {
        _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));
        _medicalHistoryRepository = medicalHistoryRepository ?? throw new ArgumentNullException(nameof(medicalHistoryRepository));
    }


    public List<Patient> GetAddictCandidates()
    {
        List<Patient> flaggedPatients = _prescriptionRepository.GetAddictCandidatePatients();

        foreach (Patient patient in flaggedPatients)
        {
            patient.MedicalHistory = _medicalHistoryRepository.GetByPatientId(patient.Id);

            if (patient.MedicalHistory is not null)
            {
                patient.MedicalHistory.ChronicConditions = _medicalHistoryRepository.GetChronicConditions(patient.MedicalHistory.Id);
            }
        }

        return flaggedPatients;
    }

    public string GetChronicConditions(int patientId)
    {
        if (patientId <= 0)
        {
            throw new ArgumentException("Invalid Patient ID.");
        }

        MedicalHistory? history = _medicalHistoryRepository.GetByPatientId(patientId);

        if (history is null)
        {
            return "None reported.";
        }

        if (history.ChronicConditions is null || history.ChronicConditions.Count == 0)
        {
            history.ChronicConditions = _medicalHistoryRepository.GetChronicConditions(history.Id);
        }

        if (history.ChronicConditions is null || history.ChronicConditions.Count == 0)
        {
            return "None reported.";
        }

        return string.Join(", ", history.ChronicConditions);
    }


    public string BuildPoliceReport(Patient patient)
    {
        if (patient is null || patient.Id <= 0)
        {
            throw new ArgumentException("Invalid patient data for building a police report.");
        }

        var filter = new Integration.PrescriptionFilter
        {
            PatientId = patient.Id,
            DateFrom = DateTime.Today.AddDays(-30),
        };

        List<Prescription> recentPrescriptions = _prescriptionRepository.GetFiltered(filter);

        var reportBuilder = new System.Text.StringBuilder();

        _ = reportBuilder.AppendLine("==================================================")
            .AppendLine("           LAW ENFORCEMENT ALERT REPORT           ")
            .AppendLine("==================================================")
            .AppendLine(CultureInfo.InvariantCulture, $"DATE GENERATED: {DateTime.Now:yyyy-MM-dd HH:mm}")
            .AppendLine(CultureInfo.InvariantCulture, $"SUBJECT: {patient.FirstName} {patient.LastName} (CNP: {patient.Cnp})")
            .AppendLine(CultureInfo.InvariantCulture, $"CONTACT: {patient.PhoneNo}")
            .AppendLine("--------------------------------------------------")
            .AppendLine("SUSPICIOUS ACTIVITY: SUSPECTED DRUG SHOPPING BEHAVIOR")
            .AppendLine("CRITERIA MET: MULTIPLE DOCTORS (>=3) WITHIN 30 DAYS\n")
            .AppendLine("--- SUPPORTING EVIDENCE (MEDICAL RECORDS) ---");

        if (recentPrescriptions.Count == 0)
        {
            _ = reportBuilder.AppendLine("No matching records pulled for this timeframe.");
        }
        else
        {
            int evidenceCount = 1;
            foreach (Prescription rx in recentPrescriptions)
            {
                string meds = "Unknown";
                if (rx.MedicationList?.Count > 0)
                {
                    meds = string.Join(", ", rx.MedicationList.Select(m => m.MedName));
                }

                _ = reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"[{evidenceCount}] Medical Record ID: {rx.RecordId}")
                    .AppendLine(CultureInfo.InvariantCulture, $"    Prescription ID: {rx.Id} | Date: {rx.Date:yyyy-MM-dd}")
                    .AppendLine(CultureInfo.InvariantCulture, $"    Dispensed Drugs: {meds}")
                    .AppendLine("");

                evidenceCount++;
            }
        }

        _ = reportBuilder.AppendLine("==================================================")
            .AppendLine("ACTION REQUIRED: AWAITING PHARMACIST CONFIRMATION.");

        return reportBuilder.ToString();
    }
}
