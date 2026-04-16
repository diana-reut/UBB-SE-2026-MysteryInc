using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;


namespace HospitalManagement.View;

internal sealed partial class OrganDonorDialog : ContentDialog
{
    public OrganDonorViewModel ViewModel { get; private set; } = null!;

    public OrganDonorDialog()
    {
        InitializeComponent();
    }


    // Initialize the dialog with a deceased donor and handle confirmation.
    public void Initialize(OrganDonorViewModel viewModel, System.Action<int, int, float> onAssigned)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        // Hook up confirmation callback
        ViewModel.OnAssignmentConfirmed = onAssigned;

        // Wire up primary button
        PrimaryButtonClick += (s, e) =>
        {
            try
            {
                // Clear previous errors
                ViewModel.ErrorMessage = "";

                // Validate selection before confirming
                if (ViewModel.SelectedMatch is null)
                {
                    e.Cancel = true; // Keep dialog open
                    ViewModel.ErrorMessage = "Please select a recipient from the list before confirming.";
                    return;
                }

                if (string.IsNullOrEmpty(ViewModel.SelectedOrgan))
                {
                    e.Cancel = true; // Keep dialog open
                    ViewModel.ErrorMessage = "Please select an organ before confirming.";
                    return;
                }

                ViewModel.ConfirmAssignment();
            }
            catch (System.Exception ex)
            {
                e.Cancel = true; // Keep dialog open
                ViewModel.ErrorMessage = $"Error: {ex.Message}";
            }
        };
    }
}
