using System;
using System.Collections.Generic;
using System.Linq;
using HospitalManagement.Entity;
using HospitalManagement.Repository;
using HospitalManagement.Integration;

namespace HospitalManagement.Service
{
    public class PrescriptionService
    {
        private readonly PrescriptionRepository _prescriptionRepository;

        public PrescriptionService(PrescriptionRepository prescriptionRepository)
        {
            _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));
        }

     
        /// <param name="n">Number of items per page</param>
        /// <param name="page">Page number (starting from 1)</param>
        /// <returns>List of Prescriptions</returns>
        public List<Prescription> GetLatestPrescriptions(int n, int page)
        {
            return _prescriptionRepository.GetTopN(n, page);
        }

      
        /// <param name="id">The ID of the prescription</param>
        /// <returns>The specified Prescription</returns>
        public Prescription GetPrescriptionDetails(int id)
        {
            var filter = new PrescriptionFilter { PrescriptionId = id };
            var prescription = _prescriptionRepository.GetFiltered(filter).FirstOrDefault();

            if (prescription == null)
            {
                throw new Exception($"Prescription with ID {id} does not exist.");
            }

            return prescription;
        }

        /// <param name="filter">The complex filter for finding prescriptions</param>
        /// <returns>A filtered list of Prescriptions sorted by Date descending.</returns>
        public List<Prescription> ApplyFilter(PrescriptionFilter filter)
        {
            if (filter == null)
            {
                return _prescriptionRepository.GetTopN(20, 1);
            }

            try
            {
                var results = _prescriptionRepository.GetFiltered(filter);

                return results;
            }
            catch (Exception)
            {
                throw new Exception("The medication search could not be completed at this time due to high system load or complex parameters. Please try simplifying your search or try again later.");
            }
        }
    }
}
