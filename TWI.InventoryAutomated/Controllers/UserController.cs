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
    public class UserController : Controller
    {
        // GET: User
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
                    var dataList = (from x in db.Users
                                    select new
                                    {
                                        x.UserID,
                                        x.UserName,
                                        x.EmailID,
                                        x.IsActive
                                    }).ToList();
                    return Json(new { data = dataList }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
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
                ViewBag.Languages = (from r in db.Languages where r.IsActive == true select new SelectListItem { Value = r.ID.ToString(), Text = r.Description + " - " + r.Code }).ToList();
                if (id == 0)
                {
                    ViewBag.selectedLanguages = db.Languages.Where(x => x.Description.Contains("English") && x.IsActive == true).Select(x => x.ID).ToList();
                    return View(new User());
                }
                else
                {
                    ViewBag.selectedLanguages = db.UserLanguages.Where(x => x.UserID == id && x.IsActive == true).Select(x => x.LanguageID).ToList();
                    User user = db.Users.Where(x => x.UserID == id).FirstOrDefault<User>();
                    user.ConfirmPassword = user.Password;
                    return View(user);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult AddOrEdit(User user, string selectedlangs)
        {
            try
            {
                List<int> selectedval = selectedlangs.Split(',').Select(int.Parse).ToList();

                if (!isDuplicate(user))
                {
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {

                        if (user.UserID == 0)
                        {
                            user.CreatedDate = DateTime.Now;
                            user.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            db.Users.Add(user);
                            db.SaveChanges();
                            updateLanguages(user, selectedval);
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            User _user = db.Users.AsNoTracking().Where(x => x.UserID == user.UserID).FirstOrDefault();
                            user.CreatedDate = _user.CreatedDate;
                            user.CreatedBy = _user.CreatedBy;
                            db.Entry(user).State = EntityState.Modified;
                            db.SaveChanges();
                            updateLanguages(user, selectedval);
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyUpdated }, JsonRequestBehavior.AllowGet);
                        }

                    }

                }
                else
                    //return Json(new { success = false, message = "User Name or Email already exists!" }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = false, message = Resources.GlobalResource.MsgAlreadyExist }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileAdding }, JsonRequestBehavior.AllowGet);
            }



        }

        private void updateLanguages(User user, List<int> languages)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                List<UserLanguage> uls = db.UserLanguages.Where(x => x.UserID == user.UserID).ToList();
                foreach (var item in languages)
                {
                    UserLanguage lang = uls.Where(x => x.LanguageID == item).FirstOrDefault();
                    if (lang == null)
                    {
                        UserLanguage ul = new UserLanguage();
                        ul.UserID = user.UserID;
                        ul.LanguageID = item;
                        ul.IsActive = true;
                        ul.CreatedDate = DateTime.Now;
                        db.UserLanguages.Add(ul);
                        db.SaveChanges();
                    }
                    else
                    {
                        lang.IsActive = true;
                        db.SaveChanges();
                    }
                }
                var rejectList = uls.Where(i => languages.Contains((int)i.LanguageID));
                var filteredList = uls.Except(rejectList);
                foreach (var item in filteredList)
                {
                    item.IsActive = false;
                }
                db.SaveChanges();
            }

        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    User user = db.Users.Where(x => x.UserID == id).FirstOrDefault<User>();
                    user.IsActive = false;
                    user.ConfirmPassword = user.Password;
                    db.SaveChanges();
                    return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullyDisabled }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = Resources.GlobalResource.MsgErrorwhileDisable }, JsonRequestBehavior.AllowGet);
            }
        }
        public bool isDuplicate(User user)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                User _user;
                if (user.UserID != 0)
                    _user = db.Users.AsNoTracking().Where(x => (x.UserName == user.UserName || (user.EmailID != null && x.EmailID == user.EmailID)) && x.UserID != user.UserID).FirstOrDefault();
                else
                    _user = db.Users.AsNoTracking().Where(x => x.UserName == user.UserName || (user.EmailID != null && x.EmailID == user.EmailID)).FirstOrDefault();
                if (_user == null)
                    return false;
                else
                    return true;
            }
        }
    }
}