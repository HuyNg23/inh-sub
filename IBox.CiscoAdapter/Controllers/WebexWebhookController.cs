using IBox.CiscoAdapter.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IBox.CiscoAdapter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebexWebhookController : ControllerBase
    {
        private readonly ILogger<WebexWebhookController> _logger;

        public WebexWebhookController(ILogger<WebexWebhookController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Webhook endpoint to receive call events from Webex Contact Center
        /// </summary>
        /// <param name="eventData">Event data from Webex CC</param>
        /// <returns>Success response</returns>
        [HttpPost("call-event")]
        public IActionResult ReceiveCallEvent([FromBody] WebexEventModel eventData)
        {
            try
            {
                if (eventData == null)
                {
                    _logger.LogWarning("Received empty event data");
                    return BadRequest(new { message = "Event data is required" });
                }

                _logger.LogInformation($"Received event: {eventData.Event} for resource: {eventData.Resource}");
                _logger.LogInformation($"Event ID: {eventData.Id}, Name: {eventData.Name}, Created: {eventData.Created}");

                if (eventData.Data != null)
                {
                    var dataJson = JsonSerializer.Serialize(eventData.Data);
                    _logger.LogInformation($"Event Data: {dataJson}");
                }

                // Process the event based on event type
                ProcessCallEvent(eventData);

                return Ok(new
                {
                    message = "Event received successfully",
                    eventId = eventData.Id,
                    processedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing call event");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Webhook endpoint for generic Webex events
        /// </summary>
        /// <param name="eventData">Generic event data</param>
        /// <returns>Success response</returns>
        [HttpPost("event")]
        public IActionResult ReceiveEvent([FromBody] WebexEventModel eventData)
        {
            try
            {
                if (eventData == null)
                {
                    _logger.LogWarning("Received empty event data");
                    return BadRequest(new { message = "Event data is required" });
                }

                _logger.LogInformation($"Received generic event: {eventData.Event} for resource: {eventData.Resource}");
                _logger.LogInformation($"Event details - ID: {eventData.Id}, Name: {eventData.Name}, OrgId: {eventData.OrgId}");

                if (eventData.Data != null)
                {
                    var dataJson = JsonSerializer.Serialize(eventData.Data);
                    _logger.LogInformation($"Event Data: {dataJson}");
                }

                return Ok(new
                {
                    message = "Event received successfully",
                    eventId = eventData.Id,
                    processedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Health check endpoint for webhook verification
        /// </summary>
        /// <returns>Success response</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                service = "IBox.CiscoAdapter Webhook",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Verification endpoint for webhook setup
        /// </summary>
        /// <returns>Challenge response</returns>
        [HttpGet("verify")]
        public IActionResult Verify([FromQuery] string challenge)
        {
            _logger.LogInformation($"Webhook verification requested with challenge: {challenge}");
            
            if (string.IsNullOrEmpty(challenge))
            {
                return BadRequest(new { message = "Challenge parameter is required" });
            }

            return Ok(new { challenge });
        }

        private void ProcessCallEvent(WebexEventModel eventData)
        {
            // Process based on event type
            switch (eventData.Event?.ToLower())
            {
                case "created":
                    _logger.LogInformation($"Processing call created event for {eventData.Resource}");
                    // Add logic for call created
                    break;

                case "updated":
                    _logger.LogInformation($"Processing call updated event for {eventData.Resource}");
                    // Add logic for call updated
                    break;

                case "deleted":
                    _logger.LogInformation($"Processing call deleted event for {eventData.Resource}");
                    // Add logic for call ended/deleted
                    break;

                default:
                    _logger.LogInformation($"Processing generic event: {eventData.Event}");
                    break;
            }

            // Extract call specific data if available
            if (eventData.Data != null)
            {
                try
                {
                    // Process call data
                    if (eventData.Data.TryGetValue("id", out var callId))
                    {
                        _logger.LogInformation($"Call ID: {callId}");
                    }

                    if (eventData.Data.TryGetValue("status", out var status))
                    {
                        _logger.LogInformation($"Call Status: {status}");
                    }

                    // Add more data extraction as needed based on Webex CC event structure
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting call data");
                }
            }
        }
    }
}
