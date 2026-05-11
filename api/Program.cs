using OmsApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers();

// Auto-register all feature handlers
foreach (var type in typeof(Program).Assembly.GetTypes()
    .Where(t => t.Name.EndsWith("Handler") && !t.IsAbstract && !t.IsInterface))
    builder.Services.AddTransient(type);

var app = builder.Build();
_ = app.Services.GetRequiredService<InMemoryStore>();
app.UseCors();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run();
