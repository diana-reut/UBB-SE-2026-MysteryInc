using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Entity;
using System.Threading.Tasks;
using System;

namespace HospitalManagement.View
{
    public sealed partial class OrganDonorDialog : ContentDialog
    {
        public OrganDonorViewModel ViewModel { get; private set; }

        public OrganDonorDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Initialize the dialog with a deceased donor and handle confirmation
        /// </summary>
        public void Initialize(OrganDonorViewModel viewModel, System.Action<int, int, float> onAssigned)
        {
            ViewModel = viewModel;
            this.DataContext = viewModel;

            // Hook up confirmation callback
            ViewModel.OnAssignmentConfirmed = onAssigned;

            // Wire up primary button
            this.PrimaryButtonClick += (s, e) =>
            {
                try
                {
                    // Clear previous errors
                    ViewModel.ErrorMessage = "";

                    // Validate selection before confirming
                    if (ViewModel.SelectedMatch == null)
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
}
