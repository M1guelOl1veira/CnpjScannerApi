using CnpjScanner.Api.Analyzers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<CSharpAnalyzer>();
builder.Services.AddScoped<VbNetAnalyzer>();
builder.Services.AddScoped<TypeScriptAnalyzerService>();
builder.Services.AddScoped<MultiLanguageAnalyzerService>();
builder.Services.AddScoped<CSharpAnalyzer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();