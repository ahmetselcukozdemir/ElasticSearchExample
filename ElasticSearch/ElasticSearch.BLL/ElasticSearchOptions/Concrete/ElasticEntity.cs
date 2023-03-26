using ElasticSearch.BLL.ElasticSearchOptions.Abstract;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch.BLL.ElasticSearchOptions.Concrete
{
    public class ElasticEntity<TEntityKey> : IElasticEntity<TEntityKey>
    {
        public virtual TEntityKey Id { get; set; }
        public virtual CompletionField Suggest { get; set; }
        public virtual string SearchingArea { get; set; }
        public virtual double? Score { get; set; }
        public virtual List<HighlightArea> HighlightAreas { get; set; }
    }

    public class HighlightArea
    {
        public virtual int Id { get; set; }
        public virtual string FindedSentence { get; set; }

    }
}
