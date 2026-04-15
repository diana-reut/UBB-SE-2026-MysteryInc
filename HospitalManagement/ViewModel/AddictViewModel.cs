using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq; 
using HospitalManagement.Entity;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    internal class AddictViewModel
    {
        private readonly AddictDetectionService _addictDetectionService;

        public ObservableCollection<Patient> AddictCandidates { get; set; }

        public AddictViewModel(AddictDetectionService addictDetectionService)
        {
            _addictDetectionService = addictDetectionService ?? throw new ArgumentNullException(nameof(addictDetectionService));

            AddictCandidates = new ObservableCollection<Patient>();
            
            LoadAddicts();
        }

        public void LoadAddicts()
        {
            AddictCandidates.Clear();
            
            var candidates = _addictDetectionService.GetAddictCandidates();
            
            foreach(var candidate in candidates)
            {
                string chronicString = _addictDetectionService.GetChronicConditions(candidate.Id);

                if (candidate.MedicalHistory != null)
                {
                    if (candidate.MedicalHistory.ChronicConditions == null || candidate.MedicalHistory.ChronicConditions.Count == 0)
                    {
                        candidate.MedicalHistory.ChronicConditions = new List<string> { chronicString }; 
                    }
                }
                else
                {
                    candidate.MedicalHistory = new MedicalHistory 
                    { 
                        ChronicConditions = new List<string> { chronicString } 
                    };
                }

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
            
            if (targetPatient != null)
            {
                AddictCandidates.Remove(targetPatient);

                // pe viitor, daca se cere la backend, aici am putea schimba starea in baza de date ca ex: "Reported = true"
            }
        }
    }
}
