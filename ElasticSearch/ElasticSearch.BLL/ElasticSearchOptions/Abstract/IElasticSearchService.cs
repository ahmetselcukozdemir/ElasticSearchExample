using ElasticSearch.BLL.DTO;
using System;
using Nest;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElasticSearch.BLL.ElasticSearchOptions.Concrete;

namespace ElasticSearch.BLL.ElasticSearchOptions.Abstract
{
    public interface IElasticSearchService
    {

        Task<NewsDTO> GetDocumentByIDAsync<T, TKey>(string indexName, int id) where T : ElasticEntity<TKey>;
        Task<bool> DeleteAliasesAsync(string indexName);
        Task<int> CreateIndexAsync<T, TKey>(string indexName) where T : ElasticEntity<TKey>;
        Task<bool> DeleteIndexAsync(string indexName);
        Task AddOrUpdateAsync<T, TKey>(string indexName, T model) where T : ElasticEntity<TKey>;

        Task AddDocumentAsync<T, TKey>(string indexName, T model) where T : ElasticEntity<TKey>;

        Task UpdateDocumentAsync<T, TKey>(string indexName, T model) where T : ElasticEntity<TKey>;

        Task DeleteDocumentAsync(string indexName, int id);

        Task<ISearchResponse<T>> DetailSearchAsync<T, TKey>(string indexName, SearchDescriptor<T> query,
            int skip, int size, string[] includeFields = null, bool disableHigh = false, params string[] highField) where T : ElasticEntity<TKey>;
        Task ReIndex<T, TKey>(string indexName) where T : ElasticEntity<TKey>;

        Task<ISearchResponse<T>> SimpleSearchAsync<T, TKey>(string indexName, SearchDescriptor<T> query) where T : ElasticEntity<TKey>;
        Task<ISearchResponse<T>> SearchAsync<T, TKey>(string indexName, SearchDescriptor<T> query,
            int skip, int size, string[] includeFields = null, bool disableHigh = false, params string[] highField) where T : ElasticEntity<TKey>;
        Task CrateIndexAsync(string indexName);

        Task ReBuild<T, TKey>(string indexName) where T : ElasticEntity<TKey>;

        Task CreateIndexSuggestAsync<T, TKey>(string indexName) where T : ElasticEntity<TKey>;

        Task CreateIndexCustomSuggestAsync<T, TKey>(string indexName) where T : ElasticEntity<TKey>;




        Task BulkAddorUpdateAsync<T, TKey>(string indexName, List<T> list, int bulkNum = 1000) where T : ElasticEntity<TKey>;



        Task BulkDeleteAsync<T, TKey>(string indexName, List<T> list, int bulkNum = 1000) where T : ElasticEntity<TKey>;

    }
}
