using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBox.Database.SQLiteMaster.Tables
{
    public class Customer
    {
        [Key]
        [MaxLength(60)]
        public string SenderId { get; set; } 
    }
}
