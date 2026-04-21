using HospitalManagement.Database;
using HospitalManagement.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using HospitalManagement.Configuration;

namespace HospitalManagement.Tests.IntegrationTests;


[TestClass]
public class PatientRepositoryIntegrationTests
{
    private IDbContext _context;
    private IPatientRepository _repo;

    [TestInitialize]
    public void Setup()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "configuration", "testconfig.local.json");
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


}
