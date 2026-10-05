using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraTrack.Controllers
{
    /// <summary>
    /// Displays public API documentation and quick-reference guides linking to Swagger UI.
    /// </summary>
    [AllowAnonymous]
    public class ApiDocsController : Controller
    {
        private readonly IConfiguration _configuration;

        public ApiDocsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: /ApiDocs
        public IActionResult Index()
        {
            ViewBag.ApiKey = _configuration["ApiKey"] ?? "libra-track-secret-api-key-2026";
            return View();
        }
    }
}
