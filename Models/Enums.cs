namespace CarePlusPharmacy.Models
{
    public enum PurchaseOrderStatus { Pending, Received, Cancelled }
    public enum PrescriptionStatus { Pending, Fulfilled, Cancelled }
    public enum PaymentStatus { Unpaid, Paid, Refunded }

    // Statutory price discounts granted at the counter (RA 9257 Senior Citizens,
    // RA 10754 PWDs). Both apply a 20% discount on the VAT-exclusive amount.
    public enum SaleDiscountType { None, Senior, Pwd }
}
