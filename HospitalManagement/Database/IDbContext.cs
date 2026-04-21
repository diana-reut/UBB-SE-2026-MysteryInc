using Microsoft.Data.SqlClient;

namespace HospitalManagement.Database;

public interface IDbContext
{
    void BeginTransaction();
    void CommitTransaction();
    void Dispose();
    void EnsureConnectionOpen();
    int ExecuteNonQuery(string sql);
    SqlDataReader ExecuteQuery(string sql);
    void RollbackTransaction();
}