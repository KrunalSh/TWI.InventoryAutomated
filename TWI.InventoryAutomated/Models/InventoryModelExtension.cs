using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TWI.InventoryAutomated.Models
{
    #region RegisteredDevices
    [MetadataType(typeof(RegisteredDeviceAttribs))]
    public partial class RegisteredDevice
    {

    }

    public class RegisteredDeviceAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string DeviceName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string MacAddress { get; set; }
        public Nullable<bool> IsActive { get; set; }
    }
    #endregion

    #region Instance
    [MetadataType(typeof(InstanceAttribs))]
    public partial class Instance
    {

    }

    public class InstanceAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string InstanceName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string WebUrl { get; set; }
        public Nullable<bool> IsActive { get; set; }
    }
    #endregion

    #region Company
    [MetadataType(typeof(CompanyAttribs))]
    public partial class Company
    {

    }

    public class CompanyAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string CompanyName { get; set; }
        public Nullable<bool> IsActive { get; set; }
    }
    #endregion

    #region Language
    [MetadataType(typeof(LanguageAttribs))]
    public partial class Language
    {

    }

    public class LanguageAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string Code { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string Description { get; set; }
        public Nullable<bool> IsActive { get; set; }
    }
    #endregion
    #region Module
    [MetadataType(typeof(ModuleAttribs))]
    public partial class Module
    {

    }

    public class ModuleAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string ModuleName { get; set; }
        public Nullable<bool> IsActive { get; set; }
    }
    #endregion
    #region Forms
    [MetadataType(typeof(FormsAttribs))]
    public partial class Forms
    {

    }

    public class FormsAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string FormName { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string SourceFileName { get; set; }
        [ForeignKey("Module")]
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public Nullable<bool> ModuleID { get; set; }
        public Nullable<bool> IsActive { get; set; }
    }
    #endregion
    #region Permissions
    [MetadataType(typeof(PermissionAttribs))]
    public partial class Permission
    {

    }

    public class PermissionAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string PermissionDesc { get; set; }
    }
    #endregion
    #region Users
    [MetadataType(typeof(UserAttribs))]
    public partial class User
    {
        [NotMapped]
        public string ConfirmPassword { get; set; }
    }

    public class UserAttribs
    {
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string UserName { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        //[StringLength(20, MinimumLength = 6, ErrorMessage = "Password must be 6 character long.")]
        public string Password { get; set; }
        [Compare("Password", ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "Passwordmismatch")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string ConfirmPassword { get; set; }
        [RegularExpression(@"^([0-9a-zA-Z]([\+\-_\.][0-9a-zA-Z]+)*)+@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,3})$",
            ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "ValidEmailID")]
        public string EmailID { get; set; }
        [Required(ErrorMessageResourceType = typeof(Resources.GlobalResource),
            ErrorMessageResourceName = "RequiredField")]
        public string DisplayName { get; set; }

        public Nullable<bool> IsActive { get; set; }
    }
    #endregion
    //#region UserAccess
    //[MetadataType(typeof(UserAttribs))]
    //public partial class UserAccess
    //{

    //}

    //public class UserAccessAttribs
    //{
    //    [Display(Name = "Permissions")]
    //    [Required(ErrorMessage = "Required Field")]
    //    public Nullable<int> PermissionID { get; set; }
    //}
    //#endregion
}