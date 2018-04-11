using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class UserAccessController : Controller
    {
        // GET: UserAccess
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult GetData(int UserId)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    var dataList = (from w in db.UserAccesses
                                    join x in db.Companies on w.CompanyID equals x.ID
                                    join y in db.Instances on w.InstanceID equals y.ID
                                    where w.UserID == UserId
                                    select new
                                    {
                                        w.ID,
                                        y.InstanceName,
                                        x.CompanyName,
                                        w.IsActive,
                                        w.UserID
                                    }).ToList();


                    return Json(new { data = dataList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        [HttpGet]

        public ActionResult Index(int UserID)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    User user = db.Users.Where(x => x.UserID == UserID).FirstOrDefault();
                    ViewBag.UserName = user.UserName == null ? user.EmailID : user.UserName;
                    ViewBag.UserID = user.UserID;
                    return View();
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
                InventoryPortalEntities db = new InventoryPortalEntities();
                ViewBag.Instances = db.Instances.Where(x => x.IsActive == true).ToList();
                ViewBag.Companies = db.Companies.Where(x => x.IsActive == true).ToList();
                ViewBag.Devices = (from r in db.RegisteredDevices where r.IsActive == true select new SelectListItem { Value = r.ID.ToString(), Text = r.DeviceName }).ToList();
                if (id == 0)
                {
                    return View(new UserAccess());
                }
                else
                {
                    ViewBag.selectedDevices = db.UserAccessDevices.Where(x => x.UserAccessID == id).Select(x => x.DeviceID).ToList();
                    return View(db.UserAccesses.Where(x => x.ID == id).FirstOrDefault<UserAccess>());
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(UserAccess useraccess, string selecteddevices)
        {
            try
            {
                List<int> selectedval = selecteddevices.Split(',').Select(int.Parse).ToList();

                if (!isDuplicate(useraccess))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {

                        if (useraccess.ID == 0)
                        {
                            useraccess.CreatedDate = DateTime.Now;
                            db.UserAccesses.Add(useraccess);
                            db.SaveChanges();
                            updateDevices(useraccess, selectedval, (bool)useraccess.IsActive);
                            return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            UserAccess useracc = db.UserAccesses.AsNoTracking().Where(x => x.UserID == useraccess.UserID).FirstOrDefault();
                            useraccess.CreatedDate = useracc.CreatedDate;
                            useraccess.ModifiedDate = DateTime.Now;
                            db.Entry(useraccess).State = EntityState.Modified;
                            db.SaveChanges();
                            updateDevices(useraccess, selectedval, (bool)useraccess.IsActive);
                            return Json(new { success = true, message = "Updated Successfully" }, JsonRequestBehavior.AllowGet);
                        }

                    }

                }

                else
                    return Json(new { success = false, message = "Record already exists!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to give access!" }, JsonRequestBehavior.AllowGet);
            }


        }

        private void updateDevices(UserAccess useraccess, List<int> selectedval, bool IsActive)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                List<UserAccessDevice> uls = db.UserAccessDevices.Where(x => x.UserAccessID == useraccess.ID).ToList();
                foreach (var item in uls)
                {
                    db.UserAccessDevices.Remove(item);
                }
                db.SaveChanges();
                foreach (var item in selectedval)
                {
                    UserAccessDevice uad = new UserAccessDevice();
                    uad.DeviceID = item;
                    uad.UserAccessID = useraccess.ID;
                    uad.IsActive = IsActive;
                    db.UserAccessDevices.Add(uad);
                }
                db.SaveChanges();
            }

        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    UserAccess acc = db.UserAccesses.Where(x => x.ID == id).FirstOrDefault<UserAccess>();
                    acc.IsActive = false;
                    db.SaveChanges();
                    DisableDevices(id);
                    return Json(new { success = true, message = "Deleted Successfully" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { success = false, message = "Unable to delete record!" }, JsonRequestBehavior.AllowGet);
            }
        }

        private void DisableDevices(int id)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                List<UserAccessDevice> acc = db.UserAccessDevices.Where(x => x.UserAccessID == id).ToList();
                foreach (var item in acc)
                {
                    item.IsActive = false;
                }
                db.SaveChanges();
            }
        }

        public string GetDevicesInformations(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<string> Devices = (from w in db.UserAccessDevices
                                            join x in db.RegisteredDevices on w.DeviceID equals x.ID
                                            where w.UserAccessID == id
                                            select x.DeviceName).ToList();
                    int totaldevices = (from r in db.RegisteredDevices where r.IsActive == true select r.ID).Count();
                    if (totaldevices == Devices.Count)
                        return "All";
                    else
                        return string.Join(",", Devices);
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public bool isDuplicate(UserAccess useracc)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                UserAccess UA;
                if (useracc.ID != 0)
                    UA = db.UserAccesses.AsNoTracking().Where(x => x.CompanyID == useracc.CompanyID && x.UserID == useracc.UserID && x.InstanceID == useracc.InstanceID && x.ID != useracc.ID).FirstOrDefault();
                else
                    UA = db.UserAccesses.AsNoTracking().Where(x => x.CompanyID == useracc.CompanyID && x.UserID == useracc.UserID && x.InstanceID == useracc.InstanceID).FirstOrDefault();
                if (UA == null)
                    return false;
                else
                    return true;
            }
        }
    }
}