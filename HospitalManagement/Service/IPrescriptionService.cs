using HospitalManagement.Entity;
using HospitalManagement.Integration;
using System.Collections.ObjectModel;

namespace HospitalManagement.Service;

public interface IPrescriptionService
{
    Collection<Prescription> ApplyFilter(PrescriptionFilter filter);

    Collection<Prescription> GetLatestPrescriptions(int n, int page);

    Prescription GetPrescriptionDetails(int id);
}