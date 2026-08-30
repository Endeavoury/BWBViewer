using Nomopsis.Api.Endpoints;
using Nomopsis.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDataPathProvider, DataPathProvider>();
builder.Services.AddSingleton<ILawDataService, LawDataService>();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapWettenEndpoints();
app.MapSwaggerEndpoints();

app.Run();
