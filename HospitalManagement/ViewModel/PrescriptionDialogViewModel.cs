using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using HospitalManagement.Entity;

namespace HospitalManagement.ViewModel;

internal class PrescriptionDialogViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string _doctorNotes = "No notes provided";

    public string DoctorNotes
    {
        get => _doctorNotes;
        private set
        {
            if (_doctorNotes != value)
            {
                _doctorNotes = value;
                OnPropertyChanged(nameof(DoctorNotes));
            }
        }
    }

    public ObservableCollection<PrescriptionItem> PrescriptionItems { get; } = new();

    private Visibility _noMedicationsVisibility = Visibility.Visible;

    public Visibility NoMedicationsVisibility
    {
        get => _noMedicationsVisibility;
        private set
        {
            if (_noMedicationsVisibility != value)
            {
                _noMedicationsVisibility = value;
                OnPropertyChanged(nameof(NoMedicationsVisibility));
            }
        }
    }

    public PrescriptionDialogViewModel()
    {
        PrescriptionItems.CollectionChanged += (_, __) =>
        {
            UpdateVisibility();
        };
    }

    private void UpdateVisibility()
    {
        NoMedicationsVisibility =
            PrescriptionItems.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    public void Initialize(Prescription prescription)
    {
        PrescriptionItems.Clear();

        if (prescription == null)
        {
            DoctorNotes = "No prescription data available";
            UpdateVisibility();
            return;
        }

        DoctorNotes = prescription.DoctorNotes ?? "No notes provided";

        if (prescription.MedicationList != null)
        {
            foreach (var item in prescription.MedicationList)
            {
                PrescriptionItems.Add(item);
            }
        }

        UpdateVisibility();
    }
}