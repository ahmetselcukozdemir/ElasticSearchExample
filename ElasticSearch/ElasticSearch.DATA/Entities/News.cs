using System;
using System.Collections.Generic;

namespace ElasticSearch.DATA.Entities;

public partial class News
{
    public int Id { get; set; }

    public string? NewsTitle { get; set; }

    public string? NewsDesc { get; set; }

    public string? NewsImage { get; set; }

    public int? NewsCategoryId { get; set; }

    public string? NewsSelflink { get; set; }

    public string? NewsSound { get; set; }
}
