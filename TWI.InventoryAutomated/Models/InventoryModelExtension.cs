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
        [Display(Name = "Device Name")]
        [Required(ErrorMessage = "Required Field")]
        public string DeviceName { get; set; }

        [Display(Name = "MAC Address")]
        [Required(ErrorMessage = "Required Field")]
        public string MacAddress { get; set; }
        [Display(Name = "Active")]
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
        [Display(Name = "Instance Name")]
        [Required(ErrorMessage = "Required Field")]
        public string InstanceName { get; set; }

        [Display(Name = "Web URL")]
        [Required(ErrorMessage = "Required Field")]
        public string WebUrl { get; set; }
        [Display(Name = "Active")]
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
        [Display(Name = "Company Name")]
        [Required(ErrorMessage = "Required Field")]
        public string CompanyName { get; set; }
        [Display(Name = "Active")]
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
        [Required(ErrorMessage = "Required Field")]
        public string Code { get; set; }
        [Required(ErrorMessage = "Required Field")]
        public string Description { get; set; }
        [Display(Name = "Active")]
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
        [Display(Name = "Module Name")]
        [Required(ErrorMessage = "Required Field")]
        public string ModuleName { get; set; }
        [Display(Name = "Active")]
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
        [Display(Name = "Form Name")]
        [Required(ErrorMessage = "Required Field")]
        public string FormName { get; set; }
        [Display(Name = "SourceFileName")]
        [Required(ErrorMessage = "Required Field")]
        public string SourceFileName { get; set; }
        [ForeignKey("Module")]
        [Display(Name = "Module ID")]
        [Required(ErrorMessage = "Required Field")]
        public Nullable<bool> ModuleID { get; set; }
        [Display(Name = "Active")]
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
        [Display(Name = "Permission Name")]
        [Required(ErrorMessage = "Required Field")]
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
        [Display(Name = "User Name")]
        [Required(ErrorMessage = "Required Field")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Required Field")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        //[StringLength(20, MinimumLength = 6, ErrorMessage = "Password must be 6 character long.")]
        public string Password { get; set; }
        [Compare("Password", ErrorMessage = "Password mismatch")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
        [RegularExpression(@"^([0-9a-zA-Z]([\+\-_\.][0-9a-zA-Z]+)*)+@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,3})$",
            ErrorMessage = "Please provide valid email id")]
        [Display(Name = "Email ID")]
        public string EmailID { get; set; }
        [Display(Name = "Permissions")]
        [Required(ErrorMessage = "Required Field")]
        public Nullable<int> PermissionID { get; set; }
        [Display(Name = "Active")]
        public Nullable<bool> IsActive { get; set; }
    }
#endregion

}