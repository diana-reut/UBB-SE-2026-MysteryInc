using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Database;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Integration;

namespace HospitalManagement.Repository
{
    class PatientRepository
    {
        private readonly HospitalDbContext _context;

        public PatientRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public Patient? GetById(int id)
        {
            string query = $"SELECT * FROM Patient WHERE PatientID={id}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read())
                {
                    return MapToPatient(reader);
                }
            }
            return null;
        }
        private Patient MapToPatient(SqlDataReader reader)
        {
            return new Patient
            {
                Id = Convert.ToInt32(reader["PatientID"]),
                FirstName = reader["FirstName"].ToString() ?? string.Empty,
                LastName = reader["LastName"].ToString() ?? string.Empty,

                Cnp = reader["CNP"].ToString() ?? string.Empty,
                Dob = Convert.ToDateTime(reader["DateOfBirth"]),
                Dod = reader["DateOfDeath"] == DBNull.Value
                      ? (DateTime?)null
                      : Convert.ToDateTime(reader["DateOfDeath"]),
                Sex = Enum.Parse<Sex>(reader["Sex"].ToString()!),
                PhoneNo = reader["Phone"]?.ToString() ?? string.Empty,
                EmergencyContact = reader["EmergencyContact"]?.ToString() ?? string.Empty,
                IsArchived = Convert.ToBoolean(reader["Archived"]),
                IsDonor = Convert.ToBoolean(reader["IsDonor"])
            };
        }

        public List<Patient> GetAll(bool include_archived)
        {
            List<Patient> patients = new List<Patient>();

            
            string query = "SELECT * FROM Patient";

            if (!include_archived)
            {
                // SQL Server BIT 0 is false
                query += " WHERE Archived = 0";
            }

            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    patients.Add(MapToPatient(reader));
                }
            }

            return patients;
        }
        public List<Patient> GetArchived()
        {
            List<Patient> archivedPatients = new List<Patient>();

            string query = $"SELECT * FROM Patient WHERE Archived=1";

            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    archivedPatients.Add(this.MapToPatient(reader));
                }
            }
            return archivedPatients;
        }

        public List<Patient> Search(PatientFilter patientFilter)
        {
            IEnumerable<Patient> patients = this.GetAll(true);
            
            if(!string.IsNullOrWhiteSpace(patientFilter.namePart))
            {
                patients = patients.Where(p =>
                    p.FirstName.Contains(patientFilter.namePart, StringComparison.OrdinalIgnoreCase) ||
                    p.LastName.Contains(patientFilter.namePart, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(patientFilter.CNP))
            {
                patients = patients.Where(p => p.Cnp == patientFilter.CNP);
            }

            int currentYear = DateTime.Now.Year;
            if(patientFilter.minAge.HasValue)
            {
                patients = patients.Where(p => (currentYear - p.Dob.Year) >= patientFilter.minAge);
            }

            if (patientFilter.maxAge.HasValue)
            {
                patients = patients.Where(p => (currentYear - p.Dob.Year) <= patientFilter.maxAge);
            }

            if(patientFilter.bloodType.HasValue)
            {
                patients = patients.Where(p => p.MedicalHistory !=null && p.MedicalHistory.BloodType == patientFilter.bloodType);
            }

            if(patientFilter.sex.HasValue)
            {
                patients = patients.Where(p => p.Sex == patientFilter.sex);
            }

            if(patientFilter.hasChronicCond == true)
            {
                patients = patients.Where(p => p.MedicalHistory != null && p.MedicalHistory.ChronicConditions.Any());
            }

            return patients.ToList<Patient>();
        }
    }
}
