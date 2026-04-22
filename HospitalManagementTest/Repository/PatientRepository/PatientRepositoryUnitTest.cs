using HospitalManagement.Database;
using HospitalManagement.Entity;
using HospitalManagement.Entity.Enums;
using HospitalManagement.Integration;
using HospitalManagement.Repository;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data.Common;
using System.Reflection;

namespace HospitalManagement.Tests.UnitTests;

[TestClass]
public class PatientRepositoryUnitTests
{
    private Mock<IDbContext> _mockContext;
    private PatientRepository _repo;

    [TestInitialize]
    public void Setup()
    {
        _mockContext = new Mock<IDbContext>();
        _repo = new PatientRepository(_mockContext.Object);
    }

    [TestMethod]
    public void Exists_ReturnsTrue_AndFalse()
    {
        var reader = new Mock<DbDataReader>();
        reader.SetupSequence(r => r.Read()).Returns(true).Returns(false);

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        Assert.IsTrue(_repo.Exists("1"));
        Assert.IsFalse(_repo.Exists("1"));
    }

    [TestMethod]
    public void GetById_MapsCorrectly_AndHandlesNullDod()
    {
        var reader = new Mock<DbDataReader>();
        reader.SetupSequence(r => r.Read()).Returns(true).Returns(false);

        reader.Setup(r => r.GetOrdinal("PatientID")).Returns(0);
        reader.Setup(r => r.GetOrdinal("DateOfBirth")).Returns(1);
        reader.Setup(r => r.GetOrdinal("DateOfDeath")).Returns(2);
        reader.Setup(r => r.GetOrdinal("Archived")).Returns(3);
        reader.Setup(r => r.GetOrdinal("IsDonor")).Returns(4);

        reader.Setup(r => r.GetInt32(0)).Returns(1);
        reader.Setup(r => r.GetDateTime(1)).Returns(DateTime.Now);
        reader.Setup(r => r.IsDBNull(2)).Returns(true);
        reader.Setup(r => r.GetBoolean(3)).Returns(false);
        reader.Setup(r => r.GetBoolean(4)).Returns(false);

        reader.Setup(r => r["FirstName"]).Returns("John");
        reader.Setup(r => r["LastName"]).Returns("Doe");
        reader.Setup(r => r["CNP"]).Returns("123");
        reader.Setup(r => r["Sex"]).Returns("M");

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        var result = _repo.GetById(1);

        Assert.IsNotNull(result);
        Assert.IsNull(result!.Dod);
    }


    [TestMethod]
    public void GetById_ReturnsNull_WhenNotFound()
    {
        var reader = new Mock<DbDataReader>();
        reader.Setup(r => r.Read()).Returns(false);

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        Assert.IsNull(_repo.GetById(1));
    }

    [TestMethod]
    public void GetById_ShouldHandleDifferentSexValues()
    {
        var reader = new Mock<DbDataReader>();
        reader.SetupSequence(r => r.Read()).Returns(true).Returns(false);

        reader.Setup(r => r.GetOrdinal("PatientID")).Returns(0);
        reader.Setup(r => r.GetOrdinal("DateOfBirth")).Returns(1);
        reader.Setup(r => r.GetOrdinal("DateOfDeath")).Returns(2);
        reader.Setup(r => r.GetOrdinal("Archived")).Returns(3);
        reader.Setup(r => r.GetOrdinal("IsDonor")).Returns(4);

        reader.Setup(r => r.GetInt32(0)).Returns(1);
        reader.Setup(r => r.GetDateTime(1)).Returns(DateTime.Now);
        reader.Setup(r => r.IsDBNull(2)).Returns(true);
        reader.Setup(r => r.GetBoolean(3)).Returns(false);
        reader.Setup(r => r.GetBoolean(4)).Returns(false);

        reader.Setup(r => r["FirstName"]).Returns("John");
        reader.Setup(r => r["LastName"]).Returns("Doe");
        reader.Setup(r => r["CNP"]).Returns("123");
        reader.Setup(r => r["Sex"]).Returns("F");

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        var result = _repo.GetById(1);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GetArchived_ReturnsList()
    {
        var reader = SetupReader(1);

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        var result = _repo.GetArchived();

        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByName()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { NamePart = "John" });
        Assert.AreEqual(1, result.Count); 
    }

