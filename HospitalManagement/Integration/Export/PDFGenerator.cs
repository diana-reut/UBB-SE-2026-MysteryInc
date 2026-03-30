using System;
using System.Collections.Generic;
using System.IO;
using HospitalManagement.Entity;

using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

using Paragraph = iText.Layout.Element.Paragraph;


namespace HospitalManagement.Integration.Export
{
    public class PDFGenerator
    {
        public FileInfo GenerateRecordPDF(
            MedicalRecord record,
            Patient patient,
            Prescription? prescription,
            List<PrescriptionItem> items)
        {
            string fileName = $"MedicalRecord_{patient.FirstName}{patient.LastName}_{record.ConsultationDate:yyyyMMdd}.pdf";
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            using (PdfWriter writer = new PdfWriter(filePath))
            using (PdfDocument pdf = new PdfDocument(writer))
            using (Document doc = new Document(pdf))
            {
                // Header
                doc.Add(new Paragraph($"Patient: {patient.FirstName} {patient.LastName}")
                    .SetFontSize(16));
                doc.Add(new Paragraph($"CNP: {patient.Cnp}"));
                doc.Add(new Paragraph($"Consultation Date: {record.ConsultationDate:dd-MM-yyyy HH:mm}"));

                doc.Add(new Paragraph("\n"));

                // Section 1 — Clinical findings
                doc.Add(new Paragraph("Section 1: Clinical Findings")
                    .SetFontSize(14));
                doc.Add(new Paragraph($"Symptoms: {record.Symptoms ?? "N/A"}"));
                doc.Add(new Paragraph($"Diagnosis: {record.Diagnosis ?? "N/A"}"));

                doc.Add(new Paragraph("\n"));

                // Section 2 — Prescribed treatment
                doc.Add(new Paragraph("Section 2: Prescribed Treatment")
                    .SetFontSize(14));

                if (prescription == null || items.Count == 0)
                {
                    doc.Add(new Paragraph("No prescription issued for this consultation."));
                }
                else
                {
                    doc.Add(new Paragraph($"Doctor Notes: {prescription.DoctorNotes ?? "None"}"));
                    doc.Add(new Paragraph("Medications:"));
                    foreach (var item in items)
                    {
                        doc.Add(new Paragraph($"  - {item.MedName}: {item.Quantity}"));
                    }
                }
            }

            return new FileInfo(filePath);
        }
    }
}
