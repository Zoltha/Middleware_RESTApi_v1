using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiddlewareApp.Models
{
    /// <summary>
    /// Tracks idempotency keys to prevent duplicate POST operations
    /// </summary>
    [Table("IdempotencyRecords")]
    public class IdempotencyRecord
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Idempotency key from request header
        /// </summary>
        [Required]
        [StringLength(256)]
        [Index(IsUnique = true)]
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// The ID of the created resource
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ResourceId { get; set; }

        /// <summary>
        /// HTTP status code of the original response
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Response body (JSON) of the original request
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        /// When the record was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the record expires (24 hours by default)
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
