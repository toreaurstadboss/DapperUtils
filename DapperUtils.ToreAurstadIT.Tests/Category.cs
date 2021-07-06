using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DapperUtils.ToreAurstadIT.Tests
{
    [Table("Categories")]
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public byte Picture { get; set; }
    }
}
