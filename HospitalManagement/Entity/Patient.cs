using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Entity
{
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Cnp { get; set; } = string.Empty;
        public DateTime Dob { get; set; }
        public DateTime? Dod { get; set; }
        public Sex Sex { get; set; }
        public string PhoneNo { get; set; } = string.Empty;
        public string EmergencyContact { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public bool IsDonor { get; set; }

        public MedicalHistory? MedicalHistory { get; set; }
    }
}