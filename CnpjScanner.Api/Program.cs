var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<CSharpAnalyzer>();
builder.Services.AddScoped<VBNetAnalyzer>();
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