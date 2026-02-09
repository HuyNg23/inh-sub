using IBox.CiscoAdapter.Models;
using IBox.CiscoAdapter.Services;
using Microsoft.AspNetCore.Mvc;

namespace IBox.CiscoAdapter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebexTokenController : ControllerBase
    {
        private readonly ITokenCacheService _tokenCacheService;
        private readonly ILogger<WebexTokenController> _logger;

        public WebexTokenController(ITokenCacheService tokenCacheService, ILogger<WebexTokenController> logger)
        {
            _tokenCacheService = tokenCacheService;
            _logger = logger;
        }

        /// <summary>
        /// Store access token and refresh token in memory cache
        /// </summary>
        /// <param name="request">Token request containing access token, refresh token and expiration time</param>
        /// <returns>Success message</returns>
        [HttpPost("store")]
        public IActionResult StoreToken([FromBody] TokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AccessToken) || string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new { message = "AccessToken and RefreshToken are required" });
                }

                if (request.ExpiresIn <= 0)
                {
                    return BadRequest(new { message = "ExpiresIn must be greater than 0" });
                }

                var token = new TokenModel
                {
                    AccessToken = request.AccessToken,
                    RefreshToken = request.RefreshToken,
                    ExpiresIn = request.ExpiresIn
                };

                _tokenCacheService.StoreToken("webexcc_token", token);

                _logger.LogInformation("Token stored successfully");

                return Ok(new
                {
                    message = "Token stored successfully",
                    expiresAt = DateTime.UtcNow.AddSeconds(request.ExpiresIn)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing token");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get valid access token. If expired, automatically refresh using refresh token
        /// </summary>
        /// <returns>Valid access token</returns>
        [HttpGet("access-token")]
        public async Task<IActionResult> GetAccessToken()
        {
            try
            {
                var token = await _tokenCacheService.GetValidTokenAsync("webexcc_token");

                if (token == null)
                {
                    return NotFound(new { message = "Token not found or refresh failed" });
                }

                var response = new TokenResponse
                {
                    AccessToken = token.AccessToken,
                    ExpiresAt = token.ExpiresAt,
                    IsValid = token.ExpiresAt > DateTime.UtcNow
                };

                _logger.LogInformation("Access token retrieved successfully");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access token");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get token info without refreshing
        /// </summary>
        /// <returns>Current token info</returns>
        [HttpGet("info")]
        public IActionResult GetTokenInfo()
        {
            try
            {
                var token = _tokenCacheService.GetToken("webexcc_token");

                if (token == null)
                {
                    return NotFound(new { message = "Token not found" });
                }

                return Ok(new
                {
                    expiresAt = token.ExpiresAt,
                    isValid = token.ExpiresAt > DateTime.UtcNow,
                    expiresInSeconds = (token.ExpiresAt - DateTime.UtcNow).TotalSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token info");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Remove token from cache
        /// </summary>
        /// <returns>Success message</returns>
        [HttpDelete("remove")]
        public IActionResult RemoveToken()
        {
            try
            {
                _tokenCacheService.RemoveToken("webexcc_token");
                _logger.LogInformation("Token removed successfully");
                return Ok(new { message = "Token removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing token");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}
