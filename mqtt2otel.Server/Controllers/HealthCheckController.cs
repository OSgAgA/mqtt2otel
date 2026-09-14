using Microsoft.AspNetCore.Mvc;

namespace mqtt2otel.Server.Controllers
{
    /// <summary>
    /// This controller provides health checks for the application.
    /// </summary>
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// A simple health check, that will return a textual representation of the current application health.
        /// </summary>
        /// <returns>A textual representation of the current application health.</returns>
        [HttpGet]
        public string CheckAvailability()
        {
            return "Applicatiion is healthy.";
        }
    }
}
