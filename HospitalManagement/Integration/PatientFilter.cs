using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity.Enums;

namespace HospitalManagement.Integration
{
    class PatientFilter
    {
        public string? namePart;
        public string? CNP;
        public int? minAge;
        public int? maxAge;
        public Sex? sex;
        public bool? hasChronicCond;
        public DateTime? lastUpdatedFrom;
        public DateTime? lastUpdatedTo;
        public BloodType? bloodType;
        public RhEnum? rh;
    }
}
