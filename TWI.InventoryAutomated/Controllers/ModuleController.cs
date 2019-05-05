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
    public class ModuleController : Controller
    {
        // GET: Module
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
                //Code to retrieve list of Modules registered in the system.
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<Module> List = db.Modules.ToList<Module>();
                    return Json(new { data = List }, JsonRequestBehavior.AllowGet);
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
                    return View(new Module());
                else
                {
                    //Linq query to retrieve module details by ID and populate respective fields
                    // in UI
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        return View(db.Modules.Where(x => x.ModuleID == id).FirstOrDefault<Module>());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(Module mod)
        {
            try
            {
                //Condition to check whether module name 
                // doesn't duplicate in the system.
                if (!isDuplicate(mod))
                {
                    //Updating "CreateDate" and "CreatedBy" details along with changes made through UI
                    //Saving data to database

                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        //Code - while create a new module in the system.
                        if (mod.ModuleID == 0)
                        {
                            mod.CreatedDate = DateTime.Now;
                            mod.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Modules.Add(mod);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //Code - while modifying details of a module
                            Module module = db.Modules.AsNoTracking().Where(x => x.ModuleID == mod.ModuleID).FirstOrDefault();
                            module.CreatedDate = mod.CreatedDate;
                            module.CreatedBy = mod.CreatedBy;
                            db.Entry(mod).State = EntityState.Modified;
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
        public bool isDuplicate(Module mod)
        {
            //check to validate entered  module name is not duplicating
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                Module module;
                if (mod.ModuleID != 0)
                    module = db.Modules.AsNoTracking().Where(x => x.ModuleName == mod.ModuleName && x.ModuleID != mod.ModuleID).FirstOrDefault();
                else
                    module = db.Modules.AsNoTracking().Where(x => x.ModuleName == mod.ModuleName).FirstOrDefault();

                //code to return false if no duplicate record found
                if (module == null)
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
                // Disable a module in the system by setting "IsActive" field to false
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    Module mod = db.Modules.Where(x => x.ModuleID == id).FirstOrDefault<Module>();
                    mod.IsActive = false;
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