using System.Data;
using System.Data.SqlClient;

namespace HospitalManagement.Database
{
    public interface IDbContext
    {
        SqlDataReader ExecuteQuery(string sql);
        int ExecuteNonQuery(string sql);

        void BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}