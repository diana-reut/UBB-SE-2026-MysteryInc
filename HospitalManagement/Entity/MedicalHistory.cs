using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Entity
{
    public class MedicalHistory
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public BloodType? BloodType { get; set; }
        public RhEnum? Rh { get; set; }

        public List<string> ChronicConditions { get; set; } = new();
        public List<MedicalRecord> MedicalRecords { get; set; } = new();
        public List<(Allergy Allergy, string SeverityLevel)> Allergies { get; set; } = new();
    }
}
