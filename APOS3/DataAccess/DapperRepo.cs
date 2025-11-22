using System.Data;
using Dapper;
using System.Linq.Expressions;

namespace APOS3.DataAccess
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<int> InsertAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<T>> QueryAsync(string sql, object parameters = null);
        Task<T?> QueryFirstOrDefaultAsync(string sql, object parameters = null);
        Task<int> ExecuteAsync(string sql, object parameters = null);
    }

    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly IDatabaseConnection _dbConnection;
        private readonly string _tableName;

        public GenericRepository(IDatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _tableName = typeof(T).Name.ToLower().Replace("es_", "es_");
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = $"SELECT * FROM {_tableName} WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = $"SELECT * FROM {_tableName}";
            return await connection.QueryAsync<T>(sql);
        }

        public async Task<int> InsertAsync(T entity)
        {
            using var connection = _dbConnection.GetConnection();

            // Generate INSERT statement dynamically
            var properties = typeof(T).GetProperties()
                .Where(p => p.Name != "Id" && p.CanWrite)
                .Select(p => p.Name);

            var columns = string.Join(", ", properties);
            var parameters = string.Join(", ", properties.Select(p => $"@{p}"));

            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            using var connection = _dbConnection.GetConnection();

            var properties = typeof(T).GetProperties()
                .Where(p => p.Name != "Id" && p.CanWrite)
                .Select(p => p.Name);

            var setClause = string.Join(", ", properties.Select(p => $"{p} = @{p}"));

            var sql = $"UPDATE {_tableName} SET {setClause} WHERE id = @Id";

            var affectedRows = await connection.ExecuteAsync(sql, entity);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = $"DELETE FROM {_tableName} WHERE id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            return affectedRows > 0;
        }

        public async Task<IEnumerable<T>> QueryAsync(string sql, object parameters = null)
        {
            using var connection = _dbConnection.GetConnection();
            return await connection.QueryAsync<T>(sql, parameters);
        }

        public async Task<T?> QueryFirstOrDefaultAsync(string sql, object parameters = null)
        {
            using var connection = _dbConnection.GetConnection();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        }

        public async Task<int> ExecuteAsync(string sql, object parameters = null)
        {
            using var connection = _dbConnection.GetConnection();
            return await connection.ExecuteAsync(sql, parameters);
        }
    }
}
