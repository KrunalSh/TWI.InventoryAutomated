using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.Security;
using TWI.InventoryAutomated.TestGmbhWh_Users;

namespace TWI.InventoryAutomated.DataAccess
{
    public class CommonServices
    {
        #region  "Global Variables"

        //public static List<NAVUsers> _navuserlist;

        public static List<string> _navuserlist;

        #endregion

        #region "Constructor & Connection Function(s)"

        #endregion 

        #region "Helper Function(s)"
        public bool IsCurrentSessionActive(object CurrentSession = null)
        {
            if (CurrentSession == null)
                return false;
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                int currentSession = Convert.ToInt32(CurrentSession);
                if (db.UserSessionLogs.Where(x => x.ID == currentSession && x.IsActive == false).FirstOrDefault() != null)
                    return false;
                else
                    return true;
            }
        }
        public bool RemoveSessions()
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    HttpContext.Current.Session["InstanceName"] = null;
                    HttpContext.Current.Session["CompanyName"] = null;
                    HttpContext.Current.Session["DeviceID"] = null;
                    HttpContext.Current.Session["InstanceName"] = null;
                    HttpContext.Current.Session["CompanyName"] = null;
                    HttpContext.Current.Session["CurrentSession"] = null;
                    HttpContext.Current.Session["DisplayName"] = null;
                    HttpContext.Current.Session["TeamID"] = null;
                    HttpContext.Current.Session["SCID"] = null;
                    HttpContext.Current.Session["IterationID"] = null;
                    SessionPersister.UserName = string.Empty;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void CloseExistingSessions(int userid)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<UserAccess> userAccess = db.UserAccesses.Where(x => x.UserID == userid).ToList();
                    foreach (var item in userAccess)
                    {
                        UserSessionLog usL = db.UserSessionLogs.Where(x => x.IsActive == true && x.UserAccessID == item.ID).FirstOrDefault();
                        if (usL != null)
                        {
                            usL.IsActive = false;
                            usL.SessionEnd = DateTime.Now;
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex) { }
        }

