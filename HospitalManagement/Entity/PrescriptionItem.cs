using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagement.Entity
{
    public class PrescriptionItem
    {
        public int PrescrItemId { get; set; }
        public int PrescriptionId { get; set; }
        public string MedName { get; set; } = string.Empty;
        public string? Quantity { get; set; }
    }
}
