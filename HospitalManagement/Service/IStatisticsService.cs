using System.Collections.Generic;

namespace HospitalManagement.Service;
internal interface IStatisticsService
{
    Dictionary<string, int> GetActiveVsArchivedRatio();
    Dictionary<string, int> GetAgeDistribution();
    Dictionary<string, int> GetConsultationDistribution();
    Dictionary<string, int> GetMostPrescribedMeds();
    Dictionary<string, int> GetPatientGenderDistribution();
    Dictionary<string, int> GetPatientsByBloodType();
    Dictionary<string, int> GetPatientsByRh();
    Dictionary<string, int> GetTopDiagnoses();
}