using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DEVPhysicalInvJournal;
using TWI.InventoryAutomated.TESTPhysicalInvJournal;
using System.Net;

namespace TWI.InventoryAutomated.Controllers
{
    public class StockCountController : Controller
    {
        object _service;


        // GET: StockCount
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult NavDataPull()
        {
            if (Convert.ToString(Session["InstanceName"]).ToLower() == "live")
            {
                _service = new TESTPhysicalInvJournal.PhysicalInvJournal_Service();
                ((TESTPhysicalInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                ((TESTPhysicalInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");
                TESTPhysicalInvJournal.PhysicalInvJournal[] _phyjournal = ((TESTPhysicalInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(null,string.Empty,0);
                List<TESTPhysicalInvJournal.PhysicalInvJournal> _obj = _phyjournal.Where(x => x.Whse_Document_No == "").ToList();
            }
            else
            {
                _service = new DEVPhysicalInvJournal.PhysicalInvJournal_Service();
                ((DEVPhysicalInvJournal.PhysicalInvJournal_Service)_service).UseDefaultCredentials = false;
                ((DEVPhysicalInvJournal.PhysicalInvJournal_Service)_service).Credentials = new NetworkCredential("vendorportal", "Twivp2015", "twi");
                DEVPhysicalInvJournal.PhysicalInvJournal[] _phyjournal = ((DEVPhysicalInvJournal.PhysicalInvJournal_Service)_service).ReadMultiple(null, string.Empty, 0);
                List<DEVPhysicalInvJournal.PhysicalInvJournal> _obj = _phyjournal.Where(x => x.Whse_Document_No == "").ToList();
            }
            return View();
        }
    }
}