using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.Security;
using System.Runtime.InteropServices;
using TWI.InventoryAutomated.DataAccess;

namespace TWI.InventoryAutomated.Controllers
{
    public class HomeController : Controller
    {
        #region "Global Variables"
        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        static extern int GetIpNetTable(IntPtr pIpNetTable,
        [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);

        /// <summary>
        /// Error codes GetIpNetTable returns that we recognise
        /// </summary>
        const int ERROR_INSUFFICIENT_BUFFER = 122;

        /// <summary>
        /// MIB_IPNETROW structure returned by GetIpNetTable
        /// DO NOT MODIFY THIS STRUCTURE.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct MIB_IPNETROW
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwIndex;
            [MarshalAs(UnmanagedType.U4)]
            public int dwPhysAddrLen;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac0;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac1;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac2;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac3;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac4;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac5;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac6;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac7;
            [MarshalAs(UnmanagedType.U4)]
            public int dwAddr;
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
        }
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

        public string FetchUserLanguage()
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    HttpCookie cookie = Request.Cookies["Language"];
                    List<string> StaticLang = new List<string>() { "English", "French", "German" };
                    if (Session["UserID"] != null)
                    {
                        int userid = Convert.ToInt32(Session["UserID"]);
                        string html = "";
                        if (cookie != null && cookie.Value != null)
                        {
                            var userlanguages = (from a in db.Languages
                                                 join b in db.UserLanguages on a.ID equals b.LanguageID
                                                 where b.UserID == userid && b.IsActive == true && StaticLang.Contains(a.Description) && a.Code != cookie.Value
                                                 select new { a.Description, b.IsDefault }).ToList();

                            foreach (var item in userlanguages)
                            {
                                html += " <a href='#' onclick='MakeitDefault(this);' style='color:white;margin-left:2px;'><b>" + item.Description + "</b></a>   ";

                            }
                        }
                        return html;
                    }

                }

            }
            catch (Exception ex)
            {
                ArchiveLogs.SaveActivityLogs("Layout", "Home", "FetchUserLanuguage", ex.ToString());
            }
            return "";
        }

        public ActionResult SubMenu()
        {
            try
            {
                ArchiveLogs.SaveActivityLogs("SubMenu", "Home", "FetchSubmenu", null);

                CommonServices cs = new CommonServices();
                if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
                    return View();
                else
                {
                    cs.RemoveSessions();
                    return RedirectToAction("Default", "Home");
                }
            }
            catch (Exception ex)
            {
                ArchiveLogs.SaveActivityLogs("SubMenu", "Home", "FetchSubmenu", ex.ToString());
                return null;
            }
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
            try
            {
                //string IP = Request.UserHostName; // Fetch Computer Name
                //IPAddress myIP = IPAddress.Parse(IP);
                //IPHostEntry GetIPHost = Dns.GetHostEntry(myIP);
                //List<string> compName = GetIPHost.HostName.ToString().Split('.').ToList();
                //return Json(new { success = false, message = IP +" _ "+ myIP + " _ " +GetIPHost + " _ " + compName.FirstOrDefault() }, JsonRequestBehavior.AllowGet);
                CommonServices cs = new CommonServices();
                InventoryPortalEntities db = new InventoryPortalEntities();
                User _user = db.Users.Where(x => x.UserName.Equals(user.UserName) && x.Password.Equals(user.Password) && x.IsActive == true).FirstOrDefault();
                if (_user == null)
                {
                    return Json(new { success = false, message = Resources.GlobalResource.MsgInvalidLoginInformation }, JsonRequestBehavior.AllowGet);
                }
                List<UserAccess> uaccess = db.UserAccesses.Where(x => x.UserID == _user.UserID).ToList();
                if (uaccess.Count == 0)
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAccessDenied }, JsonRequestBehavior.AllowGet);
                else
                {
                    if (Session["DeviceID"] != null)
                    {
                        int ID = Convert.ToInt32(Session["DeviceID"].ToString());
                        SessionPersister.UserName = _user.UserName;
                        Session["DisplayName"] = _user.DisplayName;
                        Session["UserID"] = _user.UserID;
                        List<int> UserAccessID = (from e in db.UserAccesses
                                                  join f in db.UserAccessDevices on e.ID equals f.UserAccessID
                                                  where f.DeviceID == ID && f.IsActive == true && e.UserID == _user.UserID
                                                  select e.ID).ToList();
                        if (UserAccessID.Count == 0)
                            return Json(new { success = false, message = Resources.GlobalResource.MsgAccessDenied }, JsonRequestBehavior.AllowGet);
                        if (!(bool)user.IsActive && CheckAlreadyLogin(_user))
                        {
                            return Json(new { success = false, message = "MsgAlreadyLoggedin" + Resources.GlobalResource.MsgAlreadyLoggedin }, JsonRequestBehavior.AllowGet);
                        }
                        else if ((bool)user.IsActive)
                        {
                            cs.CloseExistingSessions(_user.UserID);
                        }
                        if (uaccess.Count == 1)
                        {
                            UserAccess useraccess = uaccess.FirstOrDefault();
                            Session["InstanceName"] = db.Instances.Where(x => x.ID == useraccess.InstanceID).Select(x => x.InstanceName).FirstOrDefault();
                            Session["CompanyName"] = db.Companies.Where(x => x.ID == useraccess.CompanyID).Select(x => x.CompanyName).FirstOrDefault();
                            LocalizationWebsite();
                            AddEntryToSessionLog(uaccess.FirstOrDefault().ID);
                            return Json(new { success = true, message = Url.Action("Home", "Home") }, JsonRequestBehavior.AllowGet);
                        }
                        else
                            return Json(new { success = true, message = Url.Action("InstanceAuthentication", "Home") }, JsonRequestBehavior.AllowGet);
                    }
                    else
                        return Json(new { success = false, message = Resources.GlobalResource.MsgUnabletoLogin }, JsonRequestBehavior.AllowGet);
                }


            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult LogOut()
        {
            try
            {
                CommonServices cs = new CommonServices();
                if (Session["CurrentSession"] != null)
                {
                    cs.CloseCurrentSession(Convert.ToInt32(Session["CurrentSession"]));
                }
                cs.RemoveSessions();
                HttpCookie cookie = Request.Cookies["SystemLang"];

                if (cookie != null && cookie.Value != null)
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cookie.Value);
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(cookie.Value);
                    if (Request.Cookies["Language"] != null)
                    {
                        Response.Cookies["Language"].Expires = DateTime.Now.AddDays(-1);
                    }
                    if (Request.Cookies["SystemLang"] != null)
                    {
                        Response.Cookies["SystemLang"].Expires = DateTime.Now.AddDays(-1);
                    }
                }
                return RedirectToAction("Default", "Home");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Default", "Home");
            }
        }

        [HttpPost]
        public ActionResult Home(int InstId, int CompId, string InstName, string compName)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (Session["DeviceID"] != null && Session["UserID"] != null && InstId != null && CompId != null)
                    {
                        int DeviceID = Convert.ToInt32(Session["DeviceID"].ToString());
                        int UserID = Convert.ToInt32(Session["UserID"].ToString());
                        List<UserAccess> UserAccess = (from e in db.UserAccesses
                                                       join f in db.UserAccessDevices on e.ID equals f.UserAccessID
                                                       where e.InstanceID == InstId && e.CompanyID == CompId && f.DeviceID == DeviceID && f.IsActive == true && e.UserID == UserID
                                                       select e).ToList();
                        if (UserAccess.Count > 0)
                        {
                            AddEntryToSessionLog(UserAccess[0].ID);
                            Session["InstanceName"] = InstName;
                            Session["CompanyName"] = compName;
                            LocalizationWebsite();
                            return Json(new { success = true, message = Url.Action("Home", "Home") }, JsonRequestBehavior.AllowGet);
                        }
                        else
                            return Json(new { success = false, message = Resources.GlobalResource.MsgAccessDenied }, JsonRequestBehavior.AllowGet);
                    }
                    else
                        return Json(new { success = false, message = Resources.GlobalResource.MsgAccessDenied }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public PartialViewResult AuthenticateDevice()
        {
            //Dictionary<IPAddress, PhysicalAddress> obj = new Dictionary<IPAddress, PhysicalAddress>();
            //obj = GetAllDevicesOnLAN();
            //IPAddress clientip = IPAddress.Parse(Request.UserHostAddress);
            //string MacAddress = string.Empty, txtIPAdress = string.Empty;
            //foreach (IPAddress ip in obj.Keys)
            //{
            //    if (ip.Equals(clientip))
            //    {
            //        PhysicalAddress actual = obj[ip];
            //        MacAddress = Convert.ToString(actual);
            //        txtIPAdress = Convert.ToString(clientip);
            //    }
            //}

            string MacAddress = "A44CC82CBE25";
            if (IsDeviceRegistered(MacAddress))
                return PartialView("Index");
            else
                return PartialView("AccessDenied");
        }

        private bool IsDeviceRegistered(string macAddress)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    RegisteredDevice rDevices = db.RegisteredDevices.Where(x => x.IsActive == true && x.MacAddress == macAddress).FirstOrDefault();
                    if (rDevices != null)
                    {
                        Session["DeviceID"] = rDevices.ID;
                        return true;
                    }
                    else
                    {
                        Session["DeviceID"] = null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
                
            }
        }

        public ActionResult InstanceAuthentication()
        {
            try
            {
                InventoryPortalEntities db = new InventoryPortalEntities();
                int UserID = Convert.ToInt32(Session["UserID"].ToString());
                ViewBag.Instances = (from a in db.UserAccesses
                                     join b in db.Instances on a.InstanceID equals b.ID
                                     where a.UserID == UserID
                                     select new
                                     {   b.ID,
                                         b.InstanceName
                                     }).Distinct().ToList();
                return PartialView("InstanceAuthentication");
            }
            catch (Exception ex) { throw; }
        }
        [HttpPost]
        public ActionResult GetCompanies(int intInstID)
        {
            try
            {
                InventoryPortalEntities db = new InventoryPortalEntities();
                int userID = Convert.ToInt32(Session["UserID"].ToString());
                var Companies = (from a in db.UserAccesses
                                 join b in db.Companies on a.CompanyID equals b.ID
                                 where a.UserID == userID && a.InstanceID == intInstID
                                 select new
                                 {
                                     b.ID,
                                     b.CompanyName
                                 }).ToList();
                return Json(new { success = true, message = Companies }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "" }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region "Helper Function(s) "
        private static Dictionary<IPAddress, PhysicalAddress> GetAllDevicesOnLAN()
        {
            Dictionary<IPAddress, PhysicalAddress> all = new Dictionary<IPAddress, PhysicalAddress>();
            // Add this PC to the list...
            all.Add(GetIPAddress(), GetMacAddress());
            int spaceForNetTable = 0;
            // Get the space needed
            // We do that by requesting the table, but not giving any space at all.
            // The return value will tell us how much we actually need.
            GetIpNetTable(IntPtr.Zero, ref spaceForNetTable, false);
            // Allocate the space
            // We use a try-finally block to ensure release.
            IntPtr rawTable = IntPtr.Zero;
            try
            {
                rawTable = Marshal.AllocCoTaskMem(spaceForNetTable);
                // Get the actual data
                int errorCode = GetIpNetTable(rawTable, ref spaceForNetTable, false);
                if (errorCode != 0)
                {
                    // Failed for some reason - can do no more here.
                    throw new Exception(string.Format(
                      "Unable to retrieve network table. Error code {0}", errorCode));
                }
                // Get the rows count
                int rowsCount = Marshal.ReadInt32(rawTable);
                IntPtr currentBuffer = new IntPtr(rawTable.ToInt64() + Marshal.SizeOf(typeof(Int32)));
                // Convert the raw table to individual entries
                MIB_IPNETROW[] rows = new MIB_IPNETROW[rowsCount];
                for (int index = 0; index < rowsCount; index++)
                {
                    rows[index] = (MIB_IPNETROW)Marshal.PtrToStructure(new IntPtr(currentBuffer.ToInt64() +
                                                (index * Marshal.SizeOf(typeof(MIB_IPNETROW)))
                                               ),
                                                typeof(MIB_IPNETROW));
                }
                // Define the dummy entries list (we can discard these)
                PhysicalAddress virtualMAC = new PhysicalAddress(new byte[] { 0, 0, 0, 0, 0, 0 });
                PhysicalAddress broadcastMAC = new PhysicalAddress(new byte[] { 255, 255, 255, 255, 255, 255 });
                foreach (MIB_IPNETROW row in rows)
                {
                    IPAddress ip = new IPAddress(BitConverter.GetBytes(row.dwAddr));
                    byte[] rawMAC = new byte[] { row.mac0, row.mac1, row.mac2, row.mac3, row.mac4, row.mac5 };
                    PhysicalAddress pa = new PhysicalAddress(rawMAC);
                    if (!pa.Equals(virtualMAC) && !pa.Equals(broadcastMAC) && !IsMulticast(ip))
                    {
                        //Console.WriteLine("IP: {0}\t\tMAC: {1}", ip.ToString(), pa.ToString());
                        if (!all.ContainsKey(ip))
                        {
                            all.Add(ip, pa);
                        }
                    }
                }
            }
            finally
            {
                // Release the memory.
                Marshal.FreeCoTaskMem(rawTable);
            }
            return all;
        }
        /// <summary>
        /// Gets the IP address of the current PC
        /// </summary>
        /// <returns></returns>
        private static IPAddress GetIPAddress()
        {
            String strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            foreach (IPAddress ip in addr)
            {
                if (!ip.IsIPv6LinkLocal)
                {
                    return (ip);
                }
            }
            return addr.Length > 0 ? addr[0] : null;
        }

        /// <summary>
        /// Gets the MAC address of the current PC.
        /// </summary>
        /// <returns></returns>
        private static PhysicalAddress GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only consider Ethernet network interfaces
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.OperationalStatus == OperationalStatus.Up)
                {
                    return nic.GetPhysicalAddress();
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if the specified IP address is a multicast address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static bool IsMulticast(IPAddress ip)
        {
            bool result = true;
            if (!ip.IsIPv6Multicast)
            {
                byte highIP = ip.GetAddressBytes()[0];
                if (highIP < 224 || highIP > 239)
                {
                    result = false;
                }
            }
            return result;
        }

        private void AddEntryToSessionLog(int AccessID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    int DeviceID = Convert.ToInt32(Session["DeviceID"].ToString());
                    UserSessionLog usl = new UserSessionLog();
                    usl.UserAccessID = AccessID;
                    usl.SessionStart = DateTime.Now;
                    usl.DeviceID = DeviceID;
                    usl.IsActive = true;
                    db.UserSessionLogs.Add(usl);
                    db.SaveChanges();

                    Session["CurrentSession"] = usl.ID;
                }
                catch (Exception ex)
                {
                    Session["CurrentSession"] = ex.ToString();
                }
            }
        }
        public bool CheckAlreadyLogin(User user)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                List<int> userAccessIds = db.UserAccesses.Where(x => x.UserID == user.UserID).Select(x => x.ID).ToList();
                foreach (var item in userAccessIds)
                {
                    if (db.UserSessionLogs.Where(x => x.IsActive == true && x.UserAccessID == item).FirstOrDefault() != null)
                        return true;
                }
            }
            return false;
        }

        public void LocalizationWebsite()
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (Session["UserID"] != null)
                {
                    int userid = Convert.ToInt32(Session["UserID"]);
                    Language currentlang = (from a in db.Languages
                                            join b in db.UserLanguages on a.ID equals b.LanguageID
                                            where b.IsDefault == true && b.UserID == userid
                                            select a).FirstOrDefault();
                    if (currentlang != null)
                    {
                        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(currentlang.Code);
                        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(currentlang.Code);
                        HttpCookie cookie = new HttpCookie("Language");
                        cookie.Value = currentlang.Code;
                        Response.Cookies.Add(cookie);
                    }
                    else
                    {
                        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en");
                        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                    }
                }
                else
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en");
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
                }
            }
        }
        #endregion

    }
}