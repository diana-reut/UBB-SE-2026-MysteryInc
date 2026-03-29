using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.DTOs;
using HospitalManagement.Entity;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.Integration.PatientObserver
{
    public class PatientSyncObserver : IPatientObserver
    {
        private readonly PatientService _patientService;
        private readonly PatientRepository _patientRepo;

        public PatientSyncObserver(PatientService patientService, PatientRepository patientRepo)
        {
            _patientService = patientService;
            _patientRepo = patientRepo;
        }

        public void OnNewExternalPatient(ExternalPatientDTO newPatientData)
        {
            // IN6: check if patient exists by CNP
            bool exists = _patientRepo.Exists(newPatientData.CNP);

            if (exists)
            {
                // map DTO to patient and update
                Patient updated = MapDTOToPatient(newPatientData);
                _patientService.UpdatePatient(updated);
            }
            else
            {
                // map DTO to new patient and create
                Patient newPatient = MapDTOToPatient(newPatientData);
                _patientService.CreatePatient(newPatient);
            }
        }

        private Patient MapDTOToPatient(ExternalPatientDTO dto)
        {
            return new Patient
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Cnp = dto.CNP,
                Sex = dto.Sex
                // Injury → goes to MedicalRecord.Symptoms, not Patient
                // EmergencyTimestamp → goes to MedicalRecord.ConsultationDate
            };
        }
    }
}
