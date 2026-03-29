using HospitalManagement.Entity;
using HospitalManagement.Database;
using System;
using System.Collections.Generic;
using HospitalManagement.Integration;

namespace HospitalManagement.Repository
{
    public class PrescriptionRepository
    {
        private readonly HospitalDbContext _context;

        public PrescriptionRepository(HospitalDbContext context)
        {
            _context = context;
        }

        private string Escape(string value)
        {
            return value.Replace("'", "''");
        }

        private string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        // RP18
        public Prescription? GetByRecordId(int recordId)
        {
            if (recordId <= 0)
                return null;

            string sql = $"SELECT * FROM Prescription WHERE RecordID = {recordId}";

            using (var reader = _context.ExecuteQuery(sql))
            {
                if (!reader.Read())
                    return null;

                var prescription = new Prescription
                {
                    Id = (int)reader["PrescriptionID"],
                    RecordId = (int)reader["RecordID"],
                    DoctorNotes = reader["DoctorNotes"] == DBNull.Value ? null : reader["DoctorNotes"].ToString(),
                    Date = (DateTime)reader["Date"]
                };

                prescription.MedicationList = GetItems(prescription.Id);
                return prescription;
            }
        }

        // RP19
        public void Add(Prescription prescription)
        {
            if (prescription == null)
                throw new ArgumentNullException(nameof(prescription));

            try
            {
                _context.BeginTransaction();

                string notesValue = prescription.DoctorNotes == null
                    ? "NULL"
                    : $"'{Escape(prescription.DoctorNotes)}'";

                string dateValue = prescription.Date == default
                    ? "CONVERT(DATE, GETDATE())"
                    : $"'{FormatDate(prescription.Date)}'";

                string sqlPrescription = $@"
                    INSERT INTO Prescription (RecordID, DoctorNotes, [Date]) 
                    OUTPUT INSERTED.PrescriptionID
                    VALUES ({prescription.RecordId}, {notesValue}, {dateValue})";

                int newId;

                using (var reader = _context.ExecuteQuery(sqlPrescription))
                {
                    if (!reader.Read())
                        throw new Exception("Failed to insert prescription.");

                    newId = (int)reader["PrescriptionID"];
                }

                if (prescription.MedicationList != null)
                {
                    foreach (var item in prescription.MedicationList)
                    {
                        string quantityValue = item.Quantity == null
                            ? "NULL"
                            : $"'{Escape(item.Quantity)}'";

                        string sqlItem = $@"
                            INSERT INTO PrescriptionItems (PrescriptionID, MedName, Quantity) 
                            VALUES ({newId}, '{Escape(item.MedName)}', {quantityValue})";

                        _context.ExecuteNonQuery(sqlItem);
                    }
                }

                _context.CommitTransaction();
            }
            catch
            {
                _context.RollbackTransaction();
                throw;
            }
        }

        // RP19 Delete
        public void Delete(int id)
        {
            if (id <= 0)
                return;

            try
            {
                _context.BeginTransaction();

                _context.ExecuteNonQuery(
                    $"DELETE FROM PrescriptionItems WHERE PrescriptionID = {id}");

                _context.ExecuteNonQuery(
                    $"DELETE FROM Prescription WHERE PrescriptionID = {id}");

                _context.CommitTransaction();
            }
            catch
            {
                _context.RollbackTransaction();
                throw;
            }
        }

        // RP21 Pagination
        public List<Prescription> GetTopN(int n, int page)
        {
            if (n <= 0) n = 20;
            if (page <= 0) page = 1;

            int offset = (page - 1) * n;

            string sql = $@"
                SELECT * FROM Prescription
                ORDER BY [Date] DESC
                OFFSET {offset} ROWS FETCH NEXT {n} ROWS ONLY";

            var list = new List<Prescription>();

            using (var reader = _context.ExecuteQuery(sql))
            {
                while (reader.Read())
                {
                    list.Add(new Prescription
                    {
                        Id = (int)reader["PrescriptionID"],
                        RecordId = (int)reader["RecordID"],
                        DoctorNotes = reader["DoctorNotes"] == DBNull.Value ? null : reader["DoctorNotes"].ToString(),
                        Date = (DateTime)reader["Date"]
                    });
                }
            }

            return list;
        }

