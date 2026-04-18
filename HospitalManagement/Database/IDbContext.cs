using Microsoft.Data.SqlClient;

namespace HospitalManagement.Database;

internal interface IDbContext
{
    public SqlDataReader ExecuteQuery(string sql);

    public int ExecuteNonQuery(string sql);

    public void BeginTransaction();

    public void CommitTransaction();

    public void RollbackTransaction();
}
