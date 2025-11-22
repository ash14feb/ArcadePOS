using APOS3.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.Models
{
    public class EsBonus : BaseEntity
    {
        public int Amount { get; set; }
        public int Bonus_Amount { get; set; }
        public DateTime Created_At { get; set; }
        public DateTime? Updated_At { get; set; }
        public string Center_Code { get; set; } = "CENTER_1";
    }

    public class GameRevenue
    {
        public string GameName { get; set; } = "";
        public decimal Revenue { get; set; }
    }

    public class SalesSummary
    {
        public DateTime SaleDate { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalRechargeAmount { get; set; }
        public double? WeekOverWeekChange { get; set; } // Percentage change from last week
    }

    public class CustomerReport
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RFID { get; set; } = string.Empty;
        public decimal MainBalance { get; set; }
        public decimal BonusBalance { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastActivityDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVip { get; set; }
    }

    public class GameReport
    {
        public string GameName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public decimal GameAmount { get; set; }
        public int TotalSessions { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class EsGame : BaseEntity
    {
        public string game_name { get; set; } = string.Empty;
        public string status { get; set; } = "ACTIVE";
        public DateTime created_at { get; set; }
        public int is_sync { get; set; } = 0;
        public long? cloud_id { get; set; }
        public int? origin_local_id { get; set; }
        public string center_key { get; set; } = string.Empty;
        public long updated_at_unixtime { get; set; } = 0;
        public DateTime updated_at_dt { get; set; }
        public string updated_by { get; set; } = "-";
        public DateTime? updated_at { get; set; }
        public string center_code { get; set; } = "CENTER_1";
    }

    // Models for the new tables
    public class EsDevice
    {
        public int id { get; set; }
        public string device_mac { get; set; }
        public int? setup_id { get; set; }
        public string center_code { get; set; }
    }

    public class EsSetup
    {
        public int id { get; set; }
        public int amount { get; set; }
        public string device_type { get; set; }
        public int? game_id { get; set; }
        public string center_code { get; set; }
    }

    // Response DTO
    public class RfidValidationResult
    {
        public bool IsValid { get; set; }
        public bool HasSufficientBalance { get; set; }
        public int CurrentBalance { get; set; }
        public int DeviceAmount { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string SetupId { get; set; }
        public string GameName { get; set; }
        public string Rfid { get; set; } // Add this
    }
}
