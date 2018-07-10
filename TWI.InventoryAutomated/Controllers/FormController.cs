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
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
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

                if (id == 0)
                    return View(new Form());
                else
                {
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
                if (string.IsNullOrEmpty(_form.FormName)) {
                    return Json(new { success = false, message = Resources.GlobalResource.MsgFormNameRequired }, JsonRequestBehavior.AllowGet);
                }

                if (!isDuplicate(_form))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
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
                Form form;
                if (_form.ID != 0)
                    form = db.Forms.AsNoTracking().Where(x => x.FormName == _form.FormName && x.ModuleID == _form.ModuleID && x.ID != _form.ID).FirstOrDefault();
                else
                    form = db.Forms.AsNoTracking().Where(x => x.FormName == _form.FormName && x.ModuleID == _form.ModuleID).FirstOrDefault();
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
    }
}