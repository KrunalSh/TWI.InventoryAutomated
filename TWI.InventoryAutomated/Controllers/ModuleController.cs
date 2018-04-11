using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class ModuleController : Controller
    {
        // GET: Module
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
                if (id == 0)
                    return View(new Module());
                else
                {
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
                if (!isDuplicate(mod))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (mod.ModuleID == 0)
                        {
                            mod.CreatedDate = DateTime.Now;
                            db.Modules.Add(mod);
                            db.SaveChanges();
                            return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            Module module = db.Modules.AsNoTracking().Where(x => x.ModuleID == mod.ModuleID).FirstOrDefault();
                            module.CreatedDate = mod.CreatedDate;
                            db.Entry(mod).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = "Updated Successfully" }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    return Json(new { success = false, message = "Module Name already exists!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to add Module information!" }, JsonRequestBehavior.AllowGet);
            }


        }
        public bool isDuplicate(Module mod)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                Module module;
                if (mod.ModuleID != 0)
                    module = db.Modules.AsNoTracking().Where(x => x.ModuleName == mod.ModuleName && x.ModuleID != mod.ModuleID).FirstOrDefault();
                else
                    module = db.Modules.AsNoTracking().Where(x => x.ModuleName == mod.ModuleName).FirstOrDefault();
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
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    Module mod = db.Modules.Where(x => x.ModuleID == id).FirstOrDefault<Module>();
                    mod.IsActive = false;
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