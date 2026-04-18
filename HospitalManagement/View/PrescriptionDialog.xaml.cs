using Microsoft.UI.Xaml.Controls;
using HospitalManagement.Entity;
using System;

namespace HospitalManagement.View
{
    internal sealed partial class PrescriptionDialog : ContentDialog
    {
        public PrescriptionDialog()
        {
            this.InitializeComponent();
        }

        public void Initialize(Prescription prescription)
        {
            if (prescription == null)
            {
                DoctorNotesBlock.Text = "No prescription data available";
                NoMedicationsBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }

            // Set doctor notes
            DoctorNotesBlock.Text = prescription.DoctorNotes ?? "No notes provided";

            // Set medications list
            if (prescription.MedicationList != null && prescription.MedicationList.Count > 0)
            {
                MedicationsList.ItemsSource = prescription.MedicationList;
                NoMedicationsBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                NoMedicationsBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
        }
    }
}
