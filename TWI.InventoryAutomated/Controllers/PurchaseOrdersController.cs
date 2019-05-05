using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace TWI.InventoryAutomated.Controllers
{
    public class PurchaseOrdersController : Controller
    {
        #region "Global Variables"
        object _service;
        object _servicefilters;
        //DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder _purchaseorders;
        //object _purchaseorders;

        POHeader poheader;

        #endregion

        #region "Action Method(s)"
        // GET: PurchaseOrders
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetPOHeaderDetails(string PONo)
        {
            string _resultmsg = string.Empty;

            if (string.IsNullOrEmpty(PONo))
            { return Json(new { success = false, message = "Enter a PO Number to Pull Data from NAV" }, JsonRequestBehavior.AllowGet); }

            poheader = GetPOData(PONo, Convert.ToString(Session["InstanceName"]), Convert.ToString(Session["CompanyName"]),ref _resultmsg);

            if (poheader == null)
            { return Json(new { success = false, message = _resultmsg }, JsonRequestBehavior.AllowGet); }

            return Json(new { success = true, message = poheader }, JsonRequestBehavior.AllowGet);

            #region "Old Code"
            //_purchaseorders = GetPOData(PONo);

            //if (_purchaseorders == null)
            //{ return Json(new { success = false, message = "Invalid PO Number Enter, Kindly check." }, JsonRequestBehavior.AllowGet); }

            //if (_purchaseorders.Status == DEVGMBHPurchaseOrder.Status.Open)
            //{ return Json(new { success = false, message = "Cannot receive a PO with status open" }, JsonRequestBehavior.AllowGet); }

            //if (_purchaseorders.PurchLines.Where(x => x.Quantity_Received < x.Quantity).Count() == 0)
            //{ return Json(new { success = false, message = "All items received for the entered Purchase Order." }, JsonRequestBehavior.AllowGet); }

            //POHeader _pohead = new POHeader();

            //_pohead.LocationCode = string.IsNullOrEmpty(Convert.ToString(_purchaseorders.Location_Code)) ? "" : Convert.ToString(_purchaseorders.Location_Code);
            //_pohead.VendorName = string.IsNullOrEmpty(Convert.ToString(_purchaseorders.Buy_from_Vendor_Name)) ? "" : Convert.ToString(_purchaseorders.Buy_from_Vendor_Name);
            //_pohead.PONo = string.IsNullOrEmpty(Convert.ToString(_purchaseorders.No)) ? "" : Convert.ToString(_purchaseorders.No);
            //_pohead.OrderDate = string.IsNullOrEmpty(Convert.ToString(_purchaseorders.Order_Date)) ? "" : Convert.ToDateTime(_purchaseorders.Order_Date).ToString("dd/MM/yyyy");
            //_pohead.ContainerNo = string.IsNullOrEmpty(Convert.ToString(_purchaseorders.Vendor_Shipment_No)) ? "" : Convert.ToString(_purchaseorders.Vendor_Shipment_No);
            //_pohead.ReceiptNo = GetReceiptData(PONo);
            //_pohead.PostingDate = string.IsNullOrEmpty(Convert.ToString(_purchaseorders.Posting_Date)) ? DateTime.Now.ToString("dd/MM/yyyy") : Convert.ToDateTime(_purchaseorders.Posting_Date).ToString("dd/MM/yyyy");

            #endregion
        }

        public ActionResult GetPODetails(string PONo)
        {
            List<POLines> _polines = new List<POLines>();
            string _resultmsg = string.Empty;
            _polines = GetPOLines(PONo, Convert.ToString(Session["InstanceName"]), Convert.ToString(Session["CompanyName"]), ref _resultmsg);
            return Json(new { data = _polines }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ItemTracking(string ItemNo, string PONo)
        {
            string _item = string.Empty;
           _item = GetItemInformation(ItemNo,Convert.ToString(Session["InstanceName"]),Convert.ToString(Session["CompanyName"]));
            //string _ItemNo = string.IsNullOrEmpty(Convert.ToString(_item.No)) ? "" : Convert.ToString(_item.No);
            //string _Desc = string.IsNullOrEmpty(Convert.ToString(_item.Description)) ? "" : Convert.ToString(_item.Description);
            //DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder purchaseorders = GetPOData(PONo);
            //if (purchaseorders.PurchLines.Where(x => x.No == ItemNo).Count() > 0)
            //    QtyReceived = purchaseorders.PurchLines.Where(x => x.No == ItemNo).FirstOrDefault().Quantity_Received;

            string _msg = string.Empty;
            List<POLines> _polines = GetPOLines(PONo, Convert.ToString(Session["InstanceName"]), Convert.ToString(Session["CompanyName"]),ref _msg);

            decimal QtyReceived = 0;

            if (_polines.Where(x => x.ItemNo == ItemNo).Count() > 0)
            { QtyReceived = _polines.Where(x => x.ItemNo == ItemNo).FirstOrDefault().QuantityReceived; }

            return Json(new { ItemNo = _item, QtyRec = Convert.ToString(QtyReceived) }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetItemTrackingLine(string PONo, string ItemNo)
        {
            #region "Old Code"
            //DEVGMBHItemCard.TWIWMS_Item _item = GetItemInformation(ItemNo);
            //string Desc = string.IsNullOrEmpty(Convert.ToString(_item.Description)) ? "" : Convert.ToString(_item.Description);
            //DEVGMBHReservationEntries.TWIWMS_ReservationEntries[] _res_entries = null;
            //_res_entries = GetReservationEntries(PONo, ItemNo,Convert.ToString(Session["InstanceName"]),Convert.ToString(Session["CompanyName"]));
            #endregion

            List<ReservationEntries> _reservationEntries = new List<ReservationEntries>();
            _reservationEntries = GetReservationEntries(PONo, ItemNo, Convert.ToString(Session["InstanceName"]), Convert.ToString(Session["CompanyName"]));
            return Json(new { data = _reservationEntries }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region "Helper Function(s)"

        private POHeader GetPOData(string PONo,string InstanceName,string CompanyName,ref string _msg)
        {
            POHeader POHead = new POHeader();
            try
            {
                switch (InstanceName.ToLower())
                {
                    //case "live":
                    //    _purchaseorders =  GetPODataByCompanyFromLive(PONo, CompanyName);
                    //    break;
                    case "test":
                    case "dev":
                         POHead = GetPODataByCompanyFromDev(PONo, CompanyName, ref _msg);
                        break;
                    //case "test":
                    //    _purchaseorders = GetPODataByCompanyFromTest(PONo, CompanyName);
                    //    break;
                }
                return POHead;
            }
            catch (Exception ex)
            {
                _msg = ex.Message;
                return null;
            }
        }

        private List<POLines> GetPOLines(string PONo, string InstanceName, string CompanyName, ref string _msg)
        {
            List<POLines> _polines = new List<POLines>();
            try
            {
                switch (InstanceName.ToLower())
                {
                    case "test":
                    case "dev":
                        _polines = GetPOLinesByCompanyFromDev(PONo, CompanyName, ref _msg);
                        break;
                }

                return _polines;
            }
            catch (Exception ex)
            {
                _msg = ex.Message;
                return null;
            }
        }

        private decimal GetItemQtyHandled(string PONo, string ItemNo, string InstanceName, string Company)
        {
            decimal _resqty = 0;
            try
            {
                switch (InstanceName.ToLower())
                {
                    case "live":
                    case "test":
                    case "dev":
                        _resqty = GetItemQtyHandledFromDEV(PONo, ItemNo, Company);
                        break;
                }

                return _resqty;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        private string GetItemInformation(string ItemNo, string InstanceName, string CompanyName)
        {
            string itemdesc = string.Empty;
            try
            {
                switch (InstanceName.ToLower())
                {
                    case "live":
                    case "test":
                    case "dev":
                        itemdesc = GetItemDescFromDEV(ItemNo, CompanyName);
                        break;
                }
                return itemdesc;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private List<ReservationEntries> GetReservationEntries(string PONo, string ItemNo, string InstanceName, string CompanyName)
        {
            List<ReservationEntries> _resentries = new List<ReservationEntries>();
            try
            {
                switch (InstanceName.ToLower())
                {
                    case "live":
                    case "test":
                    case "dev":
                        _resentries = GetReservationEntriesFromDEV(PONo, ItemNo, CompanyName);
                        break;
                }
                return _resentries;
            }
            catch (Exception ex)
            {
                return _resentries;
            }
        }



        #region "DEV Instance -Functions"

        POHeader GetPODataByCompanyFromDev(string PONo, string Company,ref string msg)
        {
            POHeader _pohead = new POHeader();

            if (Company.ToLower() == "theodor wille intertrade gmbh")
            {
                DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder _gmbhPurchaseOrder = GetPOObjectFromDEV(PONo);

                if (_gmbhPurchaseOrder == null)
                { msg = "Invalid PO Number Enter, Kindly check."; return _pohead = null;}

                if (_gmbhPurchaseOrder.Status == DEVGMBHPurchaseOrder.Status.Open)
                {  msg = "Cannot receive a PO with status open"; return _pohead = null; }

                // Code if Warehouse receipt does not validate whether all items are received & posted or not.
                if (_gmbhPurchaseOrder.PurchLines.Where(x => x.Quantity_Received < x.Quantity).Count() == 0)
                { msg = "All items received for the entered Purchase Order."; return _pohead = null; }

                _pohead = new POHeader();

                _pohead.LocationCode = string.IsNullOrEmpty(Convert.ToString(_gmbhPurchaseOrder.Location_Code)) ? "" : Convert.ToString(_gmbhPurchaseOrder.Location_Code);
                _pohead.VendorName = string.IsNullOrEmpty(Convert.ToString(_gmbhPurchaseOrder.Buy_from_Vendor_Name)) ? "" : Convert.ToString(_gmbhPurchaseOrder.Buy_from_Vendor_Name);
                _pohead.PONo = string.IsNullOrEmpty(Convert.ToString(_gmbhPurchaseOrder.No)) ? "" : Convert.ToString(_gmbhPurchaseOrder.No);
                _pohead.OrderDate = string.IsNullOrEmpty(Convert.ToString(_gmbhPurchaseOrder.Order_Date)) ? "" : Convert.ToDateTime(_gmbhPurchaseOrder.Order_Date).ToString("dd/MM/yyyy");
                _pohead.ContainerNo = string.IsNullOrEmpty(Convert.ToString(_gmbhPurchaseOrder.Vendor_Shipment_No)) ? "" : Convert.ToString(_gmbhPurchaseOrder.Vendor_Shipment_No);
                _pohead.ReceiptNo = GetReceiptData(PONo,Convert.ToString(Session["InstanceName"]),Company);
                _pohead.PostingDate = string.IsNullOrEmpty(Convert.ToString(_gmbhPurchaseOrder.Posting_Date)) ? DateTime.Now.ToString("dd/MM/yyyy") : Convert.ToDateTime(_gmbhPurchaseOrder.Posting_Date).ToString("dd/MM/yyyy");
            }

            return _pohead;
        }

        private string GetReceiptData(string PONo, string InstanceName, string Company)
        {
            #region "Old Code"

            //string ReceiptNo = string.Empty;
            //DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines[] _reclines = null;
            ////Code to Initialize Warehouse Receipt Lines Webservice.
            //_service = new DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service();
            //((DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service)_service).UseDefaultCredentials = false;
            //((DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
            //    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            ////Code to set filters for the webservice. Receipt Lines will be pulled by Source_No Field and No Field will be return as Receipt No to Front End. 
            //_servicefilters = new List<DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter>();
            //((List<DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter>)_servicefilters).Add(new DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter { Field = DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Fields.Source_No, Criteria = PONo });

            ////Code to read / pull receipt lines through webservice from NAV ERP.
            //_reclines = ((DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service)_service).ReadMultiple(((List<DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            ////Code to check whether any data received. If data received then set the Receipt No field
            //// else ReceiptNo field remains blank.
            //if (_reclines != null && _reclines.Count() > 0)
            //    ReceiptNo = string.IsNullOrEmpty(Convert.ToString(_reclines[0].No)) ? "" : Convert.ToString(_reclines[0].No);

            #endregion

            string ReceiptNo = "";
            try
            {
                switch (InstanceName.ToLower())
                {
                    case "live":
                    case "test":
                    case "dev":
                        ReceiptNo = GetReceiptNoFromDEV(PONo, Company);
                        break;
                }
                return ReceiptNo;
            }
            catch (Exception ex)
            {
                return ReceiptNo;
            }
        }

        List<POLines> GetPOLinesByCompanyFromDev(string PONo, string Company, ref string msg)
        {
            List<POLines> _polines = new List<POLines>();

            if (Company.ToLower() == "theodor wille intertrade gmbh")
            {
                DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder _gmbhPurchaseOrder = GetPOObjectFromDEV(PONo);

                if (_gmbhPurchaseOrder == null)
                { msg = "Invalid PO Number Enter, Kindly check."; return _polines; }

                if (_gmbhPurchaseOrder == null || _gmbhPurchaseOrder.No != PONo)
                { _gmbhPurchaseOrder = GetPOObjectFromDEV(PONo); }

                foreach (DEVGMBHPurchaseOrder.Purchase_Order_Line _poline in _gmbhPurchaseOrder.PurchLines)
                {
                    if (_poline.Quantity_Received < _poline.Quantity)
                    {
                        decimal _resqty = 0;
                        POLines _obj = new POLines();
                        _obj.ItemNo = _poline.No;
                        _obj.Description = _poline.Description;
                        _obj.NSNo = string.IsNullOrEmpty(Convert.ToString(_poline.Cross_Reference_No)) ? "" : Convert.ToString(_poline.Cross_Reference_No);
                        _resqty = GetItemQtyHandled(PONo, _poline.No,Convert.ToString(Session["InstanceName"]),Convert.ToString(Session["CompanyName"]));
                        _obj.Quantity = Convert.ToString(_poline.Quantity_Received + _resqty) + " / " + Convert.ToString(_poline.Quantity);
                        _obj.QuantityReceived = string.IsNullOrEmpty(Convert.ToString(_poline.Quantity_Received)) ? 0 : Convert.ToDecimal(_poline.Quantity_Received); 
                        if ((_poline.Quantity_Received + _resqty) == _poline.Quantity) _obj.Received = "Y"; else _obj.Received = "N";
                        _polines.Add(_obj);
                    }
                }
            }

            return _polines;
        }

        private decimal GetItemQtyHandledFromDEV(string PONo, string ItemNo, string Company)
        {
            if (Company.ToLower() == "theodor wille intertrade gmbh")
            {
                //DEVGMBHReservationEntries.TWIWMS_ReservationEntries[] _reclines = GetReservationEntriesFromDEV(PONo, ItemNo);
                List<ReservationEntries> reservationEntries = GetReservationEntriesFromDEV(PONo, ItemNo,Company);
                if (reservationEntries.Count() == 0) return 0; else return Convert.ToDecimal(reservationEntries.Sum(x => x.Quantity));
            }

            return 0;
        }

        private string GetItemDescFromDEV(string ItemNo, string Company)
        {
            if (Company.ToLower() == "theodor wille intertrade gmbh")
            {
                DEVGMBHItemCard.TWIWMS_Item _item = GetItemInformation(ItemNo);

                if (_item == null) { return ""; }
                else
                {
                    string itemno = string.IsNullOrEmpty(Convert.ToString(_item.No)) ? "" : Convert.ToString(_item.No);
                    string desc = string.IsNullOrEmpty(Convert.ToString(_item.Description)) ? "" : Convert.ToString(_item.Description);
                    return itemno + " - " + desc;
                }
            }

            return "";
        }

        #endregion 

        #region "DEV Instance - Web Service Calls"

        private DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder GetPOObjectFromDEV(string PONo)
        {
            _service = new DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder_Service();
            ((DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder_Service)_service).UseDefaultCredentials = false;
            ((DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            return ((DEVGMBHPurchaseOrder.TWIWMS_PurchaseOrder_Service)_service).Read(PONo);
        }

        private string GetReceiptNoFromDEV(string PONo, string Company)
        {
            if (Company.ToLower() == "theodor wille intertrade gmbh")
            {
                //Code to Initialize Warehouse Receipt Code Unit Webservice to get Receipt No.
                _service = new DEVGMBHWarehouseReceiptCU.TWIWMSInbound();

                ((DEVGMBHWarehouseReceiptCU.TWIWMSInbound)_service).UseDefaultCredentials = false;
                ((DEVGMBHWarehouseReceiptCU.TWIWMSInbound)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                return ((DEVGMBHWarehouseReceiptCU.TWIWMSInbound)_service).GetWhseReceipt(PONo);
            }

            return "";
            
        }

        private List<ReservationEntries> GetReservationEntriesFromDEV(string PONo, string ItemNo,string Company)
        {
            List<ReservationEntries> reservationentries = new List<ReservationEntries>();

            if (Company.ToLower() == "theodor wille intertrade gmbh")
            {
                _service = new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service();
                ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).UseDefaultCredentials = false;
                ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                    , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                    , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

                _servicefilters = new List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>();
                ((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).Add(new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter { Field = DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Fields.Source_ID, Criteria = PONo });
                ((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).Add(new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter { Field = DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Fields.Item_No, Criteria = ItemNo });

                DEVGMBHReservationEntries.TWIWMS_ReservationEntries[] _resentries = ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).ReadMultiple(((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).ToArray(), string.Empty, 0);

                foreach (DEVGMBHReservationEntries.TWIWMS_ReservationEntries _res in _resentries)
                {
                    ReservationEntries _obj = new ReservationEntries();
                    _obj.ItemNo = string.IsNullOrEmpty(Convert.ToString(_res.Item_No)) ? "" : Convert.ToString(_res.Item_No);
                    _obj.Description = string.IsNullOrEmpty(Convert.ToString(_res.Description)) ? "" : Convert.ToString(_res.Description);
                    _obj.LotNo = string.IsNullOrEmpty(Convert.ToString(_res.Lot_No)) ? "" : Convert.ToString(_res.Lot_No);
                    _obj.Quantity = string.IsNullOrEmpty(Convert.ToString(_res.Quantity_Base)) ? 0 : Convert.ToDecimal(_res.Quantity_Base);
                    reservationentries.Add(_obj);
                }
            }
            
            return reservationentries;
        }

        private DEVGMBHItemCard.TWIWMS_Item GetItemInformation(string ItemNo)
        {
            _service = new DEVGMBHItemCard.TWIWMS_Item_Service();
            ((DEVGMBHItemCard.TWIWMS_Item_Service)_service).UseDefaultCredentials = false;
            ((DEVGMBHItemCard.TWIWMS_Item_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            return ((DEVGMBHItemCard.TWIWMS_Item_Service)_service).Read(ItemNo);
        }

        #endregion 

        #endregion

    }

    public class POReceiveModel
    {
        POHeader Pohead { get; set; }
        POLines Polines { get; set; } 
    }

    public class POHeader
    {
        public string PONo { get; set; }

        public string ReceiptNo { get; set; }

        public string VendorName { get; set; }

        public string ContainerNo { get; set; }

        public string LocationCode { get; set; }

        public string OrderDate { get; set; }

        public string PostingDate { get; set; }
    }

    public class POLines {
        public string ItemNo { get; set; }

        public string Description { get; set; }

        public string NSNo { get; set; }

        public string Quantity { get; set;} 

        public string Received { get; set; }

        public decimal QuantityReceived { get; set; }
    }

    public class ReservationEntries {
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string LotNo { get; set; }
        public string ExpiryDate { get; set; } 
        public decimal Quantity { get; set; }
    }
      
}