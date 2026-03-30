using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity;
using HospitalManagement.Repository;

namespace HospitalManagement.Service
{
    public class StatisticsService
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

    }
}
