using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Service1.Model
{
    [Index("Id")]
    public class Car
    {
        [Key]
        [Column("CarId")]
        public Int64 Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description { get; set; }

        public List<User> Users { get; set; } = new List<User>();
    }
}
