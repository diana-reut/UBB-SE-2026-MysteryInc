using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HospitalManagement.Entity;
using HospitalManagement.Repository;

namespace HospitalManagement.Integration.Export
{
    public class ExportService
    {
        private readonly PDFGenerator _pdfGen;
        private readonly MedicalRecordRepository _recordRepo;
        private readonly PrescriptionRepository _prescriptionRepo;
        private readonly PatientRepository _patientRepo;
        private readonly MedicalHistoryRepository _historyRepo;

        public ExportService(
            PDFGenerator pdfGen,
            MedicalRecordRepository recordRepo,
            PrescriptionRepository prescriptionRepo,
            PatientRepository patientRepo,
            MedicalHistoryRepository historyRepo)
        {
            _pdfGen = pdfGen;
            _recordRepo = recordRepo;
            _prescriptionRepo = prescriptionRepo;
            _patientRepo = patientRepo;
            _historyRepo = historyRepo;
        }

        public bool ExportRecordToPDF(int recordId)
        {
            // SV12: assemble all data before passing to generator
            MedicalRecord? record = _recordRepo.GetById(recordId);
            if (record == null)
                throw new ExportException($"MedicalRecord with ID={recordId} not found.");

            // Get history using HistoryId from record
            MedicalHistory? history = _historyRepo.GetById(record.HistoryId);
            if (history == null)
                throw new ExportException($"MedicalHistory for record ID={recordId} not found.");

            Patient? patient = _patientRepo.GetById(history.PatientId);
            if (patient == null)
                throw new ExportException($"Patient for history ID={history.Id} not found.");

            // Prescription is optional
            Prescription? prescription = null;
            List<PrescriptionItem> items = new();
            if (record.PrescriptionId.HasValue)
            {
                prescription = _prescriptionRepo.GetByRecordId(recordId);
                if (prescription != null)
                    items = _prescriptionRepo.GetItems(prescription.Id);
            }

            _pdfGen.GenerateRecordPDF(record, patient, prescription, items);
            return true;
        }
    }
}
