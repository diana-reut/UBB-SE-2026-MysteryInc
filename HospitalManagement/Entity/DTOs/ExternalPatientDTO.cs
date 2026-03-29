using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Entity.DTOs
{
    public class ExternalPatientDTO
    {
        public string CNP { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Sex Sex { get; set; }
        public DateTime EmergencyTimestamp { get; set; }
        public string? Injury { get; set; }
    }
}
