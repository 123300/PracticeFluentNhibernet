using Microsoft.AspNetCore.Mvc;
using NHibernate_PracticeMvc.Models;
using System.Diagnostics;

namespace NHibernate_PracticeMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost,ValidateAntiForgeryToken]
        public IActionResult Add(DeveloperCreateModel developerCreateModel)
        {
            DeveloperCreateModel model = new DeveloperCreateModel();
            model.AddDeveloper(developerCreateModel);
            return View(developerCreateModel);
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