        internal void CloseCurrentSession(int CurrentSession)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    UserSessionLog usL = db.UserSessionLogs.Where(x => x.IsActive == true && x.ID == CurrentSession).FirstOrDefault();
                    if (usL != null)
                    {
                        usL.IsActive = false;
                        usL.SessionEnd = DateTime.Now;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex) { }
        }
        public static bool IsUserhasPermissionsOnModule(string ModuleName)
        {
            try
            {
                Debug.WriteLine("Permission change");
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    UserAccess UserAcc = (from a in db.UIPermissionAssignments
                                          join b in db.UserAccesses on a.PermissionID equals b.PermissionID
                                          join c in db.Users on b.UserID equals c.UserID
                                          join d in db.Modules on a.ModuleID equals d.ModuleID
                                          where d.ModuleName == ModuleName && c.UserName == SessionPersister.UserName && a.AllowAccess == true
                                          select b).FirstOrDefault();
                    if (UserAcc != null)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static bool IsUserSuperAdmin(string InstanceName = "",string CompanyName = "")
        {
            try
            {
                var TagIds = new string[] { "Super Admin" };
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    UserAccess Useracc = (from a in db.Permissions
                                          join b in db.UserAccesses on a.ID equals b.PermissionID
                                          join c in db.Users on b.UserID equals c.UserID
                                          where TagIds.Contains(a.PermissionDesc) && c.UserName == SessionPersister.UserName
                                          && b.InstanceName == InstanceName && b.CompanyName == CompanyName
                                          select b).FirstOrDefault();
                    if (Useracc != null)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static bool IsUserhasAccessOnPage(string FormName)
        {
            try
            {
                Debug.WriteLine(FormName + "_" + SessionPersister.UserName);
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    UserAccess userAccess = (from a in db.UIPermissionAssignments
                                             join b in db.UserAccesses on a.PermissionID equals b.PermissionID
                                             join c in db.Users on b.UserID equals c.UserID
                                             join d in db.Forms on a.FormID equals d.ID
                                             where d.FormName == FormName && c.UserName == SessionPersister.UserName && a.AllowAccess == true
                                             select b).FirstOrDefault();
                    if (userAccess != null)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static StockCountModel GetStockCountDetailsById(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetStockCountDetailByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", ID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();

                    /*List<StockCountModel> _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).ToList()*/
                    StockCountModel _scm = new StockCountModel();

                    if (((IObjectContextAdapter)db).ObjectContext.Translate<StockCountHeader>(reader).Count() > 0)
                    {
                        
                        StockCountHeader _scHeader = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountHeader>(reader).ToList().FirstOrDefault();
                        //_scm._scheader = _scHeader;
                        reader.NextResult();
                        _scm._stockCountItems = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountDetail>(reader).ToList();
                        
                        //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "-- Select Status --", Value = "S" });
                        //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Ongoing", Value = "O" });
                        //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Freezed", Value = "F" });
                        //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Posted", Value = "P" });

                        return _scm;
                    }
                    else { 
                        //_scm._scheader = new StockCountHeader();
                        _scm._stockCountItems = new List<StockCountDetail>();
                        return _scm;
                    }
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        public static StockCountModel GetOpenStockCountBatch()
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetOngoingStockCountBatch]";
                    command.CommandType = CommandType.StoredProcedure;
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();


                    if (((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).Count() > 0)
                    {
                        //List<StockCountModel> _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).ToList<StockCountModel>();
                        StockCountModel _sc1 = new StockCountModel();
                        _sc1.ID = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().ID;
                        _sc1.SCCode = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().SCCode;
                        _sc1.SCDesc = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().SCDesc;
                        _sc1.TotalItemCount = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().TotalItemCount;
                        _sc1.Status = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().Status;
                        _sc1.LocationCode = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().LocationCode;
                        _sc1.CreatedDate = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().CreatedDate;
                        _sc1.CreatedBy = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault().CreatedBy;

                        reader.NextResult();
                        _sc1._stockCountItems = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountDetail>(reader).ToList();
                        //_stockcountdata[0]._stockCountItems = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountDetail>(reader).ToList();
                        return _sc1;
                    }
                    else {
                        StockCountModel _sc = new StockCountModel();
                        _sc._stockCountItems = new List<StockCountDetail>();
                        return _sc;
                    }

                    

                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "-- Select Status --", Value = "S" });
                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Ongoing", Value = "O" });
                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Freezed", Value = "F" });
                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Posted", Value = "P" });
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        public static List<StockCountDetail> GetStockCountDetailByID(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetStockCountDetailsByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", ID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();
                    List<StockCountDetail> _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountDetail>(reader).ToList();
                    return _stockcountdata;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }


        public static List<StockCountAllocations> GetStockCountAllocationsByTeamID(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetStockCountAllocationsByTeamID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", ID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();
                    List<StockCountAllocations> _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountAllocations>(reader).ToList();
                    return _stockcountdata;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        public static List<StockCountDetail> GetStockCountDetailByID(int ID,string entryType)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetStockDetailByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", ID));
                    command.Parameters.Add(new SqlParameter("@entryType", entryType));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();
                    List<StockCountDetail> _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountDetail>(reader).ToList();
                    return _stockcountdata;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        public static StockCountModel GetStockCountHeaderByID(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetStockCountHeaderByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", ID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();
                    StockCountModel _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).FirstOrDefault();
                    return _stockcountdata;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        public static int ClearFinalQtyByBatchID(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[ClearFinalqtyByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@SCID", ID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteNonQuery();
                    return reader;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        public static object GetManagerViewData(int ID)
        {
            System.Data.Entity.Core.Objects.ObjectResult<System.Data.Entity.Core.Objects.ObjectResult> _obj;
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetStockCountHeaderByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", ID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();
                    _obj = ((IObjectContextAdapter)db).ObjectContext.Translate<System.Data.Entity.Core.Objects.ObjectResult>(reader);
                    return _obj;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
            
        }

        public static List<string> GetFullItemList(int SCID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                try
                {
                    var command = db.Database.Connection.CreateCommand();
                    command.CommandText = "[dbo].[GetItemListByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", SCID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();
                    List<string> _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<string>(reader).ToList();
                    return _stockcountdata;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        public static void GetNAVUserList()
        {
            TestGmbhWh_Users.Users_Service _userservice = new Users_Service();
            _userservice.UseDefaultCredentials = false;
            _userservice.Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            List<TestGmbhWh_Users.Users> _navuser = new List<TestGmbhWh_Users.Users>();
            _navuser = _userservice.ReadMultiple(null, "", 0).ToList();
            //_navuserlist = new List<NAVUsers>();
            _navuserlist = new List<string>();
            foreach (TestGmbhWh_Users.Users _usr in _navuser)
            {
                string username = string.IsNullOrEmpty(Convert.ToString(_usr.User_Name)) ? "" : Convert.ToString(_usr.User_Name);
                string fullname =  string.IsNullOrEmpty(Convert.ToString(_usr.User_Name)) ? "" : Convert.ToString(_usr.Full_Name); 
                _navuserlist.Add(username + " - " + fullname);
            }
        }

        #endregion
    }


    public class NAVUsers {
        public string UserName { get; set; }
        public string FullName { get; set; }
    }








}