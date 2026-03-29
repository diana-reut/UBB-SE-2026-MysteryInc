using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Entity.DTOs
{
    public class RecordDTO
    {
        public int ExternalRecordId { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public string TemporaryDiagnosis { get; set; } = string.Empty;
        public string PrescribedMeds { get; set; } = string.Empty;
        public DateTime ConsultationDate { get; set; }
        public SourceType SourceType { get; set; }
    }
}
