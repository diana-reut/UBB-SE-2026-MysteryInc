using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using HospitalManagement.Entity;
using HospitalManagement.Repository;
using HospitalManagement.Service;
using LiveChartsCore.Painting;


namespace HospitalManagementTest.Tests.UnitTests;

[TestClass]
public class AddictDetectionServiceTest
{
    [TestMethod]
    public void Constructor_ShouldThrow_WhenPrescriptionRepositoryIsNull()
    {
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        try
        {
            var service = new AddictDetectionService(null!, medicalRepo.Object);
            Assert.Fail("Expected Exception was not thrown.");

        }
        catch (ArgumentNullException ex)
        {
            Assert.AreEqual("prescriptionRepository", ex.ParamName);
        }
    }

    [TestMethod]
    public void Constructor_ShouldThrow_WhenMedicalHistoryRepositoryIsNull()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();

        try
        {
            var service = new AddictDetectionService(prescriptionRepo.Object, null!);

            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException ex)
        {
            Assert.AreEqual("medicalHistoryRepository", ex.ParamName);

        }

    }

    [TestMethod]
    public void GetAddictCandidates_ShouldReturnEmptyList_WhenNoPatientsExist()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        prescriptionRepo.Setup(x => x.GetAddictCandidatePatients()).Returns(new List<Patient>());

        var service = new AddictDetectionService(prescriptionRepo.Object,medicalRepo.Object);
        var result = service.GetAddictCandidates();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetChronicConditions_ShouldThrow_WhenPatientIdIsInvalid()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        var service = new AddictDetectionService(prescriptionRepo.Object,medicalRepo.Object);

        try
        {
            service.GetChronicConditions(0);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("Invalid Patient ID.", ex.Message);
        }
    }

    [TestMethod]
    public void GetChronicConditions_ShouldReturnNoneReported_WhenHistoryIsNull()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        medicalRepo
            .Setup(x => x.GetByPatientId(1))
            .Returns((MedicalHistory?)null);

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        var result = service.GetChronicConditions(1);

        Assert.AreEqual("None reported.", result);
    }

    [TestMethod]
    public void GetChronicConditions_ShouldReturnExistingConditions_WhenAlreadyLoaded()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        medicalRepo
            .Setup(x => x.GetByPatientId(1))
            .Returns(new MedicalHistory
            {
                Id = 5,
                ChronicConditions = new List<string> { "Asthma", "Arthritis" }
            });

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        var result = service.GetChronicConditions(1);

        Assert.AreEqual("Asthma, Arthritis", result);
    }

    [TestMethod]
    public void GetChronicConditions_ShouldLoadConditions_WhenListIsNull()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        medicalRepo
            .Setup(x => x.GetByPatientId(1))
            .Returns(new MedicalHistory
            {
                Id = 5,
                ChronicConditions = null
            });

        medicalRepo
            .Setup(x => x.GetChronicConditions(5))
            .Returns(new List<string> { "Diabetes" });

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        var result = service.GetChronicConditions(1);

        Assert.AreEqual("Diabetes", result);
    }

    [TestMethod]
    public void GetChronicConditions_ShouldReturnNoneReported_WhenLoadedListIsStillEmpty()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        medicalRepo
            .Setup(x => x.GetByPatientId(1))
            .Returns(new MedicalHistory
            {
                Id = 5,
                ChronicConditions = new List<string>()
            });

        medicalRepo
            .Setup(x => x.GetChronicConditions(5))
            .Returns(new List<string>());

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        var result = service.GetChronicConditions(1);

        Assert.AreEqual("None reported.", result);
    }

    [TestMethod]
    public void BuildPoliceReport_ShouldThrow_WhenPatientIsNull()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        try
        {
            service.BuildPoliceReport(null!);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("Invalid patient data for building a police report.", ex.Message);
        }
    }

    [TestMethod]
    public void BuildPoliceReport_ShouldThrow_WhenPatientIdIsInvalid()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        var patient = new Patient { Id = 0, FirstName = "John", LastName = "Doe" };

        try
        {
            service.BuildPoliceReport(patient);
            Assert.Fail("Expected ArgumentException was not thrown.");
        }
        catch (ArgumentException ex)
        {
            Assert.AreEqual("Invalid patient data for building a police report.", ex.Message);
        }
    }

    [TestMethod]
    public void BuildPoliceReport_ShouldMentionNoRecords_WhenNoPrescriptionsExist()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        prescriptionRepo
            .Setup(x => x.GetFiltered(It.IsAny<HospitalManagement.Integration.PrescriptionFilter>()))
            .Returns(new List<Prescription>());

        var patient = new Patient
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Cnp = "123",
            PhoneNo = "0700000000"
        };

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        var result = service.BuildPoliceReport(patient);

        StringAssert.Contains(result, "No matching records pulled for this timeframe.");
    }

    [TestMethod]
    public void BuildPoliceReport_ShouldUseUnknown_WhenMedicationListIsNull()
    {
        var prescriptionRepo = new Mock<IPrescriptionRepository>();
        var medicalRepo = new Mock<IMedicalHistoryRepository>();

        prescriptionRepo
            .Setup(x => x.GetFiltered(It.IsAny<HospitalManagement.Integration.PrescriptionFilter>()))
            .Returns(new List<Prescription>
            {
                new Prescription
                {
                    Id = 100,
                    RecordId = 200,
                    Date = new DateTime(2024, 1, 10),
                    MedicationList = null
                }
            });

        var patient = new Patient
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Cnp = "123",
            PhoneNo = "0700000000"
        };

        var service = new AddictDetectionService(
            prescriptionRepo.Object,
            medicalRepo.Object);

        var result = service.BuildPoliceReport(patient);

        StringAssert.Contains(result, "Dispensed Drugs: Unknown");
    }
}
