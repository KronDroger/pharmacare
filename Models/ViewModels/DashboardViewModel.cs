namespace CarePlusPharmacy.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStockUnits { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }

        public decimal TotalRevenue { get; set; }
        public int TotalUnitsSold { get; set; }
        public decimal InventoryValue { get; set; }
        public int PendingPurchaseOrders { get; set; }

        public List<CategoryStockViewModel> StockByCategory { get; set; } = new();
        public List<TopMedicineViewModel> TopSellingMedicines { get; set; } = new();
        public List<Sale> RecentSales { get; set; } = new();
    }

    public class CategoryStockViewModel
    {
        public string Category { get; set; } = string.Empty;
        public int Stock { get; set; }
    }

    public class TopMedicineViewModel
    {
        public string MedicineName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
    }
}
