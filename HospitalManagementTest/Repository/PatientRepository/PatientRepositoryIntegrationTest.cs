using HospitalManagement.Database;
using HospitalManagement.Repository;
using System.Text.Json;
using HospitalManagement.Configuration;

namespace HospitalManagement.Tests.IntegrationTests;


[TestClass]
public class PatientRepositoryIntegrationTests
{
    private IDbContext _context;
    private IPatientRepository _repo;

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public void Setup()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "configuration", "testconfig.local.json");
        TestContext.WriteLine(filePath);
        string json = File.ReadAllText(filePath);
        string connStr = JsonSerializer.Deserialize<JsonElement>(json)
                            .GetProperty("ConnectionStrings")
                            .GetProperty("DefaultConnection")
                            .GetString()!;

        typeof(Config)
            .GetProperty("ConnectionString")!
            .SetValue(null, connStr);


        _context = new HospitalDbContext();
        _repo = new PatientRepository(_context);

    }

    [TestCleanup]
    public void Cleanup()
    {
        (_context as IDisposable)?.Dispose();
    }


    [TestMethod]
    public void Setup_ShouldConnectToDatabase()
    {
        var patients = _repo.GetAll(true);
        Assert.IsNotNull(patients);
    }

    [TestMethod]
    public void GetArchived_ShouldReturnList()
    {
        var patients = _repo.GetArchived();

        Assert.IsNotNull(patients);
    }

    [TestMethod]
    public void GetById_WhenMissing_ShouldReturnNull()
    {
        var patient = _repo.GetById(-999999);

        Assert.IsNull(patient);
    }

    [TestMethod]
    public void Exists_WhenMissing_ShouldReturnFalse()
    {
        bool exists = _repo.Exists("not-existing-cnp");

        Assert.IsFalse(exists);
    }



}