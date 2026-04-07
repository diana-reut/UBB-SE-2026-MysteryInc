using HospitalManagement.Entity.DTOs;

namespace HospitalManagement.Integration.External;

public interface IExternalProvider
{
    public RecordDTO FetchRecordByPatientId(int patientId);

    public ExternalPatientDTO FetchPatientById(int patientId);

    public void OnNewPatientDetected(ExternalPatientDTO dto);
}
