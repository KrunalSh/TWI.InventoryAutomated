using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class PermissionController : Controller
    {
        // GET: Permission
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
        public ActionResult GetData()
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    //Code to retrieve list of Permissions registered in the system.
                    List<Permission> dataList = db.Permissions.Where(x => x.PermissionDesc != "Super Admin").ToList<Permission>();
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
                //Code to load Popup screen based on ID. 
                //if ID = 0 then empty all fields in UI
                if (id == 0)
                    return View(new Permission());
                else
                {
                    //Linq query to retrieve permission details by ID and populate respective fields
                    // in UI
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
                //Condition to check whether permission description 
                // doesn't duplicate in the system.
                if (!isDuplicate(perm))
                {
                    //Updating "CreateDate" and "CreatedBy" details with changes made through UI
                    //Saving data to database
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        //Code - while creating a new permission in the system.
                        if (perm.ID == 0)
                        {
                            perm.CreatedDate = DateTime.Now;
                            perm.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Permissions.Add(perm);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //Code - while modifying details of a permission
                            Permission permission = db.Permissions.AsNoTracking().Where(x => x.ID == perm.ID).FirstOrDefault();
                            perm.CreatedDate = permission.CreatedDate;
                            perm.CreatedBy = permission.CreatedBy;
                            db.Entry(perm).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                else
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileAdding }, JsonRequestBehavior.AllowGet);
            }
        }

        public bool isDuplicate(Permission perm)
        {
            //check to validate entered permission description name is not duplicating
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                Permission permission;
                if (perm.ID != 0)
                    permission = db.Permissions.AsNoTracking().Where(x => x.PermissionDesc == perm.PermissionDesc && x.ID != perm.ID).FirstOrDefault();
                else
                    permission = db.Permissions.AsNoTracking().Where(x => x.PermissionDesc == perm.PermissionDesc).FirstOrDefault();

                //code to return false if no duplicate record found
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
                // Disable a permission in the system by setting "IsActive" field to false
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    Permission perm = db.Permissions.Where(x => x.ID == id).FirstOrDefault<Permission>();
                    perm.IsActive = false;
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyDisabled }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}