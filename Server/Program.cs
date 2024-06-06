using Asp.Versioning;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(
 config=>{
    config.ApiVersionReader = new HeaderApiVersionReader("api-version");
 });
//options =>
//  {
//      options.DefaultApiVersion = new ApiVersion(1, 0);
//      options.AssumeDefaultVersionWhenUnspecified = true;
//      options.ReportApiVersions = true;
//  })
//    .AddApiExplorer(options =>
//    {
//        options.GroupNameFormat = "'v'VVV";
//        options.SubstituteApiVersionInUrl = true;
//    });

builder.Services.AddSwaggerGen(options => { 
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title="Server v1"
    });
    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",
        Title = "Server v2"
    });
    var filename= Assembly.GetExecutingAssembly().GetName().Name+".xml";
    var filepath= Path.Combine(AppContext.BaseDirectory, filename);
    options.IncludeXmlComments(filepath);
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options=>{options.SwaggerEndpoint("/swagger/v1/swagger.json","The server api v1");});
}

app.UseAuthorization();

app.MapControllers();


app.Run();
