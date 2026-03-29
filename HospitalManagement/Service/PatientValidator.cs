using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HospitalManagement.Service
{
    public class PatientValidator
    {
        public ValidationResult ValidatePatient(Patient patient)
        {
            var result = new ValidationResult();
            try
            {
                ValidateName(patient.FirstName, "First Name"); 
                ValidateName(patient.LastName, "Last Name"); 
                
                ValidatePhone(patient.PhoneNo, "Phone Number");
                ValidatePhone(patient.EmergencyContact, "Emergency Contact");

                ValidateCnpFormat(patient.Cnp); 

                ValidateCnpCorrelation(patient.Cnp, patient.Sex, patient.Dob); 

                if (patient.Dob > DateTime.Now)
                    throw new ValidationException("Date of Birth cannot be in the future."); 

                if (patient.Dod.HasValue) 
                {
                    if (patient.Dod > DateTime.Now)
                        throw new ValidationException("Date of Death cannot be in the future."); 
                    if (patient.Dod < patient.Dob)
                        throw new ValidationException("Date of Death cannot be earlier than Date of Birth."); 
                }
            }
            catch (ValidationException ex)
            {
                result.AddError(ex.Message);
            }
            return result;
        }

        private void ValidateName(string name, string field)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                throw new ValidationException($"{field} must be between 1-100 characters."); 
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z\s\-]+$"))
                throw new ValidationException($"{field} can only contain letters, spaces, and hyphens.");
        }

        private void ValidatePhone(string phone, string field)
        {
            if (string.IsNullOrEmpty(phone) || !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+40\d{9}$"))
                throw new ValidationException($"{field} must be in format +40XXXXXXXXX."); 
        }

        private void ValidateCnpFormat(string cnp)
        {
            if (string.IsNullOrEmpty(cnp) || cnp.Length != 13 || !cnp.All(char.IsDigit))
                throw new ValidationException("CNP must be exactly 13 digits.");
            if (!"1256".Contains(cnp[0]))
                throw new ValidationException("CNP must start with 1, 2, 5, or 6.");
        }

        private void ValidateCnpCorrelation(string cnp, Sex sex, DateTime dob)
        {
            int firstDigit = int.Parse(cnp[0].ToString());
            if (sex == Sex.M && firstDigit % 2 == 0)
                throw new ValidationException("CNP first digit must be odd for Male.");
            if (sex == Sex.F && firstDigit % 2 != 0)
                throw new ValidationException("CNP first digit must be even for Female."); 

            string cnpDobPart = cnp.Substring(1, 6);
            string expectedDobPart = dob.ToString("yyMMdd");
            if (cnpDobPart != expectedDobPart)
                throw new ValidationException("CNP digits 2-7 must match the Date of Birth.");
        }
    }
}