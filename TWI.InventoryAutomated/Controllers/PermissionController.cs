using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class PermissionController : Controller
    {
        // GET: Permission
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult GetData()
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<Permission> dataList = db.Permissions.ToList<Permission>();
                    return Json(new { data = dataList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        [HttpGet]
        public ActionResult AddOrEdit(int id = 0)
        {
            try
            {
                if (id == 0)
                    return View(new Permission());
                else
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        return View(db.Permissions.Where(x => x.ID == id).FirstOrDefault<Permission>());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(Permission perm)
        {
            try
            {
                if (!isDuplicate(perm))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (perm.ID == 0)
                        {
                            perm.CreatedDate = DateTime.Now;
                            db.Permissions.Add(perm);
                            db.SaveChanges();
                            return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            Permission permission = db.Permissions.AsNoTracking().Where(x => x.ID == perm.ID).FirstOrDefault();
                            perm.CreatedDate = permission.CreatedDate;
                            db.Entry(perm).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = "Updated Successfully" }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    return Json(new { success = false, message = "MAC Address already exists!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to add Device information!" }, JsonRequestBehavior.AllowGet);
            }


        }
        public bool isDuplicate(Permission perm)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                Permission permission;
                if (perm.ID != 0)
                    permission = db.Permissions.AsNoTracking().Where(x => x.PermissionDesc == perm.PermissionDesc && x.ID != perm.ID).FirstOrDefault();
                else
                    permission = db.Permissions.AsNoTracking().Where(x => x.PermissionDesc == perm.PermissionDesc).FirstOrDefault();
                if (permission == null)
                    return false;
                else
                    return true;
            }
        }
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    Permission perm = db.Permissions.Where(x => x.ID == id).FirstOrDefault<Permission>();
                    perm.IsActive = false;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Disabled Successfully" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to disable the record!" }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}