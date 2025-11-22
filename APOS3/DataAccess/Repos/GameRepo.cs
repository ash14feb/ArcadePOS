using Dapper;
using APOS3.Models;
using APOS3.DataAccess;


namespace APOS3.DataAccess.Repos
{
    public interface IGameRepository
    {
        Task<IEnumerable<EsGame>> GetAllGamesAsync();
        Task<EsGame> GetGameByIdAsync(int id);
        Task<EsGame> GetGameByNameAsync(string gameName);
        Task<bool> CreateGameAsync(EsGame game);
        Task<bool> UpdateGameAsync(EsGame game);
        Task<bool> DeleteGameAsync(int id);
        Task<bool> ToggleGameStatusAsync(int id, string status);
    }
    public class GameRepository : IGameRepository
    {
        private readonly IDatabaseConnection _dbConnection;

        public GameRepository(IDatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<EsGame>> GetAllGamesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT * FROM es_game 
                       ORDER BY game_name ASC";
            return await connection.QueryAsync<EsGame>(sql);
        }

        public async Task<EsGame> GetGameByIdAsync(int id)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT * FROM es_game WHERE id = @Id";
            return await connection.QueryFirstOrDefaultAsync<EsGame>(sql, new { Id = id });
        }

        public async Task<EsGame> GetGameByNameAsync(string gameName)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT * FROM es_game WHERE game_name = @GameName";
            return await connection.QueryFirstOrDefaultAsync<EsGame>(sql, new { GameName = gameName });
        }

        public async Task<bool> CreateGameAsync(EsGame game)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"INSERT INTO es_game 
                       (game_name, status, created_at, center_code, updated_by)
                       VALUES 
                       (@game_name, @status, @created_at, @center_code, @updated_by)";

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    game_name = game.game_name,
                    status = game.status,
                    created_at = DateTime.Now,
                    center_code = game.center_code,
                    updated_by = game.updated_by
                });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating game: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateGameAsync(EsGame game)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"UPDATE es_game 
                       SET game_name = @game_name, 
                           status = @status,
                           updated_at = @updated_at,
                           updated_by = @updated_by,
                           updated_at_dt = @updated_at_dt,
                           updated_at_unixtime = @updated_at_unixtime
                       WHERE id = @id";

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    id = game.id,
                    game_name = game.game_name,
                    status = game.status,
                    updated_at = DateTime.Now,
                    updated_by = game.updated_by,
                    updated_at_dt = DateTime.Now,
                    updated_at_unixtime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds()
                });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating game: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteGameAsync(int id)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"DELETE FROM es_game WHERE id = @Id";

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting game: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleGameStatusAsync(int id, string status)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"UPDATE es_game 
                       SET status = @Status,
                           updated_at = @updated_at,
                           updated_by = @updated_by,
                           updated_at_dt = @updated_at_dt,
                           updated_at_unixtime = @updated_at_unixtime
                       WHERE id = @Id";

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    Id = id,
                    Status = status,
                    updated_at = DateTime.Now,
                    updated_by = "SYSTEM",
                    updated_at_dt = DateTime.Now,
                    updated_at_unixtime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds()
                });
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling game status: {ex.Message}");
                return false;
            }
        }
    }
}