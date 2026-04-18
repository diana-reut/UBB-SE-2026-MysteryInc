using System.Collections.Generic;
using HospitalManagement.Entity;
using HospitalManagement.Repository;

namespace HospitalManagement.Integration.Export;

internal class ExportService
{
    private readonly MedicalRecordRepository _recordRepo;
    private readonly PrescriptionRepository _prescriptionRepo;
    private readonly PatientRepository _patientRepo;
    private readonly MedicalHistoryRepository _historyRepo;

    public ExportService(
        MedicalRecordRepository recordRepo,
        PrescriptionRepository prescriptionRepo,
        PatientRepository patientRepo,
        MedicalHistoryRepository historyRepo)
    {
        _recordRepo = recordRepo;
        _prescriptionRepo = prescriptionRepo;
        _patientRepo = patientRepo;
        _historyRepo = historyRepo;
    }

    public string ExportRecordToPDF(int recordId)
    {
        MedicalRecord? record = _recordRepo.GetById(recordId) ?? throw new ExportException($"MedicalRecord with ID={recordId} not found.");

        MedicalHistory? history = _historyRepo.GetByPatientId(record.HistoryId) ?? throw new ExportException($"MedicalHistory for record ID={recordId} not found.");

        Patient? patient = _patientRepo.GetById(history.PatientId) ?? throw new ExportException($"Patient for history ID={history.Id} not found.");

        // THE FIX: Use the PrescriptionRepo to find the prescription!
        var items = new List<PrescriptionItem>();
        Prescription? prescription = _prescriptionRepo.GetByRecordId(recordId);

        if (prescription is not null)
        {
            items = _prescriptionRepo.GetItems(prescription.Id);
        }

        return PDFGenerator.GenerateRecordPDF(record, patient, prescription, items);
    }
}
