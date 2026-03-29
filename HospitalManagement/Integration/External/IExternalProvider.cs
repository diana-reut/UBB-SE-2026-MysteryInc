using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.DTOs;

namespace HospitalManagement.Integration.External
{
    public interface IExternalProvider
    {
        RecordDTO FetchRecordByPatientId(int patientId);
        ExternalPatientDTO FetchPatientById(int patientId);
        void OnNewPatientDetected(ExternalPatientDTO dto);
    }
}
