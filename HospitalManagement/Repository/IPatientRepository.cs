using HospitalManagement.Entity;
using HospitalManagement.Integration;
using System;
using System.Collections.Generic;

namespace HospitalManagement.Repository;

internal interface IPatientRepository
{
    void Add(Patient p);
    void Delete(int id);
    bool Exists(string cnp);
    List<Patient> GetAll(bool include_archived);
    List<Patient> GetArchived();
    Patient? GetById(int id);
    void MarkAsDeceased(int id, DateOnly dod);
    List<Patient> Search(PatientFilter patientFilter);
    void Update(Patient patientToUpdate);
}
