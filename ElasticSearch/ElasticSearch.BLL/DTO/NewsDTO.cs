using ElasticSearch.BLL.ElasticSearchOptions.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch.BLL.DTO
{
    public class NewsDTO : ElasticEntity<int>
    {
        public int PkNewsId { get; set; }
        public string? StrHeadSubject { get; set; }
        public string? StrFullNews { get; set; }
        public string? StrTags { get; set; }
        public string? StrSpot { get; set; }
        public string? StrSefLink { get; set; }
        public int? isActive { get; set; }
    }
}
