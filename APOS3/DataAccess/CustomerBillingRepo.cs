using APOS3.Components.Pages.reports;
using APOS3.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.DataAccess
{
    public interface ICustomerRepository
    {
        Task<EsCustomer?> GetByRfidAsync(string rfid);
        Task<EsCustomer?> GetByPhoneAsync(string phone);
        Task<IEnumerable<EsCustomer>> GetActiveCustomersAsync();
        Task<bool> UpdateCustomerBalanceAsync(int customerId, int newBalance);
        Task<IEnumerable<EsCustomer>> GetCustomersWithLowBalanceAsync(int minBalance);
        Task<int> GetCustomerCountByStatusAsync(string status);

        Task<EsCustomer?> CreateCustomerAsync(EsCustomer customer);
        Task<EsCustomer?> UpdateCustomerAsync(EsCustomer customer);
        Task<bool> CustomerExistsByRfidAsync(string rfid);
        Task<IEnumerable<Models.CustomerReport>> GetCustomerReportAsync(DateTime startDate, DateTime endDate);
        Task<bool> UpdateCustomerBalancesAsync(int customerId, int newMainBalance, int newBonusBalance);

    }

    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDatabaseConnection _dbConnection;

        public CustomerRepository(IDatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public async Task<IEnumerable<Models.CustomerReport>> GetCustomerReportAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbConnection.GetConnection();

            var sql = @"SELECT 
        c.customer_id as CustomerId,
        c.name as Name,
        c.phone as Phone,
        c.email as Email,
        COUNT(p.id) as TotalTransactions,
        SUM(p.recharge_amount) as TotalSpent,
        MAX(p.created_at) as LastActivityDate,
        CASE WHEN MIN(p.created_at) >= @StartDate THEN 1 ELSE 0 END as IsNewCustomer
    FROM es_customer c
    LEFT JOIN es_payment p ON c.customer_id = p.customer_id
    WHERE p.created_at BETWEEN @StartDate AND @EndDate
    AND p.is_deleted = 'NO'
    GROUP BY c.customer_id, c.name, c.phone, c.email
    ORDER BY TotalSpent DESC";

            return await connection.QueryAsync<Models.CustomerReport>(sql, new
            {
                StartDate = startDate,
                EndDate = endDate
            });
        }
        public async Task<EsCustomer?> CreateCustomerAsync(EsCustomer customer)
        {
            using var connection = _dbConnection.GetConnection();

            var sql = @"INSERT INTO es_customer 
                   (guid, rfid, name, dob, email, phone, payment_mode, balance_main, balance_bonus, 
                    status, last_activity, created_at, is_first_punch_done, is_free_time, 
                    free_time_start, free_time_end, game_id, user, is_new, total_token, 
                    restrict_single_punch_for_free_time, location, card_sno, is_vip, 
                    center_code, updated_at_dt, updated_by)
                   VALUES 
                   (@guid, @rfid, @name, @dob, @email, @phone, @payment_mode, @balance_main, @balance_bonus,
                    @status, @last_activity, @created_at, @is_first_punch_done, @is_free_time,
                    @free_time_start, @free_time_end, @game_id, @user, @is_new, @total_token,
                    @restrict_single_punch_for_free_time, @location, @card_sno, @is_vip,
                    @center_code, @updated_at_dt, @updated_by);
                   SELECT LAST_INSERT_ID();";

            try
            {
                var customerId = await connection.ExecuteScalarAsync<int>(sql, customer);

                // Return the created customer with the new ID
                var createdCustomer = await GetByRfidAsync(customer.rfid);
                return createdCustomer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating customer: {ex.Message}");
                return null;
            }
        }

        public async Task<EsCustomer?> UpdateCustomerAsync(EsCustomer customer)
        {
            using var connection = _dbConnection.GetConnection();

            var sql = @"UPDATE es_customer SET
                   name = @name,
                    dob = @dob,
                   email = @email,
                   phone = @phone,
                   payment_mode = @payment_mode,
                   balance_main = @balance_main,
                   balance_bonus = @balance_bonus,
                   status = @status,
                   updated_at_dt = @updated_at_dt,
                   updated_by = @updated_by
                   WHERE id = @id";

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, customer);
                return affectedRows > 0 ? customer : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating customer: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CustomerExistsByRfidAsync(string rfid)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = "SELECT COUNT(1) FROM es_customer WHERE rfid = @Rfid";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { Rfid = rfid });
            return count > 0;
        }
        public async Task<EsCustomer?> GetByRfidAsync(string rfid)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = "SELECT * FROM es_customer WHERE rfid = @Rfid";
            return await connection.QueryFirstOrDefaultAsync<EsCustomer>(sql, new { Rfid = rfid });
        }

        public async Task<EsCustomer?> GetByPhoneAsync(string phone)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = "SELECT * FROM es_customer WHERE phone = @Phone";
            return await connection.QueryFirstOrDefaultAsync<EsCustomer>(sql, new { Phone = phone });
        }

        public async Task<IEnumerable<EsCustomer>> GetActiveCustomersAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var sql = "SELECT * FROM es_customer WHERE status = 'ACTIVE' ORDER BY name";
            return await connection.QueryAsync<EsCustomer>(sql);
        }

        public async Task<bool> UpdateCustomerBalanceAsync(int customerId, int newBalance)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"UPDATE es_customer 
                       SET balance_main = @Balance, updated_at_dt = @UpdatedAt 
                       WHERE id = @CustomerId";

            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                Balance = newBalance,
                UpdatedAt = DateTime.UtcNow,
                CustomerId = customerId
            });

            return affectedRows > 0;
        }

        public async Task<IEnumerable<EsCustomer>> GetCustomersWithLowBalanceAsync(int minBalance)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = "SELECT * FROM es_customer WHERE balance_main < @MinBalance ORDER BY balance_main";
            return await connection.QueryAsync<EsCustomer>(sql, new { MinBalance = minBalance });
        }

        public async Task<int> GetCustomerCountByStatusAsync(string status)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = "SELECT COUNT(*) FROM es_customer WHERE status = @Status";
            return await connection.ExecuteScalarAsync<int>(sql, new { Status = status });
        }
        public async Task<bool> UpdateCustomerBalancesAsync(int customerId, int newMainBalance, int newBonusBalance)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"UPDATE es_customer 
               SET balance_main = @MainBalance, 
                   balance_bonus = @BonusBalance, 
                   updated_at_dt = @UpdatedAt 
               WHERE id = @CustomerId";

            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                MainBalance = newMainBalance,
                BonusBalance = newBonusBalance,
                UpdatedAt = DateTime.UtcNow,
                CustomerId = customerId
            });

            return affectedRows > 0;
        }
    }

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


    public interface IPaymentRepository
    {
        Task<bool> CreatePaymentAsync(EsPayment payment);
        Task<IEnumerable<EsPayment>> GetPaymentsByCustomerAsync(int customerId);
        Task<IEnumerable<EsPayment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalPaymentsByDateAsync(DateTime date);

        // Add to IPaymentRepository
        Task<decimal> GetTodaysVipRevenueAsync();
        Task<int> GetTodaysVipCardsSoldAsync();
        Task<Dictionary<string, int>> GetVipCardsBreakdownAsync();

        Task<decimal> GetTodaysSalesAsync();
        Task<decimal> GetYesterdaysSalesAsync();
        Task<decimal> GetThisMonthsSalesAsync();
        Task<decimal> GetThisWeeksSalesAsync();
        Task<decimal> GetLastWeeksSalesAsync();
        Task<decimal> GetLastMonthsSalesAsync();
        Task<decimal> GetProjectedMonthlySalesAsync();
        Task<decimal> GetLastWeekdaySalesAsync();
        Task<decimal> GetTodaysGameRevenueAsync();

        Task<int> GetTodaysCustomerCountAsync();
        Task<int> GetYesterdaysCustomerCountAsync();
        Task<int> GetThisMonthsCustomerCountAsync();
        Task<int> GetLastMonthsCustomerCountAsync();
        Task<IEnumerable<EsPayment>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<SalesSummary>> GetSalesSummaryByDateRangeAsync(DateTime startDate, DateTime endDate);

    }
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IDatabaseConnection _dbConnection;

        public PaymentRepository(IDatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<bool> CreatePaymentAsync(EsPayment payment)
        {
            using var connection = _dbConnection.GetConnection();

            var sql = @"INSERT INTO es_payment 
           (customer_id, rfid, guid, pre_amount, recharge_amount, discount_percent_on_bill, 
            discount_amount_on_bill, collect_amount_from_customer, bonus_amount, main_amount, 
            post_amount, created_at, name, email, phone, is_deleted, payment_mode, user, 
            gst_amount, is_return_as_full, wa_response, center_code, updated_at_dt, updated_by)
           VALUES 
           (@customer_id, @rfid, @guid, @pre_amount, @recharge_amount, @discount_percent_on_bill,
            @discount_amount_on_bill, @collect_amount_from_customer, @bonus_amount, @main_amount,
            @post_amount, @created_at, @name, @email, @phone, @is_deleted, @payment_mode, @user,
            @gst_amount, @is_return_as_full, @wa_response, @center_code, @updated_at_dt, @updated_by)";

            try
            {
                var affectedRows = await connection.ExecuteAsync(sql, payment);
                return affectedRows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating payment: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<EsPayment>> GetPaymentsByCustomerAsync(int customerId)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT * FROM es_payment 
           WHERE customer_id = @CustomerId AND is_deleted = 'NO'
           ORDER BY created_at DESC";
            return await connection.QueryAsync<EsPayment>(sql, new { CustomerId = customerId });
        }

        public async Task<IEnumerable<EsPayment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = startDate.Date.ToUniversalTime();
            var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT * FROM es_payment 
           WHERE created_at BETWEEN @StartDate AND @EndDate AND is_deleted = 'NO'
           ORDER BY created_at DESC";
            return await connection.QueryAsync<EsPayment>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetTotalPaymentsByDateAsync(DateTime date)
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = date.Date.ToUniversalTime();
            var utcEndDate = date.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(collect_amount_from_customer), 0) 
           FROM es_payment 
           WHERE created_at BETWEEN @StartDate AND @EndDate AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetTodaysVipRevenueAsync()
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = DateTime.Today.ToUniversalTime();
            var utcEndDate = DateTime.Today.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO' 
       AND recharge_amount >= 3000";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<int> GetTodaysVipCardsSoldAsync()
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = DateTime.Today.ToUniversalTime();
            var utcEndDate = DateTime.Today.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COUNT(*) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO' 
       AND recharge_amount >= 3000";
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<Dictionary<string, int>> GetVipCardsBreakdownAsync()
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = DateTime.Today.ToUniversalTime();
            var utcEndDate = DateTime.Today.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT 
           CASE 
               WHEN recharge_amount = 3000 THEN 'Rs 3,000 Cards'
               WHEN recharge_amount = 5000 THEN 'Rs 5,000 Cards'
               ELSE CONCAT('Rs ', recharge_amount, ' Cards')
           END as CardType,
           COUNT(*) as Count
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO' 
       AND recharge_amount >= 3000
       GROUP BY recharge_amount";

            var results = await connection.QueryAsync<(string CardType, int Count)>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
            return results.ToDictionary(x => x.CardType, x => x.Count);
        }

        public async Task<decimal> GetTodaysSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = DateTime.Today.ToUniversalTime();
            var utcEndDate = DateTime.Today.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetYesterdaysSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var yesterday = DateTime.Today.AddDays(-1);
            // Convert local dates to UTC for database comparison
            var utcStartDate = yesterday.ToUniversalTime();
            var utcEndDate = yesterday.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetThisMonthsSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endOfMonth = DateTime.Today.AddDays(1).AddTicks(-1); // Today 23:59:59

            // Convert local dates to UTC for database comparison
            var utcStartDate = startOfMonth.ToUniversalTime();
            var utcEndDate = endOfMonth.ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetThisWeeksSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var endOfWeek = DateTime.Today.AddDays(1).AddTicks(-1); // Today 23:59:59

            // Convert local dates to UTC for database comparison
            var utcStartDate = startOfWeek.ToUniversalTime();
            var utcEndDate = endOfWeek.ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetLastWeeksSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var startOfLastWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek - 7);
            var endOfLastWeek = startOfLastWeek.AddDays(7).AddTicks(-1);

            // Convert local dates to UTC for database comparison
            var utcStartDate = startOfLastWeek.ToUniversalTime();
            var utcEndDate = endOfLastWeek.ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetLastMonthsSalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var lastMonth = DateTime.Today.AddMonths(-1);
            var startOfLastMonth = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endOfLastMonth = new DateTime(lastMonth.Year, lastMonth.Month,
                                            DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month))
                                .AddDays(1).AddTicks(-1);

            // Convert local dates to UTC for database comparison
            var utcStartDate = startOfLastMonth.ToUniversalTime();
            var utcEndDate = endOfLastMonth.ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetProjectedMonthlySalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endOfMonth = DateTime.Today.AddDays(1).AddTicks(-1);

            // Convert local dates to UTC for database comparison
            var utcStartDate = startOfMonth.ToUniversalTime();
            var utcEndDate = endOfMonth.ToUniversalTime();

            // Use CONVERT_TZ for date calculations in the projection
            var sql = @"SELECT 
     CASE 
         WHEN DAY(CONVERT_TZ(NOW(), '+00:00', '+05:30')) > 1 THEN 
             (COALESCE(SUM(recharge_amount), 0) / DAY(CONVERT_TZ(NOW(), '+00:00', '+05:30'))) * DAY(LAST_DAY(CONVERT_TZ(NOW(), '+00:00', '+05:30')))
         ELSE 
             COALESCE(SUM(recharge_amount), 0)
     END as ProjectedSales
     FROM es_payment 
     WHERE created_at BETWEEN @StartDate AND @EndDate
     AND is_deleted = 'NO'";

            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetLastWeekdaySalesAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var lastWeekday = DateTime.Today.AddDays(-7);
            // Convert local dates to UTC for database comparison
            var utcStartDate = lastWeekday.ToUniversalTime();
            var utcEndDate = lastWeekday.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<decimal> GetTodaysGameRevenueAsync()
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = DateTime.Today.ToUniversalTime();
            var utcEndDate = DateTime.Today.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COALESCE(SUM(recharge_amount), 0) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'
       AND recharge_amount < 3000";
            return await connection.ExecuteScalarAsync<decimal>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<int> GetTodaysCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = DateTime.Today.ToUniversalTime();
            var utcEndDate = DateTime.Today.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COUNT(DISTINCT customer_id) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<int> GetYesterdaysCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var yesterday = DateTime.Today.AddDays(-1);
            // Convert local dates to UTC for database comparison
            var utcStartDate = yesterday.ToUniversalTime();
            var utcEndDate = yesterday.AddDays(1).AddTicks(-1).ToUniversalTime();

            var sql = @"SELECT COUNT(DISTINCT customer_id) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<int> GetThisMonthsCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endOfMonth = DateTime.Today.AddDays(1).AddTicks(-1);

            // Convert local dates to UTC for database comparison
            var utcStartDate = startOfMonth.ToUniversalTime();
            var utcEndDate = endOfMonth.ToUniversalTime();

            var sql = @"SELECT COUNT(DISTINCT customer_id) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<int> GetLastMonthsCustomerCountAsync()
        {
            using var connection = _dbConnection.GetConnection();
            var lastMonth = DateTime.Today.AddMonths(-1);
            var startOfLastMonth = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endOfLastMonth = new DateTime(lastMonth.Year, lastMonth.Month,
                                            DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month))
                                .AddDays(1).AddTicks(-1);

            // Convert local dates to UTC for database comparison
            var utcStartDate = startOfLastMonth.ToUniversalTime();
            var utcEndDate = endOfLastMonth.ToUniversalTime();

            var sql = @"SELECT COUNT(DISTINCT customer_id) 
       FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'";
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<IEnumerable<EsPayment>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbConnection.GetConnection();
            // Convert local dates to UTC for database comparison
            var utcStartDate = startDate.Date.ToUniversalTime();
            var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            // Use CONVERT_TZ to convert database UTC time to local time for display in ordering
            var sql = @"SELECT * FROM es_payment 
       WHERE created_at BETWEEN @StartDate AND @EndDate
       AND is_deleted = 'NO'
       ORDER BY CONVERT_TZ(created_at, '+00:00', '+05:30') DESC";

            return await connection.QueryAsync<EsPayment>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }

        public async Task<IEnumerable<SalesSummary>> GetSalesSummaryByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            using var connection = _dbConnection.GetConnection();

            // Convert local dates to UTC for database comparison
            var utcStartDate = startDate.Date.ToUniversalTime();
            var utcEndDate = endDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            // Use CONVERT_TZ to convert database UTC time to local time for display
            var sql = @"SELECT 
             DATE(CONVERT_TZ(created_at, '+00:00', '+05:30')) as SaleDate,
             COUNT(*) as TransactionCount,
             SUM(recharge_amount) as TotalRechargeAmount
         FROM es_payment 
         WHERE created_at BETWEEN @StartDate AND @EndDate
         AND is_deleted = 'NO'
         GROUP BY DATE(CONVERT_TZ(created_at, '+00:00', '+05:30'))
         ORDER BY SaleDate DESC";

            return await connection.QueryAsync<SalesSummary>(sql, new
            {
                StartDate = utcStartDate,
                EndDate = utcEndDate
            });
        }
    }
}
