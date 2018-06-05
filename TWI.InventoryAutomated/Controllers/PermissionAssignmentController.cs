using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class PermissionAssignmentController : Controller
    {
        // GET: PermissionAssignment
        public ActionResult Index(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    ViewBag.ModuleForms = (from y in db.Forms
                                           join z in db.Modules on y.ModuleID equals z.ModuleID
                                           select new SelectListItem
                                           {
                                               Text = z.ModuleName + " - " + y.FormName,
                                               Value = y.ID.ToString()
                                           }).ToList();
                    ViewBag.selectedForms = db.UIPermissionAssignments.Where(x => x.PermissionID == id&& x.AllowAccess==true).Select(x=>x.FormID).ToList();
                    return View();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        
        [HttpPost]
        public ActionResult AssignPermissionsUI(List<int> Ids, int currentpermission)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<UIPermissionAssignment> perms = db.UIPermissionAssignments.Where(x => x.PermissionID == currentpermission).ToList();
                    foreach (var item in Ids)
                    {
                        UIPermissionAssignment Per=perms.Where(x => x.FormID == item).FirstOrDefault();
                        if (Per == null)
                        {
                            int moduleId=(int)db.Forms.Where(x => x.ID == item).Select(y=>y.ModuleID).FirstOrDefault();
                            UIPermissionAssignment ui = new UIPermissionAssignment();
                            ui.FormID = item;
                            ui.PermissionID = currentpermission;
                            ui.ModuleID = moduleId;
                            ui.AllowAccess = true;
                            db.UIPermissionAssignments.Add(ui);
                            db.SaveChanges();
                        }
                        else
                        {
                            Per.AllowAccess = true;
                            db.SaveChanges();
                        }
                    }
                    var rejectList = perms.Where(i => Ids.Contains((int)i.FormID));
                    var filteredList = perms.Except(rejectList);
                    foreach (var item in filteredList)
                    {
                        item.AllowAccess = false;
                    }
                    db.SaveChanges();
                    return Json(new { success = true, message= Resources.GlobalResource.MsgPermissionAssigned }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorWhileUpdate }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}