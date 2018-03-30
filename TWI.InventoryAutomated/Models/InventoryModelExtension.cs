using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    }
    #endregion
}