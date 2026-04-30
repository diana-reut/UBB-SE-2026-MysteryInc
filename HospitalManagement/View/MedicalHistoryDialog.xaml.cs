using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Service;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace HospitalManagement.View;

internal sealed partial class MedicalHistoryDialog : ContentDialog
{
    public MedicalHistory MedicalHistory { get; private set; } = null!;

    public bool WasSkipped { get; private set; }

    // Track allergies being added in the dialog: wrapper objects instead of tuples
    private readonly ObservableCollection<AllergyEntry> _allergyList = [];
    private List<Allergy> _availableAllergies = [];

    private readonly IAllergyService _allergyService;

    public MedicalHistoryDialog()
    {
        InitializeComponent();
        _allergyService = (Application.Current as App)!.Services.GetRequiredService<IAllergyService>();
        Closing += MedicalHistoryDialog_Closing;
        AllergiesList.ItemsSource = _allergyList;
    }

    public void Initialize()
    {
        try
        {
            _availableAllergies = [.. _allergyService.GetAllergies()];

            AllergyNameEntry.ItemsSource = _availableAllergies;
            AllergyNameEntry.DisplayMemberPath = "AllergyName";
            AllergyNameEntry.SelectedValuePath = "AllergyId";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load allergies in Dialog: {ex.Message}");
        }
    }

    private void MedicalHistoryDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        // If user clicked "Skip" or closed dialog (not Primary button), mark as skipped
        if (args.Result != ContentDialogResult.Primary)
        {
            WasSkipped = true;
        }
    }

    private void AddAllergyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get selected allergy from ComboBox
            string severity = (AllergySeverityEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Mild";

            // Validate selection
            if (AllergyNameEntry.SelectedItem is not Allergy selectedAllergy)
            {
                System.Diagnostics.Debug.WriteLine("Allergy must be selected");
                return;
            }

            // Check if allergy already added
            if (_allergyList.Any(a => a.Allergy.AllergyId == selectedAllergy.AllergyId))
            {
                System.Diagnostics.Debug.WriteLine("This allergy has already been added");
                return;
            }

            // Add to list using wrapper class
            _allergyList.Add(new AllergyEntry { Allergy = selectedAllergy, Severity = severity, });

            System.Diagnostics.Debug.WriteLine($"ADDED ALLERGY: {selectedAllergy.AllergyName} - {severity}");
            System.Diagnostics.Debug.WriteLine($"Total allergies in list: {_allergyList.Count}");

            // Clear selection
            AllergyNameEntry.SelectedIndex = -1;
            AllergySeverityEntry.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding allergy: {ex.Message}");
        }
    }

    private void RemoveAllergyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is AllergyEntry allergyEntry)
        {
            _viewModel.RemoveAllergy(allergyEntry);
        }
    }


    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            // Parse Blood Type
            string bloodTypeStr = (BloodTypeEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "A";
            BloodType bloodType = Enum.Parse<BloodType>(bloodTypeStr);

            // Parse RH Factor
            string rhStr = (RhFactorEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Positive";
            Rh rhFactor = rhStr == "Positive" ? Rh.Positive : Rh.Negative;

            // Parse Chronic Conditions (comma-separated)
            List<string> chronicConditions = [];
            if (!string.IsNullOrWhiteSpace(ChronicConditionsEntry.Text))
            {
                chronicConditions = [.. ChronicConditionsEntry.Text
                    .Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))];
            }

            // Convert allergy list to MedicalHistory format
            List<(Allergy, string)> allergies = [.. _allergyList.Select(entry => (entry.Allergy, entry.Severity))];

            // Create MedicalHistory object
            MedicalHistory = new MedicalHistory
            {
                BloodType = bloodType,
                Rh = rhFactor,
                ChronicConditions = chronicConditions,
                Allergies = allergies,
            };

            WasSkipped = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing medical history: {ex.Message}");
            args.Cancel = true;
        }
    }
}
