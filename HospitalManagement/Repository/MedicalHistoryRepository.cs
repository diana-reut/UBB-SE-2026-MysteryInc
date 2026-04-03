using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using HospitalManagement.Database;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Repository
{
    public class MedicalHistoryRepository
    {
        private readonly HospitalDbContext _context; //RP 12

        public MedicalHistoryRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public MedicalHistory? GetByPatientId(int patientId) //RP 13
        {
            string query = $"SELECT * FROM MedicalHistory WHERE PatientID={patientId}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read())
                {
                    return MapToMedicalHistory(reader);
                }
            }
            return null;
        }

        public MedicalHistory? GetById(int historyId) //RP 13
        {
            string query = $"SELECT * FROM MedicalHistory WHERE HistoryID={historyId}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read())
                {
                    return MapToMedicalHistory(reader);
                }
            }
            return null;
        }

        public void Create(MedicalHistory history) //RP 14
        {

            string query = $"INSERT INTO MedicalHistory (PatientID, BloodType, Rh) " +
                           $"VALUES ({history.PatientId}, " +
                           $"{(history.BloodType.HasValue ? $"'{history.BloodType}'" : "NULL")}, " +
                           $"{(history.Rh.HasValue ? $"'{history.Rh}'" : "NULL")})";
            _context.ExecuteNonQuery(query);
        }

        public void Update(MedicalHistory history) // RP14
        {
            // Verify patientId matches what's in DB for this history record
            string checkQuery = $"SELECT PatientID FROM MedicalHistory WHERE HistoryID={history.Id}";
            using (SqlDataReader reader = _context.ExecuteQuery(checkQuery))
            {
                if (!reader.Read())
                    throw new KeyNotFoundException($"MedicalHistory with ID={history.Id} not found.");
                int dbPatientId = Convert.ToInt32(reader["PatientID"]);
                if (dbPatientId != history.PatientId)
                    throw new InvalidOperationException("PatientID mismatch - cannot reassign history to a different patient.");
            }

            string query = $"UPDATE MedicalHistory SET " +
                           $"BloodType = {(history.BloodType.HasValue ? $"'{history.BloodType}'" : "NULL")}, " +
                           $"Rh = {(history.Rh.HasValue ? $"'{history.Rh}'" : "NULL")} " +
                           $"WHERE HistoryID = {history.Id}";
            _context.ExecuteNonQuery(query);
        }

        private MedicalHistory MapToMedicalHistory(SqlDataReader reader) // RP 15
        {
            return new MedicalHistory
            {
                Id = Convert.ToInt32(reader["HistoryID"]),
                PatientId = Convert.ToInt32(reader["PatientID"]),
                BloodType = reader["BloodType"] == DBNull.Value
                            ? (BloodType?)null
                            : Enum.Parse<BloodType>(reader["BloodType"].ToString()!),
                Rh = reader["Rh"] == DBNull.Value
                     ? (RhEnum?)null
                     : Enum.Parse<RhEnum>(reader["Rh"].ToString()!),
                // ChronicConditions, MedicalRecords, Allergies are loaded separately
                // via their own repositories/queries
            };
        }



        //things from entity put here

        public List<string> GetChronicConditions(int historyId) // part of getRecords() scope
        {
            // ChronicConditions is stored as a single VARCHAR(2000) in DB5
            // so we parse it back into a list here
            string query = $"SELECT ChronicConditions FROM MedicalHistory WHERE HistoryID={historyId}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read() && reader["ChronicConditions"] != DBNull.Value)
                {
                    string raw = reader["ChronicConditions"].ToString()!;
                    return new List<string>(raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            }
            return new List<string>();
        }

        public List<(Allergy Allergy, string SeverityLevel)> GetAllergiesByHistoryId(int historyId) // getAllergies()
        {
            var result = new List<(Allergy, string)>();
            string query = $"SELECT a.AllergyID, a.AllergyName, a.AllergyType, a.AllergyCategory, pa.SeverityLevel " +
                           $"FROM PatientAllergies pa " +
                           $"JOIN Allergy a ON pa.AllergyID = a.AllergyID " +
                           $"WHERE pa.HistoryID={historyId}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    var allergy = new Allergy
                    {
                        AllergyId = Convert.ToInt32(reader["AllergyID"]),
                        AllergyName = reader["AllergyName"].ToString() ?? string.Empty,
                        AllergyType = reader["AllergyType"]?.ToString(),
                        AllergyCategory = reader["AllergyCategory"]?.ToString()
                    };
                    string severity = reader["SeverityLevel"].ToString() ?? string.Empty;
                    result.Add((allergy, severity));
                }
            }
            return result;
        }





    }
}