using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagement.Entity
{
    public class Allergy
    {
        public int AllergyId { get; set; }
        public string AllergyName { get; set; } = string.Empty;
        public string? AllergyType { get; set; }
        public string? AllergyCategory { get; set; }
    }
}
