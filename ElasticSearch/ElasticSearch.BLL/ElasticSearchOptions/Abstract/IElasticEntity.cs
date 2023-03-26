using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch.BLL.ElasticSearchOptions.Abstract
{
    public interface IElasticEntity<TEntityKey>
    {
        TEntityKey Id { get; set; }
    }
}
