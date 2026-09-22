#nullable enable
using System.Security.Claims;
using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Services
{
    public class CurrentCustomerService : ICurrentCustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CurrentCustomerService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Customer?> GetCurrentCustomerAsync(ClaimsPrincipal user)
        {
            var appUser = await _userManager.GetUserAsync(user);
            if (appUser == null) return null;

            // The only sanctioned path: the ApplicationUser.CustomerId link.
            if (appUser.CustomerId.HasValue)
            {
                return await _context.Customers.FindAsync(appUser.CustomerId.Value);
            }

            return null;
        }
    }
}