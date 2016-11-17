using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OldSchool.Models
{
    [Table("ChatRoom")]
    public class ChatRoom
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column("Name")]
        [StringLength(512)]
        public string Name { get; set; }
    }
}