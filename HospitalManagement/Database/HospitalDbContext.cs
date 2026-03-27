using System;
using System.Data.SqlClient;
using HospitalManagement.Configuration;

namespace HospitalManagement.Database
{
    public class HospitalDbContext : IDbContext, IDisposable
    {
        private readonly SqlConnection _connection;
        private SqlTransaction? _transaction;

        public HospitalDbContext()
        {
            if (string.IsNullOrWhiteSpace(Config.ConnectionString))
            {
                throw new Exception("Connection string is not loaded.");
            }

            _connection = new SqlConnection(Config.ConnectionString);
            _connection.Open();
        }

        public SqlDataReader ExecuteQuery(string sql)
        {
            SqlCommand command = new SqlCommand(sql, _connection);

            if (_transaction != null)
            {
                command.Transaction = _transaction;
            }

            return command.ExecuteReader();
        }

        public int ExecuteNonQuery(string sql)
        {
            SqlCommand command = new SqlCommand(sql, _connection);

            if (_transaction != null)
            {
                command.Transaction = _transaction;
            }

            return command.ExecuteNonQuery();
        }

        public void BeginTransaction()
        {
            if (_transaction == null)
            {
                _transaction = _connection.BeginTransaction();
            }
        }

        public void CommitTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            if (_connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }

            _connection.Dispose();
        }
    }
}