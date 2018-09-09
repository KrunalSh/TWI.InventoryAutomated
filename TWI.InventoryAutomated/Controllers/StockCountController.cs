using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.Models;
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

        public ActionResult NavDataPull()
        {
            return View();
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
                if (!isDuplicate(_sc.SCCode))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (Convert.ToString(Session["InstanceName"]).ToLower() == "live")
                        {
                            _service = new TESTPhyInvJournal.PhysicalInvJournal_Service();
                            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                            ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");

                            List<TESTPhyInvJournal.PhysicalInvJournal_Filter> _testfilters = new List<TESTPhyInvJournal.PhysicalInvJournal_Filter>();
                            _testfilters.Add(new TESTPhyInvJournal.PhysicalInvJournal_Filter { Field = TESTPhyInvJournal.PhysicalInvJournal_Fields.Whse_Document_No, Criteria = _sc.SCCode });

                            TESTPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(_testfilters.ToArray(), string.Empty, 0);
                            List<TESTPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.ToList();

                            if (_obj.Count == 0)
                            { return Json(new { success = false, message = Resources.GlobalResource.MsgNoRecordsFound }, JsonRequestBehavior.AllowGet); }
                            else
                            {
                                _sc.CreatedDate = DateTime.Now;
                                _sc.CreatedBy = Convert.ToInt32(Session["UserID"]);
                                _sc.TotalItemCount = _obj.Count;
                                _sc.Status = "O";

                                db.StockCountHeader.Add(_sc);
                                db.SaveChanges();

                                foreach (TESTPhyInvJournal.PhysicalInvJournal obj in _obj)
                                {
                                    StockCountDetail _std = new StockCountDetail();
                                    _std.SCID = _sc.ID;
                                    _std.Whse__Document_No_ = Convert.ToString(obj.Whse_Document_No);
                                    _std.Zone_Code = obj.Zone_Code;
                                    _std.Bin_Code = obj.Bin_Code;
                                    _std.Item_No_ = obj.Item_No;
                                    _std.Description = obj.Description;
                                    _std.Lot_No_ = obj.Lot_No;
                                    _std.Expiration_Date = obj.Expiration_Date;
                                    _std.Unit_of_Measure_Code = obj.Unit_of_Measure_Code;
                                    _std.PhyicalQty = obj.Qty_Phys_Inventory;
                                    _std.NAVQty = obj.Qty_Calculated;
                                    _std.Template_Name = obj.Journal_Template_Name;
                                    _std.Batch_Name = obj.Journal_Batch_Name;
                                    _std.Location_Code = obj.Location_Code;
                                    _std.CreatedDate = _sc.CreatedDate;
                                    _std.CreatedBy = _sc.CreatedBy;
                                    db.StockCountDetail.Add(_std);
                                }

                                db.SaveChanges();
                                return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                            }
                        }
                        else
                        {
                            _service = new DEVPhyInvJournal.PhysicalInvJournal_Service();
                            ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                            ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");
                            DEVPhyInvJournal.PhysicalInvJournal[] _phyjournal = ((DEVPhyInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(null, string.Empty, 0);
                            List<DEVPhyInvJournal.PhysicalInvJournal> _obj = _phyjournal.Where(x => x.Whse_Document_No == _sc.SCCode).ToList();

                            if (_obj.Count == 0)
                            { return Json(new { success = false, message = Resources.GlobalResource.MsgNoRecordsFound }, JsonRequestBehavior.AllowGet); }
                            else
                            {
                                _sc.CreatedDate = DateTime.Now;
                                _sc.CreatedBy = Convert.ToInt32(Session["UserID"]);
                                _sc.TotalItemCount = _obj.Count;
                                _sc.Status = "O";

                                db.StockCountHeader.Add(_sc);
                                db.SaveChanges();

                                foreach (DEVPhyInvJournal.PhysicalInvJournal obj in _obj)
                                {
                                    StockCountDetail _std = new StockCountDetail();
                                    _std.SCID = _sc.ID;
                                    _std.Whse__Document_No_ = Convert.ToString(obj.Whse_Document_No);
                                    _std.Zone_Code = obj.Zone_Code;
                                    _std.Bin_Code = obj.Bin_Code;
                                    _std.Item_No_ = obj.Item_No;
                                    _std.Description = obj.Description;
                                    _std.Lot_No_ = obj.Lot_No;
                                    _std.Expiration_Date = obj.Expiration_Date;
                                    _std.Unit_of_Measure_Code = obj.Unit_of_Measure_Code;
                                    _std.PhyicalQty = obj.Qty_Phys_Inventory;
                                    _std.NAVQty = obj.Qty_Calculated;
                                    _std.Template_Name = string.Empty;
                                    _std.Batch_Name = obj.Journal_Batch_Name;
                                    _std.Location_Code = obj.Location_Code;
                                    _std.CreatedDate = _sc.CreatedDate;
                                    _std.CreatedBy = _sc.CreatedBy;
                                    db.StockCountDetail.Add(_std);
                                }

                                db.SaveChanges();
                                return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                            }


                        }
                    }



                }
            }
            catch (Exception e)
            {
                throw e;
            }
          
            return View();
        }





        public ActionResult ViewStockCountDetails(int ID)
        {
            return View();
        }



        #region "Helper Function(s)"
        public bool isDuplicate(string _scCode)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (db.StockCountHeader.AsNoTracking().Where(x => x.SCCode == _scCode).Count() > 0) return true;
                return false;
                
                //if (_sc.ID != 0)
                //    ID = db.StockCountHeader.AsNoTracking().Where(x => x.SCCode == _sc.SCCode  && x.ID != _sc.ID).FirstOrDefault().ID;
                //else
                //    ID = db.StockCountHeader.AsNoTracking().Where(x => x.SCCode == _sc.SCCode && x.ID != _sc.ID).FirstOrDefault().ID;
                //if (ID == -1)
                //    return false;
                //else
                //    return true;
            }
        }
        #endregion





    }
}