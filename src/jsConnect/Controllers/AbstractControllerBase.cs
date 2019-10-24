using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace jsConnect.Controllers
{
    /// <summary>
    /// Base controller containing default shared functionality.
    /// </summary>
    public abstract class AbstractControllerBase<T> : Controller
    {
        protected IConfiguration Configuration { get; set; }
        protected ILogger<T> Logger { get; set; }
        protected ILoggerFactory LoggerFactory { get; set; }

        protected AbstractControllerBase(IConfiguration configuration, ILogger<T> logger, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            Logger = logger;
            LoggerFactory = loggerFactory;
        }
    }
}
