using System.Web.Mvc;

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
            BackgroundJob.Enqueue(() => TextBuffer.WriteLine("Background Job completed successfully!"));
            TextBuffer.WriteLine("Background job has been created.");

            return RedirectToAction("Index");
        }
    }
}