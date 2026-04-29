using HospitalManagement.Entity;
using HospitalManagement.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace HospitalManagement.ViewModel;

internal partial class AddictViewModel
{
    private readonly IAddictDetectionService _addictDetectionService;

    public ObservableCollection<Patient> AddictCandidates { get; set; }

    public AddictViewModel(IAddictDetectionService addictDetectionService)
    {
        _addictDetectionService = addictDetectionService ?? throw new ArgumentNullException(nameof(addictDetectionService));

        AddictCandidates = [];
        LoadAddicts();
    }

    public void LoadAddicts()
    {
        AddictCandidates.Clear();

        List<Patient> candidates = _addictDetectionService.GetAddictCandidates();

        foreach (Patient candidate in candidates)
        {
            AddictCandidates.Add(candidate);
        }
    }

    public string GetPoliceReportMessage(int patientId)
    {
        Patient? targetPatient = AddictCandidates.FirstOrDefault(p => p.Id == patientId);

        if (targetPatient is null)
        {
            return "Error: Patient not found in the current flagged list.";
        }

        return _addictDetectionService.BuildPoliceReport(targetPatient);
    }

    public void RemoveFlaggedPatient(int patientId)
    {
        Patient? targetPatient = AddictCandidates.FirstOrDefault(p => p.Id == patientId);
        if (targetPatient is not null)
        {
            _ = AddictCandidates.Remove(targetPatient);
        }
    }

    private static void PlayPoliceAlert()
    {
        _ = Task.Run(() =>
        {
            Console.Beep(1200, 200);
            Console.Beep(800, 200);
            Console.Beep(1200, 200);
            Console.Beep(800, 200);
            Console.Beep(1500, 500);
        });
    }

    [RelayCommand]
    public void ConfirmPoliceAlert(int patientId)
    {
        PlayPoliceAlert();
        RemoveFlaggedPatient(patientId);
    }
}
