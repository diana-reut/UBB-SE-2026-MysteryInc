using System;
using System.Collections.Generic;
using System.Linq;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Integration;
using HospitalManagement.Repository;

namespace HospitalManagement.Service
{
    internal class PatientService
    {
        private readonly PatientRepository _patientRepo;
       
        // private readonly MedicalHistoryRepository _historyRepo;
        // private readonly MedicalRecordRepository _recordRepo;

        public PatientService(PatientRepository patientRepo)
        {
            _patientRepo = patientRepo;
        }


        /// Validates that the CNP matches the provided Sex and Date of Birth.
        public bool ValidateCNP(string cnp, Sex sex, DateTime dob)
        {
            // 1. Basic sanity checks: Must be exactly 13 digits
            if (string.IsNullOrWhiteSpace(cnp) || cnp.Length != 13 || !cnp.All(char.IsDigit))
            {
                return false;
            }

            // 2. Validate Sex Alignment (First digit)
            // '0' is 48 in ASCII. Subtracting '0' from a numeric char gives its actual integer value.
            int firstDigit = cnp[0] - '0';

            bool isMale = (sex == Sex.M);
            bool isFirstDigitOdd = (firstDigit % 2 != 0);

            // If the patient is male, the digit MUST be odd. If female, it MUST be even.
            if (isMale != isFirstDigitOdd)
            {
                return false;
            }

            // 3. Validate Date of Birth Alignment (Digits 2 through 7)
            // Extract the 6 characters representing the DOB from the CNP
            string cnpDobPart = cnp.Substring(1, 6);

            // Format the provided DateTime object to the exact "yyMMdd" format the CNP uses
            string expectedDobPart = dob.ToString("yyMMdd");

            if (cnpDobPart != expectedDobPart)
            {
                return false;
            }

            // If it passes all checks, the CNP matches the user data!
            return true;
        }

        /// <summary>
        /// Validates the data and creates a new patient in the database.
        /// </summary>
        public Patient CreatePatient(Patient data)
        {
            // 1. Enforce data integrity
            bool isValid = ValidateCNP(data.Cnp, data.Sex, data.Dob);

            if (!isValid)
            {
                // Throwing an exception here is best practice. 
                // Your UI layer (WinForms, WPF, etc.) can catch this and show a red error to the user.
                throw new ArgumentException("Identity Mismatch: The provided CNP does not align with the selected Sex or Date of Birth.");
            }

            // 2. Optional: You could also check if the CNP already exists in the database here!
            // if (_patientRepo.Exists(data.Cnp)) { throw new Exception("Patient already exists!"); }

            // 3. Save to database
            _patientRepo.Add(data);

            return data;
        }

        /// <summary>
        /// SV2: Updates patient data while locking core identity fields (CNP/DOB) 
        /// and checking if the record is archived.
        /// </summary>
        public void UpdatePatient(Patient data)
        {
            // 1. Identity Lock: Fetch the existing record from the repository
            // Note: Assuming your Repository has a GetById or Find method
            Patient existingPatient = _patientRepo.GetById(data.Id);

            if (existingPatient == null)
            {
                throw new KeyNotFoundException($"Patient with ID {data.Id} not found.");
            }

            // 2. Audit Check: Prevent updates if the patient is currently Archived
            // Your diagram shows archivePatient(), so we check the IsArchived property
            if (existingPatient.IsArchived)
            {
                throw new InvalidOperationException("Audit Error: This patient is archived. De-archive before updating.");
            }

            // 3. Identity Consistency Check: CNP and DOB must not change
            if (existingPatient.Cnp != data.Cnp || existingPatient.Dob.Date != data.Dob.Date)
            {
                // Throwing the specific ValidationException requested in your SV2 notes
                throw new InvalidOperationException("ValidationException: Identity cannot be modified. CNP and DOB must match the original record.");
            }

            // 4. Cross-Validation: Verify the new data is internally consistent (Sex vs CNP)
            if (!ValidateCNP(data.Cnp, data.Sex, data.Dob))
            {
                throw new ArgumentException("Identity Mismatch: CNP does not align with Sex or DOB.");
            }

            // 5. Repository Call: Pass the clean object to the repository
            // TODO: Uncomment once the Update method in PatientRepository
             _patientRepo.Update(data);
        }

        /// <summary>
        /// SV5: Sets the IsArchived bit to true (1) to hide the patient from active views.
        /// </summary>
        public void ArchivePatient(int id)
        {
            Patient patient = _patientRepo.GetById(id);
            if (patient == null) throw new KeyNotFoundException("Patient not found.");

            patient.IsArchived = true;

            // TODO: Uncomment once Repository Update is ready
             _patientRepo.Update(patient);
        }

        /// <summary>
        /// SV5: Sets the IsArchived bit back to false (0) to restore patient visibility.
        /// </summary>
        public void DearchivePatient(int id)
        {
            Patient patient = _patientRepo.GetById(id);
            if (patient == null) throw new KeyNotFoundException("Patient not found.");

            patient.IsArchived = false;

            // TODO: Uncomment once Repository Update is ready
             _patientRepo.Update(patient);
        }

        /// <summary>
        /// SV6: Special archive status for deceased patients. 
        /// Marks them as Archived and records the death date.
        /// </summary>
        public void ArchiveAsDeceased(int id, DateTime deathDate)
        {
            if (deathDate > DateTime.Now)
            {
                throw new ArgumentException("Validation Error: Death date cannot be in the future.");
            }

            Patient patient = _patientRepo.GetById(id);
            if (patient == null) throw new KeyNotFoundException("Patient not found.");

            // Standard archiving
            patient.IsArchived = true;

            // Assuming your Patient entity has a DeathDate property
            patient.Dod = deathDate;

            // TODO: Uncomment once Repository Update is ready
             _patientRepo.Update(patient);
        }

        /// <summary>
        /// SV7: Validates the filter criteria and calls the repository to search patients.
        /// </summary>
        public List<Patient> SearchPatients(PatientFilter filter)
        {
            if (filter != null)
            {
                // --- 1. Age Validations ---
                if (filter.minAge.HasValue && filter.minAge < 0)
                    throw new ArgumentException("Validation Error: Minimum age cannot be negative.");

                if (filter.maxAge.HasValue && filter.maxAge < 0)
                    throw new ArgumentException("Validation Error: Maximum age cannot be negative.");

                if (filter.minAge.HasValue && filter.maxAge.HasValue && filter.minAge > filter.maxAge)
                    throw new ArgumentException("Validation Error: Minimum age cannot be greater than maximum age.");

                // --- 2. CNP Validations ---
                // Since the repo looks for an exact match, it must be valid if provided.
                if (!string.IsNullOrWhiteSpace(filter.CNP) && filter.CNP.Length != 13)
                    throw new ArgumentException("Validation Error: CNP must be exactly 13 digits for an exact search.");

                // --- 3. Date Validations ---
                if (filter.lastUpdatedFrom.HasValue && filter.lastUpdatedTo.HasValue)
                {
                    if (filter.lastUpdatedFrom.Value > filter.lastUpdatedTo.Value)
                        throw new ArgumentException("Validation Error: 'From' date cannot be after 'To' date.");
                }
            }

            // 4. Pass the clean, validated filter to the Repository!
            return _patientRepo.Search(filter);
        }
    }
}