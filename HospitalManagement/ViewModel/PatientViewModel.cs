using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitalManagement.Entity;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    public class PatientViewModel : INotifyPropertyChanged
    {
        private readonly PatientService _patientService;

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
            set { _selectedMedicalRecord = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _allergies;
        public ObservableCollection<string> Allergies
        {
            get => _allergies;
            set { _allergies = value; OnPropertyChanged(); }
        }

        public ICommand BackCommand { get; }

        public Action GoBackAction { get; set; }

        public PatientViewModel(PatientService patientService)
        {
            _patientService = patientService;
            MedicalRecords = new ObservableCollection<MedicalRecord>();
            Allergies = new ObservableCollection<string>();
            BackCommand = new RelayCommand(GoBack);
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
