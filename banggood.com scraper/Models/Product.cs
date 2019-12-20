using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace banggood.com_scraper.Models
{
    public class Product
    {
        public string ProductUrl { get; set; }
        public string Title { get; set; }
        public string ProductDetails { get; set; }
        public string Price { get; set; }
        public string Images { get; set; }
        public string Specifications { get; set; }
        public string Description { get; set; }
        public string Features { get; set; }
        public string PackageIncluded { get; set; }


    }
}