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
    public class InstanceController : Controller
    {
        // GET: NavInstances
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
                    List<Instance> InstancesList = db.Instances.ToList<Instance>();
                    return Json(new { data = InstancesList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
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
                    return View(new Instance());
                else
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        return View(db.Instances.Where(x => x.ID == id).FirstOrDefault<Instance>());
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(Instance inst)
        {
            try
            {
                if (!isDuplicate(inst))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {

                        if (inst.ID == 0)
                        {
                            inst.CreatedDate = DateTime.Now;
                            inst.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Instances.Add(inst);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            Instance instance = db.Instances.AsNoTracking().Where(x => x.ID == inst.ID).FirstOrDefault();
                            inst.CreatedDate = instance.CreatedDate;
                            inst.CreatedBy = instance.CreatedBy;
                            db.Entry(inst).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileAdding }, JsonRequestBehavior.AllowGet);
            }
            


        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    Instance inst = db.Instances.Where(x => x.ID == id).FirstOrDefault<Instance>();
                    inst.IsActive = false;
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyDisabled }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
            }
        }
        public bool isDuplicate(Instance instance)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                Instance inst;
                if (instance.ID != 0)
                    inst = db.Instances.AsNoTracking().Where(x => x.InstanceName == instance.InstanceName && x.ID != instance.ID).FirstOrDefault();
                else
                    inst = db.Instances.AsNoTracking().Where(x => x.InstanceName == instance.InstanceName).FirstOrDefault();
                if (inst == null)
                    return false;
                else
                    return true;
            }
        }
    }
}