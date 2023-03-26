using AutoMapper;
using ElasticSearch.BLL.Abstract;
using ElasticSearch.BLL.Concrete;
using ElasticSearch.BLL.ElasticSearchOptions.Abstract;
using ElasticSearch.BLL.ElasticSearchOptions.Concrete;
using ElasticSearch.BLL.Mappings;
using ElasticSearch.DATA.Contexts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


var mappingConfig = new MapperConfiguration(mc =>
{
    mc.AddProfile(new CustomMappingProfile());
});
//IMapper mapper = mappingConfig.CreateMapper();
builder.Services.AddSingleton(mappingConfig.CreateMapper());

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<INewsService, NewsService>();



var key = builder.Configuration.GetValue<string>("ConnectionStrings:db");
builder.Services.AddDbContext<NewsContext>(options => options.UseSqlServer(key));
builder.Services.AddScoped<IElasticSearchService, ElasticSearchManager>();
builder.Services.AddScoped<IElasticSearchConfigration, ElasticSearchConfigration>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
