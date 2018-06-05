using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading;
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
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            Language regdevice = db.Languages.AsNoTracking().Where(x => x.ID == lang.ID).FirstOrDefault();
                            lang.CreatedDate = regdevice.CreatedDate;
                            lang.CreatedBy = regdevice.CreatedBy;
                            db.Entry(lang).State = EntityState.Modified;
                            db.SaveChanges();
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                        }

                    }
                }
                else
                    //return Json(new { success = false, message = "Language Code already exists!" }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                //return Json(new { success = false, message = "Unable to add Language details!" }, JsonRequestBehavior.AllowGet);
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileAdding }, JsonRequestBehavior.AllowGet);
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
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyDisabled }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult MakeitDefault(string defaultlang, bool isdefault)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    if (isdefault)
                    {
                        if (Session["UserID"] != null)
                        {
                            int userid = Convert.ToInt32(Session["UserID"]);
                            List<UserLanguage> ul = db.UserLanguages.Where(x => x.UserID == userid && x.IsActive == true).ToList();
                            foreach (var item in ul)
                            {
                                item.IsDefault = false;
                            }
                            db.SaveChanges();
                            UserLanguage currentlang = (from a in db.Languages
                                                        join b in db.UserLanguages on a.ID equals b.LanguageID
                                                        where a.Description == defaultlang && b.UserID == userid
                                                        select b).FirstOrDefault();
                            currentlang.IsDefault = true;
                            db.SaveChanges();
                        }
                    }
                    string deflang = db.Languages.Where(x => x.Description == defaultlang).Select(x => x.Code).FirstOrDefault();
                    Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(deflang);
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(deflang);
                    HttpCookie cookie = new HttpCookie("Language");
                    cookie.Value = deflang;
                    Response.Cookies.Add(cookie);
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorWhileUpdate }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}