using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace jsConnect.Controllers
{
    /// <summary>
    /// Base controller containing default shared functionality.
    /// </summary>
    public abstract class AbstractControllerBase<T>(IConfiguration configuration, ILogger<T> logger, ILoggerFactory loggerFactory) : Controller
    {
        protected IConfiguration Configuration { get; set; } = configuration;
        protected ILogger<T> Logger { get; set; } = logger;
        protected ILoggerFactory LoggerFactory { get; set; } = loggerFactory;
    }
}
