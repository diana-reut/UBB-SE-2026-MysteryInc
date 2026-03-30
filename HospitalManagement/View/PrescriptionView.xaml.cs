using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace HospitalManagement.View
{
    public sealed partial class PrescriptionView : UserControl
    {
        // Instantiat din clasa Parent (PharmacistView/Model) la lansare
        public ViewModel.PrescriptionViewModel ViewModel { get; set; }

        public PrescriptionView()
        {
            this.InitializeComponent();
        }

        private void OnApplyFilterClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            int? idSearch = int.TryParse(SearchIdBox.Text, out int id) ? id : null;
            string patientOrDoctor = string.IsNullOrWhiteSpace(SearchNameBox.Text) ? null : SearchNameBox.Text;
            string medname = string.IsNullOrWhiteSpace(SearchMedBox.Text) ? null : SearchMedBox.Text;

            DateTime? fromDate = DateFromPicker.Date?.Date;
            DateTime? toDate = DateToPicker.Date?.Date;

            ViewModel.ApplyFilterCommand(idSearch, medname, fromDate, toDate, patientOrDoctor, patientOrDoctor);
        }

        private void OnNextClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.NextPage();
        }

        private void OnPrevClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.PrevPage();
        }

        private void OnCloseFlyoutClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Parent is FrameworkElement element)
            {
                var flyout = FlyoutBase.GetAttachedFlyout(btn) ?? DefaultFlyout(element);
                flyout?.Hide();
            }
        }

        private FlyoutBase DefaultFlyout(FrameworkElement element)
        {
            while (element != null && !(element is Button b && b.Flyout != null))
            {
                element = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return (element as Button)?.Flyout;
        }
    }
}
