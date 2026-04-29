using HospitalManagement.Entity;
using HospitalManagement.Entity.DTOs;
using HospitalManagement.Integration;
using HospitalManagement.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace HospitalManagement.ViewModel;

internal class PrescriptionViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private readonly IPrescriptionService _prescriptionService;
    private readonly IAddictDetectionService _addictDetectionService;

    public ObservableCollection<Prescription> Prescriptions { get; set; }

    public ObservableCollection<Patient> AddictCandidates { get; set; }

    public PrescriptionFilter ActiveFilter { get; set; }

    private int _currentPage;

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }
    }

    private string _infoMessage = "";

    public string InfoMessage
    {
        get => _infoMessage;

        set
        {
            if (_infoMessage != value)
            {
                _infoMessage = value;
                OnPropertyChanged();
            }
        }
    }

    private const int PageSize = 9;

    public PrescriptionViewModel(
        IPrescriptionService prescriptionService,
        IAddictDetectionService addictDetectionService)
    {
        _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
        _addictDetectionService = addictDetectionService ?? throw new ArgumentNullException(nameof(addictDetectionService));

        Prescriptions = new();
        AddictCandidates = new();
        ActiveFilter = new PrescriptionFilter();

        LoadPrescriptions();
    }

    public void ApplyFilterCommand(int? searchId, string? medName, DateTime? dateFrom, DateTime? dateTo, string? patientName, string? doctorName)
    {
        ApplyFilterFromView(
            searchId?.ToString(),
            medName,
            dateFrom.HasValue ? new DateTimeOffset(dateFrom.Value) : null,
            dateTo.HasValue ? new DateTimeOffset(dateTo.Value) : null,
            patientName ?? doctorName
        );
    }

    public void ApplyFilterFromView(string? idText, string? medName, DateTimeOffset? from, DateTimeOffset? to, string? searchText)
    {
        InfoMessage = "";
        CurrentPage = 1;

        ActiveFilter = new PrescriptionFilter
        {
            PrescriptionId = TryParseNullableInt(idText),
            MedName = Normalize(medName),
            DateFrom = from?.DateTime,
            DateTo = to?.DateTime,
            PatientName = Normalize(searchText),
            DoctorName = Normalize(searchText),
        };

        try
        {
            UpdatePageData();

            if (Prescriptions.Count == 0)
            {
                InfoMessage = "No prescriptions found matching those criteria.";
            }
        }
        catch (Exception ex)
        {
            InfoMessage = ex.Message;
        }
    }

    public void NextPage()
    {
        if (Prescriptions.Count == PageSize)
        {
            CurrentPage++;
            UpdatePageData();
        }
    }

    public void PrevPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdatePageData();
        }
    }

    public void LoadPrescriptions()
    {
        CurrentPage = 1;
        UpdatePageData();
    }

    public void UpdatePageData()
    {
        Prescriptions.Clear();
        InfoMessage = "";

        List<DoctorDTO> fakeDoctors = MockDoctorProvider.FakeDoctors;

        bool hasActiveFilter =
            ActiveFilter.PrescriptionId.HasValue
                || !string.IsNullOrWhiteSpace(ActiveFilter.MedName)
                || ActiveFilter.DateFrom.HasValue
                || ActiveFilter.DateTo.HasValue
                || !string.IsNullOrWhiteSpace(ActiveFilter.PatientName)
                || !string.IsNullOrWhiteSpace(ActiveFilter.DoctorName);

        List<Prescription> targetList;

        if (hasActiveFilter)
        {
            var filtered = _prescriptionService.ApplyFilter(ActiveFilter);

            targetList = filtered
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
        else
        {
            targetList = _prescriptionService.GetLatestPrescriptions(PageSize, CurrentPage);
        }

        foreach (var item in targetList)
        {
            if (string.IsNullOrEmpty(item.DoctorName)
                || item.DoctorName.Contains("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                int index = RandomNumberGenerator.GetInt32(fakeDoctors.Count);
                var doc = fakeDoctors[index];
                item.DoctorName = $"Dr. {doc.FirstName} {doc.LastName}";
            }

            Prescriptions.Add(item);
        }
    }

    public void SeeAllAddicts()
    {
        AddictCandidates.Clear();

        foreach (var patient in _addictDetectionService.GetAddictCandidates())
        {
            AddictCandidates.Add(patient);
        }
    }

    public string NotifyPolice(int patientId)
    {
        var patient = AddictCandidates.FirstOrDefault(p => p.Id == patientId);

        if (patient is null)
        {
            return "Error: Patient data not synchronized or invalid ID.";
        }

        return _addictDetectionService.BuildPoliceReport(patient);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? TryParseNullableInt(string? value)
    {
        return int.TryParse(value, out int result) ? result : null;
    }
}