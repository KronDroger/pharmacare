#nullable enable

namespace CarePlusPharmacy.Services
{
    public class RecaptchaResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string>? ErrorCodes { get; set; }
    }

    public interface IRecaptchaService
    {
        Task<RecaptchaResult> VerifyTokenAsync(string? token, string? remoteIp = null);
        string GenerateChallengeToken();
        bool ValidateChallengeToken(string token);
    }
}
