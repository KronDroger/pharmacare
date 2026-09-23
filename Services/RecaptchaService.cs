using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace CarePlusPharmacy.Services
{
    // Verifies a Google reCAPTCHA v2 checkbox token against Google's siteverify
    // endpoint. Skipped entirely in Development so local testing/demos are not
    // blocked (same convention as the Quick Demo Logins).
    public sealed class RecaptchaService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<RecaptchaService> _logger;

        public RecaptchaService(HttpClient http, IConfiguration config, IWebHostEnvironment env, ILogger<RecaptchaService> logger)
        {
            _http = http;
            _config = config;
            _env = env;
            _logger = logger;
        }

        public async Task<bool> VerifyAsync(string? token)
        {
            if (_env.IsDevelopment())
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            // The secret is loaded from configuration and must be provided by the
            // operator. `dotnet user-secrets` is only wired up in Development, so
            // in Production the value must come from the environment variable
            // Recaptcha__SecretKey (or the platform's secret store). Fall back to
            // that environment variable directly when configuration is empty.
            var secret = _config["Recaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = Environment.GetEnvironmentVariable("Recaptcha__SecretKey");
            }
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning(
                    "Recaptcha:SecretKey is not configured for the current environment. " +
                    "Set the Recaptcha__SecretKey environment variable to enable reCAPTCHA verification.");
                return false;
            }

            var verifyUrl = _config["Recaptcha:VerifyUrl"]
                ?? "https://www.google.com/recaptcha/api/siteverify";

            try
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secret,
                    ["response"] = token
                });

                var response = await _http.PostAsync(verifyUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Google reCAPTCHA siteverify returned HTTP {Status}.", (int)response.StatusCode);
                    return false;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return doc.RootElement.TryGetProperty("success", out var success)
                       && success.ValueKind == JsonValueKind.True;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to verify Google reCAPTCHA token.");
                return false;
            }
        }
    }
}