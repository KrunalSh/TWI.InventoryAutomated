using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.Security;

namespace TWI.InventoryAutomated.Controllers
{
    public class RegisteredDeviceController : Controller
    {
        // GET: RegisteredDevice
        //[CustomAuthorize(Roles = "Create Product2")]
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
                //Code to retrieve list of registered devices in the system.
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<RegisteredDevice> devicesList = db.RegisteredDevices.ToList<RegisteredDevice>();
                    return Json(new { data = devicesList }, JsonRequestBehavior.AllowGet);
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
                    return View(new RegisteredDevice());
                else
                {
                    //Linq query to retrieve device details by ID and populate respective fields
                    // in UI
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        return View(db.RegisteredDevices.Where(x => x.ID == id).FirstOrDefault<RegisteredDevice>());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(RegisteredDevice regDevice)
        {
            try
            {
                //Condition to check whether device mac address 
                // doesn't duplicate in the system.
                if (!isDuplicate(regDevice))
                {
                    //Updating "CreateDate" and "CreatedBy" details along with changes made through UI
                    //Saving data to database
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (regDevice.ID == 0)
                        {
                            //Code - while create a new device in the system.
                            regDevice.CreatedDate = DateTime.Now;
                            regDevice.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.RegisteredDevices.Add(regDevice);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //Code - while modifying details of a device
                            RegisteredDevice regdevice = db.RegisteredDevices.AsNoTracking().Where(x => x.ID == regDevice.ID).FirstOrDefault();
                            regDevice.CreatedDate = regdevice.CreatedDate;
                            regDevice.CreatedBy = regdevice.CreatedBy;
                            db.Entry(regDevice).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    return Json(new { success = false, message = Resources.GlobalResource.MsgMACAlreadyExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileAdding }, JsonRequestBehavior.AllowGet);
            }


        }


        public bool isDuplicate(RegisteredDevice regDevice)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                RegisteredDevice regdevice;
                //check to validate entered mac address is not duplicating
                if (regDevice.ID != 0)
                    regdevice = db.RegisteredDevices.AsNoTracking().Where(x => x.MacAddress == regDevice.MacAddress && x.ID != regDevice.ID).FirstOrDefault();
                else
                    regdevice = db.RegisteredDevices.AsNoTracking().Where(x => x.MacAddress == regDevice.MacAddress).FirstOrDefault();

                //code to return false if no duplicate record found
                if (regdevice == null)
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
                    // Disable a device in the system by setting "IsActive" field to false 
                    RegisteredDevice regDevice = db.RegisteredDevices.Where(x => x.ID == id).FirstOrDefault<RegisteredDevice>();
                    regDevice.IsActive = false;
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