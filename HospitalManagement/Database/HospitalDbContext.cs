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

            _connection = new SqlConnection();
            _connection.ConnectionString = Config.ConnectionString;
            _connection.Open();
        }

        public SqlDataReader ExecuteQuery(string sql)
        {
            EnsureConnectionOpen();
            SqlCommand command = new SqlCommand(sql, _connection);

            if (_transaction != null)
            {
                command.Transaction = _transaction;
            }

            return command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        }

        public int ExecuteNonQuery(string sql)
        {
            EnsureConnectionOpen();
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

        public void EnsureConnectionOpen()
        {
            if (_connection == null) return;

            // 1. If the connection forgot its string, re-assign it from Config
            if (string.IsNullOrEmpty(_connection.ConnectionString))
            {
                _connection.ConnectionString = Config.ConnectionString;
            }

            // 2. Check the state. If it's not open, try to open it.
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                // 3. Double-check again just to be 100% sure we have a string now
                if (string.IsNullOrEmpty(_connection.ConnectionString))
                {
                    throw new Exception("CRITICAL: Connection string is still missing in EnsureConnectionOpen!");
                }

                _connection.Open();
            }
        }
    }
}