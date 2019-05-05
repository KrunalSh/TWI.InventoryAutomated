using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TWI.InventoryAutomated.DataAccess;
using TWI.InventoryAutomated.Models;
using TWI.InventoryAutomated.TestGmbhWh_Users;

namespace TWI.InventoryAutomated.Controllers
{
    public class UserController : Controller
    {
        // GET: User
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

        public ActionResult GetData()
        {
            try
            {
                //Code to retrieve list of users registered in the system.
                using (InventoryPortalEntities db = new InventoryPortalEntities())
                {
                    var dataList = (from x in db.Users
                                    select new
                                    {
                                        x.UserID,
                                        x.UserName,
                                        x.DisplayName,
                                        x.NAV_ID,
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
                //code to load active languages in the dropdown list
                InventoryPortalEntities db = new InventoryPortalEntities();
                ViewBag.Languages = (from r in db.Languages where r.IsActive == true
                                     select 
                                     new SelectListItem { Value = r.ID.ToString(), Text = r.Description + " - " + r.Code })
                                     .ToList();

                //Code to load Popup screen based on ID. 
                //if ID = 0 then empty all fields in UI
                if (id == 0)
                {
                    ViewBag.selectedLanguages = db.Languages.Where(x => x.Description.Contains("English") && x.IsActive == true)
                                                  .Select(x => x.ID).ToList();
                    ViewBag.selectedNAVID = "";
                    return View(new User());
                }
                else
                {
                    //Linq query to retrieve user details by ID and populate respective fields in UI
                    ViewBag.selectedLanguages = db.UserLanguages.Where(x => x.UserID == id && x.IsActive == true)
                                                  .Select(x => x.LanguageID).ToList();

                    User user = db.Users.Where(x => x.UserID == id).FirstOrDefault<User>();
                    ViewBag.selectedNAVID = user.NAV_ID;
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
        public ActionResult AddOrEdit(User user, string selectedlangs,string navid)
        {
            try
            {
                //Code to split the language ids coming in as parameter - selecedlangs
                List<int> selectedval = selectedlangs.Split(',').Select(int.Parse).ToList();

                //Code to check whether navid field is not empty
                if (string.IsNullOrEmpty(navid))
                { return Json(new { success = false, message = "NAV ID is a required field, Kindly select NAV ID of this user." }, JsonRequestBehavior.AllowGet); }


                //Condition to check whether user name 
                // doesn't duplicate in the system.
                if (!isDuplicate(user))
                {
                    //Updating "CreateDate" and "CreatedBy" details along with changes made through UI
                    //Saving data to database
                    //Condition to check whether NAV_ID doesn't duplicate in the system.
                    using (InventoryPortalEntities db = new InventoryPortalEntities())
                    {
                        if (user.UserID == 0)
                        {
                            if (!string.IsNullOrEmpty(navid.Trim()))
                            {
                                if (db.Users.Where(x => x.NAV_ID == navid).Count() > 0) return Json(new { success = false,
                                message = "Selected NAV_ID already assigned to another user in the system, Kindly verify your selection." },
                                JsonRequestBehavior.AllowGet);
                            }
                            user.CreatedDate = DateTime.Now; user.CreatedBy = Convert.ToInt32(Session["UserID"].ToString());
                            user.NAV_ID = navid.Trim(); db.Users.Add(user); db.SaveChanges();
                            //Code to update languages selected in the database.
                            updateLanguages(user, selectedval);
                            return Json(new { success = true, message = Resources.GlobalResource.MsgSuccessfullySaved }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(navid.Trim()))
                            {
                                if (db.Users.Where(x=> x.UserID != user.UserID && x.NAV_ID == navid).Count() > 0) return Json(new { success = false,
                                    message = "Selected NAV_ID already assigned to another user in the system, Kindly verify your selection." },
                                    JsonRequestBehavior.AllowGet);
                            }
                            User _user = db.Users.AsNoTracking().Where(x => x.UserID == user.UserID).FirstOrDefault();
                            user.CreatedDate = _user.CreatedDate; user.CreatedBy = _user.CreatedBy;
                            user.NAV_ID = navid; db.Entry(user).State = EntityState.Modified; db.SaveChanges();
                            //Code to update languages selected in the database.
                            updateLanguages(user, selectedval);
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

        private void updateLanguages(User user, List<int> languages)
        {
            using (InventoryPortalEntities db = new InventoryPortalEntities())
            {
                //Code to retrieve languages saved for a user.
                List<UserLanguage> uls = db.UserLanguages.Where(x => x.UserID == user.UserID).ToList();

                //loop to check whether each selected language was saved previously / while creation of user  in the database
                foreach (var item in languages)
                {
                    UserLanguage lang = uls.Where(x => x.LanguageID == item).FirstOrDefault();
                    //if a language is not saved previously / at the time user creation then a new entry of it will be created
                    // in the database.
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
                        //if a language is saved previously / at the time user creation then the existing entry of it will be updated 
                        // in the database by making IsActive field true.
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
                    // Disable a user in the system by setting "IsActive" field to false
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
                //check to validate entered  module name is not duplicating
                User _user;
                if (user.UserID != 0)
                    _user = db.Users.AsNoTracking()
                              .Where(x => (x.UserName == user.UserName || (user.EmailID != null && x.EmailID == user.EmailID))
                                                          && x.UserID != user.UserID).FirstOrDefault();
                else
                    _user = db.Users.AsNoTracking()
                            .Where(x => x.UserName == user.UserName || (user.EmailID != null && x.EmailID == user.EmailID))
                                                   .FirstOrDefault();
                //code to return false if no duplicate record found
                if (_user == null) return false; else return true;
            }
        }

        public ActionResult GetNavUserList()
        {
            try
            {
               //Code to get User ID List from Navision ERP.
               if (CommonServices._navuserlist == null) { CommonServices.GetNAVUserList();}
                    return Json(CommonServices._navuserlist, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}