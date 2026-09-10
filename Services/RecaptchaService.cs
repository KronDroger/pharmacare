#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarePlusPharmacy.Services
{
    public class RecaptchaService : IRecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecaptchaService> _logger;

        private class GoogleVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("challenge_ts")]
            public string? ChallengeTs { get; set; }

            [JsonPropertyName("hostname")]
            public string? Hostname { get; set; }

            [JsonPropertyName("error-codes")]
            public List<string>? ErrorCodes { get; set; }
        }

        public RecaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<RecaptchaService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RecaptchaResult> VerifyTokenAsync(string? token, string? remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new RecaptchaResult
                {
                    Success = false,
                    ErrorMessage = "Please complete the CAPTCHA confirmation to proceed."
                };
            }

            // If token was produced by the interactive confirmation challenge
            if (token.StartsWith("cp_verified_"))
            {
                if (ValidateChallengeToken(token))
                {
                    return new RecaptchaResult { Success = true };
                }
                return new RecaptchaResult
                {
                    Success = false,
                    ErrorMessage = "CAPTCHA confirmation has expired or is invalid. Please confirm the challenge again."
                };
            }

            var secretKey = _configuration["Recaptcha:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogWarning("reCAPTCHA SecretKey is not configured in appsettings or environment variables.");
                return new RecaptchaResult
                {
                    Success = false,
                    ErrorMessage = "reCAPTCHA server configuration error: Secret Key is missing."
                };
            }

            try
            {
                var postData = new Dictionary<string, string>
                {
                    { "secret", secretKey },
                    { "response", token }
                };

                if (!string.IsNullOrWhiteSpace(remoteIp))
                {
                    postData.Add("remoteip", remoteIp);
                }

                var verifyUrl = _configuration["Recaptcha:VerifyUrl"] ?? "https://www.google.com/recaptcha/api/siteverify";
                var response = await _httpClient.PostAsync(verifyUrl, new FormUrlEncodedContent(postData));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google reCAPTCHA API returned HTTP status {StatusCode}", response.StatusCode);
                    return new RecaptchaResult
                    {
                        Success = false,
                        ErrorMessage = "Unable to contact Google reCAPTCHA verification service. Please try again."
                    };
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GoogleVerifyResponse>(jsonString);

                if (result == null || !result.Success)
                {
                    var errors = result?.ErrorCodes ?? new List<string>();
                    string message = "reCAPTCHA verification failed. Please try again.";

                    if (errors.Contains("timeout-or-duplicate"))
                    {
                        message = "The reCAPTCHA verification token has expired or has already been used. Please solve the challenge again.";
                    }
                    else if (errors.Contains("invalid-input-response"))
                    {
                        message = "Invalid reCAPTCHA token received. Please complete the reCAPTCHA challenge again.";
                    }

                    _logger.LogWarning("reCAPTCHA verification failed with errors: {Errors}", string.Join(", ", errors));

                    return new RecaptchaResult
                    {
                        Success = false,
                        ErrorMessage = message,
                        ErrorCodes = errors
                    };
                }

                return new RecaptchaResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during reCAPTCHA verification");
                return new RecaptchaResult
                {
                    Success = false,
                    ErrorMessage = "A network error occurred while verifying reCAPTCHA. Please try again."
                };
            }
        }

        public string GenerateChallengeToken()
        {
            var secretKey = _configuration["Recaptcha:SecretKey"] ?? "CarePlus_Default_Secret_2026";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"careplus_captcha_{timestamp}"));
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant().Substring(0, 16);
            return $"cp_verified_{timestamp}_{signature}";
        }

        public bool ValidateChallengeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("cp_verified_")) return false;
            var parts = token.Split('_');
            if (parts.Length != 4) return false;
            if (!long.TryParse(parts[2], out long timestamp)) return false;

            var currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(currentTs - timestamp) > 300) // 5 minutes validity
            {
                return false;
            }

            var secretKey = _configuration["Recaptcha:SecretKey"] ?? "CarePlus_Default_Secret_2026";
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey));
            var expectedBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"careplus_captcha_{parts[2]}"));
            var expectedSignature = Convert.ToHexString(expectedBytes).ToLowerInvariant().Substring(0, 16);

            return parts[3].Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
    }
}
