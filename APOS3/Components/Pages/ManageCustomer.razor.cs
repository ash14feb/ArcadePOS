// ManageCustomer.razor.cs
using APOS3.Models;
using APOS3.DataAccess;
using APOS3.DataAccess.Repos;
using APOS3.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace APOS3.Components.Pages
{
    public partial class ManageCustomer : ComponentBase
    {
        [Inject]
        private IBonusRepository BonusRepository { get; set; } = default!;

        [Inject]
        private ICustomerRepository CustomerRepository { get; set; } = default!;

        [Inject]
        private IBillingRepository BillingRepository { get; set; } = default!;

        [Inject]
        private IPaymentRepository PaymentRepository { get; set; } = default!; // Add this

        [Inject]
        private OrderStateService OrderStateService { get; set; } = default!;

        private int activeTab = 0;
        private string searchText = "";
        private string searchError = "";
        private bool isLoading = false;
        private EsCustomer? currentCustomer = null;
        private IEnumerable<EsBonus> bonuses = new List<EsBonus>();
        private IEnumerable<EsBilling> paymentHistory = new List<EsBilling>();
        private IEnumerable<EsPayment> customerPayments = new List<EsPayment>(); // Change to EsPayment
        private IEnumerable<EsBilling> gameHistory = new List<EsBilling>();
        private string? lastGamePlayed = null;

        // Computed properties for real-time calculations
        private int TotalBalance => (currentCustomer?.balance_main ?? 0) + (currentCustomer?.balance_bonus ?? 0);
        private int TicketsWon => currentCustomer?.total_token ?? 0;

        protected override async Task OnInitializedAsync()
        {
            await LoadBonuses();
        }

        private async Task LoadBonuses()
        {
            try
            {
                bonuses = await BonusRepository.GetBonusesOrderedByAmountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bonuses: {ex.Message}");
                bonuses = new List<EsBonus>();
            }
        }

        private async Task SearchCustomer()
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                searchError = "Please enter a phone number or RFID";
                return;
            }

            isLoading = true;
            searchError = "";
            currentCustomer = null;
            paymentHistory = new List<EsBilling>();
            customerPayments = new List<EsPayment>();
            gameHistory = new List<EsBilling>();
            lastGamePlayed = null;
            StateHasChanged();

            try
            {
                // Try searching by phone first
                currentCustomer = await CustomerRepository.GetByPhoneAsync(searchText.Trim());

                // If not found by phone, try by RFID
                if (currentCustomer == null)
                {
                    currentCustomer = await CustomerRepository.GetByRfidAsync(searchText.Trim());
                }

                if (currentCustomer == null)
                {
                    searchError = "Customer not found. Please check the phone number or RFID.";
                }
                else
                {
                    // Load payment and game history for the customer
                    await LoadCustomerHistory(currentCustomer.id);
                    await LoadCustomerPayments(currentCustomer.id); // Load from es_payment table
                    await LoadLastGamePlayed(currentCustomer.id);
                }
            }
            catch (Exception ex)
            {
                searchError = "Error searching for customer. Please try again.";
                Console.WriteLine($"Search error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadCustomerHistory(int customerId)
        {
            try
            {
                // Load payment history (all billings for this customer)
                paymentHistory = await BillingRepository.GetBillingsByCustomerAsync(customerId);

                // For game history, filter out package recharge records and only show actual game plays
                gameHistory = paymentHistory
                    .Where(b => !b.log_game.Contains("Package Recharge") &&
                                !b.log_game.Contains("Recharge") &&
                                !b.log_game.Contains("Package"))
                    .OrderByDescending(b => b.created_at);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading customer history: {ex.Message}");
                paymentHistory = new List<EsBilling>();
                gameHistory = new List<EsBilling>();
            }
        }

        private async Task LoadCustomerPayments(int customerId)
        {
            try
            {
                // Load payment history from es_payment table
                customerPayments = await PaymentRepository.GetPaymentsByCustomerAsync(customerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading customer payments: {ex.Message}");
                customerPayments = new List<EsPayment>();
            }
        }

        private async Task LoadLastGamePlayed(int customerId)
        {
            try
            {
                // Get the most recent game played that's not refunded and not a recharge
                var recentGame = paymentHistory
                    .Where(b => b.is_refund == "NO" &&
                               !b.log_game.Contains("Package Recharge") &&
                               !b.log_game.Contains("Recharge") &&
                               !b.log_game.Contains("Package"))
                    .OrderByDescending(b => b.created_at)
                    .FirstOrDefault();

                lastGamePlayed = recentGame?.log_game ?? "No games played";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading last game: {ex.Message}");
                lastGamePlayed = "Error loading game";
            }
        }

        private void HandleKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                SearchCustomer();
            }
        }

        private void SelectAmount(EsBonus bonus)
        {
            if (OrderStateService.SelectedBonus?.Amount == bonus.Amount)
            {
                OrderStateService.ClearSelection();
            }
            else
            {
                OrderStateService.SelectBonus(bonus);
            }
            StateHasChanged();
        }

        private string GetTabClass(int tabIndex)
        {
            return activeTab == tabIndex
                ? "border-primary text-primary"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300";
        }

        private void SwitchTab(int tabIndex)
        {
            activeTab = tabIndex;
            StateHasChanged();
        }

        private async Task ProcessRefund(EsBilling billing)
        {
            if (currentCustomer == null) return;

            try
            {
                // Calculate balance before refund
                int balanceBefore = currentCustomer.balance_main;
                int balanceAfter = balanceBefore + billing.amount;

                // Update the billing record to mark as refunded using the repository
                var refundSuccess = await BillingRepository.UpdateBillingAsRefundedAsync(
                    billing.id,
                    balanceBefore,
                    balanceAfter,
                    "ADMIN"
                );

                if (refundSuccess)
                {
                    // Add the refund amount to customer's balance_main
                    var newBalance = currentCustomer.balance_main + billing.amount;
                    var balanceUpdateSuccess = await CustomerRepository.UpdateCustomerBalanceAsync(currentCustomer.id, newBalance);

                    if (balanceUpdateSuccess)
                    {
                        // Refresh customer data and history
                        await RefreshCustomerData();

                        Console.WriteLine($"Successfully refunded Rs {billing.amount} for {billing.log_game}");
                        searchError = $"Successfully refunded Rs {billing.amount} for {billing.log_game}";
                    }
                    else
                    {
                        Console.WriteLine("Failed to update customer balance");
                        searchError = "Failed to update customer balance. Please try again.";
                    }
                }
                else
                {
                    Console.WriteLine("Failed to update billing record");
                    searchError = "Failed to process refund. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing refund: {ex.Message}");
                searchError = "An error occurred while processing refund. Please try again.";
            }

            StateHasChanged();
        }

        private async Task RefreshCustomerData()
        {
            if (currentCustomer == null) return;

            try
            {
                // Refresh customer data
                var updatedCustomer = await CustomerRepository.GetByRfidAsync(currentCustomer.rfid);
                if (updatedCustomer != null)
                {
                    currentCustomer = updatedCustomer;
                }

                // Refresh payment and game history
                await LoadCustomerHistory(currentCustomer.id);
                await LoadCustomerPayments(currentCustomer.id);
                await LoadLastGamePlayed(currentCustomer.id);

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing customer data: {ex.Message}");
            }
        }

        // Helper method to get row background color based on refund status
        private string GetGameRowClass(EsBilling billing)
        {
            return billing.is_refund == "YES" ? "bg-red-50" : "hover:bg-gray-50";
        }

        // Helper method to get status badge color
        private string GetStatusBadgeClass(EsBilling billing)
        {
            return billing.is_refund == "YES" ? "bg-red-100 text-red-800" : "bg-green-100 text-green-800";
        }

        // Helper method to get status text
        private string GetStatusText(EsBilling billing)
        {
            return billing.is_refund == "YES" ? "REFUNDED" : "COMPLETED";
        }

        // Helper method to format payment description
        private string GetPaymentDescription(EsPayment payment)
        {
            if (payment.recharge_amount > 0)
            {
                return $"Package Recharge - Rs {payment.recharge_amount}";
            }
            return "Payment";
        }

        // Helper method to get payment status badge class
        private string GetPaymentStatusBadgeClass(EsPayment payment)
        {
            return payment.is_deleted == "YES" ? "bg-red-100 text-red-800" : "bg-green-100 text-green-800";
        }

        // Helper method to get payment status text
        private string GetPaymentStatusText(EsPayment payment)
        {
            return payment.is_deleted == "YES" ? "DELETED" : "COMPLETED";
        }
    }
}