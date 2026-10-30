using Microsoft.AspNetCore.Mvc;
using StajYonetimSistemi.Filters;

namespace StajYonetimSistemi.Controllers
{
    [SessionAuthorize("Student")]
    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
