using System.Collections.ObjectModel;
using HospitalManagement.Entity;

namespace HospitalManagement.Interfaces.Repository;

internal interface ITransplantRepository
{
    public void Add(Transplant transplant);

    public Collection<Transplant> GetWaitingByOrgan(string organType);

    public void Update(int id, int donorId, float score);

    public Collection<Transplant> GetTopMatches(string organType);

    public Collection<Transplant> GetByReceiverId(int receiverId);

    public Collection<Transplant> GetByDonorId(int donorId);

    public Transplant? GetById(int id);
}
