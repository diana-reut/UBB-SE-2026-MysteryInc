using HospitalManagement.ViewModel;
using Microsoft.UI.Xaml.Controls;
using System;


namespace HospitalManagement.View;

internal sealed partial class OrganDonorDialog : ContentDialog
{
    public OrganDonorViewModel ViewModel { get; set; }

    public OrganDonorDialog(OrganDonorViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        PrimaryButtonClick += (s, e) =>
        {
            if (ViewModel.SelectedMatch is null)
            {
                e.Cancel = true;
                ViewModel.ErrorMessage = "Please select a recipient from the list before confirming.";
                return;
            }

            if (string.IsNullOrEmpty(ViewModel.SelectedOrgan))
            {
                e.Cancel = true;
                ViewModel.ErrorMessage = "Please select an organ before confirming.";
                return;
            }

            try
            {
                ViewModel.ConfirmAssignment();
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                ViewModel.ErrorMessage = $"Error: {ex.Message}";
            }
        };
    }


    // Initialize the dialog with a deceased donor and handle confirmation.
    public void Initialize(Action<int, int, float> onAssigned)
    {
        // deceased patient nu facea nimic asa ca l am scos
        ViewModel.OnAssignmentConfirmed = onAssigned;
    }
}
