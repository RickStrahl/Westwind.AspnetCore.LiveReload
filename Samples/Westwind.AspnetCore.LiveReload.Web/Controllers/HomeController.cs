using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Westwind.AspNetCore.LiveReload.Web.Models;

namespace Westwind.AspNetCore.LiveReload.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Message = "Surfin' and Turfin'";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
