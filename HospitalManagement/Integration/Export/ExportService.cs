using System;
using System.Collections.Generic;
using HospitalManagement.Entity;
using HospitalManagement.Repository;

namespace HospitalManagement.Integration.Export
{
    internal class ExportService
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

        public string ExportRecordToPDF(int recordId)
        {
            MedicalRecord? record = _recordRepo.GetById(recordId);
            if (record == null)
                throw new ExportException($"MedicalRecord with ID={recordId} not found.");

            MedicalHistory? history = _historyRepo.GetByPatientId(record.HistoryId);
            if (history == null)
                throw new ExportException($"MedicalHistory for record ID={recordId} not found.");

            Patient? patient = _patientRepo.GetById(history.PatientId);
            if (patient == null)
                throw new ExportException($"Patient for history ID={history.Id} not found.");

            // THE FIX: Use the PrescriptionRepo to find the prescription!
            List<PrescriptionItem> items = new List<PrescriptionItem>();
            Prescription? prescription = _prescriptionRepo.GetByRecordId(recordId);

            if (prescription != null)
            {
                items = _prescriptionRepo.GetItems(prescription.Id);
            }

            return _pdfGen.GenerateRecordPDF(record, patient, prescription, items);
        }
    }
}