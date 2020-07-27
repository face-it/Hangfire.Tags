using System;
using System.Web.Mvc;
using Hangfire.Server;
using Hangfire.Tags.Attributes;

namespace Hangfire.MvcApplication.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View((object)TextBuffer.ToString());
        }

        public ActionResult Buffer()
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

        [HttpPost]
        public ActionResult Schedule()
        {
            BackgroundJob.Schedule(() => Job("Home", null), TimeSpan.FromMinutes(1));
            TextBuffer.WriteLine("Background job has been scheduled.");

            return RedirectToAction("Index");
        }

        [Tag("job", "{0}", "{1}")]
        public void Job(string name, PerformContext ctx)
        {
            TextBuffer.WriteLine("Background Job completed succesfully!");
        }
    }
}