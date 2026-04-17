namespace HospitalManagement.Service;

internal interface IBillingService
{
    decimal ApplyDiscount(decimal basePrice, int discount);
    decimal ComputeBasePrice(int patientId, int recordId);
}