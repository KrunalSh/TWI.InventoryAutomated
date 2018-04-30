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
    public class LanguageController : Controller
    {
        // GET: Language
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
        public ActionResult GetData()
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    List<Language> LangList = db.Languages.ToList<Language>();
                    return Json(new { data = LangList }, JsonRequestBehavior.AllowGet);
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
                    return View(new Language());
                else
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        return View(db.Languages.Where(x => x.ID == id).FirstOrDefault<Language>());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(Language lang)
        {
            try
            {
                if (!isDuplicate(lang))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (lang.ID == 0)
                        {
                            lang.CreatedDate = DateTime.Now;
                            lang.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Languages.Add(lang);
                            db.SaveChanges();
                            return Json(new { success = true, message = "Saved Successfully" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            Language regdevice = db.Languages.AsNoTracking().Where(x => x.ID == lang.ID).FirstOrDefault();
                            lang.CreatedDate = regdevice.CreatedDate;
                            lang.CreatedBy = regdevice.CreatedBy;
                            db.Entry(lang).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = "Updated Successfully" }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    return Json(new { success = false, message = "Language Code already exists!" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to add Language details!" }, JsonRequestBehavior.AllowGet);
            }


        }
        public bool isDuplicate(Language lang)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                Language language;
                if (lang.ID != 0)
                    language = db.Languages.AsNoTracking().Where(x => x.Code == lang.Code && x.ID != lang.ID).FirstOrDefault();
                else
                    language = db.Languages.AsNoTracking().Where(x => x.Code == lang.Code).FirstOrDefault();
                if (language == null)
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
                    Language regDevice = db.Languages.Where(x => x.ID == id).FirstOrDefault<Language>();
                    regDevice.IsActive = false;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Disabled Successfully" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to disable the record!" }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}