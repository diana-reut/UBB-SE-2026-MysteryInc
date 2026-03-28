using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Database;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;

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
            string query = $"select * from Patient where PatientID={id}";
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
    }
}
