using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using HospitalManagement.ViewModel;
using HospitalManagement.Entity;

namespace HospitalManagement.View
{
    public sealed partial class PatientProfileView : Page
    {
        public PatientProfileViewModel ViewModel { get; }

        public PatientProfileView(int patientId)
        {
            ViewModel = new PatientProfileViewModel(patientId);
            this.InitializeComponent();

            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;
            }
        }

        // Catches the double-click from the Expander list and loads the details on the right
        private void RecordList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is MedicalRecord clickedRecord)
            {
                ViewModel.SelectedRecord = clickedRecord;
            }
        }
    }
}