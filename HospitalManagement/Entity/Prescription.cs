using System;
using System.Collections.Generic;

namespace HospitalManagement.Entity;

public class Prescription
{
    public int Id { get; set; }

    public int RecordId { get; set; }

    public List<PrescriptionItem> MedicationList { get; set; } = new();

    public string? DoctorNotes { get; set; }

    public DateTime Date { get; set; }

    public string PatientName { get; set; } = "Unknown";

    public string DoctorName { get; set; } = "Unknown";
}
