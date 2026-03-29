using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.DTOs;
using HospitalManagement.Integration.PatientObserver;

namespace HospitalManagement.Integration.External
{
    public class ExternalPatientPublisher
    {
        private readonly List<IPatientObserver> _observers = new();

        public void Subscribe(IPatientObserver observer)
        {
            _observers.Add(observer);
        }

        public void Unsubscribe(IPatientObserver observer)
        {
            _observers.Remove(observer);
        }

        public void Notify(ExternalPatientDTO dto)
        {
            foreach (var observer in _observers)
                observer.OnNewExternalPatient(dto);
        }
    }
}
