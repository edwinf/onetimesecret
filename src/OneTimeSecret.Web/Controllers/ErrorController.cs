using Microsoft.AspNetCore.Mvc;

namespace OneTimeSecret.Web.Controllers
{
    public class ErrorController : Controller
    {
        [Route("error/404")]
        public IActionResult RouteNotFound()
        {
            return this.View();
        }

        [Route("error/500")]
        public IActionResult InternalError()
        {
            return this.View();
        }
    }
}