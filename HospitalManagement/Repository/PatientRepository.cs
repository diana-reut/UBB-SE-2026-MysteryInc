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
using Windows.ApplicationModel.Chat;

namespace HospitalManagement.Repository

{
    public class PatientRepository
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

        List<Patient> GetCompatibleDonors(BloodType bloodType, RhEnum rh, Sex sex, DateTime dob, int minAge, int maxAge)
        {
            IEnumerable<Patient> potentialDonors = this.GetAll(false);

            potentialDonors = potentialDonors.Where(p => p.MedicalHistory != null);

            int currentYear = DateTime.Now.Year;
            // exclude if not in the age range
            potentialDonors = potentialDonors.Where(p => (currentYear - p.Dob.Year) >= minAge && (currentYear - p.Dob.Year) <= maxAge);

            // exclude if the possible donor has chronic condition
            potentialDonors = potentialDonors.Where(p => !(p.MedicalHistory != null && p.MedicalHistory.ChronicConditions.Any()));

            // exclude if the possible donor has an allergy with severity anaphylactic
            potentialDonors = potentialDonors.Where(p => !(p.MedicalHistory != null && p.MedicalHistory.Allergies.Any(a => string.Equals(a.SeverityLevel, "anaphylactic", StringComparison.OrdinalIgnoreCase))));

            // exclude if not a blood match
            potentialDonors = potentialDonors.Where(p => (p.MedicalHistory != null && this.IsABloodMatch(p.MedicalHistory.BloodType, bloodType)));

            // exclude if not a rh match
            potentialDonors = potentialDonors.Where(p => (p.MedicalHistory != null && this.IsARhMatch(p.MedicalHistory.Rh, rh)));

            Dictionary<Patient, int> donorsScore = new Dictionary<Patient, int>();
            foreach(Patient pd in potentialDonors){
                int score = 0;

                if (pd.MedicalHistory.BloodType == bloodType && pd.MedicalHistory.Rh == rh) score += 50;
                else score += 25;

                if (pd.Sex == sex)
                    score += 20;
                else score += 10;

                score += this.CalculateAgeScore(pd.Dob, dob);

                donorsScore.Add(pd, score);

            }

            List<Patient> sortedPatients = donorsScore.OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();
            return sortedPatients;
        }

        private bool IsABloodMatch(BloodType? donor, BloodType receiver)
        {
            if (donor == null)
                return false;

            if (donor == BloodType.O)
                return true;

            if (donor == BloodType.A && (receiver == BloodType.A || receiver == BloodType.AB))
                return true;

            if (donor == BloodType.B && (receiver == BloodType.B || receiver == BloodType.AB))
                return true;

            if (donor == BloodType.AB && receiver == BloodType.AB)
                return true;

            return false;
        }

        private bool IsARhMatch(RhEnum? donor, RhEnum receiver)
        {
            if (donor == null)
                return false;

            if (donor == RhEnum.Positive && receiver == RhEnum.Positive)
                return true;

            if (donor == RhEnum.Negative)
                return true;
            return false;
        }

        private int CalculateAgeScore(DateTime donorDob, DateTime receiverDob)
        {
            int currentYear = DateTime.Now.Year;

            int donorAge = currentYear - donorDob.Year;
            int recipientAge = currentYear - receiverDob.Year;
            int ageGap = Math.Abs(donorAge - recipientAge);

            int group = ageGap / 5; // 0 for 0-4, 1 for 5-9, etc.
            int score = 30 - (group * 5);
            return Math.Max(score, 0);
        }

        public bool Exists(string CNP)
        {
            string query = $"SELECT * FROM Patient WHERE CNP={CNP}";

            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                return reader.Read();
            }
            
        }

        public void MarkAsDeceased(int id, DateOnly dod)
        {
            string query = $"UPDATE Patient SET DateOfDeath={dod} WHERE PatientID={id}";
            _context.ExecuteNonQuery(query);
        }

        //public void Add(Patient patientToAdd)
        //{
        //    string query = $"INSERT INTO Patient VALUES({patientToAdd.FirstName}, {patientToAdd.LastName}," +
        //        $"{patientToAdd.Cnp}, {patientToAdd.Dob}, {patientToAdd.Dod}, {patientToAdd.Sex}," +
        //        $"{patientToAdd.PhoneNo}, {patientToAdd.EmergencyContact}, {patientToAdd.IsArchived}, {patientToAdd.IsDonor})";

        //    _context.ExecuteNonQuery(query);
        //}
        public void Add(Patient p)
        {
            // 1. List the columns explicitly (Highly Recommended)
            // 2. Wrap all text/dates in '{value}'
            string query = "INSERT INTO Patient (FirstName, LastName, Cnp, DateOfBirth, Sex, Phone, EmergencyContact, Archived, IsDonor) " +
                           "VALUES (" +
                           $"'{p.FirstName}', " +       // Quote
                           $"'{p.LastName}', " +        // Quote
                           $"'{p.Cnp}', " +             // Quote
                           $"'{p.Dob:yyyy-MM-dd}', " +  // Quote + Format
                           $"'{p.Sex}', " +             // Quote
                           $"'{p.PhoneNo}', " +         // Quote
                           $"'{p.EmergencyContact}', " +// Quote
                           $"{(p.IsArchived ? 1 : 0)}, " + // No quotes for numbers
                           $"{(p.IsDonor ? 1 : 0)})";      // No quotes for numbers

            _context.ExecuteNonQuery(query);
        }
        public void Update(Patient patientToUpdate)
        {
            string query = $"UPDATE Patient SET FirstName={patientToUpdate.FirstName}, LastName={patientToUpdate.LastName}," +
                $"Cnp={patientToUpdate.Cnp}, Dob={patientToUpdate.Dob}, Dod={patientToUpdate.Dod}, Sex={patientToUpdate.Sex}," +
                $"PhoneNo={patientToUpdate.PhoneNo}, EmergencyContact={patientToUpdate.EmergencyContact}, IsArchived={patientToUpdate.IsArchived}, IsDonor={patientToUpdate.IsDonor}" +
                $"WHERE PatientID={patientToUpdate.Id}";

            _context.ExecuteNonQuery(query);
        }

        public void Delete(int id)
        {
            string query = $"DELET FROM Patient WHERE PatientID={id}";
            _context.ExecuteNonQuery(query);
        }

    }
}
