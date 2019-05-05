using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace TWI.InventoryAutomated.Controllers
{
    public class TransferOrderController : Controller
    {
        #region "Global Variables"
        object _service;
        object _servicefilters;
        DEVGMBHTransferOrder.TWIWMS_TransferOrder  _transferorder;
        #endregion

        #region "Action Results"

        // GET: TransferOrder
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetTOHeaderDetails(string TONo)
        {
            if (string.IsNullOrEmpty(TONo))
            { return Json(new { success = false, message = "Enter a TO Number to Pull Data from NAV" }, JsonRequestBehavior.AllowGet); }

            _transferorder = GetTOData(TONo);

            if (_transferorder == null)
            { return Json(new { success = false, message = "Invalid TO Number Enter, Kindly check." }, JsonRequestBehavior.AllowGet); }

            TOHeader _tohead = new TOHeader();

            _tohead.LocationCode = string.IsNullOrEmpty(Convert.ToString(_transferorder.Transfer_to_Code)) ? "" : Convert.ToString(_transferorder.Transfer_to_Code);
            _tohead.VendorName = string.IsNullOrEmpty(Convert.ToString(_transferorder.Transfer_from_Name)) ? "" : Convert.ToString(_transferorder.Transfer_from_Name);
            _tohead.TONo = string.IsNullOrEmpty(Convert.ToString(_transferorder.No)) ? "" : Convert.ToString(_transferorder.No);
            _tohead.OrderDate = string.IsNullOrEmpty(Convert.ToString(_transferorder.Created_Date)) ? "" : Convert.ToDateTime(_transferorder.Created_Date).ToString("dd/MM/yyyy");
            _tohead.PostedDate = string.IsNullOrEmpty(Convert.ToString(_transferorder.Posting_Date)) ? "" : Convert.ToDateTime(_transferorder.Posting_Date).ToString("dd/MM/yyyy");
            _tohead.ContainerNo = string.IsNullOrEmpty(Convert.ToString(_transferorder.Container_No)) ? "" : Convert.ToString(_transferorder.Container_No);

            return Json(new { success = true, message = _tohead }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTODetails(string TONo)
        {
            List<TOLines> _tolines = new List<TOLines>();

            if (_transferorder == null || _transferorder.No != TONo)
            { _transferorder = GetTOData(TONo); }

            foreach (DEVGMBHTransferOrder.Transfer_Order_Line _toline in _transferorder.TransferLines)
            {
                TOLines _obj = new TOLines();
                _obj.ItemNo = string.IsNullOrEmpty(Convert.ToString(_toline.Item_No)) ? "" : Convert.ToString(_toline.Item_No);
                _obj.Description = string.IsNullOrEmpty(Convert.ToString(_toline.Description)) ? "" : Convert.ToString(_toline.Description);
                _obj.ExpiryDate = "";
                _obj.NSNo = GetItemInformation(_obj.ItemNo).TWI_NSN_No_Cross_Reference_No;
                _obj.Quantity = Convert.ToString(_toline.Quantity_Received) + " / " + Convert.ToString(_toline.Quantity);
                _tolines.Add(_obj);
            }

            return Json(new { data = _tolines }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ItemTracking(string ItemNo, string TONo)
        {
            DEVGMBHItemCard.TWIWMS_Item _item = GetItemInformation(ItemNo);
            //string _ItemNo = string.IsNullOrEmpty(Convert.ToString(_item.No)) ? "" : Convert.ToString(_item.No);
            //string _Desc = string.IsNullOrEmpty(Convert.ToString(_item.Description)) ? "" : Convert.ToString(_item.Description);
            DEVGMBHTransferOrder.TWIWMS_TransferOrder transferorder = GetTOData(TONo);

            decimal QtyReceived = 0;

            if (transferorder.TransferLines.Where(x => x.Item_No == ItemNo).Count() > 0)
                QtyReceived = transferorder.TransferLines.Where(x => x.Item_No == ItemNo).FirstOrDefault().Quantity_Received;

            return Json(new { ItemNo = string.IsNullOrEmpty(Convert.ToString(_item.No)) ? "" : Convert.ToString(_item.No), Desc = string.IsNullOrEmpty(Convert.ToString(_item.Description)) ? "" : Convert.ToString(_item.Description), QtyRec = Convert.ToString(QtyReceived) }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetItemTrackingLine(string TONo, string ItemNo)
        {
            DEVGMBHItemCard.TWIWMS_Item _item = GetItemInformation(ItemNo);
            string Desc = string.IsNullOrEmpty(Convert.ToString(_item.Description)) ? "" : Convert.ToString(_item.Description);
            DEVGMBHReservationEntries.TWIWMS_ReservationEntries[] _res_entries = null;
            _res_entries = GetReservationEntries(TONo, ItemNo);
            return Json(new { data = _res_entries.ToList() }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region "Helper Functions"

        public DEVGMBHTransferOrder.TWIWMS_TransferOrder GetTOData(string TONo)
        {
            _service = new DEVGMBHTransferOrder.TWIWMS_TransferOrder_Service();
            ((DEVGMBHTransferOrder.TWIWMS_TransferOrder_Service)_service).UseDefaultCredentials = false;
            ((DEVGMBHTransferOrder.TWIWMS_TransferOrder_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            return ((DEVGMBHTransferOrder.TWIWMS_TransferOrder_Service)_service).Read(TONo);
        }

        public DEVGMBHItemCard.TWIWMS_Item GetItemInformation(string ItemNo)
        {
            _service = new DEVGMBHItemCard.TWIWMS_Item_Service();
            ((DEVGMBHItemCard.TWIWMS_Item_Service)_service).UseDefaultCredentials = false;
            ((DEVGMBHItemCard.TWIWMS_Item_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            return ((DEVGMBHItemCard.TWIWMS_Item_Service)_service).Read(ItemNo);
        }

        public DEVGMBHReservationEntries.TWIWMS_ReservationEntries[] GetReservationEntries(string TONo, string ItemNo)
        {
            _service = new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service();
            ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).UseDefaultCredentials = false;
            ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            _servicefilters = new List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>();
            ((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).Add(new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter { Field = DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Fields.Source_ID, Criteria = TONo });
            ((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).Add(new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter { Field = DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Fields.Item_No, Criteria = ItemNo });

            return ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).ReadMultiple(((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).ToArray(), string.Empty, 0);
        }

        public string GetReceiptData(string PONo)
        {
            string ReceiptNo = string.Empty;
            DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines[] _reclines = null;

            //Code to Initialize Warehouse Receipt Lines Webservice.
            _service = new DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service();
            ((DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service)_service).UseDefaultCredentials = false;
            ((DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            //Code to set filters for the webservice. Receipt Lines will be pulled by Source_No Field and No Field will be return as Receipt No to Front End. 
            _servicefilters = new List<DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter>();
            ((List<DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter>)_servicefilters).Add(new DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter { Field = DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Fields.Source_No, Criteria = PONo });

            //Code to read / pull receipt lines through webservice from NAV ERP.
            _reclines = ((DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Service)_service).ReadMultiple(((List<DEVGMBHWarehouseReceiptLines.TWIWMS_WarehouseReceiptLines_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            //Code to check whether any data received. If data received then set the Receipt No field
            // else ReceiptNo field remains blank.
            if (_reclines != null && _reclines.Count() > 0)
                ReceiptNo = string.IsNullOrEmpty(Convert.ToString(_reclines[0].No)) ? "" : Convert.ToString(_reclines[0].No);

            return ReceiptNo;
        }

        public decimal GetItemQtyHandled(string PONo, string ItemNo)
        {
            DEVGMBHReservationEntries.TWIWMS_ReservationEntries[] _reclines = null;

            //Code to Initialize Warehouse Receipt Lines Webservice.
            _service = new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service();
            ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).UseDefaultCredentials = false;
            ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).Credentials = new NetworkCredential(System.Configuration.ConfigurationManager.AppSettings["WebService.UserName"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Password"]
                , System.Configuration.ConfigurationManager.AppSettings["WebService.Domain"]);

            _servicefilters = new List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>();
            ((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).Add(new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter { Field = DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Fields.Source_ID, Criteria = PONo });
            ((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).Add(new DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter { Field = DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Fields.Item_No, Criteria = ItemNo });

            //Code to read / pull receipt lines through webservice from NAV ERP.
            _reclines = ((DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Service)_service).ReadMultiple(((List<DEVGMBHReservationEntries.TWIWMS_ReservationEntries_Filter>)_servicefilters).ToArray(), string.Empty, 0);

            if (_reclines.Count() == 0) return 0; else return Convert.ToDecimal(_reclines.Sum(x => x.Quantity_Base));
        }

        #endregion 

    }

    public class TOReceiveModel
    {
        TOHeader Pohead { get; set; }
        TOLines Polines { get; set; }
    }


    public class TOHeader
    {
        public string TONo { get; set; }

        public string ReceiptNo { get; set; }

        public string VendorName { get; set; }

        public string ContainerNo { get; set; }

        public string LocationCode { get; set; }

        public string OrderDate { get; set; }

        public string PostedDate { get; set; }

    }

    public class TOLines
    {
        public string ItemNo { get; set; }

        public string Description { get; set; }

        public string NSNo { get; set; }

        public string Quantity { get; set; }

        public string ExpiryDate { get; set; }

    }
}