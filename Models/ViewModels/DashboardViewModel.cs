using System;
using System.Collections.Generic;

namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Stats Cards
        public decimal TotalSales { get; set; }
        public decimal SalesChangePercent { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersTodayChange { get; set; }
        public int ActiveProducts { get; set; }
        public int TotalUsers { get; set; }
        public int UsersChange { get; set; }
        public int TotalOrders { get; set; }

        // Recent Orders
        public List<RecentOrderViewModel> RecentOrders { get; set; }
        
        // Low Stock
        public List<LowStockProductViewModel> LowStockProducts { get; set; }
        
        // Best Selling Products
        public List<BestSellingProductViewModel> BestSellingProducts { get; set; }
        
        // Cooperatives
        public List<CooperativeViewModel> Cooperatives { get; set; }
    }

    public class RecentOrderViewModel
    {
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class LowStockProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Stock { get; set; }
        public int AlertThreshold { get; set; }
    }

    public class BestSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int SalesCount { get; set; }
        public decimal Price { get; set; }
    }

    public class CooperativeViewModel
    {
        public int CooperativeId { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public bool IsActive { get; set; }
    }
}

