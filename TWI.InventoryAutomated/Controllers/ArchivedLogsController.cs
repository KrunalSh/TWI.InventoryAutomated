using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.SqlServer;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class ArchivedLogsController : Controller
    {
        // GET: ArchivedLogs
        public ActionResult Index()
        {
            //Check to Validate user session to prevent unauthorized access to this web page
            CommonServices cs = new CommonServices();
            if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
                return View();
            else
            {
                //Clear all the session and redirect App to Login Screen
                cs.RemoveSessions();
                return RedirectToAction("Default", "Home");
            }
        }

        [HttpGet]
        public ActionResult Index(int SessionID)
        {
            //Code to get the Session header information.
            HeaderInfo HI = new HeaderInfo();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                HeaderInfo HeaderInfo = (from x in db.UserSessionLogs
                                         join y in db.UserAccesses on x.UserAccessID equals y.ID
                                         join z in db.RegisteredDevices on x.DeviceID equals z.ID
                                         join w in db.Users on y.UserID equals w.UserID
                                         where x.ID == SessionID
                                         select new HeaderInfo
                                         {
                                             IsActive = (bool)x.IsActive,
                                             UserName = w.UserName,
                                             InstanceName = y.InstanceName,
                                             CompanyName = y.CompanyName,
                                             DeviceName = z.DeviceName,
                                             SessionStart = (DateTime)x.SessionStart,
                                             SessionEnd = (DateTime)x.SessionEnd
                                         }).FirstOrDefault();

                return View(HeaderInfo);
            }
        }

        [HttpPost]
        public ActionResult GetData(int SessionID)
        {
            try
            {
                //Linq query to retrieve Activity Log of a user session by SessionID
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    var dataList = (from a in db.ArchivedSessionLogs
                                    where a.SessionLogID == SessionID
                                    select new
                                    {
                                        a.FormName,
                                        a.ControlName,
                                        a.ActivityPerformed,
                                        a.CreatedDate,
                                        a.Exception
                                    }).ToList();
                    return Json(new { data = dataList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
    public class HeaderInfo
    {
        public bool IsActive { get; set; }
        public string UserName { get; set; }
        public string InstanceName { get; set; }
        public string CompanyName { get; set; }
        public string DeviceName { get; set; }
        public DateTime? SessionStart { get; set; }
        public DateTime? SessionEnd { get; set; }
    }
}