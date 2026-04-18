using Microsoft.UI.Xaml.Controls;
using HospitalManagement.Entity;
using System;
using Visibility = Microsoft.UI.Xaml.Visibility; // Fixes the Visibility error
using System.Linq;

namespace HospitalManagement.View
{
    internal sealed partial class AddPatientDialog : ContentDialog
    {
        public Patient NewPatient { get; private set; }

        public AddPatientDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 1. Reset all error states using the full namespace to avoid CS0176
            FirstNameError.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            LastNameError.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            CnpError.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            PhoneError.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            EmergencyError.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            bool hasError = false;

            // 2. Validate Fields
            if (string.IsNullOrWhiteSpace(FirstNameEntry.Text))
            {
                FirstNameError.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(LastNameEntry.Text))
            {
                LastNameError.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(CnpEntry.Text) || CnpEntry.Text.Length != 13)
            {
                CnpError.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(PhoneEntry.Text) || PhoneEntry.Text.Length != 10 || !PhoneEntry.Text.All(char.IsDigit))
            {
                PhoneError.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(EmergencyEntry.Text) || EmergencyEntry.Text.Length != 10 || !EmergencyEntry.Text.All(char.IsDigit))
            {
                EmergencyError.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                hasError = true;
            }

            // 3. Block if any field is invalid
            if (hasError)
            {
                args.Cancel = true;
                return;
            }

            // 4. Success: Map data
            this.NewPatient = new Patient
            {
                FirstName = FirstNameEntry.Text,
                LastName = LastNameEntry.Text,
                Sex = Enum.Parse<HospitalManagement.Entity.Enums.Sex>((SexEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "M"),
                Dob = DobEntry.Date?.DateTime ?? DateTime.Now,
                Cnp = CnpEntry.Text,
                PhoneNo = PhoneEntry.Text,
                EmergencyContact = EmergencyEntry.Text
            };
        }
    }
}