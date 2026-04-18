using HospitalManagement.Entity;
using HospitalManagement.Integration;
using System.Collections.ObjectModel;

namespace HospitalManagement.Interfaces.Repository;

internal interface IPrescriptionRepository
{
    public Prescription? GetByRecordId(int recordId);

    public void Add(Prescription prescription);

    public void Delete(int id);

    public void Update(Prescription prescription);

    public Collection<Prescription> GetTopN(int n, int page);

    public Collection<PrescriptionItem> GetItems(int prescriptionId);

    public Collection<Prescription> GetFiltered(PrescriptionFilter filter);

    public Collection<Prescription> GetAll();

    public Collection<Patient> GetAddictCandidatePatients();
}
