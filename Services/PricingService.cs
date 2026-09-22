using CarePlusPharmacy.Models;

namespace CarePlusPharmacy.Services
{
    // A single line of a sale with its VAT classification.
    public record PriceLine(decimal Gross, bool IsVatExempt);

    // Result of pricing a sale: the two VAT pools, VAT computed on the vatable pool,
    // and the statutory Senior/PWD discount (20% of the VAT-exclusive amount).
    public sealed class PriceBreakdown
    {
        public decimal VatableSales { get; init; }
        public decimal VatExemptSales { get; init; }
        public decimal VatAmount { get; init; }
        public decimal DiscountAmount { get; init; }
        public decimal GrossTotal { get; init; }
    }

    // Pure pricing engine — no I/O, deterministic, unit-test friendly.
    // Philippine VAT (RA 10963, 12% VAT): prices are VAT-inclusive, so the VAT
    // component is obtained by backing out 12/112 of the vatable gross. Medicines
    // flagged IsVatExempt are not charged VAT. Senior Citizen (RA 9994 / 9257) and
    // PWD (RA 10754) buyers receive a 20% discount computed on the VAT-exclusive
    // amount (vatable portion divided by 1.12, VAT-exempt portion already net).
    public sealed class PricingService
    {
        public decimal VatRate => 0.12m;
        public decimal StatutoryDiscountRate => 0.20m;

        public PriceBreakdown Compute(IEnumerable<PriceLine> lines, SaleDiscountType discountType)
        {
            decimal vatableSales = 0;
            decimal vatExemptSales = 0;

            foreach (var line in lines)
            {
                if (line.IsVatExempt)
                {
                    vatExemptSales += line.Gross;
                }
                else
                {
                    vatableSales += line.Gross;
                }
            }

            var vatAmount = Math.Round(vatableSales / (1 + VatRate) * VatRate, 2, MidpointRounding.AwayFromZero);

            decimal discount = 0;
            if (discountType == SaleDiscountType.Senior || discountType == SaleDiscountType.Pwd)
            {
                var vatableExVat = vatableSales / (1 + VatRate);
                discount = Math.Round(StatutoryDiscountRate * (vatableExVat + vatExemptSales), 2, MidpointRounding.AwayFromZero);
            }

            return new PriceBreakdown
            {
                VatableSales = Math.Round(vatableSales, 2, MidpointRounding.AwayFromZero),
                VatExemptSales = Math.Round(vatExemptSales, 2, MidpointRounding.AwayFromZero),
                VatAmount = vatAmount,
                DiscountAmount = discount,
                GrossTotal = Math.Round(vatableSales + vatExemptSales, 2, MidpointRounding.AwayFromZero)
            };
        }
    }
}