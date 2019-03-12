using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class LocationController : Controller
    {
        // GET: Location
        //List all locations by CompanyID
        public ActionResult LocationView(int ID)
        {
            CommonServices cs = new CommonServices();
            if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    Company _comp = db.Companies.Where(x => x.ID == ID).FirstOrDefault();
                    Session["SelectedCompany"] = _comp.CompanyName;
                    Session["SelectedCompanyID"] = _comp.ID;
                    Session["SelectedInstanceID"] = _comp.InstanceID;
                    return View();
                }
            }
            else
            {
                cs.RemoveSessions();
                return RedirectToAction("Default", "Home");
            }
        }

        public ActionResult LoadLocationList(int ID)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    ViewBag.CompanyName = db.Companies.Where(x => x.ID == ID).FirstOrDefault().CompanyName;
                    List<Location> LocationList = db.Location.Where(x => x.CompanyID == ID).ToList<Location>();
                    return Json(new { data = LocationList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            { throw; }
        }

        public ActionResult CreateUpdateLocation(int ID = 0)
        {
            Location _loc;
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                if (ID != 0)
                    _loc = db.Location.Where(x => x.ID == ID).FirstOrDefault();
                else
                { _loc = new Location(); _loc.IsActive = true; }
                  
                return View(_loc);
            }
        }

        [HttpPost]
        public ActionResult CreateUpdateLocation(Location _loc)
        {
            int InstanceID = Convert.ToInt32(Session["SelectedInstanceID"]);
            int CompanyID = Convert.ToInt32(Session["SelectedCompanyID"]);

            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                //This condition  checks whether there is any input from the user
                if (_loc == null) { return Json(new { success = false, message = "* Marked fields are mandatory fields, Kindly enter a values for respective fields" }, JsonRequestBehavior.AllowGet); }

                //This condition checks whether Location Code field is null or left empty by the user
                if (_loc.Code == null || string.IsNullOrEmpty(_loc.Code.Trim()))
                    return Json(new { success = false, message = "Value for Code field is mandatory, Kindly enter a value" }, JsonRequestBehavior.AllowGet);

                //This condition checks whether Location Description field is null or left empty by the user
                if (_loc.Description == null || string.IsNullOrEmpty(_loc.Description.Trim()))
                    return Json(new { success = false, message = "Value for Description field is mandatory, Kindly enter a value" }, JsonRequestBehavior.AllowGet);

                if (_loc.ID == 0)
                {
                    // This condition checks whether the Location Code entered is duplicating for this Instance & Company in the system or not.
                    if (db.Location.Where(x => x.Code == _loc.Code && x.InstanceID == InstanceID && x.CompanyID == CompanyID).Count() > 0)
                        return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);

                    if (_loc.IsActive == false)
                        return Json(new { success = false, message = "Cannot create an Inactive Location entry, Kindly mark it Active to proceed." }, JsonRequestBehavior.AllowGet);

                    _loc.InstanceID = InstanceID;
                    _loc.CompanyID = CompanyID;
                    _loc.CreatedBy = Convert.ToInt32(Session["UserID"]);
                    _loc.CreatedDate = DateTime.Now;
                    db.Location.Add(_loc);
                }
                else
                {
                    Location _orginalLoc = db.Location.Where(x => x.ID == _loc.ID).FirstOrDefault();
                    _orginalLoc.Description = _loc.Description;
                    _orginalLoc.IsActive = _loc.IsActive;
                    _orginalLoc.ModifiedBy = Convert.ToInt32(Session["UserID"]);
                    _orginalLoc.ModifiedDate = DateTime.Now;
                    db.Entry(_orginalLoc).State = System.Data.Entity.EntityState.Modified;
                }

                try
                {
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgLocationSaved }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    Location _loc = db.Location.Where(x => x.ID == id).FirstOrDefault();
                    _loc.IsActive = false;
                    db.Entry(_loc).Property("IsActive").IsModified = true;
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyDisabled }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}