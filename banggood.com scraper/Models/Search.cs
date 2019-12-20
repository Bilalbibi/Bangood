using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace banggood.com_scraper.Models
{
    public class Search
    {
        public string CampaignName { get; set; }
        public string SearchTerm { get; set; }
        public string AmazonSearchTerms { get; set; }
        public string AmazonSearchVolume { get; set; }
        public string DominantCategories { get; set; }
    }
}
