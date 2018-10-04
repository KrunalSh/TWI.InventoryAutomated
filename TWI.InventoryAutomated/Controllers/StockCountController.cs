using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.DEVPhyInvJournal;
using TWI.InventoryAutomated.TESTPhyInvJournal;
using TWI.InventoryAutomated.TestItemList;
using System.Net;
using System.Globalization;
using TWI.InventoryAutomated.Security;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

namespace TWI.InventoryAutomated.Controllers
{
    public class StockCountController : Controller
    {
        #region "Global Variables"
            object _service;
            object _servicefilters;
        #endregion

        #region "Form Event(s)"

        // GET: StockCount Module Home Page 
        public ActionResult Index()
        {
            return View();
        }

        #region "NAV DATA Form Event(s)"

        public ActionResult NavDataPull(int ID =0)
        {
            string InstanceName = Convert.ToString(Session["InstanceName"]);
            string CompanyName = Convert.ToString(Session["CompanyName"]);

            using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<StockCountHeader> _batchlist = GetBatchListing();
                    ViewBag.BatchList = new SelectList(_batchlist, "ID", "SCCode");
                    StockCountModel _scm = new StockCountModel();
                    StockCountHeader _sch = new StockCountHeader();
                    if (db.StockCountHeader.Where(x=> x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0)
                    {
                            if (db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0)
                            { _sch = db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault(); }
                            else  { _sch = db.StockCountHeader.Where(x => x.Status == "C" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault(); }

                        _scm.ID = _sch.ID;
                        _scm.SCCode = _sch.SCCode;
                        _scm.SCDesc = _sch.SCDesc;
                        _scm.LocationCode = _sch.LocationCode;
                        if (_sch.Status == "O") { _scm.Status = "Open"; } else _scm.Status = "Closed";
                        _scm.TotalItemCount = Convert.ToDecimal(_sch.TotalItemCount);

                        if (db.StockCountDetail.Where(x => x.SCID == _sch.ID).Count() > 0) _scm._stockCountItems = db.StockCountDetail.Where(x => x.SCID == _sch.ID).ToList();
                        else _scm._stockCountItems = CommonServices.GetStockCountDetailByID(_sch.ID);
                        return View(_scm);
                    }
                    else {
                            
                    _scm.ID =-1;
                    _scm.SCCode = string.Empty;
                    _scm.SCDesc = string.Empty;
                    _scm.LocationCode = string.Empty;
                    _scm.Status = "";
                    _scm.TotalItemCount = 0;
                    _scm._stockCountItems = new List<StockCountDetail>();
                }
                return View(_scm);
            }
        }

        [HttpGet]
        public ActionResult NewStockCount(int ID=0)
        {
            return View();
        }

        [HttpPost]
        public ActionResult NewStockCount(StockCountHeader _sc)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (_sc == null) { return Json(new { success = false, message = "* Marked fields are mandatory fields, Kindly enter a values for respective fields" }, JsonRequestBehavior.AllowGet); }

                if (_sc.SCCode == null || string.IsNullOrEmpty(_sc.SCCode.Trim())) 
                    return Json(new { success = false, message = "Value for Code field is mandatory, Kindly enter a value" }, JsonRequestBehavior.AllowGet);
                
                if (_sc.SCDesc == null || string.IsNullOrEmpty(_sc.SCDesc.Trim()))
                        return Json(new { success = false, message = "Value for Description field is mandatory, Kindly enter a value" }, JsonRequestBehavior.AllowGet);

                if (_sc.LocationCode ==null || string.IsNullOrEmpty(_sc.LocationCode.Trim()))
                    return Json(new { success = false, message = "Value for Location Code is Mandatory, Kindly enter a value" }, JsonRequestBehavior.AllowGet);

                //Validation to check whether duplicate Batch Code is not being entered.
                if (db.StockCountHeader.AsNoTracking().Where(x => x.SCCode == _sc.SCCode && x.LocationCode == _sc.LocationCode).Count() > 0)
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);

                //Validation to check whether stock count is ongoing for any Batch in the system.
                if (db.StockCountHeader.AsNoTracking().Where(x => x.Status == "O" && x.LocationCode == _sc.LocationCode).Count() > 0)
                    return Json(new { success = false, message = Resources.GlobalResource.MsgOngoingBatchError }, JsonRequestBehavior.AllowGet);

                _sc.CreatedDate = DateTime.Now;
                _sc.CreatedBy = Convert.ToInt32(Session["UserID"]);
                _sc.TotalItemCount = 0;
                _sc.InstanceName = Convert.ToString(Session["InstanceName"]);
                _sc.CompanyName = Convert.ToString(Session["CompanyName"]);
                _sc.Status = "O";

                    try
                    {
                        db.StockCountHeader.Add(_sc);
                        db.SaveChanges();

                        //List<StockCountHeader> _batchlist = db.StockCountHeader.ToList();
                        //StockCountHeader _row0 = new StockCountHeader();
                        //_row0.ID = -1;
                        //_row0.SCCode = "-- Select Batch -- ";
                        //_batchlist.Insert(0, _row0);
                        //ViewBag.BatchList = new SelectList(_batchlist, "ID", "SCCode");

                    return Json(new { success = true, message = Resources.GlobalResource.MsgBatchSavedSuccessfully }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                    }
                }
            //return View();
        }

