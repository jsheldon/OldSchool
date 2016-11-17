using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OldSchool.Models
{
    [Table("User")]
    public class User
    {
        public User()
        {
            Properties = new Dictionary<string, object>();
        }

        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column("Username")]
        [StringLength(512)]
        public string Username { get; set; }

        [Required]
        [Column("Password")]
        [StringLength(512)]
        public string Password { get; set; }

        [Required]
        [Column("Seed")]
        public Guid Seed { get; set; }

        [Required]
        public string PropertiesBlob { get; set; }

        [NotMapped]
        public IDictionary<string, object> Properties { get; set; }

        [Column("DateAdded")]
        public DateTime DateAdded { get; set; }

        [Column("IpAddress")]
        public string IpAddress { get; set; }
    }
}