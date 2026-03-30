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

        public bool NotifyPolice(int patientId, string reason)
        {
            return true;
        }
    }
}
