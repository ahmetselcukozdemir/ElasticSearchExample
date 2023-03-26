using ElasticSearch.BLL.Abstract;
using ElasticSearch.BLL.DTO;
using ElasticSearch.BLL.ElasticSearchOptions.Abstract;
using ElasticSearch.BLL.ElasticSearchOptions.Concrete;
using ElasticSearch.DATA.Entities;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ElasticSearch.BLL.Concrete
{
    public class NewsService : INewsService
    {
        public IElasticClient EsClient { get; set; }
        private readonly IElasticSearchService _elasticSearchService;

        public NewsService(IElasticSearchService elasticSearchService)
        {
            _elasticSearchService = elasticSearchService;
        }

        public async Task<bool> CreateIndex(string indexName)
        {
            try
            {
                // Her ekleme işleminde daha önce Index oluşturulup oluşturulmadığını kontrol ediyoruz.
                var result = await _elasticSearchService.CreateIndexAsync<NewsDTO, int>(indexName);

                if (result == 1)
                {
                    return await Task.FromResult<bool>(true);
                }
                else
                {
                    return await Task.FromResult<bool>(false);
                }


            }
            catch (Exception ex)
            {
                return await Task.FromException<bool>(ex);
            }
        }

        public async Task<bool> AddDocument(NewsDTO postElasticIndexDto, string indexName)
        {
            try
            {
                // Her ekleme işleminde daha önce Index oluşturulup oluşturulmadığını kontrol ediyoruz.
                await _elasticSearchService.CreateIndexAsync<NewsDTO, int>(indexName);

                // Yeni bir elasticindex kayıt ekliyoruz(Document)
                await _elasticSearchService.AddDocumentAsync<NewsDTO, int>(indexName, postElasticIndexDto);
                return await Task.FromResult<bool>(true);
            }
            catch (Exception ex)
            {
                return await Task.FromException<bool>(ex);
            }
        }

        public async Task<bool> UpdateDocument(NewsDTO postElasticIndexDto, string indexName)
        {
            try
            {
                // Her ekleme işleminde daha önce Index oluşturulup oluşturulmadığını kontrol ediyoruz.
                await _elasticSearchService.CreateIndexAsync<NewsDTO, int>(indexName);

                // Yeni bir elasticindex kayıt ekliyoruz(Document)
                await _elasticSearchService.UpdateDocumentAsync<NewsDTO, int>(indexName, postElasticIndexDto);
                return await Task.FromResult<bool>(true);
            }
            catch (Exception ex)
            {
                return await Task.FromException<bool>(ex);
            }
        }
        public async Task<NewsDTO> GetDocument(string indexName, int id)
        {
            try
            {
                return await _elasticSearchService.GetDocumentByIDAsync<NewsDTO, int>(indexName, id);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> DeleteDocumentElasticIndex(string indexName, int id)
        {
            try
            {
                await _elasticSearchService.DeleteDocumentAsync(indexName, id);
                return true;
            }
            catch (Exception ex)
            {
                return await Task.FromException<bool>(ex);
            }
        }

        public async Task<bool> AddOrUpdateElasticIndex(NewsDTO postElasticIndexDto, string indexName)
        {
            try
            {
                // Her ekleme işleminde daha önce Index oluşturulup oluşturulmadığını kontrol ediyoruz.
                await _elasticSearchService.CreateIndexAsync<NewsDTO, int>(indexName);

                // Yeni bir elasticindex kayıt ekliyoruz(Document)
                await _elasticSearchService.AddOrUpdateAsync<NewsDTO, int>(indexName, postElasticIndexDto);
                return await Task.FromResult<bool>(true);
            }
            catch (Exception ex)
            {
                return await Task.FromException<bool>(ex);
            }
        }

        public async Task<List<NewsDTO>> SuggestSearchAsync(string searchText, string indexName, int skipItemCount = 0, int maxItemCount = 5)
        {
            try
            {

                var searchDescriptor = new SearchDescriptor<NewsDTO>()
                        .Suggest(s => s
                                .Completion("Suggest",
                        c => c.Field(f => f.Suggest)
                            .Analyzer("simple")
                            .Prefix(searchText)
                            .Fuzzy(f => f.Fuzziness(Nest.Fuzziness.Auto))
                            .Size(10))
                );

                var returnData = await _elasticSearchService.SearchAsync<NewsDTO, int>(indexName, searchDescriptor, 0, 10);


                var a = returnData.Suggest["Suggest"].First().Options.Select(x => x.Text).ToList();

                var data = JsonConvert.SerializeObject(returnData);

                var suggestsList = returnData.Suggest.Keys.Count() > 0 ? from suggest in returnData.Suggest["Suggest"]
                                                                         from option in suggest.Options
                                                                         select new NewsDTO
                                                                         {
                                                                             PkNewsId = option.Source.PkNewsId,
                                                                             StrHeadSubject = option.Source.StrHeadSubject,
                                                                             StrFullNews = option.Source.StrFullNews,
                                                                             StrTags = option.Source.StrTags,
                                                                             StrSpot = option.Source.StrSpot,
                                                                             StrSefLink = option.Source.StrSefLink
                                                                         }
                                                                  : null;

                return await Task.FromResult(suggestsList.ToList());
            }
            catch (Exception ex)
            {
                return await Task.FromException<List<NewsDTO>>(ex);
            }
        }

        public async Task<List<NewsDTO>> GetSearchAsync(string indexName, string searchText, int skipItemCount = 0, int maxItemCount = 100)
        {
            try
            {

                var searchQuery = new Nest.SearchDescriptor<NewsDTO>();


                //Termler: Sadece boolen yani “Yes / No” veya string bir kelime ile eşleşebilecek durumlarda kullanılır.
                // q.Term(t => t.UserId, currentUserId)   // tek bir parametreye ait sorgulama için
                // coklu term işlemleri birden cok parametreye ait sart işlemi için kullanılır.
                searchQuery.Query(q =>
                q.Terms(t => t
                                .Field(ff => ff.Id).Terms<int>(37)
                                .Field(ff => ff.PkNewsId).Terms<int>(37))
                                    // aranan kelime veya cümle geçmesi yeterlidir, bire bir eşleme istemez
                                    && q.MatchPhrasePrefix(m => m.Field(f => f.SearchingArea).Query(searchText))
                                  // aranan kelime veya cümlenin bire bir eşleşmesi gerekmektedir.
                                  || q.MatchPhrase(m => m.Field(f => f.SearchingArea).Query(searchText))
                                  );

                // Çoklu term işlemleri birden cok parametreye ait sart işlemi için kullanılır.
                searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                            .Query(q => q.Terms(t => t.Field(ff => ff.Id).Terms<int>(37)
                                                      .Field(ff => ff.PkNewsId).Terms<int>(37))
                                                );
                // Aranan kelime veya cümle geçmesi yeterlidir, bire bir eşleme istemez
                searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                            .Query(q => q.MatchPhrasePrefix(m => m.Field(f => f.StrFullNews).Query(searchText)));

                // Aranan kelime veya cümlenin bire bir eşleşmesi gerekmektedir.
                searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                            .Query(q => q.MatchPhrase(m => m.Field(f => f.StrFullNews).Query(searchText)));


                // Komplex sorgular seklinde birleştirip yazabiliriz.





                //  arama kelimesi Core İşlemleri sql
                //https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-multi-match-query.html
                searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                         .Query(q => q
                            .MultiMatch(m => m.Fields(f => f.Field(ff => ff.StrFullNews, 2.0)
                                                            .Field(ff => ff.StrSpot, 1.0)
                                                            .Field(ff => ff.StrHeadSubject, 1.0)
                                                     )
                                              .Query(searchText)
                                              .Type(TextQueryType.BestFields)
                                              .Operator(Operator.And)  // Operator.And  dene 
                                        )
                               );


                // 2.0 ve 1.0 ı vermeden yaz ve vererek yaz ama acıkla

                searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                         .Query(q => q
                                      .MultiMatch(m => m.Fields(f => f.Field(ff => ff.StrFullNews, 2.0)
                                                                      .Field(ff => ff.StrSpot, 1.0)
                                                               )
                                                        .Query(searchText)
                                                        .Type(TextQueryType.BestFields)
                                                        .Operator(Operator.Or)
                                                        .MinimumShouldMatch(3)
                                                  )
                              )
                         .Sort(s => s.Descending(f => f.PkNewsId));



                //searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                //          .Query(q =>
                //                       q.MultiMatch(m => m.Fields(f => f.Field(ff => ff.StrSpot, 2.0)
                //                                                       .Field(ff => ff.StrFullNews, 1.0)
                //                                                )
                //                                         .Query(searchText)
                //                                         .Type(TextQueryType.BestFields)
                //                                         .Operator(Operator.Or)  // Operator.And  dene 
                //                                   )
                //                    && q.Range(r => r.Field(rf => rf.TagNameValues.Count).GreaterThan(2))
                //                    || q.Range(r => r.Field(rf => rf.TagNameValues.Count).GreaterThanOrEquals(3))
                //               )
                //          .Sort(s => s.Descending(f => f.Id));

                /*
                 *  https://www.elastic.co/guide/en/elasticsearch/painless/7.5/painless-operators-boolean.html
                 greater_than: expression '>' expression;
                 greater_than_or_equal: expression '>=' expression;
                 less_than: expression '<' expression;
                 greater_than_or_equal: expression '<=' expression;
                 instance_of: ID 'instanceof' TYPE;
                 equality_equals: expression '==' expression;
                 equality_not_equals: expression '!=' expression;
                 identity_equals: expression '===' expression;
                 identity_not_equals: expression '!==' expression;
                 boolean_xor: expression '^' expression;
                 boolean_and: expression '&&' expression;
                 boolean_and: expression '||' expression;
                 

                /*
                 f.Field(ff => ff.SearchingArea, 2.0)  buradaki 2.0 işlemi boost işlemidir.
                 öncelik  ve katsayı işlemidir   ÖNCELİKLENDİRME İŞLEMİDİR.
                 */
                //searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                //          .Query(q =>
                //                       q.MultiMatch(m => m.Fields(f => f.Field(ff => ff.SearchingArea, 2.0)
                //                                                       .Field(ff => ff.Title, 1.0)
                //                                                )
                //                                         .Query(searchText)
                //                                         .Type(TextQueryType.BestFields)
                //                                         .Operator(Operator.Or)  // Operator.And  dene 
                //                                   )
                //                    && q.Range(r => r.Field(rf => rf.TagNameValues.Count).GreaterThan(2))
                //                    && q.Range(r => r.Field(rf => rf.TagNameValues.Count).GreaterThanOrEquals(3))
                //               )
                //          .Sort(s => s.Descending(f => f.CreatedDate.Date))
                //          .Skip(skipItemCount)
                //          .Take(maxItemCount);






                //  searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                //           .Query(q =>
                //q.Bool(b => b.Should(s => TermAny(s, "userCodeCores", userCodeList.ToArray())))
                //                        );


                //            searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                //.Query(q => q
                //    .Bool(b => b
                //        .Should(
                //            bs => bs.Term(p => p.UserId, 1),
                //            bs => bs.Term(p => p.CategoryId, 5)
                //        ).MinimumShouldMatch(3)
                //    )
                //);
                /* bool tipinde sorgular
                
                 must=>Cümle (sorgu) eşleşen belgelerde görünmelidir ve skora katkıda bulunacaktır.
                 filter=> Yan tümce (sorgu) eşleşen belgelerde görünmelidir. Ancak zorunluluktan farklı olarak, sorgunun puanı dikkate alınmaz.
                 should=> Yan tümce (sorgu) eşleşen belgede görünmelidir. Zorunlu veya filtre yan tümcesi olmayan bir boole sorgusunda, bir veya daha fazla yan tümce, 
                          bir belgeyle eşleşmelidir. Eşleşmesi gereken minimum koşul cümlesi sayısı minimum_should_match parametresi kullanılarak ayarlanabilir.
                 must_not=> Yan tümce (sorgu) eşleşen belgelerde görünmemelidir.
                  */


                //searchQuery = new Nest.SearchDescriptor<NewsDTO>()
                //    .Query(q =>
                //                q.Bool(b => b
                //                            .MustNot(m => m.MatchAll())
                //                            .Should(m => m.MatchAll())
                //                            .Must(m => m.MatchAll())
                //                            .Filter(f => f.MatchAll())
                //                            .MinimumShouldMatch(1)
                //                            .Boost(2))
                //    );




                //QueryContainer qvw = new TermQuery { Field = "x", Value = "x" };
                //var xyz = Enumerable.Range(0, 1000).Select(f => qvw).ToArray();
                //var boolQuery = new BoolQuery
                //{
                //    Must = xyz
                //};

                //var c = new QueryContainer();
                //var qq = new TermQuery { Field = "x", Value = "x" };

                //for (var i = 0; i < 10; i++)
                //{
                //    c &= qq;
                //}


                //                searchQuery = new Nest.SearchDescriptor<NewsDTO>().Query(q =>
                //q.QueryString(qs =>
                //qs.DefaultField(d => d.CategoryName).Query(" c# sql server ".Trim()).DefaultOperator(Operator.And)));


                //var dataJson = _elasticSearchService.ToJson<NewsDTO>(searchQuery);



                //, 0, 10,null, "<strong style=\"color: red;\">", "</strong>", false, new string[] { "Title" }
                var searchResultData = await _elasticSearchService.SimpleSearchAsync<NewsDTO, int>(indexName, searchQuery);

                string[] highField = { "strSpot", "strFullNews", "strHeadSubject" };
                string[] includeFields = highField;

                var searchResultData2 = await _elasticSearchService.SearchAsync<NewsDTO, int>(indexName, searchQuery, skipItemCount, maxItemCount, includeFields, false, highField);


                if (searchResultData.Hits.Count > 0)
                { var data = JsonConvert.SerializeObject(searchResultData); }

                //var midir = from opt in searchResultData.Hits
                //            select new NewsDTO
                //            {
                //                Score = (double)opt.Score,
                //                CategoryName = opt.Source.CategoryName,
                //                Title = opt.Source.Title,
                //                UserInfo = opt.Source.UserInfo,
                //                Suggest = opt.Source.Suggest,
                //                Url = opt.Source.Url,
                //                Id = opt.Source.Id,
                //                CategoryId = opt.Source.CategoryId,
                //                CreatedDate = opt.Source.CreatedDate,
                //                UserId = opt.Source.UserId,
                //                TagNameValues = opt.Source.TagNameValues,
                //                TagNameIds = opt.Source.TagNameIds
                //            };


                var result2 = from opt in searchResultData2.Documents
                              select new NewsDTO
                              {
                                  PkNewsId = opt.PkNewsId,
                                  StrHeadSubject = opt.StrHeadSubject,
                                  StrFullNews = opt.StrFullNews,
                                  StrTags = opt.StrTags,
                                  StrSpot = opt.StrSpot,
                                  StrSefLink = opt.StrSefLink
                              };

                return await Task.FromResult<List<NewsDTO>>(result2.ToList());
            }
            catch (Exception ex)
            {
                return await Task.FromException<List<NewsDTO>>(ex);
            }
        }

        public async Task<List<NewsDTO>> DetailSearchGetAsync(string indexName, string searchText, int skipItemCount = 0, int maxItemCount = 100)
        {
            try
            {

                var searchQuery = new Nest.SearchDescriptor<NewsDTO>();

                //BAŞLAYAN(*)/(*)BİTEN
                if (searchText.StartsWith("*") || searchText.EndsWith("*"))
                {
                    searchQuery = searchQuery
                    .Query(q => q
                        .QueryString(qs => qs
                        .Query(searchText)
                            .Fields(f => f
                                .Field(p => p.StrSpot)
                                .Field(p => p.StrFullNews)
                                .Field(p => p.StrHeadSubject)
                             )
                        )
                    );
                }
                //İÇEREN (-)İÇERMEYEN
                else if (searchText.Contains("-"))
                {

                    string includeWords = searchText.Substring(0, searchText.IndexOf("-")).Trim();
                    string excludeWord = searchText.Substring(searchText.IndexOf("-") + 1, (searchText.Length - searchText.Substring(0, searchText.IndexOf("-")).Length) - 1);

                    searchQuery = searchQuery
                    .Query(q => q
                        .Bool(b => b
                            .Must(sh =>
                                sh.Match(mt => mt
                                    .Field("strSpot")
                                    .Query(includeWords)
                                ) ||
                                sh.Match(mt => mt
                                    .Field("strFullNews")
                                    .Query(includeWords)
                                ) ||
                                sh.Match(mt => mt
                                    .Field("strHeadSubject")
                                    .Query(includeWords)
                                )
                            )
                            .MustNot(mn => mn
                            .Bool(bb => bb
                                .Should(ssh => ssh
                                    .Match(mmt => mmt
                                        .Field("strSpot")
                                        .Query(excludeWord)
                                    ) || ssh
                                    .Match(mmt => mmt
                                        .Field("strFullNews")
                                        .Query(excludeWord)
                                    ) || ssh
                                    .Match(mmt => mmt
                                        .Field("strHeadSubject")
                                        .Query(excludeWord)
                                    )
                                )
                            )
                            ))
                    );
                }
                //ÇOKLU BULANIK ARAMA
                else
                {
                    searchQuery = searchQuery
                    .Query(q => q
                        .MultiMatch(m => m
                        .Fields(f => f
                            .Field(f => f.StrSpot)
                            .Field(ff => ff.StrFullNews)
                            .Field(ff => ff.StrHeadSubject))
                        .Query(searchText)
                        .Fuzziness(Fuzziness.Auto)
                        )
                    );
                }


                //SİMPLE SEARCH
                //var searchResultData = await _elasticSearchService.SimpleSearchAsync<NewsDTO, int>(indexName, searchQuery);

                //DETAİLED SEARCH
                string[] highField = { "strSpot", "strFullNews", "strHeadSubject" };
                string[] includeFields = { "pkNewsId", "strSpot", "strFullNews", "strHeadSubject" };
                var searchResultData2 = await _elasticSearchService.DetailSearchAsync<NewsDTO, int>(indexName, searchQuery, skipItemCount, maxItemCount, includeFields, false, highField);


                //TÜM HİGHLİGHT CÜMLECİKLERİNİ ALMA

                List<HighlightArea> highlightAreas = new List<HighlightArea>();
                foreach (var hit in searchResultData2.Hits)
                {
                    int recordid = hit.Source.PkNewsId;

                    foreach (var highlight in hit.Highlight)
                    {
                        foreach (var val in highlight.Value)
                        {
                            HighlightArea area = new HighlightArea();
                            area.Id = recordid;
                            area.FindedSentence = val.ToString();

                            highlightAreas.Add(area);
                        }
                    }

                }


                //DÖNÜŞ MODELİNİ SETLEME
                var result2 = from opt in searchResultData2.Documents
                              select new NewsDTO
                              {
                                  PkNewsId = opt.PkNewsId,
                                  StrHeadSubject = opt.StrHeadSubject,
                                  StrFullNews = opt.StrFullNews,
                                  StrTags = opt.StrTags,
                                  StrSpot = opt.StrSpot,
                                  StrSefLink = opt.StrSefLink,
                                  HighlightAreas = highlightAreas.Where(x => x.Id == opt.PkNewsId).ToList(),
                              };

                return await Task.FromResult<List<NewsDTO>>(result2.ToList());
            }
            catch (Exception ex)
            {
                return await Task.FromException<List<NewsDTO>>(ex);
            }
        }
    }
}
