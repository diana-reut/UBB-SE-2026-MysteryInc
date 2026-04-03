using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HospitalManagement.ViewModel;
using HospitalManagement.Database;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using HospitalManagement.Entity;


namespace HospitalManagement.View
{
    public sealed partial class MedicalStaffView : Window
    {
        public MedicalStaffViewModel ViewModel { get; }

        public MedicalStaffView()
        {
            this.InitializeComponent();
            ViewModel = new MedicalStaffViewModel();

            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;
            }
        }

        // This goes in the code-behind of the page that lists all the patients
        private void PatientList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            // 1. Cast the sender to a ListView, then check if the selected item is your Patient class
            if (sender is Microsoft.UI.Xaml.Controls.ListView listView &&
                listView.SelectedItem is HospitalManagement.Entity.Patient selectedPatient)
            {
                // 2. Create the Window
                var newWindow = new Window();
                newWindow.Title = "Patient Medical Profile";

                // 3. Instantiate your Page passing the actual Patient Id
                var profilePage = new HospitalManagement.View.PatientProfileView(selectedPatient.Id);

                // 4. Attach and show
                newWindow.Content = profilePage;
                newWindow.Activate();
            }
        }
    }
}