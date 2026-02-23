using Microsoft.AspNetCore.Mvc;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
