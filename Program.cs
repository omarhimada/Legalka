using Eloi;
using Eloi.Components;
using Eloi.Models;
using Eloi.Models.Classes;
using Eloi.Services;
using Eloi.Services.Documents;
using Eloi.Services.Http;
using Eloi.Services.RetrievalAugmentation;
using Radzen;
using static Eloi.Constants;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();

builder.Services.AddHttpClient<EloiClient>(http => {
    http.BaseAddress = new Uri(_localEloiUrl);
    http.Timeout = Timeout.InfiniteTimeSpan; // streaming safety
});

builder.Services.AddLogging();

builder.Services.AddSingleton<RetrievalAugmentService>();
builder.Services.AddSingleton<BookLover>();
builder.Services.AddSingleton<RetrievalAugmentService>();
builder.Services.AddSingleton<IDocumentIngestService, DocumentIngestService>();

builder.Services.Configure<Settings>(builder.Configuration.GetSection(_eloiSettingsSection));

builder.Services.AddHostedService<EnsureOllamaBuildsEloiService>();

WebApplication app = builder.Build();


app.MapPost("/ask", async (RetrievalAugmentService ras, EloiRequest request, CancellationToken ct) => {
    try {
        if (string.IsNullOrWhiteSpace(request.Prompt)) return Results.BadRequest();

        string answer = await ras.AskAsync(request.Prompt, ct);

        return Results.Ok(new EloiResponse { Response = answer, Done = false });
    } catch {
        return Results.Ok(new EloiResponse { Response = null, Done = true });
    }
    
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler(Web._errorPagePath, createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute(Web._notFoundPagePath, createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
