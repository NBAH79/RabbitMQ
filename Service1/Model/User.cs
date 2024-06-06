using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Service1.Model
{
    [Index("Id")]
    public class User
    {
        [Key]
        [Column("UserId")]
        public Guid Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description {get;set; }

        public Car? Car { get; set; } //навигационное свойство, оно не запишется
    }
}
