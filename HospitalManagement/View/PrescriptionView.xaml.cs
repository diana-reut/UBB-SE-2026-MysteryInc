using HospitalManagement.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.View;

internal sealed partial class PrescriptionView : UserControl
{
    public ViewModel.PrescriptionViewModel ViewModel { get; }

    public PrescriptionView(PrescriptionViewModel viewModel)
    {
        ViewModel = (App.Current as App).Services.GetService<PrescriptionViewModel>();
        this.DataContext = ViewModel;
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        DateFromPicker.DateChanged += DateFromPicker_DateChanged;
        DateToPicker.DateChanged += DateToPicker_DateChanged;
    }

    private void OnApplyFilterClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        int? idSearch = int.TryParse(SearchIdBox.Text, out int id) ? id : null;
        string? patientOrDoctor = string.IsNullOrWhiteSpace(SearchNameBox.Text) ? null : SearchNameBox.Text;
        string? medname = string.IsNullOrWhiteSpace(SearchMedBox.Text) ? null : SearchMedBox.Text;

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
        DateToPicker.MinDate = DateFromPicker.Date ?? new DateTimeOffset(new DateTime(1920, 1, 1));
    }

    private void DateToPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        DateFromPicker.MaxDate = DateToPicker.Date ?? new DateTimeOffset(new DateTime(2100, 1, 1));
    }
}
