using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using HospitalManagement.Database;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Repository
{
    public class MedicalRecordRepository
    {
        private readonly HospitalDbContext _context;

        public MedicalRecordRepository(HospitalDbContext context)
        {
            _context = context;
        }

        public List<MedicalRecord> GetByHistoryId(int historyId) //RP15
        {
            List<MedicalRecord> records = new List<MedicalRecord>();
            string query = $"SELECT * FROM MedicalRecord WHERE HistoryID={historyId}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    records.Add(MapToMedicalRecord(reader));
                }
            }
            return records;
        }

        public MedicalRecord? GetById(int id) //RP15
        {
            string query = $"SELECT * FROM MedicalRecord WHERE RecordID={id}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read())
                {
                    return MapToMedicalRecord(reader);
                }
            }
            return null;
        }

        public void Add(MedicalRecord record) // RP16
        {
            // ID range validation - same pattern as PatientRepo checks CNP before Add
            if (record.HistoryId <= 0 || record.StaffId <= 0)
                throw new ArgumentException("HistoryId and StaffId must be greater than 0.");

            string query = $"INSERT INTO MedicalRecord " +
                           $"(HistoryID, SourceType, SourceID, StaffID, Symptoms, Diagnosis, ConsultationDate, " +
                           $"PrescriptionID, BasePrice, FinalPrice, DiscountApplied, PoliceNotified, TransplantID) VALUES " +
                           $"({record.HistoryId}, '{record.SourceType}', {record.SourceId}, {record.StaffId}, " +
                           $"{(record.Symptoms != null ? $"'{record.Symptoms}'" : "NULL")}, " +
                           $"{(record.Diagnosis != null ? $"'{record.Diagnosis}'" : "NULL")}, " +
                           $"'{record.ConsultationDate:yyyy-MM-dd HH:mm:ss}', " +
                           $"{(record.PrescriptionId.HasValue ? record.PrescriptionId.ToString() : "NULL")}, " +
                           $"{record.BasePrice}, {record.FinalPrice}, " +
                           $"{(record.DiscountApplied.HasValue ? record.DiscountApplied.ToString() : "NULL")}, " +
                           $"{(record.PoliceNotified ? 1 : 0)}, " +
                           $"{(record.TransplantId.HasValue ? record.TransplantId.ToString() : "NULL")})";
            _context.ExecuteNonQuery(query);
        }

        public void Update(MedicalRecord record) // RP16
        {
            // ID range validation
            if (record.Id <= 0 || record.HistoryId <= 0 || record.StaffId <= 0)
                throw new ArgumentException("Id, HistoryId and StaffId must be greater than 0.");

            // Verify historyId exists to prevent orphaned records
            string checkQuery = $"SELECT COUNT(*) FROM MedicalHistory WHERE HistoryID={record.HistoryId}";
            using (SqlDataReader reader = _context.ExecuteQuery(checkQuery))
            {
                if (reader.Read() && Convert.ToInt32(reader[0]) == 0)
                    throw new KeyNotFoundException($"No MedicalHistory found with ID={record.HistoryId}.");
            }

            string query = $"UPDATE MedicalRecord SET " +
                           $"Symptoms = {(record.Symptoms != null ? $"'{record.Symptoms}'" : "NULL")}, " +
                           $"Diagnosis = {(record.Diagnosis != null ? $"'{record.Diagnosis}'" : "NULL")}, " +
                           $"BasePrice = {record.BasePrice}, " +
                           $"FinalPrice = {record.FinalPrice}, " +
                           $"DiscountApplied = {(record.DiscountApplied.HasValue ? record.DiscountApplied.ToString() : "NULL")}, " +
                           $"PoliceNotified = {(record.PoliceNotified ? 1 : 0)}, " +
                           $"TransplantID = {(record.TransplantId.HasValue ? record.TransplantId.ToString() : "NULL")} " +
                           $"WHERE RecordID = {record.Id}";
            _context.ExecuteNonQuery(query);
        }

        public void Delete(int id) // RP16
        {
            // Void associated prescription before deleting the record
            // A prescription cannot exist without its parent consultation
            string deletePrescription = $"DELETE FROM Prescription WHERE RecordID={id}";
            _context.ExecuteNonQuery(deletePrescription);

            string query = $"DELETE FROM MedicalRecord WHERE RecordID={id}";
            _context.ExecuteNonQuery(query);
        }

        public int GetERVisitCount(int patientId, DateTime fromDate)//RP17
        {
            string query = $"SELECT COUNT(*) FROM MedicalRecord mr " +
                           $"JOIN MedicalHistory mh ON mr.HistoryID = mh.HistoryID " +
                           $"WHERE mh.PatientID = {patientId} " +
                           $"AND mr.SourceType = '{SourceType.ER}' " +
                           $"AND mr.ConsultationDate >= '{fromDate:yyyy-MM-dd}'";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read())
                    return Convert.ToInt32(reader[0]);
            }
            return 0;
        }

        private MedicalRecord MapToMedicalRecord(SqlDataReader reader)
        {
            return new MedicalRecord
            {
                Id = Convert.ToInt32(reader["RecordID"]),
                HistoryId = Convert.ToInt32(reader["HistoryID"]),
                SourceType = Enum.Parse<SourceType>(reader["SourceType"].ToString()!),
                SourceId = Convert.ToInt32(reader["SourceID"]),
                StaffId = Convert.ToInt32(reader["StaffID"]),
                Symptoms = reader["Symptoms"]?.ToString(),
                Diagnosis = reader["Diagnosis"]?.ToString(),
                ConsultationDate = Convert.ToDateTime(reader["ConsultationDate"]),
                PrescriptionId = reader["PrescriptionID"] == DBNull.Value
                                 ? (int?)null
                                 : Convert.ToInt32(reader["PrescriptionID"]),
                BasePrice = Convert.ToDecimal(reader["BasePrice"]),
                FinalPrice = Convert.ToDecimal(reader["FinalPrice"]),
                DiscountApplied = reader["DiscountApplied"] == DBNull.Value
                                  ? (int?)null
                                  : Convert.ToInt32(reader["DiscountApplied"]),
                PoliceNotified = Convert.ToBoolean(reader["PoliceNotified"]),
                TransplantId = reader["TransplantID"] == DBNull.Value
                               ? (int?)null
                               : Convert.ToInt32(reader["TransplantID"])
            };
        }



        // added things from MedicalRecord ENtity

        public Prescription? GetPrescription(int recordId) // getPrescription()
        {
            // PrescriptionRepository will own the full mapping,
            // but MedicalRecord needs basic navigation — we do a lightweight fetch here
            string query = $"SELECT * FROM Prescription WHERE RecordID={recordId}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read())
                {
                    return new Prescription
                    {
                        Id = Convert.ToInt32(reader["PrescriptionID"]),
                        RecordId = Convert.ToInt32(reader["RecordID"]),
                        DoctorNotes = reader["DoctorNotes"]?.ToString(),
                        Date = Convert.ToDateTime(reader["Date"])
                    };
                }
            }
            return null;
        }

        public int? GetConsultingStaffId(int recordId) // getConsultingStaff()
        {
            // Staff details live in an external module (StaffProxy handles the full object).
            // The repo's job is just to return the StaffID so the service/proxy can fetch the rest.
            string query = $"SELECT StaffID FROM MedicalRecord WHERE RecordID={recordId}";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                if (reader.Read())
                    return Convert.ToInt32(reader["StaffID"]);
            }
            return null;
        }


        public List<MedicalRecord> GetAll() //for statistics service
        {
            List<MedicalRecord> records = new List<MedicalRecord>();
            string query = $"SELECT * FROM MedicalRecord";
            using (SqlDataReader reader = _context.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    records.Add(MapToMedicalRecord(reader));
                }
            }
            return records;
        }
    }
}