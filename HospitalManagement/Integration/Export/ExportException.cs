using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagement.Integration.Export
{
    public class ExportException : Exception
    {
        public ExportException(string message) : base(message) { }
    }
}
