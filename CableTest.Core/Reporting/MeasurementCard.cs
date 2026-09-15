using System.Globalization;
using System.Net;
using System.Text;
using CableTest.Core.Model;

namespace CableTest.Core.Reporting;

/// <summary>Podaci za mernu kartu jednog testa.</summary>
public sealed class MeasurementCardData
{
    public required TestRun Run { get; init; }

    public Vehicle? Vehicle { get; init; }

    public Cable? Cable { get; init; }

    /// <summary>Napomena koja se ispisuje na karti, npr. da je reč o demo prikazu.</summary>
    public string? Note { get; init; }
}

/// <summary>
/// Merna karta kao HTML dokument koji se otvara u podrazumevanom pregledaču.
/// </summary>
/// <remarks>
/// <para>
/// Namerno bez biblioteke za PDF: svaki pregledač ima štampu, a u njoj i „Sačuvaj kao PDF".
/// Jedna zavisnost manje u aplikaciji koja radi na proizvodnoj mašini, a rezultat je isti.
/// </para>
/// <para>
/// Stil je prilagođen štampi (A4, <c>@media print</c>): dugmad i pozadine se ne štampaju, a
/// prelom je takav da karta stane na jedan list.
/// </para>
/// </remarks>
public static class MeasurementCard
{
    /// <summary>Pravi HTML dokument merne karte.</summary>
    public static string BuildHtml(MeasurementCardData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        TestRun run = data.Run;
        string ishod = run.Passed ? "ISPRAVAN" : "NEISPRAVAN";
        string ishodKlasa = run.Passed ? "pass" : "fail";

        var html = new StringBuilder();
        html.Append(
            """
            <!DOCTYPE html>
            <html lang="sr">
            <head>
            <meta charset="utf-8">
            <title>Merna karta</title>
            <style>
            @page { size: A4; margin: 15mm; }
            body { font-family: "Segoe UI", Arial, sans-serif; color: #111; margin: 0; padding: 15mm; }
            h1 { font-size: 22pt; margin: 0 0 2mm 0; }
            .podnaslov { color: #555; margin: 0 0 8mm 0; font-size: 11pt; }
            table.podaci { border-collapse: collapse; width: 100%; margin-bottom: 8mm; }
            table.podaci th, table.podaci td { border: 1px solid #bbb; padding: 2mm 3mm; font-size: 11pt; text-align: left; vertical-align: top; }
            table.podaci th { width: 45mm; background: #f2f2f2; font-weight: 600; }
            .ishod { border: 3px solid #111; padding: 5mm; text-align: center; margin-bottom: 8mm; }
            .ishod .oznaka { font-size: 32pt; font-weight: 700; letter-spacing: 2px; }
            .ishod.pass { border-color: #0a7a28; color: #0a7a28; }
            .ishod.fail { border-color: #b3000f; color: #b3000f; }
            .ishod .opis { font-size: 12pt; color: #111; margin-top: 2mm; }
            h2 { font-size: 13pt; margin: 0 0 3mm 0; }
            ul.greske { margin: 0 0 8mm 0; padding-left: 6mm; }
            ul.greske li { font-size: 11pt; margin-bottom: 1.5mm; }
            ol.netovi { margin: 0 0 8mm 0; padding-left: 8mm; }
            ol.netovi li { font-family: Consolas, "Courier New", monospace; font-size: 11pt; }
            .potpisi { display: flex; gap: 20mm; margin-top: 14mm; }
            .potpis { flex: 1; border-top: 1px solid #111; padding-top: 2mm; font-size: 10pt; color: #333; }
            .napomena { border-left: 4px solid #b38600; background: #fff8e1; padding: 3mm 4mm; margin-bottom: 8mm; font-size: 11pt; }
            .sirovo { font-family: Consolas, "Courier New", monospace; font-size: 9pt; color: #444; word-break: break-all; }
            .stampaj { margin-top: 10mm; }
            .stampaj button { font-size: 12pt; padding: 2mm 6mm; cursor: pointer; }
            @media print { .stampaj { display: none; } body { padding: 0; } .napomena { background: none; } }
            </style>
            </head>
            <body>

            """);

        html.Append("<h1>Merna karta</h1>\n");
        html.Append("<p class=\"podnaslov\">Ispitivanje kablovskog seta — Microtest 8761NK</p>\n");

        if (!string.IsNullOrWhiteSpace(data.Note))
        {
            html.Append("<p class=\"napomena\">").Append(Escape(data.Note)).Append("</p>\n");
        }

        html.Append($"<div class=\"ishod {ishodKlasa}\">\n");
        html.Append($"  <div class=\"oznaka\">{ishod}</div>\n");
        html.Append("  <div class=\"opis\">")
            .Append(run.Passed
                ? "Test je prošao — nije pronađena nijedna greška."
                : $"Test nije prošao — pronađenih grešaka: {run.Defects.Count.ToString(CultureInfo.InvariantCulture)}.")
            .Append("</div>\n");
        html.Append("</div>\n");

        html.Append("<table class=\"podaci\">\n");
        Row(html, "Vozilo", data.Vehicle?.Name ?? "(nije poznato)");
        Row(html, "Kabl", data.Cable?.Code ?? run.SpecFileName);

        if (!string.IsNullOrWhiteSpace(data.Cable?.Description))
        {
            Row(html, "Opis", data.Cable!.Description);
        }

        Row(html, "Spec fajl", run.SpecFileName);
        Row(html, "Redni broj testa", run.Seq.ToString(CultureInfo.InvariantCulture));
        Row(html, "Datum i vreme", FormatTime(run.TestedAt));
        Row(html, "Operater", string.IsNullOrWhiteSpace(run.Operator) ? "—" : run.Operator);

        if (!string.IsNullOrWhiteSpace(run.Barcode))
        {
            Row(html, "Barkod", run.Barcode);
        }

        html.Append("</table>\n");

        if (run.Defects.Count > 0)
        {
            html.Append("<h2>Pronađene greške</h2>\n<ul class=\"greske\">\n");
            foreach (TestDefect defect in run.Defects)
            {
                html.Append("  <li>").Append(Escape(defect.Describe()));

                if (!string.IsNullOrWhiteSpace(defect.RawText)
                    && !string.Equals(defect.RawText, defect.Describe(), StringComparison.Ordinal))
                {
                    html.Append(" <span class=\"sirovo\">(").Append(Escape(defect.RawText)).Append(")</span>");
                }

                html.Append("</li>\n");
            }

            html.Append("</ul>\n");
        }

        if (data.Cable is not null && data.Cable.Nets.Count > 0)
        {
            html.Append("<h2>Net lista</h2>\n<ol class=\"netovi\">\n");
            foreach (CableNet net in data.Cable.Nets.OrderBy(n => n.Ordinal))
            {
                html.Append("  <li>").Append(Escape(net.Points)).Append("</li>\n");
            }

            html.Append("</ol>\n");
        }

        if (!string.IsNullOrWhiteSpace(run.RawRow))
        {
            html.Append("<h2>Zapis iz CSV-a</h2>\n<p class=\"sirovo\">")
                .Append(Escape(run.RawRow))
                .Append("</p>\n");
        }

        html.Append(
            """
            <div class="potpisi">
              <div class="potpis">Operater</div>
              <div class="potpis">Kontrolor — potpis i datum</div>
            </div>

            <div class="stampaj"><button onclick="window.print()">Štampaj</button></div>

            </body>
            </html>
            """);

        return html.ToString();
    }

