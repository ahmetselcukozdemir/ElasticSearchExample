using ElasticSearch.BLL.DTO;
using ElasticSearch.BLL.ElasticSearchOptions.Abstract;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch.BLL.ElasticSearchOptions.Concrete
{
    public class ElasticSearchManager : IElasticSearchService
    {
        public IElasticClient ElasticSearchClient { get; set; }
        private readonly IElasticSearchConfigration _elasticSearchConfigration;

        public ElasticSearchManager(IElasticSearchConfigration elasticSearchConfigration)
        {
            _elasticSearchConfigration = elasticSearchConfigration;
            ElasticSearchClient = GetClient();
        }

        private ElasticClient GetClient()
        {
            var str = _elasticSearchConfigration.ConnectionString;
            var strs = str.Split('|');
            var nodes = strs.Select(s => new Uri(s)).ToList();

            var connectionString = new ConnectionSettings(new Uri(str))
                .DisablePing()
                .SniffOnStartup(false)
                .SniffOnConnectionFault(false);

            if (!string.IsNullOrEmpty(_elasticSearchConfigration.AuthUserName) && !string.IsNullOrEmpty(_elasticSearchConfigration.AuthPassWord))
            {
                connectionString.BasicAuthentication(_elasticSearchConfigration.AuthUserName, _elasticSearchConfigration.AuthPassWord);
            }

            return new ElasticClient(connectionString);
        }

        #region İNDEX İŞLEMLERİ

        public virtual async Task<int> CreateIndexAsync<T, TKey>(string indexName) where T : ElasticEntity<TKey>
        {

            var exis = await ElasticSearchClient.Indices.ExistsAsync(indexName);

            if (exis.Exists)
                return 0;
            var newName = indexName + DateTime.Now.Ticks;
            var result = await ElasticSearchClient.Indices
                .CreateAsync(newName,
                    ss =>
                        ss.Index(newName)
                            .Settings(
                                o => o.NumberOfShards(4).NumberOfReplicas(2).Setting("max_result_window", int.MaxValue)
                                         .Analysis(a => a
                        .TokenFilters(tkf => tkf.AsciiFolding("my_ascii_folding", af => af.PreserveOriginal(true)))
                        .Analyzers(aa => aa
                        .Custom("turkish_analyzer", ca => ca
                         .Filters("lowercase", "my_ascii_folding")
                         .Tokenizer("standard")
                         )))
                        )
                            .Mappings(m => m.Map<T>(mm => mm.AutoMap()
                            .Properties(p => p
                 .Text(t => t.Name(n => n.SearchingArea)
                .Analyzer("turkish_analyzer")
            ).Completion(c => c
                    .Name("suggest")

                )))));
            if (result.Acknowledged)
            {
                await ElasticSearchClient.Indices.BulkAliasAsync(al => al.Add(add => add.Index(newName).Alias(indexName)));
                return 1;
            }
            throw new ElasticSearchException($"Create Index {indexName} failed : :" + result.ServerError.Error.Reason);
        }

        public virtual async Task<bool> DeleteIndexAsync(string indexName)
        {
            var response = await ElasticSearchClient.Indices.DeleteAsync(indexName);
            if (response.Acknowledged) return true;
            return false;
            throw new ElasticSearchException($"Delete index {indexName} failed :{response.ServerError.Error.Reason}");
        }

        public async Task<bool> DeleteAliasesAsync(string aliasesName)
        {


            var indexExists = ElasticSearchClient.Indices.Exists(aliasesName).Exists;
            ElasticSearchClient.Indices.BulkAlias(aliases =>
            {
                if (indexExists)
                {
                    var oldIndices = ElasticSearchClient.GetIndicesPointingToAlias(aliasesName);
                    var indexName = oldIndices.First().ToString();

                    //remove alias from live index
                    aliases.Remove(a => a.Alias(aliasesName).Index("*"));
                }

                return null;
            });



            return true;
            //var response = await ElasticSearchClient.Indices.DeleteAliasAsync(oldIndices);

            //if (response.ServerError == null) return true;
            //return false;
            //throw new ElasticSearchException($"Delete Docuemnt at index {aliasesName} :{response.ServerError.Error.Reason}");
        }

        #endregion

        #region DOCUMENT İŞLEMLERİ
        public async Task AddDocumentAsync<T, TKey>(string indexName, T model) where T : ElasticEntity<TKey>
        {
            var exis = ElasticSearchClient.DocumentExists(DocumentPath<T>.Id(new Id(model)), dd => dd.Index(indexName));

            if (exis.Exists)
            {
                throw new ElasticSearchException($"Add Document failed at index{indexName} :" + "Doc zaten tanımlı. Güncellemek için UpdateDocumentAsync kullanın.");
            }
            else
            {
                var result = await ElasticSearchClient.IndexAsync(model, ss => ss.Index(indexName));
                if (result.ServerError == null) return;
                throw new ElasticSearchException($"Insert Docuemnt failed at index {indexName} :" + result.ServerError.Error.Reason);
            }
        }

        public async Task UpdateDocumentAsync<T, TKey>(string indexName, T model) where T : ElasticEntity<TKey>
        {
            var exis = ElasticSearchClient.DocumentExists(DocumentPath<T>.Id(new Id(model)), dd => dd.Index(indexName));

            if (exis.Exists)
            {
                var result = await ElasticSearchClient.UpdateAsync(DocumentPath<T>.Id(new Id(model)),
                    ss => ss.Index(indexName).Doc(model).RetryOnConflict(3));

                if (result.ServerError == null) return;
                throw new ElasticSearchException($"Update Document failed at index{indexName} :" + result.ServerError.Error.Reason);
            }
            else
            {
                throw new ElasticSearchException($"Update Docuemnt failed at index {indexName} :" + "Doc bulunamadı. AddDocumentAsync ile yeni eklemeyi deneyin.");
            }
        }
        public async Task<NewsDTO> GetDocumentByIDAsync<T, TKey>(string indexName, int id) where T : ElasticEntity<TKey>
        {
            var responseBasic = await ElasticSearchClient.SearchAsync<NewsDTO>(s => s.Index(indexName).Query(q => q.Term(t => t.PkNewsId, id)));

            if (responseBasic.Documents.Count() > 0)
            {
                var result = responseBasic.Documents.First();
                return result;
                throw new ElasticSearchException($"Get Document failed at index{indexName} :" + "Bilinmeyen hata!");
            }
            else
            {
                throw new ElasticSearchException($"Get Docuemnt failed at index {indexName} :" + id + "'li Document bulunamadı.");
            }
        }

        public async Task DeleteDocumentAsync(string indexName, int id)
        {
            var response = await ElasticSearchClient.DeleteAsync(new DeleteRequest(indexName, new Id(id)));
            if (response.ServerError == null && response.Result.ToString() != "NotFound") return;

            if (response.Result.ToString() == "NotFound")
            {
                throw new ElasticSearchException($"Delete Docuemnt at index {indexName} :" + id + "' li Document bulunamadı.");
            }
            throw new ElasticSearchException($"Delete Docuemnt at index {indexName} :{response.ServerError.Error.Reason}");
        }
        #endregion


        #region SEARCH İŞLEMLERİ

        public virtual async Task<ISearchResponse<T>> SimpleSearchAsync<T, TKey>(string indexName, SearchDescriptor<T> query) where T : ElasticEntity<TKey>
        {
            query.Index(indexName);
            var response = await ElasticSearchClient.SearchAsync<T>(query);
            return response;
        }
        public virtual async Task<ISearchResponse<T>> SearchAsync<T, TKey>(string indexName, SearchDescriptor<T> query, int skip, int size, string[] includeFields = null,
                                                                            bool disableHigh = false, params string[] highField) where T : ElasticEntity<TKey>
        {
            var ishigh = highField != null && highField.Length > 0;
            var hfs = new List<Func<HighlightFieldDescriptor<T>, IHighlightField>>();

            //Keyword highlighting
            if (ishigh)
            {
                foreach (var s in highField)
                {
                    hfs.Add(f => f.Field(s));
                }
            }


            //var searchResponse = ElasticSearchClient.Search<NewsDTO>(s => s
            //            .Index("elasticdb638107712686312957")
            //            .Suggest(su => su
            //                .Completion("post_suggestions",
            //                    c => c.Field(f => f.StrFullNews)
            //                    .Analyzer("simple")
            //                    .Prefix("Rec")
            //                    .Fuzzy(f => f.Fuzziness(Nest.Fuzziness.Auto))
            //                    .Size(10)
            //                )
            //            )
            //        );



            //var searchResponse = await ElasticSearchClient.SearchAsync<T>(s => s
            //       .Index(indexName)
            //       .Query(q => q
            //                   .MultiMatch(m => m.Fields(f => f
            //                                        .Field("strHeadSubject", 2.0)
            //                                        .Field("strSpot", 1.0)
            //                                        .Field("strFullNews", 1.0)
            //                                   )
            //                                  .Query("Cumhurbaşkanı Recep Tayyip Erdoğan")
            //                                  .Type(TextQueryType.BestFields)
            //                                  .Operator(Operator.Or)  // Operator.And  dene 
            //                            )
            //               )
            //               .Highlight(h => h
            //                   .Fields(hfs.ToArray())
            //                   .PreTags("<kamil>")
            //                   .PostTags("</kamil>")
            //               )
            //           );


            string preTags = "<strong style=\"color: red;\">", postTags = "</strong>";

            query.Index(indexName);
            var highdes = new HighlightDescriptor<T>();
            if (disableHigh)
            {
                preTags = "";
                postTags = "";
            }
            highdes.PreTags(preTags).PostTags(postTags);

            //Pagination
            query.Skip(skip).Take(size);


            highdes.Fields(hfs.ToArray());
            query.Highlight(h => highdes);

            if (includeFields != null)
                query.Source(ss => ss.Includes(ff => ff.Fields(includeFields.ToArray())));


            var data = JsonConvert.SerializeObject(query);
            var response = await ElasticSearchClient.SearchAsync<T>(query);


            return response;
        }

        public virtual async Task<ISearchResponse<T>> DetailSearchAsync<T, TKey>(string indexName, SearchDescriptor<T> query, int skip, int size, string[] includeFields = null,
                                                                            bool disableHigh = false, params string[] highField) where T : ElasticEntity<TKey>
        {
            query.Index(indexName);

            //Highlight
            string preTags = "<strong style=\"color: red;\">", postTags = "</strong>";
            var ishigh = highField != null && highField.Length > 0;
            var hfs = new List<Func<HighlightFieldDescriptor<T>, IHighlightField>>();

            if (ishigh)
            {
                foreach (var s in highField)
                {
                    hfs.Add(f => f.Field(s));
                }
            }
            var highdes = new HighlightDescriptor<T>();
            if (disableHigh)
            {
                preTags = "";
                postTags = "";
            }
            highdes.PreTags(preTags).PostTags(postTags);
            highdes.Fields(hfs.ToArray());


            //Pagination
            query.Skip(skip).Take(size);
            query.Highlight(h => highdes);

            //Select Column
            if (includeFields != null)
            {
                query.Source(ss => ss.Includes(ff => ff.Fields(includeFields.ToArray())));
            }

            var data = JsonConvert.SerializeObject(query);
            var response = await ElasticSearchClient.SearchAsync<T>(query);

            return response;
        }

        #region çalışan highlight
        //var searchResponse = await ElasticSearchClient.SearchAsync<NewsDTO>(s => s
        //       .Index(indexName)
        //       .Query(q => q
        //                   .Match(m => m
        //                       .Field(f => f.StrHeadSubject)
        //                       .Query("bakan")
        //                   )
        //               )
        //               .Highlight(h => h
        //                   .Fields(
        //                       f => f.Field(f => f.StrHeadSubject)
        //                   )
        //                   .PreTags("<em>")
        //                   .PostTags("</em>")
        //               )
        //           );
        #endregion




        #endregion










        public virtual async Task CrateIndexAsync(string indexName)
        {
            var exis = await ElasticSearchClient.Indices.ExistsAsync(indexName);
            if (exis.Exists)
                return;
            var newName = indexName + DateTime.Now.Ticks;
            var result = await ElasticSearchClient.Indices.CreateAsync(newName,
                    ss =>
                        ss.Index(newName)
                            .Settings(
                                o => o.NumberOfShards(4).NumberOfReplicas(2).Setting("max_result_window", int.MaxValue)));
            if (result.Acknowledged)
            {
                await ElasticSearchClient.Indices.BulkAliasAsync(al => al.Add(add => add.Index(newName).Alias(indexName)));
                return;
            }
            throw new ElasticSearchException($"Create Index {indexName} failed :" + result.ServerError.Error.Reason);
        }

        public virtual async Task AddOrUpdateAsync<T, TKey>(string indexName, T model) where T : ElasticEntity<TKey>
        {
            var exis = ElasticSearchClient.DocumentExists(DocumentPath<T>.Id(new Id(model)), dd => dd.Index(indexName));

            if (exis.Exists)
            {
                var result = await ElasticSearchClient.UpdateAsync(DocumentPath<T>.Id(new Id(model)),
                    ss => ss.Index(indexName).Doc(model).RetryOnConflict(3));

                if (result.ServerError == null) return;
                throw new ElasticSearchException($"Update Document failed at index{indexName} :" + result.ServerError.Error.Reason);
            }
            else
            {
                var result = await ElasticSearchClient.IndexAsync(model, ss => ss.Index(indexName));
                if (result.ServerError == null) return;
                throw new ElasticSearchException($"Insert Docuemnt failed at index {indexName} :" + result.ServerError.Error.Reason);
            }
        }
        public virtual async Task DeleteAsync<T, TKey>(string indexName) where T : ElasticEntity<TKey>
        {
            var response = await ElasticSearchClient.Indices.DeleteAsync(indexName);
            if (response.ServerError == null) return;
            throw new ElasticSearchException($"Delete Docuemnt at index {indexName} :{response.ServerError.Error.Reason}");
        }

        public virtual async Task ReIndex<T, TKey>(string indexName) where T : ElasticEntity<TKey>
        {
            await DeleteIndexAsync(indexName);
            await CreateIndexAsync<T, TKey>(indexName);
        }

        public virtual async Task BulkAddorUpdateAsync<T, TKey>(string indexName, List<T> list, int bulkNum = 1000) where T : ElasticEntity<TKey>
        {
            if (list.Count <= bulkNum)
                await BulkAddOrUpdate<T, TKey>(indexName, list);
            else
            {
                var total = (int)Math.Ceiling(list.Count * 1.0f / bulkNum);
                var tasks = new List<Task>();
                for (var i = 0; i < total; i++)
                {
                    var i1 = i;
                    tasks.Add(await Task.Factory.StartNew(async () => await BulkAddOrUpdate<T, TKey>(indexName, list.Skip(i1 * bulkNum).Take(bulkNum).ToList()))); ;
                }
                await Task.WhenAll(tasks.ToArray());
            }
        }
        public virtual async Task CreateIndexSuggestAsync<T, TKey>(string indexName) where T : ElasticEntity<TKey>
        {
            var exis = await ElasticSearchClient.Indices.ExistsAsync(indexName);

            if (exis.Exists)
                return;
            var newName = indexName + DateTime.Now.Ticks;

            var createIndexDescriptor = new CreateIndexDescriptor(newName)
                  .Settings(o => o.NumberOfShards(1).NumberOfReplicas(1).Setting("max_result_window", int.MaxValue))
                  .Mappings(ms => ms
                           .Map<T>(m => m
                                 .AutoMap()
                                 .Properties(ps => ps
                                     .Completion(c => c
                                         .Name(p => p.Suggest))))
                                         );

            var result = await ElasticSearchClient.Indices
                 .CreateAsync(createIndexDescriptor);

            if (result.Acknowledged)
            {
                await ElasticSearchClient.Indices.BulkAliasAsync(al => al.Add(add => add.Index(newName).Alias(indexName)));
                return;
            }
            throw new ElasticSearchException($"Create Index {indexName} failed : :" + result.ServerError.Error.Reason);
        }
        private async Task BulkAddOrUpdate<T, TKey>(string indexName, List<T> list) where T : ElasticEntity<TKey>
        {
            var bulk = new BulkRequest(indexName)
            {
                Operations = new List<IBulkOperation>()
            };
            foreach (var item in list)
            {
                bulk.Operations.Add(new BulkIndexOperation<T>(item));
            }
            var response = await ElasticSearchClient.BulkAsync(bulk);
            if (response.Errors)
                throw new ElasticSearchException($"Bulk InsertOrUpdate Docuemnt failed at index {indexName} :{response.ServerError.Error.Reason}");
        }
        private async Task BulkDelete<T, TKey>(string indexName, List<T> list) where T : ElasticEntity<TKey>
        {
            var bulk = new BulkRequest(indexName)
            {
                Operations = new List<IBulkOperation>()
            };
            foreach (var item in list)
            {
                bulk.Operations.Add(new BulkDeleteOperation<T>(new Id(item)));
            }
            var response = await ElasticSearchClient.BulkAsync(bulk);
            if (response.Errors)
                throw new ElasticSearchException($"Bulk Delete Docuemnt at index {indexName} :{response.ServerError.Error.Reason}");
        }
        public virtual async Task BulkDeleteAsync<T, TKey>(string indexName, List<T> list, int bulkNum = 100) where T : ElasticEntity<TKey>
        {
            if (list.Count <= bulkNum)
                await BulkDelete<T, TKey>(indexName, list);
            else
            {
                var total = (int)Math.Ceiling(list.Count * 1.0f / bulkNum);
                var tasks = new List<Task>();
                for (var i = 0; i < total; i++)
                {
                    var i1 = i;
                    tasks.Add(await Task.Factory.StartNew(async () => await BulkDelete<T, TKey>(indexName, list.Skip(i1 * bulkNum).Take(bulkNum).ToList())));
                }
                await Task.WhenAll(tasks);
            }
        }
        public virtual async Task ReBuild<T, TKey>(string indexName) where T : ElasticEntity<TKey>
        {

            var result = await ElasticSearchClient.Indices.GetAliasAsync(indexName);
            var oldName = result.Indices.Keys.FirstOrDefault();

            if (oldName == null)
            {
                throw new ElasticSearchException($"not found index {indexName}");
            }
            //Create a new index
            var newIndex = indexName + DateTime.Now.Ticks;
            var createResult = await ElasticSearchClient.Indices.CreateAsync(newIndex,
                c =>
                    c.Index(newIndex)
                        .Mappings(ms => ms.Map<T>(m => m.AutoMap())));
            if (!createResult.Acknowledged)
            {
                throw new ElasticSearchException($"reBuild create newIndex {indexName} failed :{result.ServerError.Error.Reason}");
            }
            //Rebuild index data
            var reResult = await ElasticSearchClient.ReindexOnServerAsync(descriptor => descriptor.Source(source => source.Index(indexName))
                .Destination(dest => dest.Index(newIndex)));

            if (reResult.ServerError != null)
            {
                throw new ElasticSearchException($"reBuild {indexName} datas failed :{reResult.ServerError.Error.Reason}");
            }

            //Delete old index
            var alReuslt = await ElasticSearchClient.Indices.BulkAliasAsync(al => al.Remove(rem => rem.Index(oldName.Name).Alias(indexName)).Add(add => add.Index(newIndex).Alias(indexName)));

            if (!alReuslt.Acknowledged)
            {
                throw new ElasticSearchException($"reBuild set Alias {indexName}  failed :{alReuslt.ServerError.Error.Reason}");
            }
            //var delResult = await ElasticSearchClient.Indices.BulkAliasAsync();
            throw new ElasticSearchException($"reBuild delete old Index {oldName.Name} failed :"/*delResult.ServerError.Error.Reason*/);
        }
        public virtual async Task CreateIndexCustomSuggestAsync<T, TKey>(string indexName) where T : ElasticEntity<TKey>
        {
            var exis = await ElasticSearchClient.Indices.ExistsAsync(indexName);

            if (exis.Exists)
                return;
            var newName = indexName + DateTime.Now.Ticks;

            var createIndexDescriptor = new CreateIndexDescriptor(newName)
                  .Settings(o => o.NumberOfShards(1).NumberOfReplicas(1).Setting("max_result_window", int.MaxValue))
                  .Mappings(ms => ms
                           .Map<T>(m => m
                                 .AutoMap()
                                 .Properties(ps => ps
                                     .Completion(c => c
                                        .Contexts(ctx => ctx.Category(csg => csg.Name("userId").Path("u")))
                                          .Name(d => d.Suggest)
                                        ))));

            var result = await ElasticSearchClient.Indices.CreateAsync(createIndexDescriptor);

            if (result.Acknowledged)
            {
                await ElasticSearchClient.Indices.BulkAliasAsync(al => al.Add(add => add.Index(newName).Alias(indexName)));
                return;
            }
            throw new ElasticSearchException($"Create Index {indexName} failed : :" + result.ServerError.Error.Reason);
        }
        public string ToJson<T>(SearchDescriptor<NewsDTO> searchDescriptor)
        {
            var stream = new MemoryStream();
            ElasticSearchClient.RequestResponseSerializer.Serialize(searchDescriptor, stream);
            var jsonQuery = Encoding.UTF8.GetString(stream.ToArray());
            return jsonQuery;
        }

        public virtual async Task CreateIndexAsync2<T, TKey>(string indexName) where T : ElasticEntity<TKey>
        {
            var exis = await ElasticSearchClient.Indices.ExistsAsync(indexName);

            if (exis.Exists)
                return;
            var newName = indexName + DateTime.Now.Ticks;
            var result = await ElasticSearchClient.Indices
                .CreateAsync(newName,
                    ss =>
                        ss.Index(newName)
                            .Settings(o => o.NumberOfShards(4).NumberOfReplicas(2).Setting("max_result_window", int.MaxValue))
                            .Mappings(m =>
                                          m.Map<T>(mm =>
                                                        mm.AutoMap()
                                                            .Properties(p =>
                                                                             p.Text(t =>
                                                                                         t.Name(n => n.SearchingArea)
                                                                                   )
                                                                        )
                                                   )
                                      )
                                 );
            if (result.Acknowledged)
            {
                await ElasticSearchClient.Indices.BulkAliasAsync(al => al.Add(add => add.Index(newName).Alias(indexName)));
                return;
            }
            throw new ElasticSearchException($"Create Index {indexName} failed : :" + result.ServerError.Error.Reason);
        }
    }
}
