using Microsoft.UI.Xaml.Controls;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HospitalManagement.View
{
    // Simple wrapper class for allergy entries
    internal class AllergyEntry
    {
        public Allergy Allergy { get; set; }
        public string Severity { get; set; }
    }

    internal sealed partial class MedicalHistoryDialog : ContentDialog
    {
        public MedicalHistory MedicalHistory { get; private set; }
        public bool WasSkipped { get; private set; }

        // Track allergies being added in the dialog: wrapper objects instead of tuples
        private ObservableCollection<AllergyEntry> _allergyList = new();
        private List<Allergy> _availableAllergies = new();

        public MedicalHistoryDialog()
        {
            this.InitializeComponent();
            this.Closing += MedicalHistoryDialog_Closing;
            AllergiesList.ItemsSource = _allergyList;
        }

        public void Initialize(List<Allergy> availableAllergies)
        {
            _availableAllergies = availableAllergies;
            
            // Bind allergies to ComboBox
            AllergyNameEntry.ItemsSource = _availableAllergies;
            AllergyNameEntry.DisplayMemberPath = "AllergyName";
            AllergyNameEntry.SelectedValuePath = "AllergyId";
        }

        private void MedicalHistoryDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            // If user clicked "Skip" or closed dialog (not Primary button), mark as skipped
            if (args.Result != ContentDialogResult.Primary)
            {
                WasSkipped = true;
            }
        }

        private void AddAllergyButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                // Get selected allergy from ComboBox
                var selectedAllergy = AllergyNameEntry.SelectedItem as Allergy;
                string severity = (AllergySeverityEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Mild";

                // Validate selection
                if (selectedAllergy == null)
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
                _allergyList.Add(new AllergyEntry { Allergy = selectedAllergy, Severity = severity });
                
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

        private void RemoveAllergyButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is AllergyEntry allergyEntry)
                {
                    _allergyList.Remove(allergyEntry);
                    System.Diagnostics.Debug.WriteLine($"REMOVED ALLERGY: {allergyEntry.Allergy.AllergyName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing allergy: {ex.Message}");
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
                RhEnum rhFactor = rhStr == "Positive" ? RhEnum.Positive : RhEnum.Negative;

                // Parse Chronic Conditions (comma-separated)
                List<string> chronicConditions = new();
                if (!string.IsNullOrWhiteSpace(ChronicConditionsEntry.Text))
                {
                    chronicConditions = ChronicConditionsEntry.Text
                        .Split(',')
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .ToList();
                }

                // Convert allergy list to MedicalHistory format
                List<(Allergy, string)> allergies = _allergyList
                    .Select(entry => (entry.Allergy, entry.Severity))
                    .ToList();

                // Create MedicalHistory object
                MedicalHistory = new MedicalHistory
                {
                    BloodType = bloodType,
                    Rh = rhFactor,
                    ChronicConditions = chronicConditions,
                    Allergies = allergies
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
}
