using Microsoft.Data.SqlClient;
using System;

namespace HospitalManagement.Database;

internal interface IDbContext : IDisposable
{
    void BeginTransaction();
    void CommitTransaction();
    void Dispose();
    void EnsureConnectionOpen();
    int ExecuteNonQuery(string sql);
    SqlDataReader ExecuteQuery(string sql);
    void RollbackTransaction();
}