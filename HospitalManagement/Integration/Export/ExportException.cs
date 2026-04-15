using System;

namespace HospitalManagement.Integration.Export;

public class ExportException : Exception
{
    public ExportException(string message)
        : base(message) { }

    public ExportException()
    {
    }

    public ExportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