    /// <summary>
    /// Upisuje mernu kartu u privremeni fajl i vraća punu putanju.
    /// </summary>
    /// <param name="data">Podaci karte.</param>
    /// <param name="folder">Folder; podrazumevano privremeni folder korisnika.</param>
    public static string WriteToFile(MeasurementCardData data, string? folder = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        string target = string.IsNullOrWhiteSpace(folder)
            ? Path.Combine(Path.GetTempPath(), "CableTest")
            : folder;

        Directory.CreateDirectory(target);

        string name = SafeFileName(
            $"merna-karta-{data.Run.SpecFileName}-{data.Run.Seq.ToString(CultureInfo.InvariantCulture)}-" +
            $"{FormatForFileName(data.Run.TestedAt)}.html");

        string path = Path.Combine(target, name);

        // UTF-8 sa BOM-om: pregledač tako sigurno prepozna kodiranje i kad se fajl otvori sa diska.
        File.WriteAllText(path, BuildHtml(data), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return path;
    }

    private static void Row(StringBuilder html, string naziv, string vrednost)
        => html.Append("  <tr><th>").Append(Escape(naziv)).Append("</th><td>")
               .Append(Escape(vrednost)).Append("</td></tr>\n");

    private static string FormatTime(DateTime value)
        => value == DateTime.MinValue
            ? "(vreme nije prepoznato u CSV-u)"
            : value.ToString("dd.MM.yyyy. HH:mm:ss", CultureInfo.InvariantCulture);

    private static string FormatForFileName(DateTime value)
        => value == DateTime.MinValue
            ? "bez-vremena"
            : value.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

    private static string SafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '-');
        }

        return name;
    }

    private static string Escape(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);
}