        public ActionResult GetPhyJournalData(int ID)
        {
            string _resultMsg = string.Empty;
            int ItemCount = 0;
            try
            {
                //Code to be moved to detail screen where pull, push everything will happen
                switch (Convert.ToString(Session["InstanceName"]).ToLower())
                {
                    case "live":
                        _resultMsg = ConnectAndPullDataFromLive(ID, Convert.ToString(Session["CompanyName"]), ref ItemCount);
                        break;
                    case "dev": _resultMsg = ConnectAndPullDataFromDev(ID,Convert.ToString(Session["CompanyName"]), ref ItemCount);
                        break;
                    case "test": _resultMsg = ConnectAndPullFromTest(ID, Convert.ToString(Session["CompanyName"]), ref ItemCount);
                        break;
                }
                return Content(_resultMsg);
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        public ActionResult PushPhyJournalData(int ID)
        {
            string _resultMsg = string.Empty;
            try
            {
                //Code to be moved to detail screen where pull, push everything will happen
                switch (Convert.ToString(Session["InstanceName"]).ToLower())
                {
                    case "live":
                        _resultMsg = ConnectAndPushDataToLive(ID, Convert.ToString(Session["CompanyName"]));
                        break;
                    case "dev":
                        _resultMsg = ConnectAndPushDataToDEV(ID, Convert.ToString(Session["CompanyName"]));
                        break;
                    case "test":
                        _resultMsg = ConnectAndPushDataToTest(ID, Convert.ToString(Session["CompanyName"]));
                        break;
                }
                return Content(_resultMsg);
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        public JsonResult GetSockCountDetailByID(int ID)
        {
            return Json(new { data = CommonServices.GetStockCountDetailByID(ID) }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetStockCountHeaderDetailByID(int ID)
        {
            return Json(new { data = CommonServices.GetStockCountHeaderByID(ID) }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult DeleteStockCountBatch(int ID)
        {
            //Validation to check whether item lines exists for the selected batch ID
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (ID == -1) { return Json(new { success = false, message = "Batch Selection is mandatory for Deletion"}, JsonRequestBehavior.AllowGet); }

                if (db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault().Status == "C") {
                    return Json(new { success = false, message = "Cannot delete a Closed Batch, Kindly select a valid batch for Deletion" }, JsonRequestBehavior.AllowGet);
                } 
                
                if ((db.StockCountDetail.Where(x => x.SCID == ID).Count() == 0) || (db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty == 0).Count() == db.StockCountDetail.Where(x => x.SCID == ID).Count()))
                {
                    db.StockCountHeader.Remove(db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault());
                    if(db.StockCountDetail.Where(x => x.SCID == ID).Count() > 0) db.StockCountDetail.RemoveRange(db.StockCountDetail.Where(x => x.SCID == ID));
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullDeletion }, JsonRequestBehavior.AllowGet);
                }
                else
                    return Json(new { success = false, message = Resources.GlobalResource.MsgOngoingBatchDeletionRestriction }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetBatchList()
        {
            List<StockCountHeader> _batchlist = GetBatchListing();
            return Json(_batchlist, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region "Counts & Teams Event(s)"
        
        //Counts / Iteration Event(s)
        public ActionResult BatchIterations()
        {
            string InstanceName = Convert.ToString(Session["InstanceName"]);
            string CompanyName = Convert.ToString(Session["CompanyName"]);

            List<StockCountHeader> _batchlist = GetBatchListing();
            ViewBag.BatchList = new SelectList(_batchlist, "ID", "SCCode");
            BatchIterationModel _bim = new BatchIterationModel();
            StockCountHeader _sch = new StockCountHeader();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            { 
                if (db.StockCountHeader.Where(x=> x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0)
                {
                    if (db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0)
                    {
                        _sch = db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault();
                    }
                    else {
                        _sch = db.StockCountHeader.Where(x => x.Status == "C" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault();
                    }

                    _bim.ID = _sch.ID;
                    _bim.SCCode = _sch.SCCode;
                    _bim.SCDesc = _sch.SCDesc;
                    _bim.LocationCode = _sch.LocationCode;
                    if (_sch.Status == "O") { _bim.Status = "Open"; } else _bim.Status = "Closed";
                    _bim.TotalItemCount = Convert.ToDecimal(_sch.TotalItemCount);

                    if (db.StockCountIterations.Where(x => x.SCID == _sch.ID).Count() > 0) _bim.Iterations = db.StockCountIterations.Where(x => x.SCID == _sch.ID).OrderBy(y => y.IterationNo).ToList();
                    else _bim.Iterations = new List<StockCountIterations>();

                    if (db.StockCountTeams.Where(x => x.SCID == _sch.ID).Count() > 0) _bim.Teams = db.StockCountTeams.Where(x => x.SCID == _sch.ID).ToList();
                    else _bim.Teams = new List<StockCountTeams>();

                    //if (db.StockCountTeams.Where(x => x.SCID == _sch.ID).Count() > 0) _bim.Iterations = db.StockCountIterations.Where(x => x.SCID == _sch.ID).ToList();
                    //else _bim.Iterations = new List<StockCountIterations>();
                }
                else
                {
                    _bim.ID = -1;
                    _bim.SCCode = string.Empty;
                    _bim.SCDesc = string.Empty;
                    _bim.LocationCode = string.Empty;
                    _bim.Status = "";
                    _bim.TotalItemCount = 0;
                    _bim.Iterations = new List<StockCountIterations>();
                    _bim.Teams = new List<StockCountTeams>();
                    //_bim.TeamAllocationSummary = new List<CountItemsSummary>();
                }
            return View(_bim);
            }
        }

        public JsonResult GetBatchIterationsByID(int ID)
        {
            List<StockCountIterations> _batchItrs;
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountIterations.Where(x => x.SCID == ID).Count() > 0) _batchItrs = db.StockCountIterations.Where(x => x.SCID == ID).ToList();
                else _batchItrs = new List<StockCountIterations>();
            }
            return Json(_batchItrs, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CheckBatchStatus(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountHeader.Where(x => x.ID == ID && x.Status == "C").Count() > 0)
                    return Json(new { success = false, message = "cannot create a count for a closed batch, kindly check your selection" }, JsonRequestBehavior.AllowGet);

                if (db.StockCountDetail.Where(x => x.SCID == ID).Count() == 0)
                    return Json(new { success = false, message = "Kindly first pull data from NAV and then create count(s) to proceed" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = true, message = "" }, JsonRequestBehavior.AllowGet);
        }

        //Iteration Event(s)
        public ActionResult CreateNewIteration(int ID)
        {
            Session["SCID"] = ID;
            return View();
        }

        [HttpPost]
        public ActionResult CreateNewIteration(StockCountIterations _newitr)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    int iterationNo = 1;
                    _newitr.SCID = Convert.ToInt32(Session["SCID"]);

                    if (string.IsNullOrEmpty(_newitr.IterationName)) return Json(new { success = false, message = "Iteration Name is a required field, Kindly enter a value" }, JsonRequestBehavior.AllowGet);

                    if (db.StockCountIterations.Where(x => x.SCID == _newitr.SCID).Count() > 0) iterationNo = db.StockCountIterations.Where(x => x.SCID == _newitr.SCID).Max(x => x.IterationNo).Value + 1;
                    
                    //StockCountIterations _newitr = new StockCountIterations();
                    _newitr.IterationNo = iterationNo;
                    _newitr.SCCode = db.StockCountHeader.Where(x=> x.ID == _newitr.SCID).FirstOrDefault().SCCode;
                    _newitr.Status = false;
                    _newitr.CreatedDate = DateTime.Now;
                    _newitr.CreatedBy = Convert.ToInt32(Session["UserID"]);
                    db.StockCountIterations.Add(_newitr);
                    db.SaveChanges();
                    ViewBag.SCID = -1;
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullIterationCreation }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Iteration Team Event(s)
        public ActionResult NewIterationTeam(int IterationID)
        {
            List<User> _userList = GetUsersListing();
            Session["IterationID"] = IterationID;
            ViewBag.MemberList = new SelectList(_userList, "UserID", "UserName");
            return View();
        }

        [HttpPost]
        public ActionResult NewIterationTeam(StockCountTeams _sct)
        {
            int _teamNo = 1;
            string teamcode = "T";
            int IterationNo = 0;

            _sct.SCIterationID = Convert.ToInt32(Session["IterationID"]);
            
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                _sct.SCID = db.StockCountIterations.Where(x => x.ID == _sct.SCIterationID).FirstOrDefault().SCID;

                if (db.StockCountHeader.Where(x => x.ID == _sct.SCID && x.Status == "C").Count() > 0)
                    return Json(new { success = false, message = "Cannot create a team for a closed batch, kindly select appropriate batch" }, JsonRequestBehavior.AllowGet);

                if (_sct.UserID == -1)
                { return Json(new { success = false, message = "Kindly select a member from the list" }, JsonRequestBehavior.AllowGet); }

                if (db.StockCountTeams.Where(x => x.SCIterationID == _sct.SCIterationID && x.UserID == _sct.UserID).Count() > 0)
                { return Json(new { success = false, message = "Selected user member already assigned to other Team, <br/> cannot have one user for multiple teams." }, JsonRequestBehavior.AllowGet); }

                if (db.StockCountTeams.Where(x => x.SCIterationID == _sct.SCIterationID).Count() > 0) {
                    _teamNo = db.StockCountTeams.Where(x => x.SCIterationID == _sct.SCIterationID).Max(y => y.TeamNo).Value + 1;
                }

                _sct.UserName = db.Users.Where(x => x.UserID == _sct.UserID).FirstOrDefault().UserName;
                _sct.TeamNo = _teamNo;
                _sct.TeamCode = teamcode + _teamNo.ToString();
                _sct.Status = true;
                _sct.CreatedDate = DateTime.Now;
                _sct.CreatedBy = Convert.ToInt32(Session["UserID"]);

                IterationNo = db.StockCountIterations.Where(x => x.ID == _sct.SCIterationID).FirstOrDefault().IterationNo.Value;

                db.StockCountTeams.Add(_sct);
                db.SaveChanges();
                Session["IterationID"] = null;
                
                return Json(new { success = true, message = "Team created Successfully for Iteration No: " + IterationNo.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult DeleteIteration(int ID)
        {
           int SCID = 0;
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    SCID = db.StockCountIterations.Where(x => x.ID == ID).FirstOrDefault().SCID.Value;

                    if (db.StockCountHeader.Where(x => x.ID == SCID && x.Status == "C").Count() > 0)
                        return Json(new { success = false, message = "Cannot delete count(s) of a closed Batch" }, JsonRequestBehavior.AllowGet);

                    if (db.StockCountIterations.Where(x => x.ID == ID && x.Status == true).Count() > 0)
                        return Json(new { success = false, message = "Cannot delete an ongoing count, kindly select a valid count" }, JsonRequestBehavior.AllowGet);




                    db.StockCountIterations.Remove(db.StockCountIterations.Where(x => x.ID == ID).FirstOrDefault());
                    db.StockCountTeams.RemoveRange(db.StockCountTeams.Where(x => x.SCIterationID == ID));
                    db.SaveChanges();


                    List<StockCountIterations> _itrlist = db.StockCountIterations.Where(x => x.SCID == SCID).ToList();
                    int count = 1;
                    foreach (StockCountIterations x in _itrlist)
                    {
                        x.IterationNo = count;
                        db.StockCountIterations.Attach(x);
                        db.Entry(x).Property(y => y.IterationNo).IsModified = true;
                        db.SaveChanges();
                        count++;
                    }

                    return Json(new { success = true, message = "Selected Count Deleted Successfully" },JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        public JsonResult DeleteTeam(int ID)
        {
            int SCID = 0;
            int ItrID = 0;
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    SCID = db.StockCountTeams.Where(x => x.ID == ID).FirstOrDefault().SCID.Value;
                    ItrID = db.StockCountTeams.Where(x => x.ID == ID).FirstOrDefault().SCIterationID.Value;

                    //Check to validate whether the batch of the selected team is a closed or not, if yes, then system should restrict deletion.
                    if (db.StockCountHeader.Where(x => x.ID == SCID && x.Status == "C").Count() > 0)
                        return Json(new { success = false, message = "Cannot delete teams of a closed Batch" }, JsonRequestBehavior.AllowGet);

                    //Check to validate whether the count of a batch is ongoing or not. if yes, then system should restrict deletion
                    if (db.StockCountIterations.Where(x => x.ID == ItrID && x.Status == true).Count() > 0)
                        return Json(new { success = false, message = "Cannot delete team of an ongoing count, kindly select a valid iteration" }, JsonRequestBehavior.AllowGet);

                    //check to validate whether the selected team is allocated items to count or not
                    //to implement this check once allocation screen is completed.
                    
                    db.StockCountTeams.Remove(db.StockCountTeams.Where(x => x.ID == ID).FirstOrDefault());
                    db.SaveChanges();

                    List<StockCountTeams> _teamlist = db.StockCountTeams.Where(x => x.SCIterationID == ItrID).ToList();
                    int count = 1;
                    foreach (StockCountTeams x in _teamlist)
                    {
                        x.TeamNo = count;
                        x.TeamCode = "T" + count.ToString();
                        db.StockCountTeams.Attach(x);
                        db.Entry(x).Property(y => y.TeamNo).IsModified = true;
                        db.Entry(x).Property(y => y.TeamCode).IsModified = true;
                        db.SaveChanges();
                        count++;
                    }

                    return Json(new { success = true, message = "Selected Team Deleted Successfully" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetTeamsByID(int ItrID)
        {
            try
            {
                List<StockCountTeams> _teams;
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.StockCountTeams.Where(x => x.SCIterationID == ItrID).Count() > 0) _teams = db.StockCountTeams.Where(x => x.SCIterationID == ItrID).ToList();
                    else _teams = new List<StockCountTeams>();
                }
                return Json(_teams, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
            
        }
               
        public ActionResult GetBatchCountAndTeams(int SCID)
        {
            string[] result;
            List<StockCountIterations> _scounts;
            List<StockCountTeams> _steams;
            string itrstring = string.Empty;
            
            int count = 0;
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountHeader.Where(x => x.ID == SCID).Count() > 0)
                {
                    if (db.StockCountIterations.Where(x => x.SCID == SCID).Count() > 0)
                    {
                        _scounts = db.StockCountIterations.Where(x => x.SCID == SCID).ToList();
                        result = new string[_scounts.Count()];
                        foreach (StockCountIterations _std in _scounts)
                        {
                            itrstring += "<div class='col-lg-2' style='margin-top:20px;'><div style='border:solid 2px #808080;border-radius:10px;box-shadow:5px 5px 0px 0px #808080;'>";
                            itrstring += "<div style='width:100%;padding-left:10px;background-color:#eae7e7 !important;min-height:40px !important;max-height:80px !important;border-top-left-radius:10px;border-top-right-radius:10px;border-bottom:solid 2px #808080;'>";
                            itrstring += "<div style='float:left;color:black;font-family:Calibri;font-size:18px;font-weight:bold;height:auto;padding-top:3px'>";
                            itrstring += _std.IterationNo + " - " + _std.IterationName + "</div>";
                            itrstring += "<div style='float:right;margin-right:5px;margin-top:7px;'>";
                            if (_std.Status == false)
                            {
                                itrstring += "<a class='btn btn-primary fa fa-lock' style='width:20px !important;height:20px !important;vertical-align:middle !important;margin-right:5px;padding-left:4px;padding-top:2px;' onclick='ChangeCountStatus("+ @_std.ID + ");'></a>";
                            }
                            else
                            {
                                itrstring += "<a class='btn btn-primary fa fa-unlock' style='width:20px !important;height:20px !important;vertical-align:middle !important;margin-right:5px;padding-left:4px;padding-top:2px;' onclick='ChangeCountStatus(" + @_std.ID + ");'></a>";
                            }
                                
                            itrstring += "<a class='btn btn-success fa fa-plus-circle' data-toggle='tooltip' style='width:20px !important;height:20px !important;padding-left:4px;padding-top:2px;vertical-align:middle !important;margin-right:5px;' onclick='CreateNewTeam(" + _std.ID + ");'></a>";
                            itrstring += "<a class='btn btn-danger fa fa-trash' style='width:20px !important;height:20px !important;vertical-align:middle !important;padding-top:2px;padding-left:4px;' onclick='DeleteIteration(" + _std.ID + ");'></a>";
                            itrstring += "</div></div>";

                            if (db.StockCountTeams.Where(x => x.SCIterationID == _std.ID).Count() > 0)
                            {
                                _steams = db.StockCountTeams.Where(x => x.SCIterationID == _std.ID).ToList();

                                foreach (StockCountTeams _st in _steams)
                                {
                                    itrstring += "<div style='width:100%;'>";
                                    itrstring += "<div class='row' style='height:30px !important;margin-left:5px;margin-right:7px;margin-bottom:7px;border-bottom:solid 1px #808080;'>";
                                    itrstring += "<div style='float:left;font-family:Calibri;font-size:20px;color:black;'>" + _st.TeamCode + " - " + _st.UserName + "</div>";
                                    itrstring += "<div style='float:right;margin-top:5px;'><a class='btn btn-danger fa fa-trash' style='padding-top:2px;padding-left:4px;width:20px !important;height:20px !important;vertical-align:middle !important;margin-left:10px;' onclick='DeleteIterationTeam(" + _st.ID + ");'></a></div>";
                                    itrstring += "</div></div>";
                                }
                            }
                            else {
                                itrstring += "<div style='margin:5px;padding:5px 5px 5px 10px;border:solid 1px #808080;border-radius:3px;background-color: #808080 !important;color:white;height:30px !important;width:98% !important;font-family:Calibri;font-size:14px;font-weight:600;'>No Teams Registered</div>";
                            }

                            result[count] = itrstring;
                            count++;
                            itrstring = string.Empty;
                        }
                    }
                    else {
                        result = new string[1];
                        result[0] = "<div style='margin:15px;padding:5px 5px 5px 10px;border:solid 1px #808080;border-radius:10px;background-color: #eae7e7!important;color:black;height:40px !important;width:98% !important;font-family:Calibri;font-size:18px;font-weight:400;'>No Counts Registered for this batch.</ div >";
                    }
                }
                else {
                    result = new string[1];
                    result[0] = "<div style='margin:15px;padding:5px 5px 5px 10px;border:solid 1px #808080;border-radius:10px;background-color: #eae7e7!important;color:black;height:40px !important;width:98% !important;font-family:Calibri;font-size:18px;font-weight:400;'>No Counts Registered for this batch.</ div >";
                }
            }
            return Json(result,JsonRequestBehavior.AllowGet);
        }

        public ActionResult ChangeCountStatus(int CountID)
        {
            int SCID = -1;
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                SCID = db.StockCountIterations.Where(x => x.ID == CountID).FirstOrDefault().SCID.Value;
                if(db.StockCountHeader.Where(x=>x.ID == SCID && x.Status == "C").Count() > 0)
                    return Json(new { success = false, message = "Cannot lock or release count(s) of a closed Batch" }, JsonRequestBehavior.AllowGet);

                //Status of selected Count is Release
                if (db.StockCountIterations.Where(x => x.ID == CountID && x.Status == true).Count() > 0)
                {
                    

                }
                //Stats=us of selected Count is locked
                else {
                    if (db.StockCountIterations.Where(x => x.SCID == SCID && x.Status == true && x.ID != CountID).Count() > 0)
                        return Json(new { success = false, message = "Cannot release this count as another count is in progress" }, JsonRequestBehavior.AllowGet);
                }

                StockCountIterations _scitr = db.StockCountIterations.Where(x => x.ID == CountID).FirstOrDefault();
                bool _currentStatus = _scitr.Status.Value;
                _scitr.Status = !_currentStatus;

                db.StockCountIterations.Attach(_scitr);
                db.Entry(_scitr).Property(x => x.Status).IsModified = true;
                db.SaveChanges();
            }

            return Json(new { success = true, message = "Count Status Changed Successfully" }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region "Stock Count Allocation(s) / Admin Stock Count Sheet"

        public ActionResult AdminStockCountSheet(int TeamID = -1)
        {
            int SCID = -1;
            int CountID = -1;
            string InstanceName = Convert.ToString(Session["InstanceName"]);
            string CompanyName = Convert.ToString(Session["CompanyName"]);
            List<StockCountIterations> _countList = new List<StockCountIterations>();
            List<StockCountTeams> _teamlist = new List<StockCountTeams>();
            AdminStockCountSheetModel _adminsheet = new AdminStockCountSheetModel();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                List<StockCountHeader> _batchlist = new List<StockCountHeader>();
                if (db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0)
                { _batchlist = db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).ToList(); }

                StockCountHeader _row0 = new StockCountHeader();
                _row0.ID = -1; _row0.SCCode = "-- Select Batch Code-- "; _batchlist.Insert(0, _row0);
                ViewBag.BatchList = new SelectList(_batchlist, "ID", "SCCode");

                
                if (TeamID != -1)
                {
                    SCID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCID.Value;
                    CountID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCIterationID.Value;
                }
                else {
                    if (_batchlist.Count() > 1) {
                        SCID = _batchlist[1].ID;
                        if(db.StockCountIterations.Where(x => x.SCID == SCID).Count() > 0)
                            CountID = db.StockCountIterations.Where(x => x.SCID == SCID).FirstOrDefault().ID;

                        if (CountID != -1)
                        {
                            if (db.StockCountTeams.Where(x => x.SCIterationID == CountID).Count() > 0)
                            { TeamID = db.StockCountTeams.Where(x => x.SCIterationID == CountID).FirstOrDefault().ID; }
                        }
                    }
                }

                if (db.StockCountIterations.Where(x => x.SCID == SCID).Count() > 0)
                    _countList = db.StockCountIterations.Where(x => x.SCID == SCID).ToList();

                if (db.StockCountTeams.Where(x => x.SCIterationID == CountID).Count() > 0)
                    _teamlist = db.StockCountTeams.Where(x => x.SCIterationID == CountID).ToList();

                StockCountIterations _sct = new StockCountIterations();
                _sct.ID = -1;
                _sct.IterationName = "-- Select Count --";
                _countList.Insert(0, _sct);
                ViewBag.CountList = new SelectList(_countList, "ID", "IterationName");

                StockCountTeams _team = new StockCountTeams();
                _team.ID = -1;
                _team.TeamCode = "-- Select Team Code --";
                _teamlist.Insert(0, _team);
                ViewBag.TeamList = new SelectList(_teamlist, "ID", "TeamCode");

                ViewBag.SCID = SCID;
                ViewBag.CountID = CountID;
                ViewBag.TeamID = TeamID;

                if (SCID != -1)
                {
                    _adminsheet.ID = SCID;
                    _adminsheet.LocationCode = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().LocationCode.ToString();
                    _adminsheet.SCCode = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().SCCode;
                    _adminsheet.SCDesc = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().SCDesc;
                    _adminsheet.Status = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().Status == "O" ? "Open" : "Closed";
                    _adminsheet.TotalItemCount = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().TotalItemCount.Value;
                }
                else {
                    _adminsheet.ID = SCID;
                    _adminsheet.LocationCode = string.Empty;
                    _adminsheet.SCCode = string.Empty;
                    _adminsheet.SCDesc = string.Empty;
                    _adminsheet.Status = string.Empty;
                    _adminsheet.TotalItemCount = 0;
                }

                if (db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).Count() > 0)
                {
                    _adminsheet.AllocatedItems = new List<StockCountAllocations>();
                    _adminsheet.AllocatedItems = db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).ToList();
                }
                else { _adminsheet.AllocatedItems = new List<StockCountAllocations>(); }
            }
            return View(_adminsheet);
        }

        public ActionResult SelectStockCountItems(int ID)
        {
            Session["TeamID"] = ID;
            int CurrCountID = 0;
            int SCID = 0;
            List<StockCountIterations> _countlist = new List<StockCountIterations>();

            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountTeams.Where(x => x.ID == ID).Count() > 0)
                {
                    CurrCountID = db.StockCountTeams.Where(x => x.ID == ID).FirstOrDefault().SCIterationID.Value;
                    SCID = db.StockCountTeams.Where(x => x.ID == ID).FirstOrDefault().SCID.Value;

                    if (db.StockCountIterations.Where(x => x.SCID == SCID && x.ID < CurrCountID).Count() > 0)
                        _countlist = db.StockCountIterations.Where(x => x.SCID == SCID && x.ID < CurrCountID).ToList();
                }

                StockCountIterations _scitr = new StockCountIterations();
                _scitr.ID = 0;
                _scitr.IterationName = "Count0";
                _countlist.Insert(0, _scitr);
                ViewBag.CountList = new SelectList(_countlist, "ID", "IterationName");
            }
            return View();
        }

        public ActionResult GetAllocatedItemsByTeamID(int TeamID)
        {
            List<StockCountAllocations> _allocateditems = new List<StockCountAllocations>();
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (TeamID != -1)
                    {
                        if (db.StockCountAllocations.Where(x => x.TeamID == TeamID).Count() > 0)
                        { _allocateditems = db.StockCountAllocations.Where(x => x.TeamID == TeamID).ToList(); }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { data = _allocateditems }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { data = _allocateditems }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetBatchCounts(int ID)
        {
            try
            {
                List<StockCountIterations> _countList = GetCountListByBatchId(ID);
                return Json(_countList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }
        
        public List<StockCountIterations> GetCountListByBatchId(int ID)
        {
            List<StockCountIterations> _counts = new List<StockCountIterations>();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountIterations.Where(x => x.SCID == ID).Count() > 0)
                    _counts = db.StockCountIterations.Where(x => x.SCID == ID).ToList();

                StockCountIterations _sct = new StockCountIterations();
                _sct.ID = -1;
                _sct.IterationName = "-- Select Count --";
                _counts.Insert(0, _sct);

                return _counts;
            }

        }

        public JsonResult GetTeamsByCountID(int ID)
        {
            try
            {
                List<StockCountTeams> _teamList = GetTeamListByCountId(ID);
                return Json(_teamList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }


        }

        public List<StockCountTeams> GetTeamListByCountId(int CountID)
        {
            List<StockCountTeams> _teams = new List<StockCountTeams>();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountTeams.Where(x => x.SCIterationID == CountID).Count() > 0)
                    _teams = db.StockCountTeams.Where(x => x.SCIterationID == CountID).ToList();

                StockCountTeams _team = new StockCountTeams();
                _team.ID = -1;
                _team.TeamCode = "-- Select Team Code --";
                _teams.Insert(0, _team);

                return _teams;
            }
        }

        public JsonResult DeleteAllocation(int ID)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    db.StockCountAllocations.Remove(db.StockCountAllocations.Where(x => x.ID == ID).FirstOrDefault());
                    db.SaveChanges();

                    return Json(new { success = true, message = "Selected Item Deleted Successfully" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetSearchItem(int TeamID, int PrevCount, string searchfield, string searchcriteria)
        {
            int SCID = 0;
            List<StockCountDetail> _items = new List<StockCountDetail>();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (PrevCount == 0)
                {
                    if (db.StockCountTeams.Where(x => x.ID == TeamID).Count() > 0)
                        SCID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCID.Value;

                    //select all items from stockcountdetail table
                    if (searchfield == "0")
                    _items = db.StockCountDetail.Where(x => x.SCID == SCID).ToList();

                    //filter by zone code
                    if (searchfield == "1" && !string.IsNullOrEmpty(searchcriteria))
                    {
                        if (db.StockCountDetail.Where(x => x.SCID == SCID && x.ZoneCode.Contains(searchcriteria)).Count() > 0)
                        {
                            _items = (from e in db.StockCountDetail
                                      where e.SCID == SCID && e.ZoneCode.Contains(searchcriteria)
                                      select e).ToList();
                        }
                    }

                    //filter by bin code
                    if (searchfield == "2" && !string.IsNullOrEmpty(searchcriteria))
                    {
                        string[] binfilter = searchcriteria.Split('|');
                        string searchfilter = "";
                        List<string> bins = new List<string>();
                        if (binfilter.Count() == 1)
                        {
                            if (binfilter[0].Contains(","))
                            {
                                //search Items from particular bins separated by comma's
                                bins = searchcriteria.Split(',').ToList(); ;
                                if (db.StockCountDetail.Where(x => bins.Contains(x.BinCode)).Count() > 0)
                                    _items = db.StockCountDetail.Where(x => bins.Contains(x.BinCode)).ToList();
                            }
                            else if (binfilter[0].Contains("?"))
                            {
                                //search Items of a particular bin series
                                searchfilter = binfilter[0].Substring(0, 4).Replace('?', ' ').Trim();
                                if (db.StockCountDetail.Where(x => x.BinCode.Substring(0, 4).Contains(searchfilter)).OrderBy(x => x.BinCode).Count() > 0)
                                    _items = db.StockCountDetail.Where(x => x.BinCode.Substring(0, 4).Contains(searchfilter)).OrderBy(x => x.BinCode).ToList();
                            }
                            else
                            {
                                //search Items of a particular bin
                                searchfilter = binfilter[0].Trim();
                                if (db.StockCountDetail.Where(x => x.BinCode == searchfilter).Count() > 0)
                                    _items = db.StockCountDetail.Where(x => x.BinCode == searchfilter).ToList();
                            }
                        }
                        else {
                            //Search Items in a series of bins
                            bins = searchcriteria.Split('|').ToList();
                            for(int i=0; i <= bins.Count -1; i++) { bins[i] = bins[i].Replace('?', ' ').Trim(); }

                            if (db.StockCountDetail.Where(x => bins.Contains(x.BinCode.Substring(0, 4))).Count() > 0)
                                _items = db.StockCountDetail.Where(x => bins.Contains(x.BinCode.Substring(0, 4))).ToList();
                        }
                    }
                }
            }

            return Json(new { data = _items }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AllocateItems(int TeamID, string ID)
        {
            string[] ItemId = ID.Split(',');
            int ItemID = 0;
            int CountID = 0;
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    for (int i = 0; i <= ItemId.Length - 1; i++)
                    {
                        ItemID = Convert.ToInt32(ItemId[i].Replace('\"', ' ').Trim());
                        StockCountAllocations _item = new StockCountAllocations();

                        StockCountDetail _std = db.StockCountDetail.Where(x => x.ID == ItemID).FirstOrDefault();

                        CountID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCIterationID.Value;
                        if(db.StockCountAllocations.Where(x => x.SCIterationID == CountID && x.ItemNo == _std.ItemNo && x.BinCode == _std.BinCode && x.ZoneCode == _std.ZoneCode && x.LotNo == _std.LotNo && x.ExpirationDate == _std.ExpirationDate).Count() > 0)
                            continue;

                        _item.StockCountID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCID;
                        _item.SCIterationID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCIterationID;
                        _item.SCIterationName = db.StockCountIterations.Where(x => x.ID == _item.SCIterationID).FirstOrDefault().IterationName;
                        _item.AuditorQty = 0;
                        _item.BatchName = _std.BatchName;
                        _item.BinCode = _std.BinCode;
                        _item.CreatedBy = Convert.ToInt32(Session["UserID"]);
                        _item.CreatedDate = DateTime.Now;
                        _item.Description = _std.Description;
                        _item.DocType = "IN";
                        _item.ExpirationDate = _std.ExpirationDate;
                        //_item.FinalQty = 0;
                        _item.ItemNo = _std.ItemNo;
                        _item.LocationCode = _std.LocationCode;
                        _item.LotNo = _std.LotNo;
                        _item.NAVQty = _std.NAVQty;
                        //_item.PhysicalQty = _std.PhyicalQty;
                        _item.TeamID = TeamID;
                        _item.TeamCode = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().TeamCode;
                        _item.TemplateName = _std.TemplateName;
                        _item.UOMCode = _std.UOMCode;
                        _item.WhseDocumentNo = _std.WhseDocumentNo;
                        _item.ZoneCode = _std.ZoneCode;
                        db.StockCountAllocations.Add(_item);
                    }
                    db.SaveChanges();
                }
                return Json(new { success = true, message = "Allocated Successfully" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            
        }

        #endregion

        #region "Stock Count Sheet"

        public JsonResult UserStockCountSheetValidation()
        {
            string InstanceName = Convert.ToString(Session["InstanceName"]);
            string CompanyName = Convert.ToString(Session["CompanyName"]);
            string UserID = SessionPersister.UserName;
            int SCID = 0;
            int ItrID = 0;
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() == 0)
                        return Json(new { success = false, message = "No Open Batches found for this company, Contact your administrator" },JsonRequestBehavior.AllowGet);

                    SCID = db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault().ID;

                    if (db.StockCountIterations.Where(x => x.SCID == SCID && x.Status == true).Count() == 0)
                        return Json(new { success = false, message = "No Count(s) are released for stock counting, Contact your administrator" }, JsonRequestBehavior.AllowGet);

                    ItrID = db.StockCountIterations.Where(x => x.SCID == SCID && x.Status == true).FirstOrDefault().ID;

                    if (db.StockCountTeams.Where(x => x.SCIterationID == ItrID && x.SCID == SCID && x.UserName == UserID).Count() == 0)
                        return Json(new { success = false, message = "You are not part of any current stock count team(s), Contact your administrator" },JsonRequestBehavior.AllowGet);

                    int TeamID = db.StockCountTeams.Where(x => x.SCIterationID == ItrID && x.SCID == SCID && x.UserName == UserID).FirstOrDefault().ID;

                    return Json(new { success = true, message = TeamID }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult StockCountSheet(int TeamID)
        {
            int SCID = -1;
            int CountID = -1;
            AdminStockCountSheetModel _adminsheet = new AdminStockCountSheetModel();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                SCID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCID.Value;
                CountID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCIterationID.Value;

                List<string> _zonecodes = (from e in db.StockCountAllocations
                                           where e.TeamID == TeamID
                                           select e.ZoneCode).Distinct().ToList();

                _zonecodes.Insert(0,"--Select Zone --");

                List<string> _bincode = (from q in db.StockCountAllocations
                                         where q.TeamID == TeamID
                                         select q.BinCode).Distinct().ToList();

                _bincode.Insert(0,"-- Select Bin Code");

                ViewBag.ZoneCodes = new SelectList(_zonecodes);
                ViewBag.BinCodes = new SelectList(_bincode);

                _adminsheet.ID = SCID;
                _adminsheet.LocationCode = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().LocationCode.ToString();
                _adminsheet.SCCode = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().SCCode;
                _adminsheet.SCDesc = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().SCDesc;
                _adminsheet.CountName = db.StockCountAllocations.Where(x => x.TeamID == TeamID).FirstOrDefault().SCIterationName;
                _adminsheet.TeamCode = db.StockCountAllocations.Where(x => x.TeamID == TeamID).FirstOrDefault().TeamCode;
                _adminsheet.Status = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().Status == "O" ? "Open" : "Closed";
                _adminsheet.TotalItemCount = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().TotalItemCount.Value;

                //    if (db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).Count() > 0)
                //    {
                //        _adminsheet.AllocatedItems = new List<StockCountAllocations>();
                //        _adminsheet.AllocatedItems = db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).ToList();
                //    }
                //    else { _adminsheet.AllocatedItems = new List<StockCountAllocations>(); }
                }
            Session["TeamID"] = TeamID;
            return View(_adminsheet);
        }

        public ActionResult StockCountSheetTest(int page =1,string sort= "ItemNo",string sortdir="asc",string search="")
        {
            //int TeamID,
            try
            {
                int pageSize = 5;
                int totalRecord = 0;
                if (page < 1) page = 1;
                int skip = (page * pageSize) - pageSize;
                //Session["TeamID"] = TeamID;
                List<StockCountAllocations> _allocationlist = GetTeamAllocatedItems(search, sort, sortdir, skip, pageSize, out totalRecord);
                ViewBag.TotalRows = totalRecord;
                ViewBag.Search = search;
                return View(_allocationlist);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<StockCountAllocations> GetTeamAllocatedItems(string search, string sort, string sortdir, int skip, int pageSize, out int totalRecord)
        {
            int SCID = -1;
            int CountID = -1;
            List<StockCountAllocations> v = new List<StockCountAllocations>();

            string InstanceName = Convert.ToString(Session["InstanceName"]);
            string CompanyName = Convert.ToString(Session["CompanyName"]);
            string UserID = SessionPersister.UserName;

            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                SCID = db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault().ID;
                CountID = db.StockCountIterations.Where(x => x.SCID == SCID && x.Status == true).FirstOrDefault().ID;
                //SCID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCID.Value;
                //CountID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCIterationID.Value;
                
                int TeamID = db.StockCountTeams.Where(x => x.SCIterationID == CountID && x.SCID == SCID && x.UserName == UserID).FirstOrDefault().ID;
                Session["TeamID"] = TeamID;
                List<string> _zonecodes = (from e in db.StockCountAllocations
                                           where e.TeamID == TeamID
                                           select e.ZoneCode).Distinct().ToList();

                _zonecodes.Insert(0, "--Select Zone --");

                List<string> _bincode = (from q in db.StockCountAllocations
                                         where q.TeamID == TeamID
                                         select q.BinCode).Distinct().ToList();

                _bincode.Insert(0, "-- Select Bin Code");

                ViewBag.ZoneCodes = new SelectList(_zonecodes);
                ViewBag.BinCodes = new SelectList(_bincode);

                if (db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).Count() > 0)
                {
                    v = db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).ToList();
                }
                else { v = new List<StockCountAllocations>(); }

                //var v = db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).ToList();

                var data = (from e in v
                            where e.ItemNo.Contains(search) ||
                                   e.ZoneCode.Contains(search) ||
                                   e.BinCode.Contains(search) select e);


                totalRecord = data.Count();
                data = data.OrderBy(x => x.ItemNo);

                if (pageSize > 0) { data = data.Skip(skip).Take(pageSize).ToList(); }
                return data.ToList();
            }
        }


        [HttpPost]
        public ActionResult UpdateQty(int id, string propertyName, string value)
        {
            bool status =false;
            string message = "";
            //Update data to database
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                StockCountAllocations _sct = db.StockCountAllocations.Find(id);
                if (_sct != null)
                {
                    _sct.PhysicalQty = Convert.ToDecimal(value);
                    db.StockCountAllocations.Attach(_sct);
                    db.Entry(_sct).Property(x => x.PhysicalQty).IsModified = true;
                    db.SaveChanges();
                    status = true;
                }
                else {
                    message = "error!";
                }
            }

            var response = new { value = value, status = status, message = message };
            JObject o = JObject.FromObject(response);
            return Content(o.ToString()); 
        }


        public ActionResult UpdatePhysicalQty(int ID)
        {
            return View();
        }

        [HttpPost]
        public ActionResult UpdatePhysicalQty(StockCountAllocations _sca)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    db.StockCountAllocations.Attach(_sca);
                    db.Entry(_sca).Property(y => y.PhysicalQty).IsModified = true;
                    db.SaveChanges(); 
                    return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            
        }

        public ActionResult SaveAdjustments(int TeamID,string ZoneCode,string BinCode,string ItemNo, string LotNo, string ExpDate,string Qty)
        {
            string InstanceName = Session["InstanceName"].ToString();
            string CompanyName = Session["CompanyName"].ToString();
            List<TestItemList.ItemsList> _obj= new List<ItemsList>();
            try
            {
                if (InstanceName.ToLower() == "test" && CompanyName.ToLower() == "theodor wille intertrade gmbh")
                {
                    _service = new TestItemList.ItemsList_Service();
                    ((TestItemList.ItemsList_Service)_service).UseDefaultCredentials = false;
                    ((TestItemList.ItemsList_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                    _servicefilters = new List<TestItemList.ItemsList_Filter>();
                    ((List<TestItemList.ItemsList_Filter>)_servicefilters).Add(new TestItemList.ItemsList_Filter { Field = TestItemList.ItemsList_Fields.No, Criteria = ItemNo });

                    TestItemList.ItemsList[] _phyjournal = ((TestItemList.ItemsList_Service)_service).ReadMultiple(((List<TestItemList.ItemsList_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                    _obj = _phyjournal.ToList();

                    if (_obj.Count == 0) { return Json(new { success = false, message = "Item not Found in the Item Master, Kindly check" }, JsonRequestBehavior.AllowGet); }
                }

                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    int SCID = 0;
                    StockCountAllocations _sca = new StockCountAllocations();
                    SCID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCID.Value;

                    StockCountDetail _std = db.StockCountDetail.Where(x => x.SCID == SCID).FirstOrDefault();
                    _sca.StockCountID = SCID;
                    _sca.SCIterationID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCIterationID;
                    _sca.SCIterationName = db.StockCountIterations.Where(x => x.ID == _sca.SCIterationID).FirstOrDefault().IterationName;
                    _sca.AuditorQty = 0;
                    _sca.BatchName = _std.BatchName;
                    _sca.BinCode = BinCode;
                    _sca.MemberName = SessionPersister.UserName;
                    _sca.CreatedBy = Convert.ToInt32(Session["UserID"]);
                    _sca.CreatedDate = DateTime.Now;
                    _sca.Description = _obj[0].Description;
                    _sca.DocType = "ADJ";
                    _sca.ExpirationDate = ExpDate;
                    //_sca.FinalQty =;
                    _sca.ItemNo = ItemNo;
                    _sca.LocationCode = _std.LocationCode;
                    _sca.LotNo = LotNo;
                    _sca.NAVQty = 0;
                    _sca.PhysicalQty = Convert.ToDecimal(Qty);
                    _sca.TeamID = TeamID;
                    _sca.TeamCode = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().TeamCode;
                    _sca.TemplateName = _std.TemplateName;
                    _sca.UOMCode = _obj[0].Base_Unit_of_Measure;
                    _sca.WhseDocumentNo = _std.WhseDocumentNo;
                    _sca.ZoneCode = ZoneCode;
                    db.StockCountAllocations.Add(_sca);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region "Manager View"

        public JsonResult ManagerViewValidation()
        {
            string InstanceName = Convert.ToString(Session["InstanceName"]);
            string CompanyName = Convert.ToString(Session["CompanyName"]);
            int SCID = 0;
            //string BatchName = "";

            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.StockCountHeader.Where(x => x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() == 0)
                        return Json(new { success = false, message = "No Batches found for this company, Contact your administrator" }, JsonRequestBehavior.AllowGet);

                    if (db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0)
                        SCID = db.StockCountHeader.Where(x => x.Status == "O" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault().ID;
                    else if(db.StockCountHeader.Where(x => x.Status == "C" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0) {
                        SCID = db.StockCountHeader.Where(x => x.Status == "C" && x.InstanceName == InstanceName && x.CompanyName == CompanyName).FirstOrDefault().ID;
                    }

                    //BatchName = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().SCCode;
                    //if (db.StockCountIterations.Where(x => x.SCID == SCID).Count() == 0)
                    //    return Json(new { success = false, message = "No Count(s) are registered for " + BatchName + "  Batch, Contact your administrator" }, JsonRequestBehavior.AllowGet);

                    //if (db.StockCountAllocations.Where(x => x.StockCountID == SCID).Count() == 0)
                    //    return Json(new { success = false, message = "No Item(s)  allocation are made for " + BatchName + "  Batch, Contact your administrator" }, JsonRequestBehavior.AllowGet);

                    return Json(new { success = true, message = SCID }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ManagerStockCountSheet(int ID)
        {
            StockCountHeader _sch = new StockCountHeader();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            { _sch = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault(); }
            List<StockCountHeader> _batchlist = GetBatchListing();
            ViewBag.BatchList = new SelectList(_batchlist, "ID", "SCCode");
            ViewBag.BatchID = ID;
            return View(_sch);
        }

        public JsonResult GetManagerViewData(int ID)
        {
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection("Data Source=AE01LP83\\SQL2014EXP;Initial Catalog=TWIInventoryPortal;User ID=itsupport;Password=saSql2014"))
            {
                using (SqlCommand cmd = new SqlCommand("GetManagerViewByBatch", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@SCID", ID));
                    con.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(ds);
                    //da.Fill(dt);
                    System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                    Dictionary<string, object> row;


                    if (ds.Tables.Count == 0)
                    { /*row = new Dictionary<string, object>(); rows.Add(row);*/ return Json(rows, JsonRequestBehavior.AllowGet); }

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        row = new Dictionary<string, object>();
                        string colhead = "";
                        int count = 0;
                        int colcount = ds.Tables[0].Columns.Count;
                        bool status = false;
                        string countstatusbtn = "";
                        //int colcount = dt.Columns.Count;
                        foreach (DataColumn col in ds.Tables[0].Columns)
                        {
                            if (count >= 7 && col.ColumnName.ToLower() != "final qty") {
                                DataRow[] dr1 = ds.Tables[1].Select("IterationName ='" + col.ColumnName + "'");
                                if (dr1.Length == 1)
                                {
                                    if (Convert.ToBoolean(dr1[0]["Status"]))
                                    {
                                        status = true;
                                        countstatusbtn = " <a class='btn btn-primary fa fa-unlock' style='margin-left:5px;width:20px;height:20px;padding-left:3px;padding-top:2px;'></a>";
                                    }

                                    else { countstatusbtn = " <a class='btn btn-primary fa fa-lock' style='margin-left:5px;width:17px;height:20px;padding-left:3px; padding-top:2px;'></a>"; }
                                }

                                DataRow[] dr2 = ds.Tables[2].Select("SCIterationName ='" + col.ColumnName + "'");
                                if (dr2.Length == 1)
                                {
                                    countstatusbtn += "<br /><span style'float:left;margin:auto;font-family:'Calibri';font-size:9px;color:blue;'>" + Convert.ToString(dr2[0]["ItemsCounted"]) + " / "+ Convert.ToString(dr2[0]["TotalItems"]) + " counted</span>";
                                }
                                colhead = col.ColumnName + countstatusbtn;
                            }
                            else { colhead = col.ColumnName; }
                            row.Add(colhead, dr[col]);
                            count++;
                        }
                        rows.Add(row);
                    }
                    return Json( rows , JsonRequestBehavior.AllowGet);
                }
            }

           
        }

        #endregion 

        #endregion

        #region "Helper Function(s)"

        #region "General Function(s)"
        void DeleteStockCountDetail(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountDetail.Where(x => x.SCID == ID).Count() > 0)
                {
                    db.StockCountDetail.RemoveRange(db.StockCountDetail.Where(x => x.SCID == ID));
                    db.SaveChanges();
                }
            }
                
        }

        StockCountDetail NewStockCountDetail(DEVPhyInvJournal.PhysicalInvJournal obj, int ID)
        {
            StockCountDetail _std = new StockCountDetail();
            _std.SCID = ID;
            _std.WhseDocumentNo = Convert.ToString(obj.Whse_Document_No);
            _std.ZoneCode = obj.Zone_Code;
            _std.BinCode = obj.Bin_Code;
            _std.ItemNo = obj.Item_No;
            _std.Description = obj.Description;
            _std.LotNo = obj.Lot_No;
            _std.ExpirationDate = obj.Expiration_Date.ToString("dd/MM/yyyy");
            _std.UOMCode = obj.Unit_of_Measure_Code;
            _std.PhyicalQty = obj.Qty_Phys_Inventory;
            _std.NAVQty = obj.Qty_Calculated;
            _std.TemplateName = string.Empty;
            _std.BatchName = obj.Journal_Batch_Name;
            _std.LocationCode = obj.Location_Code;
            _std.CreatedDate = DateTime.Now;
            _std.CreatedBy = Convert.ToInt32(Session["UserID"]);
            return _std;
        }

        StockCountDetail NewStockCountDetail(TESTPhyInvJournal.PhysicalInvJournal obj, int ID)
        {
            StockCountDetail _std = new StockCountDetail();
            _std.SCID = ID;
            _std.WhseDocumentNo = Convert.ToString(obj.Whse_Document_No);
            _std.ZoneCode = obj.Zone_Code;
            _std.BinCode = obj.Bin_Code;
            _std.ItemNo = obj.Item_No;
            _std.Description = obj.Description;
            _std.LotNo = obj.Lot_No;
            _std.ExpirationDate = string.IsNullOrEmpty(Convert.ToString(obj.Expiration_Date)) ? "" : obj.Expiration_Date.ToString("dd/MM/yyyy");
            _std.UOMCode = obj.Unit_of_Measure_Code;
            _std.PhyicalQty = obj.Qty_Phys_Inventory;
            _std.NAVQty = obj.Qty_Calculated;

            _std.TemplateName = string.Empty;
            _std.BatchName = obj.Journal_Batch_Name;
            _std.LocationCode = obj.Location_Code;
            _std.CreatedDate = DateTime.Now;
            _std.CreatedBy = Convert.ToInt32(Session["UserID"]);
            return _std;
        }

        TESTPhyInvJournal.PhysicalInvJournal NewPhyInvJournal(StockCountDetail obj)
        {
            TESTPhyInvJournal.PhysicalInvJournal _obj = new TESTPhyInvJournal.PhysicalInvJournal();
            _obj.Whse_Document_No = Convert.ToString(obj.WhseDocumentNo);
            _obj.Zone_Code = obj.ZoneCode;
            _obj.Bin_Code = obj.BinCode;
            _obj.Item_No = obj.ItemNo;
            _obj.Description = obj.Description;
            _obj.Lot_No = obj.LotNo;
            _obj.Expiration_Date = DateTime.ParseExact(obj.ExpirationDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            _obj.Unit_of_Measure_Code = obj.UOMCode;
            _obj.Qty_Phys_Inventory = Convert.ToDecimal(obj.PhyicalQty);
            _obj.Qty_Calculated = Convert.ToDecimal(obj.NAVQty);
            _obj.Journal_Template_Name = obj.TemplateName;
            _obj.Journal_Batch_Name = obj.BatchName;
            _obj.Location_Code = obj.LocationCode;
            return _obj;
        }

        List<StockCountHeader> GetBatchListing()
        {
            List<StockCountHeader> _batchList = new List<StockCountHeader>();
            string InstanceName = Convert.ToString(Session["InstanceName"]);
            string CompanyName = Convert.ToString(Session["CompanyName"]);
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountHeader.Where(x => x.InstanceName == InstanceName && x.CompanyName == CompanyName).Count() > 0) { _batchList = db.StockCountHeader.Where(x => x.InstanceName == InstanceName && x.CompanyName == CompanyName).ToList(); }
                StockCountHeader _row0 = new StockCountHeader();
                _row0.ID = -1;
                _row0.SCCode = "-- Select Batch -- ";
                _batchList.Insert(0, _row0);
            }
            return _batchList;
        }

        List<User> GetUsersListing()
        {
            List<User> _userList = new List<User>();
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.Users.Count() > 0) { _userList = db.Users.ToList(); }
                User _row0 = new User();
                _row0.UserID = -1;
                _row0.UserName = "-- Select Member -- ";
                _userList.Insert(0, _row0);
            }
            return _userList;
        }

        #endregion

        #region "Helper Function(s) To Pull Data"
        private string ConnectAndPullDataFromLive(int ID,string CompanyName,ref int ItemCount)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.StockCountHeader.Where(x => x.ID == ID).Count() == 0) return Resources.GlobalResource.MsgNoRecordsFound;
                    StockCountHeader _sc = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault();
                    if (_sc.Status == "C") { return Resources.GlobalResource.MsgClosedBatchPull; }

                    if (CompanyName.ToLower() == "theodor wille intertrade usa")
                    {
                        //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                        //List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                        //if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                        //if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                        //    return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                        //if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                        //    return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                        //DeleteStockCountDetail(ID);

                        //foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                        //{ db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                        //ItemCount = _obj.Count;

                    }
                    else if (CompanyName.ToLower() == "theodor wille intertrade gmbh")
                    {
                        _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                            , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                            , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        _servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                        List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                        if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                        if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                            return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                        if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                            return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                        DeleteStockCountDetail(ID);

                        foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                        { db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                        ItemCount = _obj.Count;

                    }
                    else if ((CompanyName.ToLower() == "twi gmbh switzerland"))
                    {
                        //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                        //List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                        //if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                        //if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                        //    return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                        //if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                        //    return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                        //DeleteStockCountDetail(ID);

                        //foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                        //{ db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                        //ItemCount = _obj.Count;
                    }

                    //_sc.TotalItemCount = ItemCount;
                    //db.StockCountHeader.Attach(_sc);
                    //db.Entry(_sc).Property(x => x.TotalItemCount).IsModified = true;
                    //db.SaveChanges();
                    return Resources.GlobalResource.MsgSuccessfullyPulled + ItemCount.ToString() + Resources.GlobalResource.MsgItemsFromERP;
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        private string ConnectAndPullDataFromDev(int ID,string CompanyName,  ref int ItemCount)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.StockCountHeader.Where(x => x.ID == ID).Count() == 0) return Resources.GlobalResource.MsgNoRecordsFound;
                    StockCountHeader _sc = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault();
                    if (_sc.Status == "C") { return Resources.GlobalResource.MsgClosedBatchPull; }

                    if (CompanyName.ToLower() == "theodor wille intertrade usa")
                    {
                        //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                        //List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                        //if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                        //if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                        //    return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                        //if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                        //    return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                        //DeleteStockCountDetail(ID);

                        //foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                        //{ db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                        //ItemCount = _obj.Count;

                    }
                    else if (CompanyName.ToLower() == "theodor wille intertrade gmbh")
                    {
                        _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                            , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                            , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        _servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                        List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                        if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                        if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                            return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                        if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                            return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                        DeleteStockCountDetail(ID);

                        foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                        { db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                        ItemCount = _obj.Count;

                    }
                    else if ((CompanyName.ToLower() == "twi gmbh switzerland"))
                    {
                        //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                        //List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                        //if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                        //if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                        //    return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                        //if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                        //    return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                        //DeleteStockCountDetail(ID);

                        //foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                        //{ db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                        //ItemCount = _obj.Count;
                    }

                    //_sc.TotalItemCount = ItemCount;
                    //db.StockCountHeader.Attach(_sc);
                    //db.Entry(_sc).Property(x => x.TotalItemCount).IsModified = true;
                    //db.SaveChanges();
                    return Resources.GlobalResource.MsgSuccessfullyPulled + ItemCount.ToString() + Resources.GlobalResource.MsgItemsFromERP;
                }
            }
            catch (Exception ex) { return ex.Message; }
        }

        private string ConnectAndPullFromTest(int ID, string CompanyName, ref int ItemCount)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                if (db.StockCountHeader.Where(x => x.ID == ID).Count() == 0) return Resources.GlobalResource.MsgNoRecordsFound;
                StockCountHeader _sc = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault();
                if (_sc.Status == "C") { return Resources.GlobalResource.MsgClosedBatchPull; }

                if (CompanyName.ToLower() == "theodor wille intertrade usa")
                {
                    //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                    //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                    //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                    //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                    //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                    //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                    //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                    //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                    //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                    //List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                    //if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                    //if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                    //    return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                    //if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                    //    return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                    //DeleteStockCountDetail(ID);

                    //foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                    //{ db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                    //ItemCount = _obj.Count;

                }
                else if (CompanyName.ToLower() == "theodor wille intertrade gmbh")
                {
                    _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                    ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                    ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                    _servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                    ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                    ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                    TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                    List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                    if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                    if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                        return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                    if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                        return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                    DeleteStockCountDetail(ID);

                    foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                    { db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                    ItemCount = _obj.Count;

                }
                else if ((CompanyName.ToLower() == "twi gmbh switzerland"))
                {
                    //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                    //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                    //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                    //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                    //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                    //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                    //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                    //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                    //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);
                    //List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                    //if (_obj.Count == 0) { return Resources.GlobalResource.MsgCreateValidBatch; }

                    //if (_obj.Where(x => x.Qty_Phys_Inventory < 0).Count() > 0)
                    //    return _obj.Where(x => x.Qty_Phys_Inventory < 0).Count() + " Item Line(s) is having negative value(s), Kindly rectify in NAV to proceed.";

                    //if (_obj.Where(x => x.Qty_Phys_Inventory > 0).Count() > 0)
                    //    return Resources.GlobalResource.MsgNAVNonZeroPhyQtyMsg;

                    //DeleteStockCountDetail(ID);

                    //foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                    //{ db.StockCountDetail.Add(NewStockCountDetail(obj, ID)); }
                    //ItemCount = _obj.Count;
                }

                _sc.TotalItemCount = ItemCount;
                db.StockCountHeader.Attach(_sc);
                db.Entry(_sc).Property(x => x.TotalItemCount).IsModified = true;
                db.SaveChanges();
                return Resources.GlobalResource.MsgSuccessfullyPulled + ItemCount.ToString() + Resources.GlobalResource.MsgItemsFromERP;
            }
            }
            catch (Exception ex) { return ex.Message; }
        }

        #endregion

        #region "Helper Function(s) To Push Data"

        string ConnectAndPushDataToLive(int ID, string CompanyName)
        {
            return "";
            //string ItemNo = string.Empty;
            //string BinCode = string.Empty;
            //string locationcode = string.Empty;
            //string LotNo = string.Empty;
            //int itemcount = 0;
            //try
            //{
            //    using (InventoryPortalEntities db = new InventoryPortalEntities())
            //    {
            //        if (db.StockCountHeader.Where(x => x.ID == ID).Count() == 0) return Resources.GlobalResource.MsgNoRecordsFound;
            //        StockCountHeader _sc = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault();
            //        if (_sc.Status == "C") { return Resources.GlobalResource.MsgClosedBatchPush; }

            //        //Validation to check whether records pulled from NAV.
            //        if (db.StockCountDetail.Where(x => x.SCID == ID).Count() == 0)
            //            return "No Data Pulled for the selected Batch, cannot proceed.";

            //        //Validation to check whether physical Qty entered for Items is having Negative Values
            //        if ((db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty < 0).Count()) > 0)
            //            return db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty < 0).Count().ToString() + " Item Line(s) is having Negative Value(s), Kindly check cannot proceed further";

            //        //Validation to check whether physical Qty entered for all items or not
            //        if ((db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty >= 0).Count() != db.StockCountDetail.Where(x => x.SCID == ID).Count()))
            //            return "Final Value not posted for all Item Lines, cannot push data to NAV";

            //        if (CompanyName.ToLower() == "theodor wille intertrade gmbh")
            //        {
            //            //Connect to PhysicalInvJournal Service To Post Data
            //            _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
            //            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
            //            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
            //                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
            //                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            //            _servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
            //            ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
            //            ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

            //            TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            //            for (int i = 0; i <= _phyjournal.Length - 1; i++)
            //            {
            //                ItemNo = _phyjournal[i].Item_No;
            //                BinCode = _phyjournal[i].Bin_Code;
            //                locationcode = _phyjournal[i].Location_Code;
            //                LotNo = _phyjournal[i].Lot_No;

            //                StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
            //                _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
            //                itemcount++;
            //            }

            //            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
            //        }
            //        else if (CompanyName.ToLower() == "theodor wille intertrade usa")
            //        {
            //            ////Connect to PhysicalInvJournal Service To Post Data
            //            //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            //            //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

            //            //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            //            //for (int i = 0; i <= _phyjournal.Length - 1; i++)
            //            //{
            //            //    ItemNo = _phyjournal[i].Item_No;
            //            //    BinCode = _phyjournal[i].Bin_Code;
            //            //    locationcode = _phyjournal[i].Location_Code;
            //            //    LotNo = _phyjournal[i].Lot_No;

            //            //    StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
            //            //    _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
            //            //    itemcount++;
            //            //}

            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
            //        }
            //        else if (CompanyName.ToLower() == "twi gmbh switzerland")
            //        {
            //            ////Connect to PhysicalInvJournal Service To Post Data
            //            //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            //            //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

            //            //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            //            //for (int i = 0; i <= _phyjournal.Length - 1; i++)
            //            //{
            //            //    ItemNo = _phyjournal[i].Item_No;
            //            //    BinCode = _phyjournal[i].Bin_Code;
            //            //    locationcode = _phyjournal[i].Location_Code;
            //            //    LotNo = _phyjournal[i].Lot_No;

            //            //    StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
            //            //    _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
            //            //    itemcount++;
            //            //}

            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
            //        }

            //        //After successfull data push to NAV, Close the batch in our Web App System.
            //        _sc.Status = "C";
            //        db.StockCountHeader.Attach(_sc);
            //        db.Entry(_sc).Property(x => x.Status).IsModified = true;
            //        db.SaveChanges();
            //        return "Data Successfully Pushed to NAV";
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return ex.Message;
            //}
        }

        string ConnectAndPushDataToTest(int ID, string CompanyName)
        {
            string ItemNo = string.Empty;
            string BinCode = string.Empty;
            string locationcode = string.Empty;
            string LotNo = string.Empty;
            int itemcount = 0;
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.StockCountHeader.Where(x => x.ID == ID).Count() == 0) return Resources.GlobalResource.MsgNoRecordsFound;
                    StockCountHeader _sc = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault();
                    if (_sc.Status == "C") { return Resources.GlobalResource.MsgClosedBatchPush; }

                    //Validation to check whether records pulled from NAV.
                    if (db.StockCountDetail.Where(x => x.SCID == ID).Count() == 0)
                        return "No Data Pulled for the selected Batch, cannot proceed.";

                    //Validation to check whether physical Qty entered for Items is having Negative Values
                    if ((db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty < 0).Count()) > 0)
                        return db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty < 0).Count().ToString() + " Item Line(s) is having Negative Value(s), Kindly check cannot proceed further";

                    //Validation to check whether physical Qty entered for all items or not
                    if ((db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty >= 0).Count() != db.StockCountDetail.Where(x => x.SCID == ID).Count()))
                        return "Final Value not posted for all Item Lines, cannot push data to NAV";

                    if (CompanyName.ToLower()  == "theodor wille intertrade gmbh")
                    {
                        //Connect to PhysicalInvJournal Service To Post Data
                        _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                            , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                            , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        _servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

                        for (int i = 0; i <= _phyjournal.Length - 1; i++)
                        {
                            ItemNo = _phyjournal[i].Item_No;
                            BinCode = _phyjournal[i].Bin_Code;
                            locationcode = _phyjournal[i].Location_Code;
                            LotNo = _phyjournal[i].Lot_No;

                            StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
                            _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
                            itemcount++;
                        }

                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
                    }
                    else if (CompanyName.ToLower() == "theodor wille intertrade usa")
                    {
                        ////Connect to PhysicalInvJournal Service To Post Data
                        //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

                        //for (int i = 0; i <= _phyjournal.Length - 1; i++)
                        //{
                        //    ItemNo = _phyjournal[i].Item_No;
                        //    BinCode = _phyjournal[i].Bin_Code;
                        //    locationcode = _phyjournal[i].Location_Code;
                        //    LotNo = _phyjournal[i].Lot_No;

                        //    StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
                        //    _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
                        //    itemcount++;
                        //}

                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
                    }
                    else if (CompanyName.ToLower() == "twi gmbh switzerland")
                    {
                        ////Connect to PhysicalInvJournal Service To Post Data
                        //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                        //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                        //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
                        //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

                        //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

                        //for (int i = 0; i <= _phyjournal.Length - 1; i++)
                        //{
                        //    ItemNo = _phyjournal[i].Item_No;
                        //    BinCode = _phyjournal[i].Bin_Code;
                        //    locationcode = _phyjournal[i].Location_Code;
                        //    LotNo = _phyjournal[i].Lot_No;

                        //    StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
                        //    _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
                        //    itemcount++;
                        //}

                        //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
                    }

                    //After successfull data push to NAV, Close the batch in our Web App System.
                    _sc.Status = "C";
                    db.StockCountHeader.Attach(_sc);
                    db.Entry(_sc).Property(x => x.Status).IsModified = true;
                    db.SaveChanges();
                    return "Data Successfully Pushed to NAV";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string ConnectAndPushDataToDEV(int ID, string CompanyName)
        {
            //string ItemNo = string.Empty;
            //string BinCode = string.Empty;
            //string locationcode = string.Empty;
            //string LotNo = string.Empty;
            //int itemcount = 0;
            //try
            //{
            //    using (InventoryPortalEntities db = new InventoryPortalEntities())
            //    {
            //        if (db.StockCountHeader.Where(x => x.ID == ID).Count() == 0) return Resources.GlobalResource.MsgNoRecordsFound;
            //        StockCountHeader _sc = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault();
            //        if (_sc.Status == "C") { return Resources.GlobalResource.MsgClosedBatchPush; }

            //        //Validation to check whether records pulled from NAV.
            //        if (db.StockCountDetail.Where(x => x.SCID == ID).Count() == 0)
            //            return "No Data Pulled for the selected Batch, cannot proceed.";

            //        //Validation to check whether physical Qty entered for Items is having Negative Values
            //        if ((db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty < 0).Count()) > 0)
            //            return db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty < 0).Count().ToString() + " Item Line(s) is having Negative Value(s), Kindly check cannot proceed further";

            //        //Validation to check whether physical Qty entered for all items or not
            //        if ((db.StockCountDetail.Where(x => x.SCID == ID && x.PhyicalQty >= 0).Count() != db.StockCountDetail.Where(x => x.SCID == ID).Count()))
            //            return "Final Value not posted for all Item Lines, cannot push data to NAV";

            //        if (CompanyName.ToLower() == "theodor wille intertrade gmbh")
            //        {
            //            //Connect to PhysicalInvJournal Service To Post Data
            //            _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
            //            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
            //            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
            //                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
            //                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            //            _servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
            //            ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
            //            ((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

            //            TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            //            for (int i = 0; i <= _phyjournal.Length - 1; i++)
            //            {
            //                ItemNo = _phyjournal[i].Item_No;
            //                BinCode = _phyjournal[i].Bin_Code;
            //                locationcode = _phyjournal[i].Location_Code;
            //                LotNo = _phyjournal[i].Lot_No;

            //                StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
            //                _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
            //                itemcount++;
            //            }

            //            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
            //        }
            //        else if (CompanyName.ToLower() == "theodor wille intertrade usa")
            //        {
            //            ////Connect to PhysicalInvJournal Service To Post Data
            //            //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            //            //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

            //            //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            //            //for (int i = 0; i <= _phyjournal.Length - 1; i++)
            //            //{
            //            //    ItemNo = _phyjournal[i].Item_No;
            //            //    BinCode = _phyjournal[i].Bin_Code;
            //            //    locationcode = _phyjournal[i].Location_Code;
            //            //    LotNo = _phyjournal[i].Lot_No;

            //            //    StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
            //            //    _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
            //            //    itemcount++;
            //            //}

            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
            //        }
            //        else if (CompanyName.ToLower() == "twi gmbh switzerland")
            //        {
            //            ////Connect to PhysicalInvJournal Service To Post Data
            //            //_service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
            //            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            //            //_servicefilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });
            //            //((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Location_Code, Criteria = _sc.LocationCode });

            //            //TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(((List<TESTPhyInvJournal.PhysicalInvJournal_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            //            //for (int i = 0; i <= _phyjournal.Length - 1; i++)
            //            //{
            //            //    ItemNo = _phyjournal[i].Item_No;
            //            //    BinCode = _phyjournal[i].Bin_Code;
            //            //    locationcode = _phyjournal[i].Location_Code;
            //            //    LotNo = _phyjournal[i].Lot_No;

            //            //    StockCountDetail _scd = db.StockCountDetail.Where(x => x.SCID == _sc.ID && x.ItemNo == ItemNo && x.BinCode == BinCode && x.LocationCode == locationcode && x.LotNo == LotNo).FirstOrDefault();
            //            //    _phyjournal[i].Qty_Phys_Inventory = Convert.ToDecimal(_scd.PhyicalQty);
            //            //    itemcount++;
            //            //}

            //            //((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UpdateMultiple(ref _phyjournal);
            //        }

            //        //After successfull data push to NAV, Close the batch in our Web App System.
            //        _sc.Status = "C";
            //        db.StockCountHeader.Attach(_sc);
            //        db.Entry(_sc).Property(x => x.Status).IsModified = true;
            //        db.SaveChanges();
            //        return "Data Successfully Pushed to NAV";
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return ex.Message;
            //}
            return "";
        }

        #endregion 

       
        #endregion

    }
}