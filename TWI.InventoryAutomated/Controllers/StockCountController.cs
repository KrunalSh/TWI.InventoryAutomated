using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.DEVPhyInvJournal;
using TWI.InventoryAutomated.TESTPhyInvJournal;
using System.Net;
using System.Globalization;

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

        #region "Counts & Iterations Event(s)"
        
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

            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                List<StockCountHeader> _batchlist = new List<StockCountHeader>();
                if (db.StockCountHeader.Where(x => x.Status == "O").Count() > 0) { _batchlist = db.StockCountHeader.Where(x => x.Status == "O").ToList(); }
                StockCountHeader _row0 = new StockCountHeader();
                _row0.ID = -1; _row0.SCCode = "-- Select Batch -- "; _batchlist.Insert(0, _row0);
                ViewBag.BatchList = new SelectList(_batchlist, "ID", "SCCode");

                List<StockCountIterations> _countList = new List<StockCountIterations>();
                List<StockCountTeams> _teamlist = new List<StockCountTeams>();
                AdminStockCountSheetModel _adminsheet = new AdminStockCountSheetModel();
                if (TeamID != -1)
                {
                    SCID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCID.Value;
                    CountID = db.StockCountTeams.Where(x => x.ID == TeamID).FirstOrDefault().SCIterationID.Value;

                    if (db.StockCountIterations.Where(x => x.SCID == SCID).Count() > 0)
                        _countList = db.StockCountIterations.Where(x => x.SCID == SCID).ToList();

                    if (db.StockCountTeams.Where(x => x.SCIterationID == CountID).Count() > 0)
                        _teamlist = db.StockCountTeams.Where(x => x.SCIterationID == CountID).ToList();

                    _adminsheet.ID = SCID;
                    _adminsheet.LocationCode = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().LocationCode;
                    _adminsheet.SCCode = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().SCCode;
                    _adminsheet.SCDesc = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().SCDesc;
                    _adminsheet.Status = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().Status;
                    _adminsheet.TotalItemCount = db.StockCountHeader.Where(x => x.ID == SCID).FirstOrDefault().TotalItemCount.Value;

                    _adminsheet.AllocatedItems = db.StockCountAllocations.Where(x => x.StockCountID == SCID && x.SCIterationID == CountID && x.TeamID == TeamID).ToList();

                }

                ViewBag.SCID = SCID;
                ViewBag.CountID = CountID;

                StockCountIterations _sct = new StockCountIterations();
                _sct.ID = -1;
                _sct.IterationName = "-- Select Count --";
                _countList.Insert(0, _sct);
                ViewBag.CountList = new SelectList(_countList, "ID", "IterationName");

                StockCountTeams _team = new StockCountTeams();
                _team.ID = -1;
                _team.TeamCode = "-- Select Team --";
                _teamlist.Insert(0, _team);
                ViewBag.TeamList = new SelectList(_teamlist, "ID", "TeamCode");
            }
            return View();
        }

        #endregion 
        
        #endregion

        #region "Helper Function(s)"

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

        StockCountDetail NewStockCountDetail(DEVPhyInvJournal.PhysicalInvJournal obj,int ID)
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
            _std.ExpirationDate = string.IsNullOrEmpty(Convert.ToString(obj.Expiration_Date)) ? "" :  obj.Expiration_Date.ToString("dd/MM/yyyy");
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
            _obj.Whse_Document_No  = Convert.ToString(obj.WhseDocumentNo);
            _obj.Zone_Code  = obj.ZoneCode;
            _obj.Bin_Code = obj.BinCode;
            _obj.Item_No = obj.ItemNo ;
            _obj.Description  = obj.Description;
            _obj.Lot_No  = obj.LotNo;
            _obj.Expiration_Date = DateTime.ParseExact(obj.ExpirationDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            _obj.Unit_of_Measure_Code = obj.UOMCode;
            _obj.Qty_Phys_Inventory = Convert.ToDecimal(obj.PhyicalQty);
            _obj.Qty_Calculated = Convert.ToDecimal(obj.NAVQty);
             _obj.Journal_Template_Name = obj.TemplateName;
            _obj.Journal_Batch_Name = obj.BatchName ;
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

    }
}