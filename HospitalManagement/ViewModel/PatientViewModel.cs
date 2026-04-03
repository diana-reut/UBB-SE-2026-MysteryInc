using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Entity;
using HospitalManagement.Service;
using HospitalManagement.Integration.Export;

namespace HospitalManagement.ViewModel
{
    public class PatientViewModel : INotifyPropertyChanged
    {
        private readonly PatientService _patientService;
        private readonly ExportService _exportService;
        private readonly BillingService _billingService;

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged();
                if (_selectedPatient != null)
                {
                    LoadMedicalHistory();
                    LoadMedicalRecords();
                }
            }
        }

        private MedicalHistory _medicalHistory;
        public MedicalHistory MedicalHistory
        {
            get => _medicalHistory;
            set 
            { 
                _medicalHistory = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(ChronicConditionsFormatted));
            }
        }

        public string ChronicConditionsFormatted
        {
            get => _medicalHistory?.ChronicConditions != null && _medicalHistory.ChronicConditions.Count > 0
                ? string.Join("; ", _medicalHistory.ChronicConditions)
                : "None";
        }

        private ObservableCollection<MedicalRecord> _medicalRecords;
        public ObservableCollection<MedicalRecord> MedicalRecords
        {
            get => _medicalRecords;
            set { _medicalRecords = value; OnPropertyChanged(); }
        }

        private MedicalRecord _selectedMedicalRecord;
        public MedicalRecord SelectedMedicalRecord
        {
            get => _selectedMedicalRecord;
            set 
            { 
                _selectedMedicalRecord = value; 
                OnPropertyChanged();
                // Calculate base price when record is selected
                if (_selectedMedicalRecord != null && _billingService != null && SelectedPatient != null)
                {
                    try
                    {
                        BasePrice = _billingService.computeBasePrice(SelectedPatient.Id, _selectedMedicalRecord.Id);
                        FinalPrice = _selectedMedicalRecord.FinalPrice > 0 ? _selectedMedicalRecord.FinalPrice : BasePrice;
                        DiscountApplied = _selectedMedicalRecord.DiscountApplied.HasValue;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error calculating base price: {ex.Message}");
                    }
                }
            }
        }

        private ObservableCollection<string> _allergies;
        public ObservableCollection<string> Allergies
        {
            get => _allergies;
            set { _allergies = value; OnPropertyChanged(); }
        }

        private Prescription _selectedPrescription;
        public Prescription SelectedPrescription
        {
            get => _selectedPrescription;
            set { _selectedPrescription = value; OnPropertyChanged(); }
        }

        private decimal _basePrice;
        public decimal BasePrice
        {
            get => _basePrice;
            set { _basePrice = value; OnPropertyChanged(); }
        }

        private decimal _finalPrice;
        public decimal FinalPrice
        {
            get => _finalPrice;
            set { _finalPrice = value; OnPropertyChanged(); }
        }

        private bool _discountApplied;
        public bool DiscountApplied
        {
            get => _discountApplied;
            set { _discountApplied = value; OnPropertyChanged(); }
        }

        public ICommand BackCommand { get; }
        public ICommand ExportRecordCommand { get; }
        public ICommand ViewPrescriptionCommand { get; }
        public ICommand ApplyDiscountCommand { get; }

        public Action GoBackAction { get; set; }
        public Action<decimal, Action<int, decimal>> OpenRouletteAction { get; set; }

        public PatientViewModel(PatientService patientService, ExportService exportService = null, BillingService billingService = null)
        {
            _patientService = patientService;
            _exportService = exportService;
            _billingService = billingService;
            MedicalRecords = new ObservableCollection<MedicalRecord>();
            Allergies = new ObservableCollection<string>();
            BackCommand = new RelayCommand(GoBack);
            ExportRecordCommand = new RelayCommand(ExportSelectedRecord, CanExportRecord);
            ViewPrescriptionCommand = new RelayCommand(ViewSelectedPrescription, CanViewPrescription);
            ApplyDiscountCommand = new RelayCommand(ApplyDiscount, CanApplyDiscount);
        }

        private void LoadMedicalHistory()
        {
            if (SelectedPatient == null)
            {
                MedicalHistory = null;
                return;
            }

            try
            {
                // Assuming PatientService has a method to get medical history
                MedicalHistory = _patientService.GetMedicalHistory(SelectedPatient.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading medical history: {ex.Message}");
            }
        }

        private void LoadMedicalRecords()
        {
            if (SelectedPatient == null || MedicalHistory == null)
            {
                MedicalRecords.Clear();
                Allergies.Clear();
                return;
            }

            try
            {
                var records = _patientService.GetMedicalRecords(MedicalHistory.Id);
                
                MedicalRecords.Clear();
                foreach (var record in records.OrderByDescending(r => r.ConsultationDate))
                {
                    MedicalRecords.Add(record);
                }

                // Load allergies
                var allergies = _patientService.GetPatientAllergies(SelectedPatient.Id);
                Allergies.Clear();
                foreach (var allergy in allergies)
                {
                    Allergies.Add(allergy);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading medical records: {ex.Message}");
            }
        }

        private void GoBack()
        {
            GoBackAction?.Invoke();
        }

        private bool CanExportRecord()
        {
            return SelectedMedicalRecord != null && _exportService != null;
        }

        private bool CanViewPrescription()
        {
            return SelectedMedicalRecord != null && SelectedMedicalRecord.PrescriptionId.HasValue;
        }

        private bool CanApplyDiscount()
        {
            return SelectedMedicalRecord != null && !DiscountApplied && _billingService != null;
        }

        private void ViewSelectedPrescription()
        {
            if (SelectedMedicalRecord?.PrescriptionId == null)
                return;

            try
            {
                SelectedPrescription = _patientService.GetPrescriptionByRecordId(SelectedMedicalRecord.Id);
                if (SelectedPrescription == null)
                    System.Diagnostics.Debug.WriteLine($"No prescription found for record {SelectedMedicalRecord.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading prescription: {ex.Message}");
            }
        }

        private void ApplyDiscount()
        {
            if (SelectedMedicalRecord == null || _billingService == null)
                return;

            OpenRouletteAction?.Invoke(BasePrice, (discount, finalPrice) =>
            {
                try
                {
                    // Update the medical record with discount and final price
                    SelectedMedicalRecord.DiscountApplied = discount;
                    SelectedMedicalRecord.FinalPrice = finalPrice;
                    
                    // Update the UI
                    FinalPrice = finalPrice;
                    DiscountApplied = true;
                    OnPropertyChanged(nameof(SelectedMedicalRecord));

                    System.Diagnostics.Debug.WriteLine($"Discount applied: {discount}% | Final Price: {finalPrice}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying discount: {ex.Message}");
                }
            });
        }

        private void ExportSelectedRecord()
        {
            if (SelectedMedicalRecord == null || _exportService == null)
                return;

            try
            {
                _exportService.ExportRecordToPDF(SelectedMedicalRecord.Id);
                System.Diagnostics.Debug.WriteLine($"Successfully exported record {SelectedMedicalRecord.Id} to PDF");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting record: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
