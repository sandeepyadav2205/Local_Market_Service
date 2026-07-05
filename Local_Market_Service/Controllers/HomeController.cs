using Local_Market_Service.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Local_Market_Service.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
