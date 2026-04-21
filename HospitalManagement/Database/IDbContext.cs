using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace HospitalManagement.Database;

internal interface IDbContext : IDisposable
{
    // added this
    public void EnsureConnectionOpen();

    public IDataReader ExecuteQuery(string sql);

    public int ExecuteNonQuery(string sql);

    public void BeginTransaction();

    public void CommitTransaction();

    public void RollbackTransaction();
}
