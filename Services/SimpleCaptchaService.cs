using System.Security.Cryptography;

namespace CarePlusPharmacy.Services
{
    // A lightweight, self-contained CAPTCHA: no external API, no key, no network call.
    // The question is generated per page-load, the answer is signed into a token that
    // round-trips through a hidden form field, and verified server-side on submit -
    // this is real bot-friction (unlike the old client-only checkbox), just without
    // depending on Google's service being reachable.
    public interface ISimpleCaptchaService
    {
        (string Question, string Token) Generate();
        bool Verify(string? token, string? answer);
    }

    public class SimpleCaptchaService : ISimpleCaptchaService
    {
        // Demo-project secret: fine for a course project token-signing purpose.
        // For a real deployment this would come from configuration/user-secrets instead.
        private const string Pepper = "CarePlusPharmacy.Captcha.v1";
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

        public (string Question, string Token) Generate()
        {
            var a = RandomNumberGenerator.GetInt32(2, 10);
            var b = RandomNumberGenerator.GetInt32(2, 10);
            var answer = a + b;
            var expires = DateTimeOffset.UtcNow.Add(Ttl).ToUnixTimeSeconds();
            var payload = $"{answer}|{expires}";
            var sig = Sign(payload);
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{payload}|{sig}"));
            return ($"What is {a} + {b}?", token);
        }

        public bool Verify(string? token, string? answer)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(answer)) return false;
            if (!int.TryParse(answer.Trim(), out var given)) return false;

            string decoded;
            try { decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token)); }
            catch { return false; }

            var parts = decoded.Split('|');
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], out var expected)) return false;
            if (!long.TryParse(parts[1], out var expiresUnix)) return false;

            var payload = $"{parts[0]}|{parts[1]}";
            if (!CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(Sign(payload)),
                    System.Text.Encoding.UTF8.GetBytes(parts[2])))
                return false;

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnix) return false; // expired - ask again

            return given == expected;
        }

        private static string Sign(string payload)
        {
            using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(Pepper));
            return Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));
        }
    }
}
