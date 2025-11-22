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
}
