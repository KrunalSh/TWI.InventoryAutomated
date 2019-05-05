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
                //Code to retrieve list of registered instances in the system.
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
                //Code to load Popup screen based on ID. 
                //if ID = 0 then empty all fields in UI
                if (id == 0)
                    return View(new Instance());
                else
                {
                    //Linq query to retrieve instance details by ID and populate respective fields
                    // in UI
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
                //Condition to check whether instance name
                // doesn't duplicate in the system.
                if (!isDuplicate(inst)) // 
                {
                    //Updating "CreatedDate" and "CreatedBy" details along with changes made through UI
                    //Saving data to database
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (inst.ID == 0)
                        {
                            //Code - while create a new instance in the system.
                            inst.CreatedDate = DateTime.Now;
                            inst.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Instances.Add(inst);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //Code - while modifying details of a instance
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

        public bool isDuplicate(Instance instance)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                //check to validate entered instance name is not duplicating
                Instance inst;
                if (instance.ID != 0)
                    inst = db.Instances.AsNoTracking().Where(x => x.InstanceName == instance.InstanceName && x.ID != instance.ID).FirstOrDefault();
                else
                    inst = db.Instances.AsNoTracking().Where(x => x.InstanceName == instance.InstanceName).FirstOrDefault();

                //code to return false if no duplicate record found
                if (inst == null)
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
                    // Disable a instance in the system by setting "IsActive" field to false
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
       
    }
}