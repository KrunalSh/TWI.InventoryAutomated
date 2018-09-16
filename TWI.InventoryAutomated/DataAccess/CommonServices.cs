﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.Security;

namespace TWI.InventoryAutomated.DataAccess
{
    public class CommonServices
    {
        #region  "Global Variables"
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
        public static bool IsUserSuperAdmin()
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
                    command.CommandText = "[dbo].[GetStockCountDetailsByID]";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ID", ID));
                    db.Database.Connection.Open();
                    var reader = command.ExecuteReader();

                    List<StockCountModel> _stockcountdata= ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).ToList();
                    reader.NextResult();
                    _stockcountdata[0]._stockCountItems = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountDetail>(reader).ToList();

                    _stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "-- Select Status --", Value = "S" });
                    _stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Ongoing", Value = "O" });
                    _stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Freezed", Value = "F" });
                    _stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Posted", Value = "P" });

                    return _stockcountdata[0];
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

                    List<StockCountModel> _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountModel>(reader).ToList();
                    reader.NextResult();
                    _stockcountdata[0]._stockCountItems = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountDetail>(reader).ToList();

                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "-- Select Status --", Value = "S" });
                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Ongoing", Value = "O" });
                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Freezed", Value = "F" });
                    //_stockcountdata[0]._statusList.Add(new System.Web.Mvc.SelectListItem { Text = "Posted", Value = "P" });

                    return _stockcountdata[0];
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

        public static StockCountHeader GetStockCountHeaderByID(int ID)
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
                    StockCountHeader _stockcountdata = ((IObjectContextAdapter)db).ObjectContext.Translate<StockCountHeader>(reader).FirstOrDefault();
                    return _stockcountdata;
                }
                catch (Exception ex)
                { throw; }
                finally { db.Database.Connection.Close(); }
            }
        }

        #endregion
    }
}