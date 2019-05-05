using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Controllers
{
    public class CompanyController : Controller
    {
        // GET: Company
        public ActionResult Index()
        {
            //Check to Validate user session to prevent unauthorized access to this web page
            CommonServices cs = new CommonServices();
            if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
                return View();
            else
            {
                //Clear all the session and redirect App to Login Screen
                cs.RemoveSessions();
                return RedirectToAction("Default", "Home");
            }
        }

        [HttpGet]
        public ActionResult Index(int InstanceID)
        {
            try
            {
             
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    //Code to retrieve the instance name displayed to indicate 
                    //for which instance user is working on.
                    Instance instance = db.Instances.Where(x => x.ID == InstanceID).FirstOrDefault();
                    ViewBag.InstanceName = instance.InstanceName;
                    ViewBag.InstanceId = instance.ID;
                    return View();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        
        [HttpPost]
        public ActionResult GetData(int InstanceId)
        {
            try
            {
                //Code to retrieve list of companies for a instance in the system by passing Instance ID.
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<Company> compList = db.Companies.Where(x => x.InstanceID == InstanceId).ToList<Company>();
                    return Json(new { data = compList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
      

        [HttpGet]
        public ActionResult AddOrEdit(int id = 0)
        {
            try
            {
                //Code to load Popup screen based on ID. 
                //if ID = 0 then empty all fields in UI
                if (id == 0)
                    return View(new Company());
                else
                {
                    //Linq query to retrieve instance details by ID and populate respective fields
                    // in UI
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        return View(db.Companies.Where(x => x.ID == id).FirstOrDefault<Company>());
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(Company company)
        {
            try
            {
                //Condition to check whether company name
                // doesn't duplicate in the system.
                if (!isDuplicate(company))
                {
                    //Updating "CreatedDate" and "CreatedBy" details along with changes made through UI
                    //Saving data to database
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        //Code - while create a new company in the system.
                        if (company.ID == 0)
                        {
                            company.CreatedDate = DateTime.Now;
                            company.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Companies.Add(company);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //Code - while modifying details of a company
                            Company comp = db.Companies.AsNoTracking().Where(x => x.ID == company.ID).FirstOrDefault();
                            comp.CreatedDate = comp.CreatedDate;
                            comp.CreatedBy = comp.CreatedBy;
                            db.Entry(company).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileAdding }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    // Disable a company in the system by setting "IsActive" field to false
                    Company comp = db.Companies.Where(x => x.ID == id).FirstOrDefault<Company>();
                    comp.IsActive = false;
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyDisabled }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {

                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
            }
        }


        public bool isDuplicate(Company comp)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                //check to validate entered company name is not duplicating within the same instance 
                Company company;
                if (comp.ID != 0)
                    company = db.Companies.AsNoTracking().Where(x => x.CompanyName == comp.CompanyName && x.InstanceID == comp.InstanceID && x.ID != comp.ID).FirstOrDefault();
                else
                    company = db.Companies.AsNoTracking().Where(x => x.CompanyName == comp.CompanyName && x.InstanceID == comp.InstanceID).FirstOrDefault();

                //code to return false if no duplicate record found
                if (company == null)
                    return false;
                else
                    return true;
            }
        }
    }
}