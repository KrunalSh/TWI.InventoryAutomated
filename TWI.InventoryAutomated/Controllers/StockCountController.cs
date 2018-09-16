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

namespace TWI.InventoryAutomated.Controllers
{
    public class StockCountController : Controller
    {
        #region "Global Variables"
            object _service;
        #endregion 

        // GET: StockCount
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult NavDataPull(int ID =0)
        {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<StockCountHeader> _batchlist = db.StockCountHeader.ToList();
                    StockCountHeader _row0 = new StockCountHeader();
                    _row0.ID = -1;
                    _row0.SCCode = "-- Select Batch -- ";
                    _batchlist.Insert(0, _row0);
                    ViewBag.BatchList = new SelectList(_batchlist, "ID", "SCCode");
                    if (ID == 0) { return View(CommonServices.GetOpenStockCountBatch()); }
                    else { return Json(new { data = CommonServices.GetStockCountDetailsById(ID) }, JsonRequestBehavior.AllowGet); }
                }
        }

        public ActionResult GetSockCountList()
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<GetStockCountList_Result> _scList = db.GetStockCountList().ToList<GetStockCountList_Result>();
                    return Json(new { data = _scList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw ex;
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
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    //Validation to check whether duplicate Batch Code is not being entered.
                    if (db.StockCountHeader.AsNoTracking().Where(x => x.SCCode == _sc.SCCode && x.LocationCode == _sc.LocationCode).Count() > 0)
                        return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);

                    //Validation to check whether stock count is ongoing for any Batch in the system.
                    if (db.StockCountHeader.AsNoTracking().Where(x => x.Status == "O").Count() > 0)
                        return Json(new { success = false, message = Resources.GlobalResource.MsgOngoingBatchError }, JsonRequestBehavior.AllowGet);

                    _sc.CreatedDate = DateTime.Now;
                    _sc.CreatedBy = Convert.ToInt32(Session["UserID"]);
                    _sc.TotalItemCount = 0;
                    _sc.Status = "O";

                    db.StockCountHeader.Add(_sc);
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);

                    //Code to be moved to detail screen where pull, push everything will happen
                    //if (Convert.ToString(Session["InstanceName"]).ToLower() == "live")
                    //    {
                    //        _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                    //        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                    //        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");

                    //        List<TESTPhyInvJournal.PhysicalInvJournal_Filter> _testfilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                    //        _testfilters.Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });

                    //        TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(_testfilters.ToArray(), string.Empty, 0);
                    //        List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                    //        if (_obj.Count == 0)
                    //        { return Json(new { success = false, message = Resources.GlobalResource.MsgNoRecordsFound }, JsonRequestBehavior.AllowGet); }
                    //        else
                    //        {


                    //            foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                    //            {
                    //                StockCountDetail _std = new StockCountDetail();
                    //                _std.SCID = _sc.ID;
                    //                _std.WhseDocumentNo = Convert.ToString(obj.Whse_Document_No);
                    //                _std.ZoneCode = obj.Zone_Code;
                    //                _std.BinCode = obj.Bin_Code;
                    //                _std.ItemNo = obj.Item_No;
                    //                _std.Description = obj.Description;
                    //                _std.LotNo = obj.Lot_No;
                    //                _std.ExpirationDate = obj.Expiration_Date;
                    //                _std.UOMCode = obj.Unit_of_Measure_Code;
                    //                _std.PhyicalQty = obj.Qty_Phys_Inventory;
                    //                _std.NAVQty = obj.Qty_Calculated;
                    //                _std.TemplateName = obj.Journal_Template_Name;
                    //                _std.BatchName = obj.Journal_Batch_Name;
                    //                _std.LocationCode = obj.Location_Code;
                    //                _std.CreatedDate = _sc.CreatedDate;
                    //                _std.CreatedBy = _sc.CreatedBy;
                    //                db.StockCountDetail.Add(_std);
                    //            }

                    //            db.SaveChanges();
                    //            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        _service = new DEVPhyInvJournal.PhysicalInvJournal_Service();
                    //        ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                    //        ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");
                    //        DEVPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(null, string.Empty, 0);
                    //        List<DEVPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.Where(x => x.Whse_Document_No == _sc.SCCode).ToList();

