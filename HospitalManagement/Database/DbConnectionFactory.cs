using System;
using System.Data.SqlClient;
using HospitalManagement.Configuration;

namespace HospitalManagement.Database
{
    public static class DbConnectionFactory
    {
        public static SqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(Config.ConnectionString))
            {
                throw new Exception("Connection string is not loaded.");
            }

            return new SqlConnection(Config.ConnectionString);
        }

        public static void TestConnection()
        {
            using (SqlConnection connection = CreateConnection())
            {
                connection.Open();
            }
        }
    }
}