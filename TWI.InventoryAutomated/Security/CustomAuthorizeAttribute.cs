using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Security
{
    public class CustomAuthorizeAttribute:AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            InventoryPortalEntities db = new InventoryPortalEntities();
            if (string.IsNullOrEmpty(SessionPersister.UserName))
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { Controller = "Login", Action = "Index" }));
            else
            {
                User _user = new User();
                CustomPrincipal mp = new CustomPrincipal(db.Users.Where(acc => acc.UserName.Equals(SessionPersister.UserName)).FirstOrDefault());
                if (!mp.IsInRole(Roles))
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { Controller = "Home", Action = "AccessDenied" }));

            }
        }
    }
}