using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Entity
{
    public class Transplant
    {
        public int TransplantId { get; set; }
        public int ReceiverId { get; set; }
        public int? DonorId { get; set; }
        public string OrganType { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public DateTime? TransplantDate { get; set; }
        public TransplantStatus Status { get; set; }
        public float CompatibilityScore { get; set; }
    }
}
