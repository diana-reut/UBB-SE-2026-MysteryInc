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
            if (sender is Button btn)
            {
                // Un FlyoutPresenter e panoul ascuns de sistem unde stã tot con?inutul tãu (StackPanel-ul).
                // WinUI ne lasã sã tragem container-ul în care stã acest Buton direct.
                if (btn.Parent is DependencyObject obj)
                {
                    // Cãutãm în ascenden?ã foarte rapid direct spre FlyoutPresenter-ul nativ
                    var presenter = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(obj);
                    while (presenter != null && !(presenter is Microsoft.UI.Xaml.Controls.FlyoutPresenter))
                    {
                        presenter = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(presenter);
                    }

                    // Ne for?ãm la cel mai apropiat Popup din sistem ?i îl oprim
                    if (presenter is Microsoft.UI.Xaml.Controls.FlyoutPresenter fp)
                    {
                        var popup = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(fp) as Microsoft.UI.Xaml.Controls.Primitives.Popup;
                        if (popup != null)
                        {
                            popup.IsOpen = false;
                        }
                    }
                }
            }
        }
    }
}
