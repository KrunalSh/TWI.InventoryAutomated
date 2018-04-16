using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
            return View();
        }
        public ActionResult GetData()
        {
            try
            {
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
                if (id == 0)
                    return View(new RegisteredDevice());
                else
                {
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
                if (!isDuplicate(regDevice))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (regDevice.ID == 0)
                        {
                            regDevice.CreatedDate = DateTime.Now;
                            db.RegisteredDevices.Add(regDevice);
                            db.SaveChanges();
                            return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            RegisteredDevice regdevice = db.RegisteredDevices.AsNoTracking().Where(x => x.ID == regDevice.ID).FirstOrDefault();
                            regDevice.CreatedDate = regdevice.CreatedDate;
                            db.Entry(regDevice).State = EntityState.Modified;
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
        public bool isDuplicate(RegisteredDevice regDevice)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                RegisteredDevice regdevice;
                if (regDevice.ID != 0)
                    regdevice = db.RegisteredDevices.AsNoTracking().Where(x => x.MacAddress == regDevice.MacAddress && x.ID != regDevice.ID).FirstOrDefault();
                else
                    regdevice = db.RegisteredDevices.AsNoTracking().Where(x => x.MacAddress == regDevice.MacAddress).FirstOrDefault();
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
                    RegisteredDevice regDevice = db.RegisteredDevices.Where(x => x.ID == id).FirstOrDefault<RegisteredDevice>();
                    regDevice.IsActive = false;
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