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
    public class FormController : Controller
    {
        // GET: Form
        //public ActionResult Index()
        //{
        //    CommonServices cs = new CommonServices();
        //    if (cs.IsCurrentSessionActive(Session["CurrentSession"]))
        //        return View();
        //    else
        //    {
        //        cs.RemoveSessions();
        //        return RedirectToAction("Default", "Home");
        //    }
        //}
        //public ActionResult GetData()
        //{
        //    try
        //    {
        //        using (InventoryPortalEntities db = new InventoryPortalEntities())
        //        {
        //            var List = db.Forms.ToList();
        //            return Json(new { data = List }, JsonRequestBehavior.AllowGet);
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        public ActionResult Index(int ModuleID)
        {
            try
            {
                // 
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    //Code to retrieve the module name for display purpose indicating
                    //for which module user is working on.
                    Module _module = db.Modules.Where(x => x.ModuleID == ModuleID).FirstOrDefault();
                    ViewBag.ModuleName = _module.ModuleName;
                    ViewBag.ModuleId = _module.ModuleID;
                    return View();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult GetData(int ModuleId)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    //Code to retrieve list of Forms for the selectded module in the system by passing ModuleID parameter.

                    //var List = db.Forms.Where(x => x.ModuleID == ModuleId && x.IsActive == true).ToList();
                    List<Form> formList = db.Forms.Where(x => x.ModuleID == ModuleId).ToList<Form>();
                    return Json(new { data = formList }, JsonRequestBehavior.AllowGet);
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
                InventoryPortalEntities db = new InventoryPortalEntities();
                //ViewBag.ModuleID = new SelectList(db.Modules, "ModuleID", "ModuleName");
                //ViewBag.Modules = db.Modules.ToList();


                //Code to load Popup screen based on ID. 
                //if ID = 0 then empty all fields in UI
                if (id == 0)
                    return View(new Form());
                else
                {
                    //Linq query to retrieve form details by ID and populate respective fields
                    // in UI
                    return View(db.Forms.Where(x => x.ID == id).FirstOrDefault<Form>());
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(Form _form)
        {
            try
            {
                //Condition to check whether form name is not empty.
                if (string.IsNullOrEmpty(_form.FormName)) {
                    return Json(new { success = false, message = Resources.GlobalResource.MsgFormNameRequired }, JsonRequestBehavior.AllowGet);
                }

                //Condition to check whether form name doesn't duplicate within the module
                if (!isDuplicate(_form))
                {
                    //Updating "CreatedDate" and "CreatedBy" details along with changes made through UI
                    //Saving data to database
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        //Code - while creating a new form in the system.
                        if (_form.ID == 0)
                        {
                            _form.CreatedDate = DateTime.Now;
                            _form.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Forms.Add(_form);
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            //Code - while modifying details of a form
                            Form form = db.Forms.AsNoTracking().Where(x => x.ID == _form.ID).FirstOrDefault();
                            _form.CreatedDate = form.CreatedDate;
                            _form.CreatedBy = form.CreatedBy;
                            db.Entry(_form).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileAdding }, JsonRequestBehavior.AllowGet);
            }


        }

        public bool isDuplicate(Form _form)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                //check to validate entered form name is not duplicating within the same module
                Form form;
                if (_form.ID != 0)
                    form = db.Forms.AsNoTracking().Where(x => x.FormName == _form.FormName && x.ModuleID == _form.ModuleID && x.ID != _form.ID).FirstOrDefault();
                else
                    form = db.Forms.AsNoTracking().Where(x => x.FormName == _form.FormName && x.ModuleID == _form.ModuleID).FirstOrDefault();

                //code to return false if no duplicate record found
                if (form == null)
                    return false;
                else
                    return true;
            }
        }
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    // Disable a form in the system by setting "IsActive" field to false
                    Form form = db.Forms.Where(x => x.ID == id).FirstOrDefault<Form>();
                    form.IsActive = false;
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgDisableRecord }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
            }
        }

        #region "Function not used"

        public string PopulateModuleName(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    return db.Modules.Where(x=>x.ModuleID==id).Select(x=>x.ModuleName).FirstOrDefault();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion 
    }
}