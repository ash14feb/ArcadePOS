using APOS3.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.DataAccess.Repos
{
    public interface IDeviceRepository
    {
        Task<EsDevice> GetDeviceByMacAsync(string macAddress);
        Task<EsSetup> GetSetupByIdAsync(int setupId);
    }

    public interface IRfidService
    {
        Task<string> ProcessRfidMessageAsync(string message, IPEndPoint remoteEndPoint);
        Task<RfidValidationResult> ValidateRfidAndBalanceAsync(string rfid, string deviceMac);
        Task<bool> CreateBillingRecordAsync(RfidValidationResult validationResult, string deviceMac, string deviceIp);
    }

    public class DeviceRepository : IDeviceRepository
    {
        private readonly IDatabaseConnection _dbConnection;

        public DeviceRepository(IDatabaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<EsDevice> GetDeviceByMacAsync(string macAddress)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT id, device_mac, setup_id, center_code 
                   FROM es_device 
                   WHERE device_mac = @MacAddress 
                   AND status = 'ACTIVE'";

            return await connection.QueryFirstOrDefaultAsync<EsDevice>(sql, new { MacAddress = macAddress });
        }

        public async Task<EsSetup> GetSetupByIdAsync(int setupId)
        {
            using var connection = _dbConnection.GetConnection();
            var sql = @"SELECT id, amount, device_type, game_id, center_code 
                   FROM es_setup 
                   WHERE id = @SetupId ";
                //   AND is_current = 'YES'";

            return await connection.QueryFirstOrDefaultAsync<EsSetup>(sql, new { SetupId = setupId });
        }
    }
}
