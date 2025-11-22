using APOS3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APOS3.Services
{
    public class OrderStateService
    {
        public EsBonus? SelectedBonus { get; private set; }

        public event Action? OnChange;

        public void SelectBonus(EsBonus bonus)
        {
            // Single selection only - replace previous selection
            SelectedBonus = bonus;
            NotifyStateChanged();
        }

        public void ClearSelection()
        {
            SelectedBonus = null;
            NotifyStateChanged();
        }

        public decimal CalculateSubtotal()
        {
            return SelectedBonus?.Amount ?? 0;
        }

        public decimal CalculateTax(decimal taxRate = 0.18m)
        {
            // Tax is inclusive, so calculate tax amount from total
            var subtotal = CalculateSubtotal();
            return subtotal * taxRate;
        }

        public decimal CalculateTotal(decimal taxRate = 0.18m)
        {
            // Since tax is inclusive, total is just the subtotal
            return CalculateSubtotal();
        }

        public decimal CalculateBaseAmount(decimal taxRate = 0.18m)
        {
            // Calculate base amount before tax
            var total = CalculateSubtotal();
            return total / (1 + taxRate);
        }

        public decimal CalculateTaxAmount(decimal taxRate = 0.18m)
        {
            // Calculate actual tax amount from inclusive price
            var baseAmount = CalculateBaseAmount(taxRate);
            return baseAmount * taxRate;
        }

        public int CalculateTotalBonus()
        {
            return SelectedBonus?.Bonus_Amount ?? 0;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
