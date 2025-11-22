using APOS3.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.DataAccess
{
    public interface IBillingRepository
    {
        Task<IEnumerable<EsBilling>> GetBillingsByCustomerAsync(int customerId);
        Task<IEnumerable<EsBilling>> GetBillingsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalBillingAmountByDateAsync(DateTime date);
        Task<IEnumerable<EsBilling>> GetRecentBillingsAsync(int count = 50);
        Task<bool> CreateBillingAsync(EsBilling billing);

        Task<bool> UpdateBillingAsRefundedAsync(int billingId, int balanceBefore, int balanceAfter, string refundBy = "SYSTEM");

        Task<bool> CreateRechargeBillingAsync(EsBilling billing, int bonusAmount);




        // Add to IBillingRepository
        Task<decimal> GetTodaysSalesAsync();
        Task<decimal> GetYesterdaysSalesAsync();
        Task<decimal> GetThisMonthsSalesAsync();
        Task<decimal> GetThisWeeksSalesAsync();
        Task<decimal> GetLastWeeksSalesAsync();
        Task<IEnumerable<GameRevenue>> GetTopPerformingGamesAsync(int days = 1);
        Task<IEnumerable<GameRevenue>> GetLowestPerformingGamesAsync(int days = 1);

        Task<decimal> GetTodaysGameRevenueAsync();
        // Add to IBillingRepository
        Task<decimal> GetLastMonthsSalesAsync();
        Task<decimal> GetProjectedMonthlySalesAsync();


        // Add to IBillingRepository
        Task<decimal> GetLastWeekdaySalesAsync();
        Task<int> GetTodaysCustomerCountAsync();
        Task<int> GetYesterdaysCustomerCountAsync();
        Task<int> GetThisMonthsCustomerCountAsync();
        Task<int> GetLastMonthsCustomerCountAsync();

        Task<IEnumerable<GameReport>> GetGameRevenueReportAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<EsBilling>> GetRefundsByDateRangeAsync(DateTime startDate, DateTime endDate);

    }

    public class BillingRepository : IBillingRepository
    {
        private readonly IDatabaseConnection _dbConnection;

        public BillingRepository(IDatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        // Implementation in BillingRepository
        public async Task<IEnumerable<EsBilling>> GetRefundsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbConnection.GetConnection();

            // Convert to UTC for database comparison
            var utcStartDate = startDate.Date.ToUniversalTime();
            var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT * FROM es_billing 
                WHERE (refund_at BETWEEN @StartDate AND @EndDate OR created_at BETWEEN @StartDate AND @EndDate)
                AND is_refund = 'YES'
                AND is_deleted = 'NO'
                ORDER BY refund_at DESC, created_at DESC";

            return await connection.QueryAsync<EsBilling>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }
        public async Task<bool> CreateRechargeBillingAsync(EsBilling billing, int bonusAmount)
        {
            using var connection = _dbConnection.GetConnection();

            var sql = @"INSERT INTO es_billing 
                   (guid, rfid, setup_id, customer_id, pre_amount, amount, post_amount, 
                    created_at, log_device_ip, log_game, name, email, phone, is_deleted,
                    customer_main_amount_used, customer_bonus_amount_used, center_code)
                   VALUES 
                   (@guid, @rfid, @setup_id, @customer_id, @pre_amount, @amount, @post_amount,
                    @created_at, @log_device_ip, @log_game, @name, @email, @phone, @is_deleted,
                    @customer_main_amount_used, @customer_bonus_amount_used, @center_code)";

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, billing);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating recharge billing: {ex.Message}");
                return false;
            }
        }
        public async Task<IEnumerable<EsBilling>> GetBillingsByCustomerAsync(int customerId)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT * FROM es_billing 
                       WHERE customer_id = @CustomerId AND is_deleted = 'NO'
                       ORDER BY created_at DESC";
            return await connection.QueryAsync<EsBilling>(sql, new { CustomerId = customerId });
        }

        public async Task<IEnumerable<EsBilling>> GetBillingsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT * FROM es_billing 
                       WHERE created_at BETWEEN @StartDate AND @EndDate
                       ORDER BY created_at DESC";
            return await connection.QueryAsync<EsBilling>(sql, new
            {
                StartDate = startDate,
                EndDate = endDate
            });
        }

        public async Task<decimal> GetTotalBillingAmountByDateAsync(DateTime date)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
                       FROM es_billing 
                       WHERE DATE(created_at) = @Date AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new { Date = date.Date });
        }

        public async Task<IEnumerable<EsBilling>> GetRecentBillingsAsync(int count = 50)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT * FROM es_billing 
                       WHERE is_deleted = 'NO'
                       ORDER BY created_at DESC 
                       LIMIT @Count";
            return await connection.QueryAsync<EsBilling>(sql, new { Count = count });
        }

        public async Task<bool> CreateBillingAsync(EsBilling billing)
        {
            using var connection = _dbConnection.GetConnection();

            //var sql = @"INSERT INTO es_billing 
            //           (guid, rfid, setup_id, customer_id, pre_amount, amount, post_amount, 
            //            created_at, log_device_ip, log_game, name, email, phone, is_deleted,
            //            customer_main_amount_used, customer_bonus_amount_used, center_code)
            //           VALUES 
            //           (@Guid, @Rfid, @setup_id, @CustomerId, @PreAmount, @Amount, @PostAmount,
            //            @CreatedAt, @LogDeviceIp, @LogGame, @Name, @Email, @Phone, @IsDeleted,
            //            @CustomerMainAmountUsed, @CustomerBonusAmountUsed, @CenterCode)";
            var sql = @"INSERT INTO es_billing 
                   (guid, rfid, setup_id, customer_id, pre_amount, amount, post_amount, 
                    created_at, log_device_ip, log_game, name, email, phone, is_deleted,
                    customer_main_amount_used, customer_bonus_amount_used, center_code)
                   VALUES 
                   (@guid, @rfid, @setup_id, @customer_id, @pre_amount, @amount, @post_amount,
                    @created_at, @log_device_ip, @log_game, @name, @email, @phone, @is_deleted,
                    @customer_main_amount_used, @customer_bonus_amount_used, @center_code)";

            var affectedRows = await connection.ExecuteAsync(sql, billing);
            return affectedRows > 0;
        }


        // Add this method to your BillingRepository class
        public async Task<bool> UpdateBillingAsRefunded(int billingId, int balanceBefore, int balanceAfter, string refundBy = "SYSTEM")
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"UPDATE es_billing 
               SET is_refund = 'YES', 
                   refund_at = @RefundAt,
                   refund_by = @RefundBy,
                   customer_main_balance_before_refund = @BalanceBefore,
                   customer_main_balance_after_refund = @BalanceAfter
               WHERE id = @BillingId";

            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                RefundAt = DateTime.UtcNow,
                RefundBy = refundBy,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                BillingId = billingId
            });

            return affectedRows > 0;
        }

        public async Task<bool> UpdateBillingAsRefundedAsync(int billingId, int balanceBefore, int balanceAfter, string refundBy = "SYSTEM")
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"UPDATE es_billing 
                   SET is_refund = 'YES', 
                       refund_at = @RefundAt,
                       refund_by = @RefundBy,
                       customer_main_balance_before_refund = @BalanceBefore,
                       customer_main_balance_after_refund = @BalanceAfter
                   WHERE id = @BillingId";

            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                RefundAt = DateTime.UtcNow,
                RefundBy = refundBy,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                BillingId = billingId
            });

            return affectedRows > 0;
        }



        // Add to BillingRepository
        public async Task<decimal> GetTodaysSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE DATE(created_at) = CURDATE() AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<decimal> GetYesterdaysSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE DATE(created_at) = DATE_SUB(CURDATE(), INTERVAL 1 DAY) AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<decimal> GetThisMonthsSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE YEAR(created_at) = YEAR(CURDATE()) 
               AND MONTH(created_at) = MONTH(CURDATE()) 
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<decimal> GetThisWeeksSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE YEARWEEK(created_at, 1) = YEARWEEK(CURDATE(), 1) 
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<decimal> GetLastWeeksSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE YEARWEEK(created_at, 1) = YEARWEEK(CURDATE(), 1) - 1 
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<IEnumerable<GameRevenue>> GetTopPerformingGamesAsync(int days = 1)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT log_game as GameName, SUM(amount) as Revenue
               FROM es_billing 
               WHERE created_at >= DATE_SUB(CURDATE(), INTERVAL @Days DAY) 
               AND is_deleted = 'NO'
               AND log_game NOT LIKE '%Package Recharge%'
               AND log_game NOT LIKE '%Recharge%'
               AND log_game NOT LIKE '%Package%'
               AND log_game NOT LIKE '%VIP%'
               GROUP BY log_game 
               HAVING Revenue > 0
               ORDER BY Revenue DESC 
               LIMIT 5";
            return await connection.QueryAsync<GameRevenue>(sql, new { Days = days });
        }

        public async Task<IEnumerable<GameRevenue>> GetLowestPerformingGamesAsync(int days = 30)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT log_game as GameName, SUM(amount) as Revenue
               FROM es_billing 
               WHERE created_at >= DATE_SUB(CURDATE(), INTERVAL @Days DAY) 
               AND is_deleted = 'NO'
               AND log_game NOT LIKE '%Package Recharge%'
               AND log_game NOT LIKE '%Recharge%'
               AND log_game NOT LIKE '%Package%'
               AND log_game NOT LIKE '%VIP%'
               GROUP BY log_game 
               HAVING Revenue > 0
               ORDER BY Revenue ASC 
               LIMIT 5";
            return await connection.QueryAsync<GameRevenue>(sql, new { Days = days });
        }

        public async Task<decimal> GetTodaysGameRevenueAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE DATE(created_at) = CURDATE() 
               AND is_deleted = 'NO'
               AND log_game NOT LIKE '%Package Recharge%'
               AND log_game NOT LIKE '%Recharge%'
               AND log_game NOT LIKE '%Package%'
               AND log_game NOT LIKE '%VIP%'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }
        // Add to BillingRepository
        public async Task<decimal> GetLastMonthsSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE YEAR(created_at) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH)
               AND MONTH(created_at) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH)
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<decimal> GetProjectedMonthlySalesAsync()
        {
            using var connection = _dbConnection.GetConnection();

            // Calculate projection based on current month's performance so far
            var sql = @"SELECT 
        CASE 
            WHEN DAY(CURDATE()) > 1 THEN 
                (COALESCE(SUM(amount), 0) / DAY(CURDATE())) * DAY(LAST_DAY(CURDATE()))
            ELSE 
                COALESCE(SUM(amount), 0)
        END as ProjectedSales
        FROM es_billing 
        WHERE YEAR(created_at) = YEAR(CURDATE())
        AND MONTH(created_at) = MONTH(CURDATE())
        AND is_deleted = 'NO'";

            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        // Add to BillingRepository
        public async Task<decimal> GetLastWeekdaySalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COALESCE(SUM(amount), 0) 
               FROM es_billing 
               WHERE DATE(created_at) = DATE_SUB(CURDATE(), INTERVAL 7 DAY)
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }

        public async Task<int> GetTodaysCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COUNT(DISTINCT customer_id) 
               FROM es_billing 
               WHERE DATE(created_at) = CURDATE() 
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetYesterdaysCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COUNT(DISTINCT customer_id) 
               FROM es_billing 
               WHERE DATE(created_at) = DATE_SUB(CURDATE(), INTERVAL 1 DAY)
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetThisMonthsCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COUNT(DISTINCT customer_id) 
               FROM es_billing 
               WHERE YEAR(created_at) = YEAR(CURDATE()) 
               AND MONTH(created_at) = MONTH(CURDATE())
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> GetLastMonthsCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT COUNT(DISTINCT customer_id) 
               FROM es_billing 
               WHERE YEAR(created_at) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH)
               AND MONTH(created_at) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH)
               AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql);
        }
        // Implementation in BillingRepository
        public async Task<IEnumerable<GameReport>> GetGameRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbConnection.GetConnection();

            // Convert to UTC for database comparison
            var utcStartDate = startDate.Date.ToUniversalTime();
            var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT 
        COALESCE(g.game_name, 'Unknown Game') as GameName,
        COALESCE(s.device_type, 'Unknown Device') as DeviceType,
        COALESCE(s.amount, 0) as GameAmount,
        COUNT(b.id) as TotalSessions,
        COALESCE(SUM(b.amount), 0) as TotalRevenue
    FROM es_billing b
    LEFT JOIN es_setup s ON b.setup_id = s.id
    LEFT JOIN es_game g ON s.game_id = g.id
    WHERE b.created_at BETWEEN @StartDate AND @EndDate
    AND b.log_game NOT LIKE '%Package Recharge%'
               AND b.log_game NOT LIKE '%Recharge%'
               AND b.log_game NOT LIKE '%Package%'
AND b.log_game != ''
    AND b.is_deleted = 'NO'
    AND b.is_refund = 'NO'
    GROUP BY g.game_name, s.device_type, s.amount
    ORDER BY TotalRevenue DESC";

            return await connection.QueryAsync<GameReport>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }
    }

}
