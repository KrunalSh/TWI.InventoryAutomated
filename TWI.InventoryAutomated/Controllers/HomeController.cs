using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.Security;

namespace TWI.InventoryAutomated.Controllers
{
    public class HomeController : Controller
    {
        #region "Global Variables"

        #endregion

        #region "Action Methods"
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Home()
        {
            return View();
        }
        public ActionResult SubMenu()
        {
            return View();
        }
        public ActionResult Default()
        {
            return View();
        }
        public ActionResult AccessDenied()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(User user)
        {
            InventoryPortalEntities db = new InventoryPortalEntities();
            User _user = db.Users.Where(x => x.UserName.Equals(user.UserName) && x.Password.Equals(user.Password)).FirstOrDefault();
            if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password) || _user == null)
            {
                return Json(new { success = false, message = "Invalid Login Information" }, JsonRequestBehavior.AllowGet);
            }
            SessionPersister.UserName = _user.UserName;
            SessionPersister.UserID = _user.UserID;
            List<UserAccess> uaccess = db.UserAccesses.Where(x => x.UserID == _user.UserID).ToList();
            if (uaccess.Count > 1)
            {
                return Json(new { success = true, message = Url.Action("InstanceAuthentication", "Home") }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = true, message = Url.Action("Home", "Home") }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult Home(int? InstId,int? CompId,string InstName,string compName)
        {
            if (InstId != null && CompId != null)
            {
                Session["InstanceName"] = InstName;
                Session["CompanyName"] = compName;
            }
            return Json(new { success = true, message = Url.Action("Home", "Home") }, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult AuthenticateDevice()
        {
            string userIpAddress = this.Request.UserHostAddress;
            System.Net.IPAddress ipaddress = System.Net.IPAddress.Parse(userIpAddress);
            NetworkUtils nu = new NetworkUtils();
            string MacAddress = nu.GetMacAddress(ipaddress);
            if (IsDeviceRegistered(MacAddress))
                return PartialView("Index");
            else
                return PartialView("Unauthorized");

        }

        private bool IsDeviceRegistered(string macAddress)
        {
            return true;
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.RegisteredDevices.Where(x => x.IsActive == true && x.MacAddress == macAddress).FirstOrDefault() != null)
                    return true;
                else
                    return false;
            }
        }

        public ActionResult InstanceAuthentication()
        {
            InventoryPortalEntities db = new InventoryPortalEntities();
            ViewBag.Instances = (from a in db.UserAccesses
                                 join b in db.Instances on a.InstanceID equals b.ID
                                 where a.UserID == SessionPersister.UserID
                                 select new
                                 {
                                     b.ID,
                                     b.InstanceName
                                 }).ToList();
            return View("InstanceAuthentication");
        }

        public ActionResult GetCompanies(int intInstID)
        {
            InventoryPortalEntities db = new InventoryPortalEntities();
            var Companies = (from a in db.UserAccesses
                             join b in db.Companies on a.CompanyID equals b.ID
                             where a.UserID == SessionPersister.UserID && a.InstanceID == intInstID
                             select new
                             {
                                 b.ID,
                                 b.CompanyName
                             }).ToList();
            return Json(Companies, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region "Helper Function(s) "
        public class NetworkUtils
        {
            [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
            static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);

            /// <summary>
            /// Gets the MAC address (<see cref="PhysicalAddress"/>) associated with the specified IP.
            /// </summary>
            /// <param name="ipAddress">The remote IP address.</param>
            /// <returns>The remote machine's MAC address.</returns>
            public string GetMacAddress(IPAddress ipAddress)
            {
                try
                {
                    const int MacAddressLength = 6;
                    int length = MacAddressLength;
                    var macBytes = new byte[MacAddressLength];
                    SendARP(BitConverter.ToInt32(ipAddress.GetAddressBytes(), 0), 0, macBytes, ref length);
                    return new PhysicalAddress(macBytes).ToString();
                }
                catch (Exception)
                {

                    return "";
                }
            }
        }
        #endregion

    }
}