using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using HospitalManagement.ViewModel;
using HospitalManagement.Entity;
using System;

namespace HospitalManagement.View
{
    public sealed partial class PatientProfileView : Page
    {
        public PatientProfileViewModel ViewModel { get; }

        public PatientProfileView(int patientId)
        {
            ViewModel = new PatientProfileViewModel(patientId);
            this.InitializeComponent();

            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;
            }
        }

        // Catches the double-click from the Expander list and loads the details on the right
        private void RecordList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is MedicalRecord clickedRecord)
            {
                ViewModel.SelectedRecord = clickedRecord;
            }
        }

        // Catches the button click to open the prescription details
        private async void ViewPrescription_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedRecord != null)
            {
                // 1. Set up the database connection
                var dbContext = new HospitalManagement.Database.HospitalDbContext();
                var recordRepo = new HospitalManagement.Repository.MedicalRecordRepository(dbContext);

                // 2. Actually query the database to find the prescription using the Record ID!
                var actualPrescription = recordRepo.GetPrescription(ViewModel.SelectedRecord.Id);

                // 3. Check if the database found one
                if (actualPrescription != null)
                {
                    var prescriptionWindow = new Window();
                    prescriptionWindow.Title = "Prescription Details";

                    var prescriptionRepo = new HospitalManagement.Repository.PrescriptionRepository(dbContext);
                    var medicalHistoryRepo = new HospitalManagement.Repository.MedicalHistoryRepository(dbContext);

                    var prescriptionService = new HospitalManagement.Service.PrescriptionService(prescriptionRepo);
                    var addictService = new HospitalManagement.Service.AddictDetectionService(prescriptionRepo, medicalHistoryRepo);

                    var prescriptionVM = new HospitalManagement.ViewModel.PrescriptionViewModel(prescriptionService, addictService);

                    // 4. Use the ID from the prescription we just pulled from the DB!
                    prescriptionVM.ApplyFilterCommand(
                        searchId: actualPrescription.Id,
                        medName: null,
                        dateFrom: null,
                        dateTo: null,
                        patientName: null,
                        doctorName: null
                    );

                    var prescriptionPage = new HospitalManagement.View.PrescriptionView();

                    prescriptionPage.ViewModel = prescriptionVM;
                    if (prescriptionPage.Content is FrameworkElement root)
                    {
                        root.DataContext = prescriptionVM;
                    }

                    prescriptionWindow.Content = prescriptionPage;
                    prescriptionWindow.Activate();
                }
                else
                {
                    // If the DB query returns null, there genuinely is no prescription.
                    var dialog = new ContentDialog()
                    {
                        Title = "No Prescription",
                        Content = "This consultation does not have an associated prescription.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }


        private async void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedRecord != null)
            {
                try
                {
                    var dbContext = new HospitalManagement.Database.HospitalDbContext();
                    var recordRepo = new HospitalManagement.Repository.MedicalRecordRepository(dbContext);
                    var prescriptionRepo = new HospitalManagement.Repository.PrescriptionRepository(dbContext);
                    var patientRepo = new HospitalManagement.Repository.PatientRepository(dbContext);
                    var historyRepo = new HospitalManagement.Repository.MedicalHistoryRepository(dbContext);

                    var pdfGen = new HospitalManagement.Integration.Export.PDFGenerator();

                    var exportService = new HospitalManagement.Integration.Export.ExportService(
                        pdfGen, recordRepo, prescriptionRepo, patientRepo, historyRepo);

                    // 1. Generate the PDF straight to the Desktop
                    string savedFilePath = exportService.ExportRecordToPDF(ViewModel.SelectedRecord.Id);

                    // 2. THE FIX: Use standard .NET to launch the file (bulletproof for unpackaged apps)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = savedFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Export Failed",
                        Content = $"An error occurred during export: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }






    }
}