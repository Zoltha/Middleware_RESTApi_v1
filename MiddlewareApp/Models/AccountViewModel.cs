using System;
using System.ComponentModel.DataAnnotations;

namespace MiddlewareApp.Models
{
    /// <summary>
    /// View model representing a Dynamics 365 CRM Account entity
    /// </summary>
    public class AccountViewModel
    {
        /// <summary>
        /// Unique identifier for the account (Dynamics GUID)
        /// </summary>
        public Guid? AccountId { get; set; }

        /// <summary>
        /// Account name
        /// </summary>
        [Required]
        [StringLength(160)]
        public string Name { get; set; }

        /// <summary>
        /// Account number
        /// </summary>
        [StringLength(20)]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Primary email address
        /// </summary>
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// Main phone number
        /// </summary>
        [Phone]
        [StringLength(50)]
        public string Phone { get; set; }

        /// <summary>
        /// Website URL
        /// </summary>
        [Url]
        [StringLength(200)]
        public string WebsiteUrl { get; set; }

        /// <summary>
        /// Address line 1
        /// </summary>
        [StringLength(250)]
        public string Address1_Line1 { get; set; }

        /// <summary>
        /// City
        /// </summary>
        [StringLength(80)]
        public string Address1_City { get; set; }

        /// <summary>
        /// State or province
        /// </summary>
        [StringLength(50)]
        public string Address1_StateOrProvince { get; set; }

        /// <summary>
        /// Postal code
        /// </summary>
        [StringLength(20)]
        public string Address1_PostalCode { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        [StringLength(80)]
        public string Address1_Country { get; set; }

        /// <summary>
        /// Industry type
        /// </summary>
        [StringLength(50)]
        public string Industry { get; set; }

        /// <summary>
        /// Annual revenue
        /// </summary>
        public decimal? Revenue { get; set; }

        /// <summary>
        /// Number of employees
        /// </summary>
        public int? NumberOfEmployees { get; set; }

        /// <summary>
        /// Account description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Date the record was created in CRM
        /// </summary>
        public DateTime? CreatedOn { get; set; }

        /// <summary>
        /// Date the record was last modified in CRM
        /// </summary>
        public DateTime? ModifiedOn { get; set; }
    }
}
