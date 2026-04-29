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
using HospitalManagement.ViewModel;

namespace HospitalManagement.View;

internal sealed partial class MedicalHistoryDialog : ContentDialog
{
    private readonly MedicalHistoryDialogViewModel _viewModel;

    public MedicalHistory MedicalHistory => _viewModel.MedicalHistory!;
    public bool WasSkipped { get; private set; }

    public MedicalHistoryDialog()
    {
        InitializeComponent();

        IAllergyService allergyService =
          (Application.Current as App)!.Services.GetRequiredService<IAllergyService>();

        _viewModel = new MedicalHistoryDialogViewModel(allergyService);

        Closing += MedicalHistoryDialog_Closing;
        AllergiesList.ItemsSource = _viewModel.AllergyList;
    }

    public void Initialize()
    {
        _viewModel.LoadAllergies();
        AllergyNameEntry.ItemsSource = _viewModel.AvailableAllergies;
        AllergyNameEntry.DisplayMemberPath = "AllergyName";
        AllergyNameEntry.SelectedValuePath = "AllergyId";

    }

    private void MedicalHistoryDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (args.Result != ContentDialogResult.Primary)
        {
            WasSkipped = true;
        }
    }

    private void AddAllergyButton_Click(object sender, RoutedEventArgs e)
    {
        string severity =(AllergySeverityEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Mild";

        bool added = _viewModel.TryAddAllergy(
            AllergyNameEntry.SelectedItem as Allergy,
            severity);

        if (added)
        {
            AllergyNameEntry.SelectedIndex = -1;
            AllergySeverityEntry.SelectedIndex = 0;
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

        string bloodType =
            (BloodTypeEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "A";

        string rh =
            (RhFactorEntry.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Positive";

        bool created = _viewModel.TryCreateMedicalHistory(
            bloodType,
            rh,
            ChronicConditionsEntry.Text);

        if (!created)
        {
            args.Cancel = true;
            return;
        }
        WasSkipped = false;
    }
}