                    //        if (_obj.Count == 0)
                    //        { return Json(new { success = false, message = Resources.GlobalResource.MsgNoRecordsFound }, JsonRequestBehavior.AllowGet); }
                    //        else
                    //        {
                    //            _sc.CreatedDate = DateTime.Now;
                    //            _sc.CreatedBy = Convert.ToInt32(Session["UserID"]);
                    //            _sc.TotalItemCount = _obj.Count;
                    //            _sc.Status = "O";

                    //            db.StockCountHeader.Add(_sc);
                    //            db.SaveChanges();

                    //            foreach (DEVPhyInvJournal.PhysicalInvJournal obj in _obj)
                    //            {
                    //                StockCountDetail _std = new StockCountDetail();
                    //                _std.SCID = _sc.ID;
                    //                _std.WhseDocumentNo = Convert.ToString(obj.Whse_Document_No);
                    //                _std.ZoneCode = obj.Zone_Code;
                    //                _std.BinCode = obj.Bin_Code;
                    //                _std.ItemNo = obj.Item_No;
                    //                _std.Description = obj.Description;
                    //                _std.LotNo = obj.Lot_No;
                    //                _std.ExpirationDate = obj.Expiration_Date;
                    //                _std.UOMCode = obj.Unit_of_Measure_Code;
                    //                _std.PhyicalQty = obj.Qty_Phys_Inventory;
                    //                _std.NAVQty = obj.Qty_Calculated;
                    //                _std.TemplateName = string.Empty;
                    //                _std.BatchName = obj.Journal_Batch_Name;
                    //                _std.LocationCode = obj.Location_Code;
                    //                _std.CreatedDate = _sc.CreatedDate;
                    //                _std.CreatedBy = _sc.CreatedBy;
                    //                db.StockCountDetail.Add(_std);
                    //            }

                    //            db.SaveChanges();
                    //            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                    //        }
                    //    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            //return View();
        }

        //public ActionResult ViewStockCountDetails(int ID)
        //{
        //    return View(CommonServices.GetStockCountDetailsById(ID));
        //}
        
        public ActionResult GetPhyJournalData(int ID)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (db.StockCountHeader.Where(x => x.ID == ID).Count() == 0) return Content(" No Records found for the selected Batch");

                    StockCountHeader _sc = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault();
                    DeleteStockCountDetail(ID);


                    //Code to be moved to detail screen where pull, push everything will happen
                    if (Convert.ToString(Session["InstanceName"]).ToLower() == "live")
                    {
                        _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");

                        List<TESTPhyInvJournal.PhysicalInvJournal_Filter> _testfilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                        _testfilters.Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });

                        TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(_testfilters.ToArray(), string.Empty, 0);
                        List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                        if (_obj.Count == 0) {   return Content("No Records Found for the selected Batch"); }
                                                
                        foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                        {
                                db.StockCountDetail.Add(NewStockCountDetail(obj,ID));
                        }


                        _sc.TotalItemCount = _obj.Count;
                        db.StockCountHeader.Attach(_sc);
                        db.Entry(_sc).Property(x => x.TotalItemCount).IsModified = true;
                        db.SaveChanges();
                        return Content("Successfully pulled " + _obj.Count.ToString() + " Items for the selected batch from NAV ERP");
                    }
                    else
                    {
                        _service = new DEVPhyInvJournal.PhysicalInvJournal_Service();
                        ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                        ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");
                        DEVPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(null, string.Empty, 0);
                        List<DEVPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.Where(x => x.Whse_Document_No == _sc.SCCode).ToList();

                        if (_obj.Count == 0) { return Content("No Records Found for the selected Batch"); }
                        
                        foreach (DEVPhyInvJournal.PhysicalInvJournal obj in _obj)
                        {
                            db.StockCountDetail.Add(NewStockCountDetail(obj, ID));
                        }

                        _sc.TotalItemCount = _obj.Count;
                        db.StockCountHeader.Attach(_sc);
                        db.Entry(_sc).Property(x => x.TotalItemCount).IsModified = true;
                        db.SaveChanges();
                        return Content("Successfully pulled " + _obj.Count.ToString() + " Items for the selected batch from NAV ERP");
                    }
                }
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
            
        }


        public JsonResult GetSockCountDetailByID(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                return Json(new { data = CommonServices.GetStockCountDetailByID(ID) }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetStockCountHeaderDetailByID(int ID)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                StockCountHeader _schd = db.StockCountHeader.Where(x => x.ID == ID).FirstOrDefault<StockCountHeader>();
                return Json(new { data = _schd }, JsonRequestBehavior.AllowGet);
            }
        }

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

        #endregion
    }
}