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

        public RecaptchaService(HttpClient http, IConfiguration config, IWebHostEnvironment env)
        {
            _http = http;
            _config = config;
            _env = env;
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

            var secret = _config["Recaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
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
                    return false;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                return doc.RootElement.TryGetProperty("success", out var success)
                       && success.ValueKind == JsonValueKind.True;
            }
            catch
            {
                return false;
            }
        }
    }
}