using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    public class FieldController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
