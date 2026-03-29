using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.DTOs;

namespace HospitalManagement.Integration.External
{
    public class ERProxy : IExternalProvider // THIS IS A SKELETON FOR THE NEXT TEAM , ER PART , IN THEIR TEMPORARY LIST LOGIC SHOULD IMPLEMENT THIS
    {
        private readonly ExternalPatientPublisher _publisher;

        public ERProxy(ExternalPatientPublisher publisher)
        {
            _publisher = publisher;
        }

        public ExternalPatientDTO FetchPatientById(int patientId)
        {
            throw new NotImplementedException("To be implemented by ER team");
        }

        public RecordDTO FetchRecordByPatientId(int patientId)
        {
            throw new NotImplementedException("To be implemented by ER team");
        }

        public void OnNewPatientDetected(ExternalPatientDTO dto)
        {
            _publisher.Notify(dto);
        }
    }
}
