using APOS3.DataAccess;
using APOS3.DataAccess.Repos;
using APOS3.Models;
using Microsoft.Extensions.Logging;
using System.Net;

public class RfidService : IRfidService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IBillingRepository _billingRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly ILogger<RfidService> _logger;
    private readonly IGameRepository _gameRepository;
    public RfidService(
        ICustomerRepository customerRepository,
        IBillingRepository billingRepository,
        IDeviceRepository deviceRepository, IGameRepository gameRepository,
        ILogger<RfidService> logger)
    {
        _customerRepository = customerRepository;
        _billingRepository = billingRepository;
        _deviceRepository = deviceRepository;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<string> ProcessRfidMessageAsync(string message, IPEndPoint remoteEndPoint)
    {
        try
        {
            _logger.LogInformation($"Processing RFID message: {message} from {remoteEndPoint}");

            // Parse the message format: {RFID}<MAC>
            var rfid = ParseRfidFromMessage(message);
            var deviceMac = ParseMacFromMessage(message);

            if (string.IsNullOrEmpty(rfid) || string.IsNullOrEmpty(deviceMac))
            {
                _logger.LogWarning($"Invalid message format: {message}");
                return "@ERROR"; // Default error response
            }

            // Validate RFID and check balance
            var validationResult = await ValidateRfidAndBalanceAsync(rfid, deviceMac);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning($"Invalid RFID or customer not found: {rfid}");
                return "@ERROR";
            }

            // Prepare response based on balance
            string response;
            if (validationResult.HasSufficientBalance)
            {
                response = $"(@0{validationResult.CurrentBalance:D3})";

                // Create billing record since balance is sufficient
                await CreateBillingRecordAsync(validationResult, deviceMac, remoteEndPoint.Address.ToString());
            }
            else
            {
                response = $"<@{validationResult.CurrentBalance:D3}>";
            }

            _logger.LogInformation($"RFID validation completed. Response: {response}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing RFID message: {message}");
            return "@ERROR";
        }
    }

    public async Task<RfidValidationResult> ValidateRfidAndBalanceAsync(string rfid, string deviceMac)
    {
        var result = new RfidValidationResult();

        try
        {
            // Get customer by RFID
            var customer = await _customerRepository.GetByRfidAsync(rfid);
            if (customer == null)
            {
                result.IsValid = false;
                return result;
            }

            // Get device by MAC
            var device = await _deviceRepository.GetDeviceByMacAsync(deviceMac);
            if (device == null || device.setup_id == null)
            {
                result.IsValid = false;
                return result;
            }

            // Get setup details
            var setup = await _deviceRepository.GetSetupByIdAsync(device.setup_id.Value);
            if (setup == null)
            {
                result.IsValid = false;
                return result;
            }
            var game = await _gameRepository.GetGameByIdAsync(Convert.ToInt32(setup.game_id));

            // Calculate total balance
            int totalBalance = (customer.balance_main ) + (customer.balance_bonus ?? 0);

            result.IsValid = true;
            result.Rfid = rfid; // Set the RFID here

            result.CustomerId = customer.id;
            result.CustomerName = customer.name;
            result.CurrentBalance = totalBalance;
            result.DeviceAmount = setup.amount;
            result.SetupId = device.setup_id.ToString();
            result.GameName = Convert.ToString(game.game_name);
            result.HasSufficientBalance = totalBalance >= setup.amount;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating RFID: {rfid}");
            result.IsValid = false;
            return result;
        }
    }

    public async Task<bool> CreateBillingRecordAsync(RfidValidationResult validationResult, string deviceMac, string deviceIp)
    {
        try
        {
            var billing = new EsBilling
            {
                guid = Guid.NewGuid().ToString(),
                rfid = validationResult.Rfid.ToString(), // You might want to store actual RFID here
                setup_id = int.Parse(validationResult.SetupId),
                customer_id = validationResult.CustomerId,
                pre_amount = validationResult.CurrentBalance,
                amount = validationResult.DeviceAmount,
                post_amount = validationResult.CurrentBalance - validationResult.DeviceAmount,
                created_at = DateTime.UtcNow,
                log_device_ip = deviceIp,
                log_game = validationResult.GameName,
                name = validationResult.CustomerName,
                email = "", // You might want to get this from customer
                phone = "", // You might want to get this from customer
                is_deleted = "NO",
                customer_main_amount_used = validationResult.DeviceAmount,
                customer_bonus_amount_used = 0, // You might want to calculate this based on your logic
                center_code = "CENTER_1" // You might want to get this from device or setup
            };

            var success = await _billingRepository.CreateBillingAsync(billing);

            if (success)
            {
                // Get current customer to check both balances
                var customer = await _customerRepository.GetByRfidAsync(validationResult.Rfid); // You'll need to store RFID in validationResult
                if (customer == null) return false;

                int currentMainBalance = customer.balance_main ;
                int currentBonusBalance = customer.balance_bonus ?? 0;
                int deviceAmount = validationResult.DeviceAmount;

                int newMainBalance, newBonusBalance;

                if (currentMainBalance >= deviceAmount)
                {
                    // Deduct entirely from main balance
                    newMainBalance = currentMainBalance - deviceAmount;
                    newBonusBalance = currentBonusBalance; // No change to bonus
                }
                else
                {
                    // Calculate how much we can take from main balance (minimum -1)
                    int amountFromMain = Math.Max(currentMainBalance - (-1), 0);
                    amountFromMain = Math.Min(amountFromMain, deviceAmount);

                    // The remaining amount comes from bonus balance
                    int amountFromBonus = deviceAmount - amountFromMain;

                    newMainBalance = currentMainBalance - amountFromMain;
                    newBonusBalance = currentBonusBalance - amountFromBonus;

                    // Ensure main balance doesn't go below -1
                    if (newMainBalance < -1)
                    {
                        // Adjust if needed (this shouldn't happen with above logic, but safety check)
                        int adjustment = -1 - newMainBalance;
                        newMainBalance = -1;
                        newBonusBalance -= adjustment; // Take the extra from bonus
                    }
                }

                // Update both balances in the database
                await _customerRepository.UpdateCustomerBalancesAsync(validationResult.CustomerId, newMainBalance, newBonusBalance);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating billing record for customer: {validationResult.CustomerId}");
            return false;
        }
    }

    private string ParseRfidFromMessage(string message)
    {
        try
        {
            var startIndex = message.IndexOf('{');
            var endIndex = message.IndexOf('}') + 1;

            if (startIndex >= 0 && endIndex > startIndex)
            {
                return message.Substring(startIndex, endIndex - startIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing RFID from message: {message}");
        }

        return string.Empty;
    }
    private string ParseMacFromMessage(string message)
    {
        try
        {
            var startIndex = message.IndexOf('<') + 1;
            var endIndex = message.IndexOf('>');

            if (startIndex > 0 && endIndex > startIndex)
            {
                return message.Substring(startIndex, endIndex - startIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing MAC from message: {message}");
        }

        return string.Empty;
    }
}