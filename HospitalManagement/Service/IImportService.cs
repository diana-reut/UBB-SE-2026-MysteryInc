namespace HospitalManagement.Service;

internal interface IImportService
{
    void ImportFromAppointment(int patientId, int externalId);
    void ImportFromER(int patientId, int externalId);
}