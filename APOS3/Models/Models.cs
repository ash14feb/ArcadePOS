using APOS3.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.Models
{
    public class EsActivity : BaseEntity
    {
        public string type { get; set; } = ""; // 'time-restricted','non-time-restricted'
        public string name { get; set; } = "";
        public DateTime created_at { get; set; }
        public string? user { get; set; }
        public string center_code { get; set; } = "CENTER_1";
    }

    public class EsCustomer : BaseEntity
    {
        public string? guid { get; set; }
        public string rfid { get; set; } = "";
        public string name { get; set; } = "";
        public string? dob { get; set; }
        public string? email { get; set; }
        public string phone { get; set; } = "";
        public string payment_mode { get; set; } = "CASH";
        public int balance_main { get; set; }
        public int? balance_bonus { get; set; }
        public string status { get; set; } = "";
        public DateTime? last_activity { get; set; }
        public DateTime created_at { get; set; }
        public int is_first_punch_done { get; set; } = 0;
        public string is_free_time { get; set; } = "NO";
        public int? free_time_start { get; set; }
        public int? free_time_end { get; set; }
        public string? game_id { get; set; }
        public string? user { get; set; }
        public int? is_new { get; set; }
        public int? total_token { get; set; }
        public int restrict_single_punch_for_free_time { get; set; } = 0;
        public string location { get; set; } = "";
        public int card_sno { get; set; } = 0;
        public int is_vip { get; set; } = 0;
        public DateTime? updated_at { get; set; }
        public string center_code { get; set; } = "CENTER_1";
    }

    public class EsBilling : BaseEntity
    {
        public string? guid { get; set; }
        public string rfid { get; set; } = "";
        public int setup_id { get; set; }
        public int customer_id { get; set; }
        public int pre_amount { get; set; }
        public int amount { get; set; }
        public int post_amount { get; set; }
        public DateTime created_at { get; set; }
        public string log_device_ip { get; set; } = "";
        public string log_game { get; set; } = "";
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public string phone { get; set; } = "";
        public string is_deleted { get; set; } = "NO";
        public int? created_at_unix { get; set; }
        public int? token_earn { get; set; }
        public int customer_main_amount_used { get; set; } = 0;
        public int customer_bonus_amount_used { get; set; } = 0;
        public string is_refund { get; set; } = "NO";
        public int customer_main_balance_before_refund { get; set; } = 0;
        public int customer_bonus_balance_before_refund { get; set; } = 0;
        public int customer_main_balance_after_refund { get; set; } = 0;
        public int customer_bonus_balance_after_refund { get; set; } = 0;
        public DateTime? refund_at { get; set; }
        public string refund_by { get; set; } = "--";
        public string card_type { get; set; } = "--";
        public DateTime? updated_at { get; set; }
        public string center_code { get; set; } = "CENTER_1";
    }

    public class EsPayment : BaseEntity
    {
        public int customer_id { get; set; }
        public string rfid { get; set; } = "";
        public string? guid { get; set; }
        public int pre_amount { get; set; }
        public int? recharge_amount { get; set; }
        public int discount_percent_on_bill { get; set; } = 0;
        public int discount_amount_on_bill { get; set; } = 0;
        public int collect_amount_from_customer { get; set; } = 0;
        public int bonus_amount { get; set; }
        public int main_amount { get; set; }
        public int post_amount { get; set; }
        public DateTime created_at { get; set; }
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public string phone { get; set; } = "";
        public string is_deleted { get; set; } = "NO";
        public string payment_mode { get; set; } = "CASH";
        public string? user { get; set; }
        public decimal? gst_amount { get; set; }
        public int is_return_as_full { get; set; } = 0;
        public string wa_response { get; set; } = "";
        public DateTime? updated_at { get; set; }
        public string center_code { get; set; } = "CENTER_1";
    }
}
