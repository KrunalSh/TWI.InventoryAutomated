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
            CommonServices cs = new CommonServices();
            if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
                return View();
            else
            {
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
                            mod.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Modules.Add(mod);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
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