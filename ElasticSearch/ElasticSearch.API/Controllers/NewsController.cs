using ElasticSearch.BLL.Abstract;
using ElasticSearch.BLL.DTO;
using ElasticSearch.BLL.ElasticSearchOptions.Abstract;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ElasticSearch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly INewsService _newsService;
        private readonly IElasticSearchService _elasticSearchService;

        public NewsController(INewsService newsService, IElasticSearchService elasticSearchService)
        {
            _newsService = newsService;
            _elasticSearchService = elasticSearchService;
        }

        //İndex işlemleri

        [HttpGet("createIndex")]
        public async Task<IActionResult> CreateIndex(string indexName)
        {
            var result = await _newsService.CreateIndex(indexName);

            if (result)
                return Ok(200);


            return Ok(404);
        }

        [HttpGet("deleteIndex")]
        public async Task<IActionResult> DeleteIndex(string indexName)
        {
            var result = await _elasticSearchService.DeleteIndexAsync(indexName);

            if (result)
                return Ok(200);


            return Ok(404);
        }

        [HttpGet("deleteIndexbyAliases")]
        public async Task<IActionResult> DeleteIndexByAliasesName(string aliasName)
        {
            var result = await _elasticSearchService.DeleteAliasesAsync(aliasName);

            if (result)
                return Ok(200);


            return Ok(404);
        }



        //Document işlemleri

        [HttpPost("addNewsDocument")]
        public async Task<IActionResult> AddDocumentInIndex(NewsDTO document, string indexName)
        {
            NewsDTO newsDTO = new NewsDTO();
            newsDTO.StrFullNews = document.StrFullNews;
            newsDTO.PkNewsId = document.PkNewsId;
            newsDTO.StrSefLink = document.StrSefLink;
            newsDTO.StrSpot = document.StrSpot;
            newsDTO.SearchingArea = document.SearchingArea;
            newsDTO.StrHeadSubject = document.StrHeadSubject;
            newsDTO.StrTags = document.StrTags;
            newsDTO.Id = document.PkNewsId;
            newsDTO.Suggest = document.Suggest;

            var result = await _newsService.AddDocument(newsDTO, indexName);

            if (result)
                return Ok(200);


            return Ok(404);
        }


        [HttpPost("updateNewsDocument")]
        public async Task<IActionResult> UpdateDocumentInIndex(NewsDTO document, string indexName)
        {
            NewsDTO newsDTO = new NewsDTO();
            newsDTO.StrFullNews = document.StrFullNews;
            newsDTO.PkNewsId = document.PkNewsId;
            newsDTO.StrSefLink = document.StrSefLink;
            newsDTO.StrSpot = document.StrSpot;
            newsDTO.SearchingArea = document.SearchingArea;
            newsDTO.StrHeadSubject = document.StrHeadSubject;
            newsDTO.StrTags = document.StrTags;
            newsDTO.Id = document.PkNewsId;
            newsDTO.isActive = document.isActive;

            var result = await _newsService.UpdateDocument(newsDTO, indexName);

            if (result)
                return Ok(200);


            return Ok(404);
        }


        [HttpGet("getNewsDocumentById")]
        public async Task<IActionResult> GetDocumentInIndex(string indexName, int id)
        {
            var result = await _newsService.GetDocument(indexName, id);
            var json = JsonConvert.SerializeObject(result);


            if (result != null)
                return Content(json);

            return Ok(404);
        }


        [HttpGet("ChangeStatusNewsDocument")]
        public async Task<IActionResult> ChangeStatusDocumentInIndex(string indexName, int id, int value)
        {
            var document = await _newsService.GetDocument(indexName, id);

            if (document != null)
            {
                document.isActive = value;
                var result = await _newsService.UpdateDocument(document, indexName);

                if (result)
                    return Ok(200);

                return Content("Status güncelleme işlemi yapılamadı.");
            }

            return Content("Document bulunamadı.");
        }


        [HttpDelete("DeleteNewsDocumentById")]
        public async Task<IActionResult> DeleteDocumentInIndex(string indexName, int id)
        {
            var result = await _newsService.DeleteDocumentElasticIndex(indexName, id);

            if (result)
                return Ok(200);

            return Content("Document silme işlemi yapılamadı.");
        }




        //Search işlemleri


        [HttpGet("SearchDetailed")]
        public async Task<IActionResult> SearchDetailed(string indexName, string searchText)
        {
            var result = await _newsService.DetailSearchGetAsync(indexName, searchText, 0, 10);

            if (result != null)
                return Ok(JsonConvert.SerializeObject(result));

            return Content("Arama işlemi yapılamadı.");
        }


        [HttpGet("SearchSuggest")]
        public async Task<IActionResult> SearchSuggest(string indexName, string searchText)
        {
            var result = await _newsService.SuggestSearchAsync(searchText, indexName);

            if (result != null)
                return Ok(JsonConvert.SerializeObject(result));

            return Content("Arama işlemi yapılamadı.");
        }

    }
}