    [TestMethod]
    public void Search_FiltersByName_NoMatch()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { NamePart = "ZZZZZ" });
        Assert.AreEqual(0, result.Count); 
    }

    [TestMethod]
    public void Search_FiltersByCNP()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { CNP = "123" });
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByCNP_NullCnp()
    {
        var reader = SetupReader(1);
        reader.Setup(r => r["CNP"]).Returns(null!);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { CNP = "123" });
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByCNP_NoMatch_WhenNotStartingWith()
    {
        var reader = SetupReader(1);
        reader.Setup(r => r["CNP"]).Returns("999"); 

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { CNP = "123" });

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByBloodType_Match()
    {
        var reader = SetupReader(1);

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        var result = _repo.Search(new PatientFilter
        {
            BloodType = BloodType.A
        });

        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByMinAge()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { MinAge = 0 });
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByMaxAge()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { MaxAge = 200 });
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByBloodType_NoMedicalHistory()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { BloodType = BloodType.A });
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Search_FiltersBySex()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { Sex = Sex.M });
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Search_FiltersBySex_NoMatch()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { Sex = Sex.F });
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Search_FiltersByChronicCond_NoMedicalHistory()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter { HasChronicCond = true });
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Search_EmptyFilter_ReturnsAll()
    {
        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>())).Returns(reader.Object);

        var result = _repo.Search(new PatientFilter());
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void Search_Throws_WhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => _repo.Search(null!));
    }

    [TestMethod]
    public void MarkAsDeceased_Executes()
    {
        _repo.MarkAsDeceased(1, new DateOnly(2024, 1, 1));

        _mockContext.Verify(c => c.ExecuteNonQuery(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public void Add_CoversAllBranches()
    {
        var reader = new Mock<DbDataReader>();
        reader.SetupSequence(r => r.Read()).Returns(true).Returns(false);
        reader.Setup(r => r[0]).Returns("10");

        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        var p = new Patient();
        _repo.Add(p);

        Assert.AreEqual(10, p.Id);

        reader.Setup(r => r[0]).Returns("bad");
        var p2 = new Patient();
        _repo.Add(p2);

        Assert.AreEqual(0, p2.Id);
    }

    [TestMethod]
    public void Add_Throws_WhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => _repo.Add(null!));
    }

    [TestMethod]
    public void Update_CoversBranches()
    {
        var p1 = new Patient { Id = 1, FirstName = "A", LastName = "B", Sex = Sex.M, Dod = DateTime.Now };
        var p2 = new Patient { Id = 2, FirstName = "A", LastName = "B", Sex = Sex.M, Dod = null };

        _repo.Update(p1);
        _repo.Update(p2);

        _mockContext.Verify(c => c.ExecuteNonQuery(It.IsAny<string>()), Times.Exactly(2));
    }

    [TestMethod]
    public void Update_Throws_WhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => _repo.Update(null!));
    }

    [TestMethod]
    public void Delete_ExecutesQuery()
    {
        _repo.Delete(5);

        _mockContext.Verify(c =>
            c.ExecuteNonQuery(It.Is<string>(q => q.Contains("PatientID=5"))),
            Times.Once);
    }

    [TestMethod]
    public void IsABloodMatch_CoversAllBloodTypeCombinations()
    {
        var repoType = typeof(PatientRepository);
        var blood = repoType.GetMethod("IsABloodMatch", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsFalse((bool)blood.Invoke(null, new object[] { null, BloodType.A }));

        Assert.IsTrue((bool)blood.Invoke(null, new object[] { BloodType.O, BloodType.A }));
        Assert.IsFalse((bool)blood.Invoke(null, new object[] { BloodType.A, BloodType.B }));

        Assert.IsTrue((bool)blood.Invoke(null, new object[] { BloodType.A, BloodType.A }));
        Assert.IsTrue((bool)blood.Invoke(null, new object[] { BloodType.A, BloodType.AB }));

        Assert.IsTrue((bool)blood.Invoke(null, new object[] { BloodType.B, BloodType.B }));
        Assert.IsTrue((bool)blood.Invoke(null, new object[] { BloodType.B, BloodType.AB }));

        Assert.IsTrue((bool)blood.Invoke(null, new object[] { BloodType.AB, BloodType.AB }));
    }

    [TestMethod]
    public void IsARhMatch_CoversAllRhCombinations()
    {
        var repoType = typeof(PatientRepository);
        var rh = repoType.GetMethod("IsARhMatch", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsFalse((bool)rh.Invoke(null, new object[] { null, BloodType.A }));

        Assert.IsTrue((bool)rh.Invoke(null, new object[] { RhEnum.Negative, RhEnum.Positive }));
        Assert.IsFalse((bool)rh.Invoke(null, new object[] { RhEnum.Positive, RhEnum.Negative }));

        Assert.IsTrue((bool)rh.Invoke(null, new object[] { RhEnum.Positive, RhEnum.Positive }));
    }

    [TestMethod]
    public void CalculateAgeScore_ReturnsCorrectScore()
    {
        var repoType = typeof(PatientRepository);
        var age = repoType.GetMethod("CalculateAgeScore", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.AreEqual(0, (int)age.Invoke(null, new object[]
       {
            DateTime.Now.AddYears(-100),
            DateTime.Now
       }));

    }

    [TestMethod]
    public void GetCompatibleDonors_ReturnsEmpty_WhenNoPatientsHaveMedicalHistory()
    {
        var repoType = typeof(PatientRepository);
        var donors = repoType.GetMethod("GetCompatibleDonors", BindingFlags.NonPublic | BindingFlags.Instance);

        var reader = SetupReader(1);
        _mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns(reader.Object);

        var result = donors.Invoke(_repo, new object[]
        {
            BloodType.A, RhEnum.Positive, Sex.M, DateTime.Now, 0, 100
        });

        Assert.IsNotNull(result);
    }


    private Mock<DbDataReader> SetupReader(int id)
    {
        var reader = new Mock<DbDataReader>();

        reader.SetupSequence(r => r.Read())
            .Returns(true)
            .Returns(false);

        reader.Setup(r => r.GetOrdinal("PatientID")).Returns(0);
        reader.Setup(r => r.GetOrdinal("DateOfBirth")).Returns(1);
        reader.Setup(r => r.GetOrdinal("DateOfDeath")).Returns(2);
        reader.Setup(r => r.GetOrdinal("Archived")).Returns(3);
        reader.Setup(r => r.GetOrdinal("IsDonor")).Returns(4);

        reader.Setup(r => r.GetInt32(0)).Returns(id);
        reader.Setup(r => r.GetDateTime(1)).Returns(DateTime.Now.AddYears(-30));
        reader.Setup(r => r.IsDBNull(2)).Returns(true);

        reader.Setup(r => r.GetBoolean(3)).Returns(false);
        reader.Setup(r => r.GetBoolean(4)).Returns(false);

        reader.Setup(r => r["FirstName"]).Returns("John");
        reader.Setup(r => r["LastName"]).Returns("Doe");
        reader.Setup(r => r["CNP"]).Returns("123");
        reader.Setup(r => r["Sex"]).Returns("M");

        var history = new MedicalHistory
        {
            BloodType = BloodType.A,
            Rh = RhEnum.Positive,
            ChronicConditions = new List<string>(),
            Allergies = new List<(Allergy, string)>()
        };

        reader.Setup(r => r["MedicalHistory"]).Returns(history);

        return reader;
    }
}