        // RP22
        public List<PrescriptionItem> GetItems(int prescriptionId)
        {
            var items = new List<PrescriptionItem>();

            if (prescriptionId <= 0)
                return items;

            string sql = $"SELECT * FROM PrescriptionItems WHERE PrescriptionID = {prescriptionId}";

            using (var reader = _context.ExecuteQuery(sql))
            {
                while (reader.Read())
                {
                    items.Add(new PrescriptionItem
                    {
                        PrescrItemId = (int)reader["PrescrItemID"],
                        PrescriptionId = (int)reader["PrescriptionID"],
                        MedName = reader["MedName"].ToString() ?? "",
                        Quantity = reader["Quantity"] == DBNull.Value
                            ? null
                            : reader["Quantity"].ToString()
                    });
                }
            }

            return items;
        }

        // RP20 Filtering
        public List<Prescription> GetFiltered(PrescriptionFilter filter)
        {
            if (filter == null)
                return GetTopN(20, 1);

            var list = new List<Prescription>();

            string sql = @"
                SELECT DISTINCT p.*
                FROM Prescription p
                LEFT JOIN PrescriptionItems pi ON p.PrescriptionID = pi.PrescriptionID
                LEFT JOIN MedicalRecord mr ON p.RecordID = mr.RecordID
                LEFT JOIN MedicalHistory mh ON mr.HistoryID = mh.HistoryID
                LEFT JOIN Patient pat ON mh.PatientID = pat.PatientID
                LEFT JOIN Doctor d ON mr.DoctorID = d.DoctorID
                WHERE 1=1";
            //aici daca voi avea tabelul Doctor, cred ca fac LEFT JOIN Doctor d ON mr.StaffID = d.DoctorID

            if (filter.PrescriptionId.HasValue)
                sql += $" AND p.PrescriptionID = {filter.PrescriptionId.Value}";

            if (filter.PatientId.HasValue)
                sql += $" AND pat.PatientID = {filter.PatientId.Value}";

            //momentan aici am pus StaffID pentru ca nu avem tabelul Doctor, daca il hardcodam schimbam
            if (filter.DoctorId.HasValue)
                sql += $" AND d.StaffID = {filter.DoctorId.Value}";

            if (!string.IsNullOrWhiteSpace(filter.MedName))
                sql += $" AND pi.MedName LIKE '%{Escape(filter.MedName)}%'";

            if (filter.DateFrom.HasValue)
                sql += $" AND p.[Date] >= '{FormatDate(filter.DateFrom.Value)}'";

            if (filter.DateTo.HasValue)
                sql += $" AND p.[Date] <= '{FormatDate(filter.DateTo.Value)}'";

            if (!string.IsNullOrWhiteSpace(filter.PatientName))
            {
                string name = Escape(filter.PatientName);
                sql += $" AND (pat.FirstName LIKE '%{name}%' OR pat.LastName LIKE '%{name}%')";
            }

            if (!string.IsNullOrWhiteSpace(filter.DoctorName))
            {
                string name = Escape(filter.DoctorName);
                sql += $" AND (d.FirstName LIKE '%{name}%' OR d.LastName LIKE '%{name}%')";
            }

            sql += " ORDER BY p.[Date] DESC";

            using (var reader = _context.ExecuteQuery(sql))
            {
                while (reader.Read())
                {
                    var prescription = new Prescription
                    {
                        Id = (int)reader["PrescriptionID"],
                        RecordId = (int)reader["RecordID"],
                        DoctorNotes = reader["DoctorNotes"] == DBNull.Value ? null : reader["DoctorNotes"].ToString(),
                        Date = (DateTime)reader["Date"]
                    };

                    prescription.MedicationList = GetItems(prescription.Id);
                    list.Add(prescription);
                }
            }

            return list;
        }
    }
}