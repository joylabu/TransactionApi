using System;

namespace TransactionApi.Function
{
    public static class DiscountCalculator
    {
        /// <summary>
        /// Calculates the total discount and final amount based on the given total amount.
        /// </summary>
        /// <param name="totalAmount">The total amount of the transaction.</param>
        /// <returns>A tuple containing (totalDiscount, finalAmount).</returns>
        public static (long totalDiscount, long finalAmount) CalculateDiscount(long totalAmount)
        {
            // Calculate Base Discount
            double baseDiscount = 0;
            if (totalAmount > 80100 && totalAmount <= 120000)
            {
                baseDiscount = 0.10; // 10% discount
            }
            else if (totalAmount > 120000)
            {
                baseDiscount = 0.15; // 15% discount
            }

            //// Calculate Conditional Discount
            //double conditionalDiscount = 0;

            //if (baseDiscount % 10 == 5 && totalAmount > 900) 
            //{
            //    conditionalDiscount = 0.10; // Additional 10% discount
            //}
            string baseDiscountStr = baseDiscount.ToString();
            double conditionalDiscount = 0;
            if (baseDiscountStr.EndsWith("5") && totalAmount > 900)
            {
                conditionalDiscount = 0.10; // Additional 10% discount
            }

            // Calculate Total Discount Rate
            double totalDiscountRate = baseDiscount + conditionalDiscount;

            // Apply Cap on Maximum Discount (If greater than 20%, force it to 20%)
            if (totalDiscountRate > 0.20)
            {
                totalDiscountRate = 0.20;
            }

            // Calculate Final Discount & Amount
            long totalDiscount = (long)(totalAmount * totalDiscountRate);
            long finalAmount = totalAmount - totalDiscount;

            return (totalDiscount, finalAmount);
        }
    }
}
