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
