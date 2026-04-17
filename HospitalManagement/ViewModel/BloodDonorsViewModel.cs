using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel;

internal class BloodDonorsViewModel : INotifyPropertyChanged
{
    private ObservableCollection<DonorMatchModel>? _donors;

    public ObservableCollection<DonorMatchModel>? Donors
    {
        get => _donors;

        set
        {
            _donors = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public BloodDonorsViewModel(int patientId)
    {
        using var dbContext = new HospitalDbContext();

        var patientRepo = new PatientRepository(dbContext);
        var historyRepo = new MedicalHistoryRepository(dbContext);

        var bloodService = new BloodCompatibilityService(patientRepo, historyRepo);

        Entity.Patient? recipient = patientRepo.GetById(patientId);

        // THE FIX: We must eagerly load the recipient's medical history
        // from the database so the CalculateScore method doesn't crash!
        if (recipient is not null)
        {
            recipient.MedicalHistory = historyRepo.GetByPatientId(patientId);
        }

        System.Collections.Generic.List<Entity.Patient> topDonors = bloodService.GetTopCompatibleDonors(patientId);

        var displayList = new ObservableCollection<DonorMatchModel>();

        // Added a safety check: recipient.MedicalHistory != null
        if (recipient is not null && topDonors is not null && recipient.MedicalHistory is not null)
        {
            foreach (Entity.Patient donor in topDonors)
            {
                // Now this has all the data it needs and will not crash!
                int matchScore = bloodService.CalculateScore(donor, recipient);

                displayList.Add(new DonorMatchModel
                {
                    FirstName = donor.FirstName,
                    LastName = donor.LastName,
                    Cnp = donor.Cnp,
                    BloodType = donor.MedicalHistory?.BloodType?.ToString() ?? "Unknown",
                    RhFactor = donor.MedicalHistory?.Rh?.ToString() ?? "Unknown",
                    Score = matchScore,
                });
            }
        }

        Donors = displayList;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
