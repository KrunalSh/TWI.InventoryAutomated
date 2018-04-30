using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using TWI.InventoryAutomated.Models;

namespace TWI.InventoryAutomated.Security
{
    public class CustomPrincipal : IPrincipal
    {
        private User user;
        InventoryPortalEntities db = new InventoryPortalEntities();
        public CustomPrincipal(User user)
        {
            this.user = user;
            this.Identity = new GenericIdentity(user.UserName);
        }
        public IIdentity Identity
        {
            get;
            set;
        }

        public bool IsInRole(string permissions)
        {
            var roles = permissions.Split(new char[] { ',' });
            List<string> perms = (from u in db.Users
                                  join v in db.UserAccesses on u.UserID equals v.UserID
                                  join w in db.Permissions on v.PermissionID equals w.ID
                                  where u.UserName == this.user.UserName
                                  select w.PermissionDesc).ToList();

            return roles.Any(r => perms.Contains(r));
        }
    }
}