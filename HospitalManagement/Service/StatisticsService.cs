using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity;
using HospitalManagement.Repository;

namespace HospitalManagement.Service
{
    internal class StatisticsService
    {
        private PatientRepository patientRepo;
        private MedicalRecordRepository recordRepo;
        private PrescriptionRepository prescriptionRepo;

        public StatisticsService(PatientRepository patientRepo, MedicalRecordRepository recordRepo, PrescriptionRepository prescriptionRepo)
        {
            this.patientRepo = patientRepo;
            this.recordRepo = recordRepo;
            this.prescriptionRepo = prescriptionRepo;
        }

        public Dictionary<string, int> GetPatientsByBloodType()
        {
            IEnumerable<Patient> patients = patientRepo.GetAll(true);

            Dictionary<string, int> bloodCount = patients.Where(p => p.MedicalHistory != null && p.MedicalHistory.BloodType.HasValue)
                .GroupBy(p => p.MedicalHistory!.BloodType!.Value.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return bloodCount;
        }

        public Dictionary<string, int> GetPatientsByRh()
        {
            IEnumerable<Patient> patients = patientRepo.GetAll(true);

            Dictionary<string, int> rhCount = patients.Where(p => p.MedicalHistory != null && p.MedicalHistory.Rh.HasValue)
                .GroupBy(p => p.MedicalHistory!.Rh!.Value.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return rhCount;
        }

        public Dictionary<string, int> GetPatientGenderDistribution()
        {
            IEnumerable<Patient> patients = patientRepo.GetAll(true);

            Dictionary<string, int> genderCount = patients.GroupBy(p => p.Sex.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return genderCount;
        }

        public Dictionary<string, int> GetConsultationDistribution()
        {
            IEnumerable<MedicalRecord> records = recordRepo.GetAll();

            Dictionary<string, int> consultationTypeCount = records.GroupBy(r => r.SourceType.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return consultationTypeCount;
        }

        public Dictionary<string, int> GetTopDiagnoses()
        {
            IEnumerable<MedicalRecord> records = recordRepo.GetAll();

            Dictionary<string, int> diagnosesCount = records.Where(r => !string.IsNullOrWhiteSpace(r.Diagnosis))
                .GroupBy(r => r.Diagnosis!.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            return diagnosesCount;
        }

        public Dictionary<string, int> GetAgeDistribution()
        {
            IEnumerable<Patient> patients = patientRepo.GetAll(true);

            var ageGroups = new Dictionary<string, int>
                {
                    { "Pediatric (0-17)", 0 },
                    { "Adult (18-64)", 0 },
                    { "Geriatric (65+)", 0 }
                };

            foreach (var patient in patients)
            {
                int age = patient.GetAge();

                if (age <= 17)
                    ageGroups["Pediatric (0-17)"]++;
                else if (age <= 64)
                    ageGroups["Adult (18-64)"]++;
                else
                    ageGroups["Geriatric (65+)"]++;
            }

            return ageGroups;
        }

        public Dictionary<string, int> GetMostPrescribedMeds()
        {
            IEnumerable<Prescription> prescriptions = prescriptionRepo.GetAll();

            var allItems = prescriptions.Where(p => p.MedicationList != null)
                .SelectMany(p => p.MedicationList);

            var topMeds = allItems.Where(item => !string.IsNullOrWhiteSpace(item.MedName))
                .Where(item => !string.IsNullOrEmpty(item.MedName))
                .GroupBy(item => item.MedName.Trim().ToLowerInvariant())
                .OrderByDescending(g => g.Count())
                .Take(20)
                .ToDictionary(g => g.Key, g => g.Count());

            return topMeds;
        }

        public Dictionary<string, int> GetActiveVsArchivedRatio()
        {
            IEnumerable<Patient> patients = patientRepo.GetAll(true);

            return new Dictionary<string, int>
            {
                { "Active", patients.Count(p => !p.IsArchived) },
                { "Archived", patients.Count(p => p.IsArchived) }
            };
        }
    }
}
