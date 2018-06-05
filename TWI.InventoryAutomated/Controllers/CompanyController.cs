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
            CommonServices cs = new CommonServices();
            if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
                return View();
            else
            {
                cs.RemoveSessions();
                return RedirectToAction("Default", "Home");
            }
        }

        [HttpPost]
        public ActionResult GetData(int InstanceId)
        {
            try
            {
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

        public ActionResult Index(int InstanceID)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
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

        [HttpGet]
        public ActionResult AddOrEdit(int id = 0)
        {
            try
            {
                if (id == 0)
                    return View(new Company());
                else
                {
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
                if (!isDuplicate(company))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {

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
                Company company;
                if (comp.ID != 0)
                    company = db.Companies.AsNoTracking().Where(x => x.CompanyName == comp.CompanyName && x.InstanceID == comp.InstanceID && x.ID != comp.ID).FirstOrDefault();
                else
                    company = db.Companies.AsNoTracking().Where(x => x.CompanyName == comp.CompanyName && x.InstanceID == comp.InstanceID).FirstOrDefault();
                if (company == null)
                    return false;
                else
                    return true;
            }
        }
    }
}