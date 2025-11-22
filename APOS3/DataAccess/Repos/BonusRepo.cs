using APOS3.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.DataAccess.Repos
{

 
        public interface IBonusRepository
        {
            Task<IEnumerable<EsBonus>> GetAllBonusesAsync();
            Task<EsBonus?> GetBonusByAmountAsync(int amount);
            Task<IEnumerable<EsBonus>> GetBonusesOrderedByAmountAsync();
            Task<bool> AddBonusAsync(EsBonus bonus);
            Task<bool> UpdateBonusAsync(EsBonus bonus);
        }

        public class BonusRepository : IBonusRepository
        {
            private readonly IDatabaseConnection _dbConnection;

            public BonusRepository(IDatabaseConnection dbConnection)
            {
                _dbConnection = dbConnection;
            }

            public async Task<IEnumerable<EsBonus>> GetAllBonusesAsync()
            {
                using var connection = _dbConnection.GetConnection();
                var sql = "SELECT * FROM es_bonus ORDER BY amount ASC";
                return await connection.QueryAsync<EsBonus>(sql);
            }

            public async Task<EsBonus?> GetBonusByAmountAsync(int amount)
            {
                using var connection = _dbConnection.GetConnection();
                var sql = "SELECT * FROM es_bonus WHERE amount = @Amount";
                return await connection.QueryFirstOrDefaultAsync<EsBonus>(sql, new { Amount = amount });
            }

            public async Task<IEnumerable<EsBonus>> GetBonusesOrderedByAmountAsync()
            {
                using var connection = _dbConnection.GetConnection();
                var sql = "SELECT * FROM es_bonus ORDER BY amount ASC";
                return await connection.QueryAsync<EsBonus>(sql);
            }

            public async Task<bool> AddBonusAsync(EsBonus bonus)
            {
                using var connection = _dbConnection.GetConnection();
                var sql = @"INSERT INTO es_bonus 
                       (amount, bonus_amount, created_at, center_code)
                       VALUES 
                       (@Amount, @BonusAmount, @CreatedAt, @CenterCode)";

                var affectedRows = await connection.ExecuteAsync(sql, bonus);
                return affectedRows > 0;
            }

            public async Task<bool> UpdateBonusAsync(EsBonus bonus)
            {
                using var connection = _dbConnection.GetConnection();
                var sql = @"UPDATE es_bonus 
                       SET bonus_amount = @BonusAmount, updated_at = @UpdatedAt,
                           updated_at_dt = @UpdatedAtDt, updated_by = @UpdatedBy
                       WHERE amount = @Amount";

                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    bonus.Bonus_Amount,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedAtDt = DateTime.UtcNow,
                    bonus.updated_by,
                    bonus.Amount
                });

                return affectedRows > 0;
            }
        }
    }
