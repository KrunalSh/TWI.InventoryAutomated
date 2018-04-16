using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TWI.InventoryAutomated.Security
{
    public static class SessionPersister
    {
        static string usernameSessionvar = "username";
        static int userid;
        public static string UserName
        {
            get
            {
                if (HttpContext.Current == null)
                    return String.Empty;
                var sessionVar = HttpContext.Current.Session[usernameSessionvar];
                if (sessionVar != null)
                    return sessionVar as string;
                return null;
            }
            set
            {
                HttpContext.Current.Session[usernameSessionvar] = value;
            }
        }
        public static int UserID
        {
            get
            {
                if (HttpContext.Current == null)
                    return 0;
                var sessionVar = HttpContext.Current.Session[userid];
                if (sessionVar != null)
                    return (int)sessionVar;
                return 0;
            }
            set
            {
                HttpContext.Current.Session[userid] = value;
            }
        }
    }
}