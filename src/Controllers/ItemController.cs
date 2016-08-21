namespace todo.Controllers
{
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Models;
    using Microsoft.ApplicationInsights;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System;
    using System.Web;

    public class AddUserIdToTelemetryFilter : ActionFilterAttribute
    {
        private static TelemetryClient telemetry = new TelemetryClient();
        static ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();
        static Random rand = new Random();

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            telemetry.Context.User.Id = GetUsername(filterContext.RequestContext.HttpContext.Request);
            telemetry.TrackEvent(filterContext.ActionDescriptor.ControllerDescriptor.ControllerName + "." + filterContext.ActionDescriptor.ActionName + "-" + filterContext.RequestContext.HttpContext.Request.RequestType);
            base.OnActionExecuting(filterContext);
        }

        private string GetUsername(HttpRequestBase request)
        {
            if (_users.ContainsKey(request.UserHostAddress))
                return _users[request.UserHostAddress];

            List<string> names = new List<string> { "James Kirk", "Brunt", "Abe", "Chell", "Tom", "Jean-Luc Picard", "Christopher", "Quark", "Rom", "Q", "Wash", "Krall", "Spock", "Montgomery Scotty Scott", "Sulu", "Benjamin Sisko" };

            var name = names[rand.Next(0, names.Count - 1)];
            _users.AddOrUpdate(request.UserHostAddress, name, (key, value) => value);
            return name;
        }
    }
    [AddUserIdToTelemetryFilter]
    public class ItemController : Controller
    {
        private static TelemetryClient telemetry = new TelemetryClient();

        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            var items = await DocumentDBRepository<Item>.GetItemsAsync(d => !d.Completed);

            return View(items);
        }

#pragma warning disable 1998
        [ActionName("Create")]
        public async Task<ActionResult> CreateAsync()
        {
            return View();
        }
#pragma warning restore 1998

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind(Include = "Id,Name,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<Item>.CreateItemAsync(item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind(Include = "Id,Name,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<Item>.UpdateItemAsync(item.Id, item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Item item = await DocumentDBRepository<Item>.GetItemAsync(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            return View(item);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Item item = await DocumentDBRepository<Item>.GetItemAsync(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind(Include = "Id")] string id)
        {
            await DocumentDBRepository<Item>.DeleteItemAsync(id);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            Item item = await DocumentDBRepository<Item>.GetItemAsync(id);
            return View(item);
        }
    }
}