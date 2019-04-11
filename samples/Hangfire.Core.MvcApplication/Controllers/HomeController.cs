using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Hangfire.Core.MvcApplication.Models;
using Hangfire.Server;
using Hangfire.Tags.Attributes;

namespace Hangfire.Core.MvcApplication.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Buffer()
        {
            return Content(TextBuffer.ToString());
        }

        [HttpPost]
        public ActionResult Create()
        {
            BackgroundJob.Enqueue(() => Job("Home", null));
            TextBuffer.WriteLine("Background job has been created.");

            return RedirectToAction("Index");
        }

        [Tag("job", "{0}", "{1}")]
        public void Job(string name, PerformContext ctx)
        {
            TextBuffer.WriteLine("Background Job completed succesfully!");
        }
    }
}
