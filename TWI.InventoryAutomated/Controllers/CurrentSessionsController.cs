﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class CurrentSessionsController : Controller
    {
        // GET: CurrentSessions
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
        
        [HttpPost]
        public ActionResult GetData(bool isActive)
        {
            try
            {
                //&& a.ID != currentSession && x.PermissionDesc != "Super Admin"

                int currentSession = Session["CurrentSession"] != null ? Convert.ToInt32(Session["CurrentSession"].ToString()) : 0;

                //Code to retrieve list of Active Session of users in the system.
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    var List = (from a in db.UserSessionLogs
                                join b in db.UserAccesses on a.UserAccessID equals b.ID
                                join c in db.Users on b.UserID equals c.UserID
                                join x in db.Permissions on b.PermissionID equals x.ID
                                join e in db.RegisteredDevices on a.DeviceID equals e.ID
                                where a.IsActive == isActive 
                                orderby a.SessionStart descending
                                select new
                                {
                                    a.ID,
                                    c.UserID,
                                    c.DisplayName,
                                    a.SessionStart,
                                    e.MacAddress,
                                    b.InstanceName,
                                    b.CompanyName
                                }).ToList();
                    return Json(new { data = List }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpPost]
        public ActionResult RemoveSession(int id)
        {
            try
            {
                //Code to kill user session by Session ID
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    UserSessionLog session = db.UserSessionLogs.Where(x => x.ID == id).FirstOrDefault<UserSessionLog>();
                    session.IsActive = false;
                    session.SessionEnd = DateTime.Now;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Session successfully deactivated" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to disable the session!" }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}