using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HospitalManagement.Repository;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Entity;

namespace HospitalManagement.Service
{
    public class BillingService
    {
        private MedicalHistoryRepository historyRepo;
        private MedicalRecordRepository recordRepo;
        private PrescriptionRepository prescriptionRepo;
        private TransplantRepository transplantRepo;

        public BillingService(MedicalHistoryRepository historyRepo, MedicalRecordRepository recordRepo, PrescriptionRepository prescriptionRepo, TransplantRepository transplantRepo)
        {
            this.historyRepo = historyRepo;
            this.recordRepo = recordRepo;
            this.prescriptionRepo = prescriptionRepo;
            this.transplantRepo = transplantRepo;
        }

        public decimal computeBasePrice(int patientId, int recordId)
        {
            decimal score = 0;
            var record = recordRepo.GetById(recordId);
            var prescriptionItems = prescriptionRepo.GetItems(recordId);
            MedicalHistory? history = historyRepo.GetByPatientId(patientId);

            var chronicConditions = historyRepo.GetChronicConditions(history.Id);
            var allergies = historyRepo.GetAllergiesByHistoryId(history.Id);
            var associatedTransplants = transplantRepo.GetByReceiverId(patientId);

            if (record.SourceType == SourceType.ER) score += 500;
            else if (record.SourceType == SourceType.App) score += 200;

            score += 50 * prescriptionItems.Count();

            score += 100 * chronicConditions.Count();

            foreach(var allergy in allergies)
            {
                if (string.Compare(allergy.SeverityLevel, "mild", StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(allergy.SeverityLevel, "moderate", StringComparison.OrdinalIgnoreCase) == 0)
                    score += 20;
                else if (string.Compare(allergy.SeverityLevel, "severe", StringComparison.OrdinalIgnoreCase)==0 || string.Compare(allergy.SeverityLevel, "anaphylactic", StringComparison.OrdinalIgnoreCase) == 0)
                    score += 100;
            }

            if (associatedTransplants.Count() > 0) 
                score += 2000;
            return score;
        }

        public decimal ApplyDiscount(decimal basePrice, int discount)
        {
            return basePrice - (basePrice * discount) / 100;
        }
    }
}
