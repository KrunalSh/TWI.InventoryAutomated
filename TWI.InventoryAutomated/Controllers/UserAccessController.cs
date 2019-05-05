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
    public class UserAccessController : Controller
    {
        // GET: UserAccess

        public ActionResult UserAccessList(int UserID)
        {
            //Check to Validate user session to prevent unauthorized access to this web page
            CommonServices cs = new CommonServices();
            if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
            {
                try
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        //Code to retrieve the User name displayed to indicate 
                        //for which user logged in user is working on.
                        User user = db.Users.Where(x => x.UserID == UserID).FirstOrDefault();
                        ViewBag.UserName = user.UserName == null ? user.EmailID : user.UserName;
                        ViewBag.UserID = user.UserID;
                        //ViewBag.Devices = (from r in db.RegisteredDevices where r.IsActive == true 
                        //select new SelectListItem { Value = r.ID.ToString(), Text = r.DeviceName }).ToList();
                        return View();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                //Clear all the session and redirect App to Login Screen
                cs.RemoveSessions();
                return RedirectToAction("Default", "Home");
            }
        }

        [HttpPost]
        public ActionResult GetData(int UserId)
        {
            try
            {
                //Code to retrieve list of user accesses of a user in the system by passing UserID.
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    var dataList = (from w in db.UserAccesses
                                    join x in db.Companies on w.CompanyID equals x.ID
                                    join y in db.Instances on w.InstanceID equals y.ID
                                    join z in db.Permissions on w.PermissionID equals z.ID
                                    where w.UserID == UserId
                                    select new
                                    {
                                        w.ID,
                                        y.InstanceName,
                                        x.CompanyName,
                                        z.PermissionDesc,
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

        public ActionResult GetMasterData(int AccessID)
        {
            List<Instance> _instances = new List<Instance>(); List<Company> _companies = new List<Company>();
            List<Location> _locations = new List<Location>(); List<Permission> _permissions = new List<Permission>();
            List<RegisteredDevice> _regdevices = new List<RegisteredDevice>();List<UserAccess> _useracc = new List<UserAccess>();
            List<UserAccessDevice> _userdevice = new List<UserAccessDevice>();List<object> _masterdata = new List<object>();
            try
            {
                //Code to load User Access details in the Popup screen based on Access ID. 
                //Code to load master data required to display User Access field values
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.Instances.Where(x => x.IsActive == true).Count() > 0) { _instances = db.Instances.Where(x => x.IsActive == true).ToList(); }
                    Instance _ins = new Instance(); _ins.ID = -1; _ins.InstanceName = "Select Instance"; _instances.Insert(0, _ins);
                    _masterdata.Add(_instances);

                    Company _co = new Company(); _co.ID = -1; _co.CompanyName = "Select Company"; _companies.Insert(0, _co);
                    _masterdata.Add(_companies);

                    Location _loc = new Location(); _loc.ID = -1; _loc.Code = "Select Location"; _locations.Insert(0, _loc);
                    _masterdata.Add(_locations);

                    if (db.Permissions.Where(x => x.IsActive == true).Count() > 0)
                    { _permissions = db.Permissions.Where(x => x.IsActive == true).ToList(); }

                    Permission _per = new Permission(); _per.ID = -1; _per.PermissionDesc = "Select Permission"; _permissions.Insert(0, _per);
                    _masterdata.Add(_permissions);

                    if (db.RegisteredDevices.Where(x => x.IsActive == true).Count() > 0)
                    { _regdevices = db.RegisteredDevices.Where(x => x.IsActive == true).ToList(); }
                    _masterdata.Add(_regdevices);

                    if (AccessID > 0)
                    {
                        if (db.UserAccesses.Where(x => x.ID == AccessID).Count() > 0)
                        { _useracc = db.UserAccesses.Where(x => x.ID == AccessID).ToList(); }
                        if (db.UserAccessDevices.Where(x => x.UserAccessID == AccessID).Count() > 0)
                        { _userdevice = db.UserAccessDevices.Where(x => x.UserAccessID == AccessID).ToList();  }
                    }

                    _masterdata.Add(_useracc);
                    _masterdata.Add(_userdevice);

                    return Json(new { success= true, message = _masterdata }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, data = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult SaveUserAccessDetails(int UserID,int AccessID,int InstanceID, int CompanyID, int LocationID, int PermissionID,string DeviceID,bool isActive)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    UserAccess _useracc;
                    //Query to check whether access for the instance,company & location exists or not. if exists then, return below message.
                    //Updating "CreatedDate",Instance Name,Company Name,Location Code,"CreatedBy" details along with changes made through UI
                    if (AccessID == 0)
                    {
                        if (db.UserAccesses.Where(x => x.UserID == UserID && x.InstanceID == InstanceID && x.CompanyID == CompanyID 
                                                  && x.LocationID == LocationID).Count() > 0)
                            return Json(new { success = false, message = "Access for the selected Instance, Company & Location already exists," +
                                " cannot duplicate it." }, JsonRequestBehavior.AllowGet);


                        //Save User Access details in the database after successfull validation.
                        _useracc = new UserAccess();
                        _useracc.UserID = UserID;
                        _useracc.InstanceID = InstanceID; _useracc.CompanyID = CompanyID; _useracc.LocationID = LocationID; _useracc.PermissionID = PermissionID;
                        _useracc.IsActive = isActive;
                        _useracc.InstanceName = db.Instances.Where(x => x.ID == InstanceID).FirstOrDefault().InstanceName;
                        _useracc.CompanyName = db.Companies.Where(x => x.ID == CompanyID).FirstOrDefault().CompanyName;
                        _useracc.LocationCode = db.Location.Where(x => x.ID == LocationID).FirstOrDefault().Code;
                        _useracc.CreatedBy = Convert.ToInt32(Session["UserID"]); _useracc.CreatedDate = DateTime.Now;
                        db.UserAccesses.Add(_useracc);
                    }
                    else
                    {
                        if (db.UserAccesses.Where(x => x.UserID == UserID && x.InstanceID == InstanceID && x.CompanyID == CompanyID 
                        && x.LocationID == LocationID && x.ID != AccessID).Count() > 0)
                            return Json(new { success = false, message = "Access for the selected Instance, Company & Location already" +
                                " exists, cannot duplicate it." }, JsonRequestBehavior.AllowGet);

                        _useracc = db.UserAccesses.Where(x => x.ID == AccessID).FirstOrDefault();
                        _useracc.PermissionID = PermissionID;
                        _useracc.ModifiedBy = Convert.ToInt32(Session["UserID"]); _useracc.ModifiedDate = DateTime.Now;
                        _useracc.IsActive = isActive;

                        db.Entry(_useracc).State = EntityState.Modified;
                    }

                    db.SaveChanges();

                    //Get the User Access ID if a new entry is being created
                    if (AccessID == 0)
                    {   AccessID = _useracc.ID; }


                    //Remove existing Devices if found any for a particular User Access ID
                    if (db.UserAccessDevices.Where(x => x.UserAccessID == AccessID).Count() > 0)
                        db.UserAccessDevices.RemoveRange(db.UserAccessDevices.Where(x => x.UserAccessID == AccessID));

                    //Get the Device ID's from the Parameter received.
                    string[] _deviceids = DeviceID.Substring(0, DeviceID.Length - 1).Split(',');

                    

                    //Creating  the Devices entries for the new devices selected
                    for (int i = 0; i <= _deviceids.Length - 1; i++)
                    {
                        UserAccessDevice _deviceaccess = new UserAccessDevice();
                        _deviceaccess.UserAccessID = AccessID; _deviceaccess.DeviceID = Convert.ToInt32(_deviceids[i]);
                        _deviceaccess.IsActive = true;
                        db.UserAccessDevices.Add(_deviceaccess);
                    }
                    db.SaveChanges();
                }
                return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public string GetDevicesInformations(int id)
        {
            try
            {
                //Query to get device name for a specific user access by passing User Access ID as parameter
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<RegisteredDevice> Devices = (from w in db.UserAccessDevices
                                                      join x in db.RegisteredDevices on w.DeviceID equals x.ID
                                                      where w.UserAccessID == id
                                                      select x).ToList();
                    int totaldevices = (from r in db.RegisteredDevices where r.IsActive == true select r.ID).Count();
                    if (totaldevices == Devices.Count)
                        return "All";
                    else
                    {
                        string devices = "";
                        foreach (var item in Devices)
                        {
                            devices += item.DeviceName + " (" + item.MacAddress + " ),";
                        }
                        return devices.TrimEnd(',');
                    }

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
                //check to validate whether user access is not duplicating for the same user
                UserAccess UA;
                if (useracc.ID != 0)
                    UA = db.UserAccesses.AsNoTracking().Where(x => x.CompanyID == useracc.CompanyID && x.UserID == useracc.UserID && x.InstanceID == useracc.InstanceID && x.ID != useracc.ID).FirstOrDefault();
                else
                    UA = db.UserAccesses.AsNoTracking().Where(x => x.CompanyID == useracc.CompanyID && x.UserID == useracc.UserID && x.InstanceID == useracc.InstanceID).FirstOrDefault();

                //code to return false if no duplicate record found
                if (UA == null)
                    return false;
                else
                    return true;
            }
        }

        public ActionResult GetCompany(int InstanceID)
        {
            //Code to get list of companies for a instance
            InventoryPortalEntities db = new InventoryPortalEntities();
            List<Company> _companies = new List<Company>();

            if (db.Companies.Where(x => x.InstanceID == InstanceID && x.IsActive == true).Count() > 0)
            { _companies = db.Companies.Where(x => x.InstanceID == InstanceID && x.IsActive == true).ToList(); }
            Company _co = new Company(); _co.ID = -1; _co.CompanyName = "Select Company"; _companies.Insert(0, _co);

            return Json(new { success = true, message = _companies }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetLocation(int CompanyID)
        {
            //Code to get list of locations for a company
            InventoryPortalEntities db = new InventoryPortalEntities();
            List<Location> _location = new List<Location>();

            if (db.Location.Where(x => x.CompanyID == CompanyID && x.IsActive == true).Count() > 0)
            { _location = db.Location.Where(x => x.CompanyID == CompanyID && x.IsActive == true).ToList(); }
            Location _loc = new Location(); _loc.ID = -1; _loc.Code = "Select Location"; _location.Insert(0, _loc);

            return Json(new { success = true, message = _location }, JsonRequestBehavior.AllowGet);
        }

        #region "Old Code"

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


        [HttpGet]
        public ActionResult AddOrEdit(int id = 0)
        {
            try
            {
                InventoryPortalEntities db = new InventoryPortalEntities();
                ViewBag.Instances = db.Instances.Where(x => x.IsActive == true).ToList();
                ViewBag.Companies = db.Companies.Where(x => x.IsActive == true).ToList();
                ViewBag.Locations = db.Location.Where(x => x.IsActive == true).ToList();
                ViewBag.Permissions = db.Permissions.Where(x => x.IsActive == true).ToList();
                ViewBag.Devices = (from r in db.RegisteredDevices where r.IsActive == true select new SelectListItem { Value = r.ID.ToString(), Text = r.DeviceName }).ToList();
                if (id == 0)
                {
                    return View(new UserAccess());
                }
                else
                {
                    ViewBag.selectedDevices = db.UserAccessDevices.Where(x => x.UserAccessID == id).Select(x => x.DeviceID).ToList();
                    UserAccess useracc = db.UserAccesses.Where(x => x.ID == id).FirstOrDefault<UserAccess>();
                    if (db.Permissions.Where(x => x.ID == useracc.PermissionID && x.PermissionDesc == "Super Admin").FirstOrDefault() != null)
                        ViewBag.IsSuperUser = true;
                    else
                        ViewBag.IsSuperUser = null;
                    return View(useracc);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //Comment - Original Code of Inaam
        //[HttpGet]
        //public ActionResult AddOrEdit(int id = 0)
        //{
        //    try
        //    {
        //        InventoryPortalEntities db = new InventoryPortalEntities();
        //        ViewBag.Instances = db.Instances.Where(x => x.IsActive == true).ToList();
        //        ViewBag.Companies = db.Companies.Where(x => x.IsActive == true).ToList();
        //        ViewBag.Locations = db.Location.Where(x => x.IsActive == true).ToList();
        //        ViewBag.Permissions = db.Permissions.Where(x => x.IsActive == true).ToList();
        //        ViewBag.Devices = (from r in db.RegisteredDevices where r.IsActive == true select new SelectListItem { Value = r.ID.ToString(), Text = r.DeviceName }).ToList();
        //        if (id == 0)
        //        {
        //            return View(new UserAccess());
        //        }
        //        else
        //        {
        //            ViewBag.selectedDevices = db.UserAccessDevices.Where(x => x.UserAccessID == id).Select(x => x.DeviceID).ToList();
        //            UserAccess useracc = db.UserAccesses.Where(x => x.ID == id).FirstOrDefault<UserAccess>();
        //            if (db.Permissions.Where(x => x.ID == useracc.PermissionID && x.PermissionDesc == "Super Admin").FirstOrDefault() != null)
        //                ViewBag.IsSuperUser = true;
        //            else
        //                ViewBag.IsSuperUser = null;
        //            return View(useracc);
        //        }
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}

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
                            useraccess.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.UserAccesses.Add(useraccess);
                            db.SaveChanges();
                            updateDevices(useraccess, selectedval, (bool)useraccess.IsActive);
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            UserAccess useracc = db.UserAccesses.Where(x => x.ID == useraccess.ID).FirstOrDefault();
                            useracc.CreatedDate = useracc.CreatedDate;
                            useracc.CreatedBy = useracc.CreatedBy;
                            useracc.ModifiedDate = DateTime.Now;
                            useracc.PermissionID = useraccess.PermissionID != null ? useraccess.PermissionID : useracc.PermissionID;
                            useracc.ModifiedBy = Convert.ToInt32(Session["UserID"].ToString());
                            useracc.IsActive = useraccess.IsActive;
                            db.Entry(useracc).State = EntityState.Modified;
                            db.SaveChanges();
                            updateDevices(useracc, selectedval, (bool)useraccess.IsActive);
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
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyDisabled }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
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

        [HttpPost]
        public ActionResult GetCompanies(int intInstID)
        {
            InventoryPortalEntities db = new InventoryPortalEntities();
            var Companies = (from b in db.Companies
                             where b.InstanceID == intInstID
                             select new
                             {
                                 b.ID,
                                 b.CompanyName
                             }).ToList();
            return Json(new { success = true, message = Companies }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GetLocations(int CompanyID)
        {
            InventoryPortalEntities db = new InventoryPortalEntities();
            var Locations = (from b in db.Location
                             where b.CompanyID == CompanyID
                             select new
                             {
                                 b.ID,
                                 b.Code
                             }).ToList();
            return Json(new { success = true, message = Locations }, JsonRequestBehavior.AllowGet);
        }

        #endregion 
    }
}