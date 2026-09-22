#nullable enable
using System.Security.Claims;
using CarePlusPharmacy.Models;

namespace CarePlusPharmacy.Services
{
    // Resolves "the logged-in customer" for the Customer portal.
    // Lookups are ALWAYS driven by the ApplicationUser.CustomerId link that is
    // set at registration / linking time — never by matching email addresses,
    // which would let one person sign into another patient's record.
    public interface ICurrentCustomerService
    {
        Task<Customer?> GetCurrentCustomerAsync(ClaimsPrincipal user);
    }
}