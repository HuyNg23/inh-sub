using IBox.CiscoAdapter.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IBox.CiscoAdapter.Services
{
    public interface ITokenCacheService
    {
        void StoreToken(string key, TokenModel token);
        TokenModel? GetToken(string key);
        Task<TokenModel?> GetValidTokenAsync(string key);
        void RemoveToken(string key);
    }

    public class TokenCacheService : ITokenCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenCacheService> _logger;
        private const string DEFAULT_TOKEN_KEY = "webexcc_token";

        public TokenCacheService(
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TokenCacheService> logger)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public void StoreToken(string key, TokenModel token)
        {
            if (string.IsNullOrEmpty(key))
                key = DEFAULT_TOKEN_KEY;

            token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(token.ExpiresAt);

            _cache.Set(key, token, cacheOptions);
            _logger.LogInformation($"Token stored with key: {key}, expires at: {token.ExpiresAt}");
        }

        public TokenModel? GetToken(string key)
        {
            if (string.IsNullOrEmpty(key))
                key = DEFAULT_TOKEN_KEY;

            _cache.TryGetValue(key, out TokenModel? token);
            return token;
        }

        public async Task<TokenModel?> GetValidTokenAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                key = DEFAULT_TOKEN_KEY;

            var token = GetToken(key);

            if (token == null)
            {
                _logger.LogWarning($"No token found for key: {key}");
                return null;
            }

            // Check if token is still valid (with 5 minute buffer)
            if (token.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogInformation($"Token is still valid for key: {key}");
                return token;
            }

            // Token expired or about to expire, refresh it
            _logger.LogInformation($"Token expired or expiring soon for key: {key}, attempting refresh");
            var newToken = await RefreshTokenAsync(token.RefreshToken);

            if (newToken != null)
            {
                StoreToken(key, newToken);
                return newToken;
            }

            _logger.LogError($"Failed to refresh token for key: {key}");
            return null;
        }

        public void RemoveToken(string key)
        {
            if (string.IsNullOrEmpty(key))
                key = DEFAULT_TOKEN_KEY;

            _cache.Remove(key);
            _logger.LogInformation($"Token removed for key: {key}");
        }

        private async Task<TokenModel?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var webexTokenUrl = _configuration["WebexCC:TokenUrl"] ?? "https://webexapis.com/v1/access_token";
                var clientId = _configuration["WebexCC:ClientId"];
                var clientSecret = _configuration["WebexCC:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("WebexCC ClientId or ClientSecret not configured");
                    return null;
                }

                var client = _httpClientFactory.CreateClient();
                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("refresh_token", refreshToken)
                });

                var response = await client.PostAsync(webexTokenUrl, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenRefreshResponse>(content);

                    if (tokenResponse != null)
                    {
                        var newToken = new TokenModel
                        {
                            AccessToken = tokenResponse.access_token ?? string.Empty,
                            RefreshToken = tokenResponse.refresh_token ?? refreshToken,
                            ExpiresIn = tokenResponse.expires_in,
                            TokenType = tokenResponse.token_type ?? "Bearer"
                        };

                        _logger.LogInformation("Token refreshed successfully");
                        return newToken;
                    }
                }

                _logger.LogError($"Failed to refresh token. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        private class TokenRefreshResponse
        {
            public string? access_token { get; set; }
            public string? refresh_token { get; set; }
            public int expires_in { get; set; }
            public string? token_type { get; set; }
        }
    }
}
