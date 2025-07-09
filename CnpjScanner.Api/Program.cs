using CnpjScanner.Api.Analyzers;
using CnpjScanner.Api.Interfaces;
using CnpjScanner.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<CSharpAnalyzer>();
builder.Services.AddScoped<VBNetAnalyzer>();
builder.Services.AddScoped<TypeScriptAnalyzerService>();
builder.Services.AddScoped<IGitService, GitService>();
builder.Services.AddScoped<MultiLanguageAnalyzerService>();
builder.Services.AddScoped<CSharpAnalyzer>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular dev URL
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAngularApp");
app.UseAuthorization();

app.MapControllers();

app.Run();