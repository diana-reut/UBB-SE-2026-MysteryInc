using Microsoft.UI.Xaml.Controls;
using HospitalManagement.Entity;
using System;
using HospitalManagement.Validators;

namespace HospitalManagement.View;

internal sealed partial class AddPatientDialog : ContentDialog
{
    public Patient NewPatient { get; private set; } = null!;

    public AddPatientDialog()
    {
        InitializeComponent();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        bool formHasError = false;
        (TextBlock Error, bool Valid)[] validationResults =
        [
            (Error: FirstNameError, Valid: ValidationHelper.IsValidName(FirstNameEntry.Text)),
            (Error: LastNameError, Valid: ValidationHelper.IsValidName(LastNameEntry.Text)),
            (Error: CnpError, Valid: ValidationHelper.IsValidCnp(CnpEntry.Text)),
            (Error: PhoneError, Valid: ValidationHelper.IsValidPhone(PhoneEntry.Text)),
            (Error: EmergencyError, Valid: ValidationHelper.IsValidPhone(EmergencyEntry.Text)),
        ];

        for (int i = 0; i < validationResults.Length; i++)
        {
            (TextBlock error, bool valid) = validationResults[i];
            if (valid)
            {
                error.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                error.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                formHasError = true;
            }
        }


        if (formHasError)
        {
            args.Cancel = true;
            return;
        }

        NewPatient = new Patient
        {
            FirstName = FirstNameEntry.Text,
            LastName = LastNameEntry.Text,
            Sex = Enum.Parse<Entity.Enums.Sex>((SexEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "M"),
            Dob = DobEntry.Date?.DateTime ?? DateTime.Now,
            Cnp = CnpEntry.Text,
            PhoneNo = PhoneEntry.Text,
            EmergencyContact = EmergencyEntry.Text,
        };
    }
}
