using Microsoft.Data.SqlClient;
using System;
using System.Data.Common;

namespace HospitalManagement.Database;

internal interface IDbContext : IDisposable
{
    void BeginTransaction();
    void CommitTransaction();
    void Dispose();
    void EnsureConnectionOpen();
    int ExecuteNonQuery(string sql);
    DbDataReader ExecuteQuery(string sql);
    void RollbackTransaction();
}