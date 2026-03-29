using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.DTOs;

namespace HospitalManagement.Integration.PatientObserver
{
    public interface IPatientObserver
    {
        void OnNewExternalPatient(ExternalPatientDTO newPatientData);
    }
}
