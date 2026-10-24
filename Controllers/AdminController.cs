using Microsoft.AspNetCore.Mvc;
using StajYonetimSistemi.Filters;

namespace StajYonetimSistemi.Controllers
{
    [SessionAuthorize("Admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return Ok("Admin erişimi başarılı.");
        }
    }
}
