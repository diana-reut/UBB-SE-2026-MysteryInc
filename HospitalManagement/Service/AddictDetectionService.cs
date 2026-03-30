using System;
using System.Collections.Generic;
using System.Linq;
using HospitalManagement.Entity;
using HospitalManagement.Repository;

namespace HospitalManagement.Service
{
    public class AddictDetectionService
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

            foreach (var patient in flaggedPatients)
            {
                patient.MedicalHistory = _medicalHistoryRepository.GetByPatientId(patient.Id);
            }

            return flaggedPatients;
        }

        public string GetChronicConditions(int patientId)
        {
            if (patientId <= 0)
                throw new ArgumentException("Invalid Patient ID.");

            var history = _medicalHistoryRepository.GetByPatientId(patientId);

            if (history == null || history.ChronicConditions == null || !history.ChronicConditions.Any())
            {
                return "None reported.";
            }

            return string.Join(", ", history.ChronicConditions);
        }

    
        public string BuildPoliceReport(Patient patient)
        {
            if (patient == null || patient.Id <= 0)
                throw new ArgumentException("Invalid patient data for building a police report.");

            var filter = new Integration.PrescriptionFilter
            {
                PatientId = patient.Id,
                DateFrom = DateTime.Today.AddDays(-30)
            };

            List<Prescription> recentPrescriptions = _prescriptionRepository.GetFiltered(filter);

            var reportBuilder = new System.Text.StringBuilder();

            reportBuilder.AppendLine("==================================================");
            reportBuilder.AppendLine("           LAW ENFORCEMENT ALERT REPORT           ");
            reportBuilder.AppendLine("==================================================");
            reportBuilder.AppendLine($"DATE GENERATED: {DateTime.Now:yyyy-MM-dd HH:mm}");
            reportBuilder.AppendLine($"SUBJECT: {patient.FirstName} {patient.LastName} (CNP: {patient.Cnp})");
            reportBuilder.AppendLine($"CONTACT: {patient.PhoneNo}");
            reportBuilder.AppendLine("--------------------------------------------------");
            reportBuilder.AppendLine("SUSPICIOUS ACTIVITY: SUSPECTED DRUG SHOPPING BEHAVIOR");
            reportBuilder.AppendLine("CRITERIA MET: MULTIPLE DOCTORS (>=3) WITHIN 30 DAYS\n");
            reportBuilder.AppendLine("--- SUPPORTING EVIDENCE (MEDICAL RECORDS) ---");

            if (!recentPrescriptions.Any())
            {
                reportBuilder.AppendLine("No matching records pulled for this timeframe.");
            }
            else
            {
                int evidenceCount = 1;
                foreach (var rx in recentPrescriptions)
                {
                    string meds = "Unknown";
                    if (rx.MedicationList != null && rx.MedicationList.Any())
                    {
                        meds = string.Join(", ", rx.MedicationList.Select(m => m.MedName));
                    }

                    reportBuilder.AppendLine($"[{evidenceCount}] Medical Record ID: {rx.RecordId}");
                    reportBuilder.AppendLine($"    Prescription ID: {rx.Id} | Date: {rx.Date:yyyy-MM-dd}");
                    reportBuilder.AppendLine($"    Dispensed Drugs: {meds}");
                
                    reportBuilder.AppendLine("");

                    evidenceCount++;
                }
            }

            reportBuilder.AppendLine("==================================================");
            reportBuilder.AppendLine("ACTION REQUIRED: AWAITING PHARMACIST CONFIRMATION.");

            return reportBuilder.ToString();
        }
    }
}
