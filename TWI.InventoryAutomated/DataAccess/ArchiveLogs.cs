using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.DataAccess
{
    public class ArchiveLogs
    {
        public static void SaveActivityLogs(string FormName, string ControlName, string ActivityPerformed, string Exception)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                ArchivedSessionLog sessionlogs = new ArchivedSessionLog();
                if (HttpContext.Current.Session["CurrentSession"] != null)
                    sessionlogs.SessionLogID = Convert.ToInt32(HttpContext.Current.Session["CurrentSession"]);
                sessionlogs.FormName = FormName;
                sessionlogs.ControlName = ControlName;
                sessionlogs.ActivityPerformed = ActivityPerformed;
                sessionlogs.CreatedDate = DateTime.Now;
                sessionlogs.Exception = Exception;
                db.ArchivedSessionLogs.Add(sessionlogs);
                db.SaveChanges();
            }
        }
    }
}