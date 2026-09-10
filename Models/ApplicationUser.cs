using Microsoft.AspNetCore.Identity;

namespace CarePlusPharmacy.Models
{
    // Extends Identity's built-in Users table.
    // Roles (RBAC) are managed separately via AspNetRoles / AspNetUserRoles:
    // Admin, Pharmacist, Cashier, InventoryCoordinator, Customer
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
