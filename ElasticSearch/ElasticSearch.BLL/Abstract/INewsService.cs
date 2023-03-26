using ElasticSearch.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch.BLL.Abstract
{
    public interface INewsService
    {
        Task<bool> CreateIndex(string indexName);
        Task<bool> AddOrUpdateElasticIndex(NewsDTO postElasticIndexDto, string indexName);
        Task<bool> AddDocument(NewsDTO postElasticIndexDto, string indexName);
        Task<bool> UpdateDocument(NewsDTO postElasticIndexDto, string indexName);
        Task<NewsDTO> GetDocument(string indexName, int id);

        Task<bool> DeleteDocumentElasticIndex(string indexName, int id);
        Task<List<NewsDTO>> SuggestSearchAsync(string suggestText, string indexName, int skipItemCount = 0, int maxItemCount = 100);
        Task<List<NewsDTO>> GetSearchAsync(string indexName, string searchText, int skipItemCount = 0, int maxItemCount = 100);
        Task<List<NewsDTO>> DetailSearchGetAsync(string indexName, string searchText, int skipItemCount = 0, int maxItemCount = 100);



    }
}
