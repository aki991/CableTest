using System.Text;
using CableTest.Web.Components;
using CableTest.Web.Services;

// Demo se uključuje argumentom komandne linije ili prekidačem u podešavanjima.
// Argument se sklanja pre pravljenja host-a: ASP.NET konfiguracija očekuje "--kljuc vrednost",
// pa bi na samo "--demo" pukla.
bool demo = args.Any(a => string.Equals(a, TestingService.DemoArgument, StringComparison.OrdinalIgnoreCase));
string[] hostArgs = args
    .Where(a => !string.Equals(a, TestingService.DemoArgument, StringComparison.OrdinalIgnoreCase))
    .ToArray();

WebApplicationBuilder builder = WebApplication.CreateBuilder(hostArgs);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Jedan tester, jedno stanje: servis je singleton i živi koliko i server.
builder.Services.AddSingleton(_ => TestingService.Create(demo));

WebApplication app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Merna karta trenutnog rezultata. Desktop verzija upisuje HTML na disk i otvara ga u
// pregledaču; ovde je pregledač već tu, pa se karta služi kao stranica u novoj kartici.
app.MapGet("/merna-karta", (TestingService testing) =>
{
    string? html = testing.BuildMeasurementCardHtml();

    return html is null
        ? Results.Content(
            "<!DOCTYPE html><html lang=\"sr\"><head><meta charset=\"utf-8\"><title>Merna karta</title></head>" +
            "<body style=\"font-family:Segoe UI,Arial,sans-serif;padding:24px\">" +
            "<h1>Nema rezultata</h1><p>Merna karta se pravi za rezultat koji je prikazan na ekranu.</p>" +
            "</body></html>",
            "text/html",
            Encoding.UTF8)
        : Results.Content(html, "text/html", Encoding.UTF8);
});

// Nadgledanje se pokreće sa serverom, a ne kad se prvi pretraživač javi: rezultati ne smeju da
// se propuste zato što niko trenutno ne gleda ekran.
app.Services.GetRequiredService<TestingService>().Start();

app.Run();
