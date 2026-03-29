using HospitalManagement.Entity;
using HospitalManagement.Database;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace HospitalManagement.Repository
{
    public class TransplantRepository
    {
        private readonly HospitalDbContext _context;

        public TransplantRepository(HospitalDbContext context)
        {
            _context = context;
        }

        private string Escape(string value)
        {
            return value?.Replace("'", "''") ?? "";
        }

        private string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // RP23: AddRequest
        public void Add(Transplant transplant)
        {
            if (transplant == null)
                throw new ArgumentNullException(nameof(transplant));

            //DECI TOATA PARTEA ASTA CU TRANSPLANT NU VA FUNCTIONA PANA NU SE SCOATE DONOR_ID NOT NULL
            //DUPA CE VA FI NULL VA FI OK ACEST COD (si requirementurile)
            string sql = $@"
            INSERT INTO Transplants (ReceiverID, DonorID, OrganType, RequestDate, TransplantDate, Status, CompatibilityScore)
            VALUES (
                {transplant.ReceiverId}, 
                NULL, 
                '{Escape(transplant.OrganType)}', 
                '{FormatDate(DateTime.Now)}', 
                NULL, 
                'Pending', 
                0
            )";

            _context.ExecuteNonQuery(sql);
        }

        public List<Transplant> GetWaitingByOrgan(string organType)
        {

            string sql = $@"
            SELECT t.*, mh.BloodType, mh.RH, mh.ChronicConditions 
            FROM Transplants t
            JOIN MedicalHistory mh ON t.ReceiverID = mh.PatientID
            WHERE t.OrganType = '{Escape(organType)}' AND t.Status = 'Pending'";

            return GetListByQuery(sql);
        }

       
        public void Update(int id, int donorId, float score)
        {
            string sql = $@"
                UPDATE Transplants SET
                    DonorID = {donorId},
                    Status = 'Scheduled',
                    CompatibilityScore = {score.ToString().Replace(",", ".")}
                WHERE TransplantID = {id}";

            _context.ExecuteNonQuery(sql);
        }

        
        public List<Transplant> GetTopMatches(string organType)
        {
          
            string sql = $@"
                SELECT TOP 5 t.* FROM Transplants t
                WHERE t.OrganType = '{Escape(organType)}' AND t.Status = 'Pending'
                ORDER BY t.CompatibilityScore DESC";

            return GetListByQuery(sql);
        }

        public List<Transplant> GetByReceiverId(int receiverId)
        {
            return GetListByQuery($"SELECT * FROM Transplants WHERE ReceiverID = {receiverId}");
        }

        public List<Transplant> GetByDonorId(int donorId)
        {
            return GetListByQuery($"SELECT * FROM Transplants WHERE DonorID = {donorId}");
        }

        private List<Transplant> GetListByQuery(string sql)
        {
            var list = new List<Transplant>();
            using (var reader = _context.ExecuteQuery(sql))
            {
                while (reader.Read())
                {
                    list.Add(new Transplant
                    {
                        TransplantId = (int)reader["TransplantID"],
                        ReceiverId = (int)reader["ReceiverID"],
                        DonorId = (int)reader["DonorID"],
                        OrganType = reader["OrganType"].ToString(),
                        RequestDate = (DateTime)reader["RequestDate"],
                        TransplantDate = reader["TransplantDate"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["TransplantDate"],
                        Status = (Entity.Enums.TransplantStatus)Enum.Parse(typeof(Entity.Enums.TransplantStatus), reader["Status"].ToString()),
                        CompatibilityScore = Convert.ToSingle(reader["CompatibilityScore"])
                    });
                }
            }
            return list;
        }
    }
}