using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq; 
using HospitalManagement.Entity;
using HospitalManagement.Entity.DTOs;
using HospitalManagement.Integration;
using HospitalManagement.Service;

namespace HospitalManagement.ViewModel
{
    internal class PrescriptionViewModel
    {
      
        private readonly IPrescriptionService _prescriptionService;
        private readonly IAddictDetectionService _addictDetectionService;

        
        public ObservableCollection<Prescription> Prescriptions { get; set; }

        public ObservableCollection<Patient> AddictCandidates { get; set; }

        public PrescriptionFilter ActiveFilter { get; set; }
        public int CurrentPage { get; set; }
        public string InfoMessage { get; set; } = string.Empty;

        private const int PageSize = 9;

        public PrescriptionViewModel(
            IPrescriptionService prescriptionService, 
            IAddictDetectionService addictDetectionService)
        {
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _addictDetectionService = addictDetectionService ?? throw new ArgumentNullException(nameof(addictDetectionService));

            Prescriptions = new ObservableCollection<Prescription>();
            AddictCandidates = new ObservableCollection<Patient>();
            
            ActiveFilter = new PrescriptionFilter();
            CurrentPage = 1;
            LoadPrescriptions();
        }

        public void UpdatePageData()
        {
            Prescriptions.Clear();
            InfoMessage = string.Empty;

            var fakeDoctors = MockDoctorProvider.FakeDoctors;
            var random = new Random();

            bool hasActiveFilter = 
                ActiveFilter.PrescriptionId.HasValue || 
                !string.IsNullOrWhiteSpace(ActiveFilter.MedName) ||
                ActiveFilter.DateFrom.HasValue || 
                ActiveFilter.DateTo.HasValue || 
                !string.IsNullOrWhiteSpace(ActiveFilter.PatientName) || 
                !string.IsNullOrWhiteSpace(ActiveFilter.DoctorName);

            List<Prescription> targetList;

            if (hasActiveFilter)
            {
                var allFilteredResults = _prescriptionService.ApplyFilter(ActiveFilter);
                targetList = allFilteredResults
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
                if (string.IsNullOrEmpty(item.DoctorName) || item.DoctorName.Contains("Unknown"))
                {
                    var randomDoc = fakeDoctors[random.Next(fakeDoctors.Count)];
                    item.DoctorName = $"Dr. {randomDoc.FirstName} {randomDoc.LastName}";
                }
                
                Prescriptions.Add(item);
            }
        }

        public void LoadPrescriptions() 
        {
            CurrentPage = 1;
            UpdatePageData();
        }

        public void ApplyFilterCommand(int? searchId, string? medName, DateTime? dateFrom, DateTime? dateTo, string? patientName, string? doctorName)
        {
            InfoMessage = string.Empty;
            CurrentPage = 1; 

            ActiveFilter.PrescriptionId = searchId;
            ActiveFilter.MedName = medName;
            ActiveFilter.DateFrom = dateFrom;
            ActiveFilter.DateTo = dateTo;
            ActiveFilter.PatientName = patientName;
            ActiveFilter.DoctorName = doctorName;

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
        
     
        public Prescription ShowMedsList(int id)
        {
            return _prescriptionService.GetPrescriptionDetails(id);
        }
  
        public void SeeAllAddicts()
        {
            AddictCandidates.Clear();
            
            var candidates = _addictDetectionService.GetAddictCandidates();
            
            foreach(var candidate in candidates)
            {
                AddictCandidates.Add(candidate);
            }
        }

  
        public string NotifyPolice(int patientId)
        {
            var flaggedPatient = AddictCandidates.FirstOrDefault(p => p.Id == patientId);

            if (flaggedPatient == null)
            {
                return "Error: Patient data not completely synced or patient ID is invalid.";
            }

            return _addictDetectionService.BuildPoliceReport(flaggedPatient);
        }
    }
}
