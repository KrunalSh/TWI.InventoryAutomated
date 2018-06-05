using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Session_Start(object sender, EventArgs e)
        {

        }
        protected void Session_End(object sender, EventArgs e)
        {

        }
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["Language"];
            if (cookie != null && cookie.Value != null)
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cookie.Value);
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(cookie.Value);
            }
            else
            {
                string currentLang = System.Threading.Thread.CurrentThread.CurrentCulture.Parent.Name;
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(currentLang);
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(currentLang);
                HttpCookie myCookie = new HttpCookie("Language");
                myCookie.Value = currentLang;
                HttpContext.Current.Response.Cookies.Add(myCookie);
                HttpCookie SystemLang = new HttpCookie("SystemLang");
                SystemLang.Value = currentLang;
                HttpContext.Current.Response.Cookies.Add(SystemLang);
            }
        }
        
    }
}
