using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;

namespace HospitalManagement.View;

internal sealed partial class BloodDonorsView : Page
{
    public BloodDonorsViewModel ViewModel { get; }

    public BloodDonorsView(int patientId)
    {
        // Pass the patient ID so it knows exactly who to search for!
        ViewModel = new BloodDonorsViewModel(patientId);
        InitializeComponent();
    }
}
