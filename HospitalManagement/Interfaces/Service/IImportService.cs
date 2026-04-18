namespace HospitalManagement.Interfaces.Service;

internal interface IImportService
{
    public void ImportFromER(int patientId, int externalId);

    public void ImportFromAppointment(int patientId, int externalId);
}
