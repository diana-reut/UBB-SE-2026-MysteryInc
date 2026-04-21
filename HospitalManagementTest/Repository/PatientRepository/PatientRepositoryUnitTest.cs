using Microsoft.Data;
using HospitalManagement.Repository;
using HospitalManagement.Database;
using Moq;
using System.Data;
namespace HospitalManagement.Tests.UnitTests;

[TestClass]
public class PatientRepositoryUnitTests
{
    [TestMethod]
    public void Exists_ShouldReturnTrue_WhenPatientExists()
    {
        var mockContext = new Mock<IDbContext>();
        var mockReader = new Mock<IDataReader>();

        _ = mockReader.Setup(static r => r.Read())
            .Returns(true);

        _ = mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns((Microsoft.Data.SqlClient.SqlDataReader)mockReader.Object);

        var repo = new PatientRepository(mockContext.Object);
        bool result = repo.Exists("123");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Exists_ShouldReturnFalse_WhenPatientDoesntExist()
    {
        var mockContext = new Mock<IDbContext>();
        var mockReader = new Mock<IDataReader>();

        _ = mockReader.Setup(static r => r.Read())
            .Returns(false);

        _ = mockContext.Setup(c => c.ExecuteQuery(It.IsAny<string>()))
            .Returns((Microsoft.Data.SqlClient.SqlDataReader)mockReader.Object);

        var repo = new PatientRepository(mockContext.Object);
        bool result = repo.Exists("123");

        Assert.IsFalse(result);
    }
}