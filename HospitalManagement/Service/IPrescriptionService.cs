using HospitalManagement.Entity;
using HospitalManagement.Integration;
using System.Collections.Generic;

namespace HospitalManagement.Service;
public interface IPrescriptionService
{
    List<Prescription> ApplyFilter(PrescriptionFilter filter);
    List<Prescription> GetLatestPrescriptions(int n, int page);
    Prescription GetPrescriptionDetails(int id);
}