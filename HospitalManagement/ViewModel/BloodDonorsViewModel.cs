using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitalManagement.Database;
using HospitalManagement.Entity;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    public class DonorMatchModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Cnp { get; set; }
        public string BloodType { get; set; }
        public string RhFactor { get; set; }
        public int Score { get; set; }
    }

    public class BloodDonorsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<DonorMatchModel> _donors;

        public ObservableCollection<DonorMatchModel> Donors
        {
            get => _donors;
            set { _donors = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BloodDonorsViewModel(int patientId)
        {
            var dbContext = new HospitalDbContext();

            var patientRepo = new PatientRepository(dbContext);
            var historyRepo = new MedicalHistoryRepository(dbContext);

            var bloodService = new BloodCompatibilityService(patientRepo, historyRepo);

            var recipient = patientRepo.GetById(patientId);

            // THE FIX: We must eagerly load the recipient's medical history 
            // from the database so the CalculateScore method doesn't crash!
            if (recipient != null)
            {
                recipient.MedicalHistory = historyRepo.GetByPatientId(patientId);
            }

            var topDonors = bloodService.GetTopCompatibleDonors(patientId);

            var displayList = new ObservableCollection<DonorMatchModel>();

            // Added a safety check: recipient.MedicalHistory != null
            if (recipient != null && topDonors != null && recipient.MedicalHistory != null)
            {
                foreach (var donor in topDonors)
                {
                    // Now this has all the data it needs and will not crash!
                    int matchScore = BloodCompatibilityService.CalculateScore(donor, recipient);

                    displayList.Add(new DonorMatchModel
                    {
                        FirstName = donor.FirstName,
                        LastName = donor.LastName,
                        Cnp = donor.Cnp,
                        BloodType = donor.MedicalHistory?.BloodType?.ToString() ?? "Unknown",
                        RhFactor = donor.MedicalHistory?.Rh?.ToString() ?? "Unknown",
                        Score = matchScore
                    });
                }
            }

            Donors = displayList;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}