using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.DTOs;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Integration.External
{
    public class MockStaffProxy : IExternalProvider
    {
        private readonly ExternalPatientPublisher _publisher;

        public MockStaffProxy(ExternalPatientPublisher publisher)
        {
            _publisher = publisher;
        }

        public ExternalPatientDTO FetchPatientById(int patientId)
        {
            return new ExternalPatientDTO
            {
                CNP = "2850215654321",
                FirstName = "Jane",
                LastName = "Smith",
                Sex = Sex.F,
                EmergencyTimestamp = DateTime.Now,
                Injury = null
            };
        }

        public RecordDTO FetchRecordByPatientId(int patientId)
        {
            return new RecordDTO
            {
                ExternalRecordId = patientId,
                Symptoms = "Persistent headache",
                TemporaryDiagnosis = "Migraine",
                PrescribedMeds = "Sumatriptan 50mg",
                ConsultationDate = DateTime.Now,
                SourceType = SourceType.App
            };
        }

        public void OnNewPatientDetected(ExternalPatientDTO dto)
        {
            _publisher.Notify(dto);
        }
    }
}
