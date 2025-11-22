using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.DataAccess
{
    public interface IDatabaseConnection
    {
        IDbConnection GetConnection();
        string ConnectionString { get; }
    }

    public class DatabaseConnection : IDatabaseConnection
    {
        private readonly string _connectionString;

        public DatabaseConnection(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public string ConnectionString => _connectionString;
    }

    public class BaseEntity
    {
        public int id { get; set; }
        public int is_sync { get; set; } = 0;
        public long? cloud_id { get; set; }
        public int? origin_local_id { get; set; }
        public string center_key { get; set; } = "";
        public long updated_at_unixtime { get; set; } = 0;
        public DateTime updated_at_dt { get; set; } = DateTime.UtcNow;
        public string updated_by { get; set; } = "-";
    }
}
