using HospitalManagement.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace HospitalManagement.View;

internal sealed partial class PrescriptionView : UserControl
{
    public PrescriptionViewModel ViewModel { get; }

    public PrescriptionView(PrescriptionViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;

        DateFromPicker.DateChanged += DateFromPicker_DateChanged;
        DateToPicker.DateChanged += DateToPicker_DateChanged;
    }

    private void OnApplyFilterClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyFilterFromView(
            SearchIdBox.Text,
            SearchMedBox.Text,
            DateFromPicker.Date,
            DateToPicker.Date,
            SearchNameBox.Text
        );
    }

    private void OnNextClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.NextPage();
    }

    private void OnPrevClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.PrevPage();
    }

    private void DateFromPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        DateToPicker.MinDate = DateFromPicker.Date ?? new DateTimeOffset(new DateTime(1920, 1, 1));
    }

    private void DateToPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        DateFromPicker.MaxDate = DateToPicker.Date ?? new DateTimeOffset(new DateTime(2100, 1, 1));
    }
}