using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace HospitalManagement.View
{
    internal sealed partial class PrescriptionView : UserControl
    {
        public ViewModel.PrescriptionViewModel ViewModel { get; set; }

        public PrescriptionView()
        {
            this.InitializeComponent();
            DateFromPicker.DateChanged += DateFromPicker_DateChanged;
            DateToPicker.DateChanged += DateToPicker_DateChanged;
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

        private void DateFromPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (DateFromPicker.Date.HasValue)
            {
                DateToPicker.MinDate = DateFromPicker.Date.Value;
            }
            else
            {
                DateToPicker.MinDate = new DateTimeOffset(new DateTime(1920, 1, 1));
            }
        }

        private void DateToPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (DateToPicker.Date.HasValue)
            {
                DateFromPicker.MaxDate = DateToPicker.Date.Value;
            }
            else
            {
                DateFromPicker.MaxDate = new DateTimeOffset(new DateTime(2100, 1, 1));
            }
        }
    }
}
