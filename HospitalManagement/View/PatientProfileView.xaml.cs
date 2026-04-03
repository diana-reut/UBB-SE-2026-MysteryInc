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
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CurrentPatient != null)
            {
                var dbContext = new HospitalManagement.Database.HospitalDbContext();
                var patientRepo = new HospitalManagement.Repository.PatientRepository(dbContext);
                var historyRepo = new HospitalManagement.Repository.MedicalHistoryRepository(dbContext);
                var recordRepo = new HospitalManagement.Repository.MedicalRecordRepository(dbContext);

                var patientService = new HospitalManagement.Service.PatientService(patientRepo, historyRepo, recordRepo);

                // Check if the current patient is high risk
                if (patientService.IsHighRiskPatient(ViewModel.CurrentPatient.Id))
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "High Risk Patient Alert",
                        Content = "Warning: This patient is flagged as High Risk because they have had more than 10 ER visits in the last 3 months.",
                        CloseButtonText = "Acknowledge",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
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





        // ==========================================
        // IMPORT RECORD FEATURE
        // ==========================================

        private async void ImportER_Click(object sender, RoutedEventArgs e)
        {
            await HandleImport(isER: true);
        }

        private async void ImportStaff_Click(object sender, RoutedEventArgs e)
        {
            await HandleImport(isER: false);
        }

        private async System.Threading.Tasks.Task HandleImport(bool isER)
        {
            if (ViewModel.CurrentPatient == null) return;

            try
            {
                // 1. Initialize databases & repositories
                var dbContext = new HospitalManagement.Database.HospitalDbContext();
                var patientRepo = new HospitalManagement.Repository.PatientRepository(dbContext);
                var historyRepo = new HospitalManagement.Repository.MedicalHistoryRepository(dbContext);
                var recordRepo = new HospitalManagement.Repository.MedicalRecordRepository(dbContext);
                var prescriptionRepo = new HospitalManagement.Repository.PrescriptionRepository(dbContext);

                var patientService = new HospitalManagement.Service.PatientService(patientRepo, historyRepo, recordRepo);

                // 2. Initialize the Mock External Proxies
                // (We pass 'null' for the publisher since we are only fetching records, not triggering the observer)
                var externalER = new HospitalManagement.Integration.External.MockERProxy(null); ////////////OBSERVEEEERRRRR!!!!!!!!!!!!!!!!!!!!!1111111111
                var externalStaff = new HospitalManagement.Integration.External.MockStaffProxy(null); ////////////OBSERVEEEERRRRR!!!!!!!!!!!!!!11

                // 3. Initialize your teammate's Import Service
                var importService = new HospitalManagement.Service.ImportService(
                    patientService, recordRepo, prescriptionRepo, externalER, externalStaff);

                int patientId = ViewModel.CurrentPatient.Id;

                // 4. Trigger the Import (we pass '1' as a dummy external ID since the mocks ignore it anyway)
                if (isER)
                {
                    importService.ImportFromER(patientId, 1);
                }
                else
                {
                    importService.ImportFromAppointment(patientId, 1);
                }

                // 5. Refresh the ViewModel so the new record instantly appears in the list!
                ViewModel.LoadFullPatientProfile(patientId);

                // 6. Show Success Dialog
                var dialog = new ContentDialog
                {
                    Title = "Import Successful",
                    Content = $"Record and associated prescriptions successfully imported from {(isER ? "the ER" : "the Staff App")}.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Import Failed",
                    Content = $"An error occurred during import: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }






    }
}