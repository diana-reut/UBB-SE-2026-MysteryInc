using HospitalManagement.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;



namespace HospitalManagement.View;

internal sealed partial class MedicalStaffView : Window
{
    public MedicalStaffViewModel ViewModel { get; }

    public MedicalStaffView()
    {
        InitializeComponent();
        ViewModel = new MedicalStaffViewModel();

        if (Content is FrameworkElement rootElement)
        {
            rootElement.DataContext = ViewModel;
        }
    }

    // This goes in the code-behind of the page that lists all the patients
    private void PatientList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        // 1. Cast the sender to a ListView, then check if the selected item is your Patient class
        if (sender is Microsoft.UI.Xaml.Controls.ListView listView
            && listView.SelectedItem is Entity.Patient selectedPatient)
        {
            // 2. Create the Window
            var newWindow = new Window
            {
                Title = "Patient Medical Profile",
            };

            // 3. Instantiate your Page passing the actual Patient Id
            IServiceProvider scope = (Application.Current as App).Services;
            PatientProfileView profilePage = scope.GetRequiredService<PatientProfileView>();
            profilePage.Initialize(selectedPatient.Id);

            // 4. Attach and show
            newWindow.Content = profilePage;
            newWindow.Activate();
        }
    }
}
