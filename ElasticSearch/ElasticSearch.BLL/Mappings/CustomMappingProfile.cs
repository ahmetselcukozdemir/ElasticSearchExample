using ElasticSearch.BLL.DTO;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using ElasticSearch.DATA.Entities;

namespace ElasticSearch.BLL.Mappings
{
    public class CustomMappingProfile : AutoMapper.Profile
    {
        public CustomMappingProfile()
        {
            CreateMap<News, NewsDTO>();
            CreateMap<NewsDTO, News>();
        }


    }
}
