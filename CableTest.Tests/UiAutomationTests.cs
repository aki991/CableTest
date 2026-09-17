using System.Text.Json;
using CableTest.App.UiAutomation;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Provere čistih delova razvojne strane „CableConnector“.
/// </summary>
/// <remarks>
/// <para>
/// Ispituje se ono što se <b>može</b> ispitati bez pokrenutog CableConnector-a: čišćenje imena,
/// red kojim se kontrola traži, pravila poklapanja po koraku, čitanje verzije iz naslova, spisak
/// kontrola i mapa koja se izvozi.
/// </para>
/// <para>
/// Sam UI Automation se ovde ne ispituje. Za to bi bio potreban tuđi program na ekranu, a test
/// koji zavisi od toga da li je nešto pokrenuto nije test nego kockanje.
/// </para>
/// </remarks>
public class UiAutomationTests
{
    // -------------------------------------------------------------------------------------
    // Čišćenje imena
    // -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("START", "start")]
    [InlineData("  Save spec. file  ", "save spec. file")]
    [InlineData("Save   spec.\tfile", "save spec. file")]
    [InlineData("Download ( AUTO Save and Exit )", "download ( auto save and exit )")]
    [InlineData("Multi-DUT", "multi-dut")]
    [InlineData("Test\r\nitems", "test items")]
    public void Normalize_SvodiImeNaOblikZaPorede(string? ulaz, string ocekivano)
        => Assert.Equal(ocekivano, UiaNames.Normalize(ulaz));

    [Fact]
    public void MatchesExact_TraziZnakZaZnak()
    {
        Assert.True(UiaNames.MatchesExact("START", "START"));
        Assert.False(UiaNames.MatchesExact("start", "START"));
        Assert.False(UiaNames.MatchesExact(" START", "START"));
        Assert.False(UiaNames.MatchesExact(null, "START"));
        Assert.False(UiaNames.MatchesExact("START", null));
        Assert.False(UiaNames.MatchesExact("", ""));
    }

    /// <summary>
    /// Korak „sadrži" postoji upravo zbog ovakvih imena: prečica sa <c>&amp;</c>, dvostruki
    /// razmak i drugačija veličina slova ne smeju da spreče pogodak.
    /// </summary>
    [Theory]
    [InlineData("&File", "File", true)]
    [InlineData("Save   spec. file", "Save spec. file", true)]
    [InlineData("  START  ", "start", true)]
    [InlineData("Download ( AUTO Save and Exit )", "Download ( AUTO Save and Exit )", true)]
    [InlineData("Zero", "Connect Set", false)]
    [InlineData("Save", "Save spec. file", false)]
    [InlineData("Save spec. file", "Save", true)]
    [InlineData("Bilo šta", "", false)]
    [InlineData("", "Save", false)]
    public void MatchesNormalizedContains_PoredIOciscenaImena(string kandidat, string trazeno, bool ocekivano)
        => Assert.Equal(ocekivano, UiaNames.MatchesNormalizedContains(kandidat, trazeno));

    [Fact]
    public void NearestNames_PrvoImenaKojaDeleNajviseReci()
    {
        string?[] videna = { "Zero", "Save spec. file", null, "Connect Set", "   ", "Save and Exit" };

        IReadOnlyList<string> najbliza = UiaNames.NearestNames(videna, "Save spec. file", max: 2);

        Assert.Equal(new[] { "Save spec. file", "Save and Exit" }, najbliza);
    }

    [Fact]
    public void NearestNames_KadNijednoNeLici_VracaPrvaVidjena()
    {
        string?[] videna = { "Zero", "Modify", "Open" };

        Assert.Equal(new[] { "Zero", "Modify" }, UiaNames.NearestNames(videna, "START", max: 2));
    }

    [Fact]
    public void NearestNames_PreskaceePraznaIPonovljena()
    {
        string?[] videna = { "Open", "  open  ", null, "", "Open" };

        Assert.Equal(new[] { "Open" }, UiaNames.NearestNames(videna, "Close"));
    }

    [Fact]
    public void NearestNames_BezMesta_VracaPrazno()
        => Assert.Empty(UiaNames.NearestNames(new[] { "Open" }, "Open", max: 0));

    // -------------------------------------------------------------------------------------
    // Red pretrage
    // -------------------------------------------------------------------------------------

    /// <summary>Bez poznatog AutomationId-a prvi korak otpada, a ostala tri idu istim redom.</summary>
    [Fact]
    public void SearchPlan_BezPoznatogAutomationId_PocinjeTacnimImenom()
    {
        UiaControlTarget cilj = Dugme("START");

        Assert.Equal(
            new[]
            {
                UiaSearchStrategy.NameExact,
                UiaSearchStrategy.NameNormalizedContains,
                UiaSearchStrategy.LegacyName
            },
            UiaSearchPlan.For(cilj));
    }

    [Fact]
    public void SearchPlan_SaPoznatimAutomationId_PocinjeNjime()
    {
        UiaControlTarget cilj = Dugme("START") with { KnownAutomationId = "btnStart" };

        Assert.Equal(
            new[]
            {
                UiaSearchStrategy.AutomationId,
                UiaSearchStrategy.NameExact,
                UiaSearchStrategy.NameNormalizedContains,
                UiaSearchStrategy.LegacyName
            },
            UiaSearchPlan.For(cilj));
    }

    [Fact]
    public void SearchPlan_SkipReason_ObjasnjavaZastoAutomationIdOtpada()
    {
        Assert.NotEqual(
            string.Empty,
            UiaSearchPlan.SkipReason(UiaSearchStrategy.AutomationId, Dugme("START")));

        Assert.Equal(
            string.Empty,
            UiaSearchPlan.SkipReason(
                UiaSearchStrategy.AutomationId,
                Dugme("START") with { KnownAutomationId = "btnStart" }));

        Assert.Equal(string.Empty, UiaSearchPlan.SkipReason(UiaSearchStrategy.NameExact, Dugme("START")));
    }

    // -------------------------------------------------------------------------------------
    // Poklapanje po koraku
    // -------------------------------------------------------------------------------------

    [Fact]
    public void Matches_AutomationId_PogadjaSamoKadJeIdPoznatIJednak()
    {
        UiaControlTarget saId = Dugme("START") with { KnownAutomationId = "btnStart" };
        UiaElementInfo element = Element(name: "nesto sasvim deseto", automationId: "btnStart");

        Assert.True(UiaSearchPlan.Matches(UiaSearchStrategy.AutomationId, element, saId));
        Assert.False(UiaSearchPlan.Matches(UiaSearchStrategy.AutomationId, element, Dugme("START")));
        Assert.False(UiaSearchPlan.Matches(
            UiaSearchStrategy.AutomationId,
            Element(name: "START", automationId: "btnZero"),
            saId));
    }

    [Fact]
    public void Matches_NameExact_NePustaRazlikuUVelicinSlovaNiRazmak()
    {
        UiaControlTarget cilj = Dugme("START");

        Assert.True(UiaSearchPlan.Matches(UiaSearchStrategy.NameExact, Element(name: "START"), cilj));
        Assert.False(UiaSearchPlan.Matches(UiaSearchStrategy.NameExact, Element(name: "Start"), cilj));
        Assert.False(UiaSearchPlan.Matches(UiaSearchStrategy.NameExact, Element(name: " START"), cilj));
    }

    [Fact]
    public void Matches_NameNormalizedContains_PogadjaImeSaPrecicomIViskomRazmaka()
    {
        Assert.True(UiaSearchPlan.Matches(
            UiaSearchStrategy.NameNormalizedContains,
            Element(name: "&File"),
            StavkaMenija("File")));

        Assert.True(UiaSearchPlan.Matches(
            UiaSearchStrategy.NameNormalizedContains,
            Element(name: "Save   spec. file"),
            Dugme("Save spec. file", UiaWindowKind.Edit)));
    }

    /// <summary>
    /// LegacyIAccessible ime se čita tek kad prva tri koraka promaše; dok nije pročitano
    /// (<c>null</c>), taj korak ne sme da pogodi ništa.
    /// </summary>
    [Fact]
    public void Matches_LegacyName_RadiSamoNadProcitanimImenom()
    {
        UiaControlTarget cilj = Dugme("Zero");

        Assert.False(UiaSearchPlan.Matches(UiaSearchStrategy.LegacyName, Element(name: ""), cilj));

        Assert.True(UiaSearchPlan.Matches(
            UiaSearchStrategy.LegacyName,
            Element(name: "") with { LegacyName = "Zero" },
            cilj));

        Assert.True(UiaSearchPlan.Matches(
            UiaSearchStrategy.LegacyName,
            Element(name: "") with { LegacyName = "  zero  " },
            cilj));
    }

    // -------------------------------------------------------------------------------------
    // Verzija iz naslova
    // -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("CableConnector V3.12.17", "V3.12.17")]
    [InlineData("CableConnector  V3.12.17  ", "V3.12.17")]
    [InlineData("CableConnector v4.0", "v4.0")]
    [InlineData("CableConnector", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Version_Parse_CitaVerzijuIzNaslova(string? naslov, string? ocekivano)
        => Assert.Equal(ocekivano, CableConnectorVersion.Parse(naslov));

    [Fact]
    public void Version_IsExpected_PorediSaVerzijomNaKojojJeStranaRadjena()
    {
        Assert.True(CableConnectorVersion.IsExpected("V3.12.17"));
        Assert.True(CableConnectorVersion.IsExpected("v3.12.17"));
        Assert.False(CableConnectorVersion.IsExpected("V3.12.18"));
        Assert.False(CableConnectorVersion.IsExpected(null));
    }

    [Theory]
    [InlineData("CableConnector V3.12.17", true)]
    [InlineData("  CableConnector", true)]
    [InlineData("cableconnector v1", true)]
    [InlineData("Cable Linker", false)]
    [InlineData("", false)]
    public void Version_IsMainWindowTitle_PrepoznajeGlavniProzor(string naslov, bool ocekivano)
        => Assert.Equal(ocekivano, CableConnectorVersion.IsMainWindowTitle(naslov));

    [Theory]
    [InlineData("Edit", true)]
    [InlineData("  Edit  ", true)]
    [InlineData("edit", false)]
    [InlineData("Edit spec.", false)]
    public void Version_IsEditWindowTitle_TraziTacanNaslov(string naslov, bool ocekivano)
        => Assert.Equal(ocekivano, CableConnectorVersion.IsEditWindowTitle(naslov));

    // -------------------------------------------------------------------------------------
    // Spisak kontrola
    // -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("START")]
    [InlineData("Zero")]
    [InlineData("Connect Set")]
    [InlineData("Data File")]
    [InlineData("Add spec.")]
    [InlineData("Open")]
    [InlineData("Save")]
    [InlineData("Close")]
    [InlineData("Modify")]
    [InlineData("File")]
    [InlineData("Settings")]
    [InlineData("View")]
    [InlineData("Help")]
    [InlineData("New spec. file")]
    [InlineData("Save spec. file")]
    [InlineData("Save and Exit")]
    [InlineData("Download ( AUTO Save and Exit )")]
    [InlineData("Test items")]
    [InlineData("O/S")]
    [InlineData("Cond")]
    [InlineData("HV")]
    [InlineData("Mode")]
    [InlineData("Multi-DUT")]
    [InlineData("Ground")]
    [InlineData("R/C/D")]
    [InlineData("Model")]
    [InlineData("Type")]
    [InlineData("Point")]
    [InlineData("CMD Ver")]
    public void Controls_SadrzeSveTrazeneNatpise(string natpis)
        => Assert.Contains(CableConnectorControls.All, t => t.Name == natpis);

    [Fact]
    public void Controls_LogickaImenaSuJedinstvena()
    {
        string[] imena = CableConnectorControls.All.Select(t => t.LogicalName).ToArray();

        Assert.Equal(imena.Length, imena.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Potvrdu traže tačno dve radnje — one iza kojih stoji stvarni hardver.</summary>
    [Fact]
    public void Controls_PotvrduTrazeSamoStartIDownload()
    {
        string[] saPotvrdom = CableConnectorControls.All
            .Where(t => t.NeedsConfirmation)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { CableConnectorControls.DownloadName, CableConnectorControls.StartName }, saPotvrdom);
    }

    [Fact]
    public void Controls_PoljaSuJedinaUKojaSeUpisuje()
    {
        foreach (UiaControlTarget cilj in CableConnectorControls.All)
        {
            Assert.Equal(cilj.Kind == UiaControlKind.Field, cilj.Writable);
        }
    }

    [Fact]
    public void Controls_SveIzSkupineZaEditPripadajuEditProzoru()
    {
        foreach (UiaControlGroup skupina in CableConnectorControls.Groups)
        {
            Assert.All(skupina.Targets, t => Assert.Equal(skupina.Window, t.Window));
        }
    }

    [Fact]
    public void Controls_NijedanAutomationIdJosNijePoznat()
        => Assert.All(CableConnectorControls.All, t => Assert.False(t.HasKnownAutomationId));

    // -------------------------------------------------------------------------------------
    // Mapa otkrivenih kontrola
    // -------------------------------------------------------------------------------------

    [Fact]
    public void Map_PamtiPogodakIVracaAutomationId()
    {
        var mapa = new UiaControlMap();
        UiaControlTarget cilj = Dugme("START");

        Assert.Null(mapa.KnownAutomationId(cilj.LogicalName));
        Assert.False(mapa.Contains(cilj.LogicalName));

        mapa.Remember(cilj, Element(name: "START", automationId: "btnStart"), UiaSearchStrategy.NameExact);

        Assert.True(mapa.Contains(cilj.LogicalName));
        Assert.Equal("btnStart", mapa.KnownAutomationId(cilj.LogicalName));
        Assert.Equal(1, mapa.Count);
    }

    /// <summary>Kontrola bez AutomationId-a se pamti, ali nema šta da ponudi sledećoj pretrazi.</summary>
    [Fact]
    public void Map_BezAutomationId_NeVracaPrazanNiz()
    {
        var mapa = new UiaControlMap();
        UiaControlTarget cilj = Dugme("Zero");

        mapa.Remember(cilj, Element(name: "Zero"), UiaSearchStrategy.NameExact);

        Assert.True(mapa.Contains(cilj.LogicalName));
        Assert.Null(mapa.KnownAutomationId(cilj.LogicalName));
    }

    [Fact]
    public void Map_Json_SadrziSvePodatkeOKontroli()
    {
        var mapa = new UiaControlMap();

        mapa.Remember(
            Dugme("START"),
            Element(name: "START", automationId: "btnStart") with
            {
                ClassName = "Button",
                FrameworkId = "Win32",
                Patterns = UiaPatterns.Invoke | UiaPatterns.LegacyIAccessible
            },
            UiaSearchStrategy.NameExact);

        using JsonDocument dokument = JsonDocument.Parse(mapa.ToJson());
        JsonElement zapis = Assert.Single(dokument.RootElement.EnumerateArray().ToArray());

        Assert.Equal("Main.START", zapis.GetProperty("LogicalName").GetString());
        Assert.Equal("Main", zapis.GetProperty("Window").GetString());
        Assert.Equal("Button", zapis.GetProperty("ControlType").GetString());
        Assert.Equal("btnStart", zapis.GetProperty("AutomationId").GetString());
        Assert.Equal("START", zapis.GetProperty("Name").GetString());
        Assert.Equal("Button", zapis.GetProperty("ClassName").GetString());
        Assert.Equal("Win32", zapis.GetProperty("FrameworkId").GetString());
        Assert.True(zapis.GetProperty("IsEnabled").GetBoolean());
        Assert.Equal("Invoke, LegacyIAccessible", zapis.GetProperty("Patterns").GetString());
        Assert.Equal("NameExact", zapis.GetProperty("FoundBy").GetString());
    }

    [Fact]
    public void Map_PonovniPogodak_MenjaStariZapis()
    {
        var mapa = new UiaControlMap();
        UiaControlTarget cilj = Dugme("START");

        mapa.Remember(cilj, Element(name: "START", automationId: "staro"), UiaSearchStrategy.NameExact);
        mapa.Remember(cilj, Element(name: "START", automationId: "novo"), UiaSearchStrategy.AutomationId);

        Assert.Equal(1, mapa.Count);
        Assert.Equal("novo", mapa.KnownAutomationId(cilj.LogicalName));
    }

    // -------------------------------------------------------------------------------------
    // Pretraga u drvetu
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// Drvo na kome se ispituje pretraga; isto kao ono što probe vrati, samo sastavljeno ručno.
    /// </summary>
    /// <remarks>
    /// <code>
    /// 1 Window   „CableConnector V3.12.17“   FrameworkId=Win32
    ///   2 Pane   ClassName=Static
    ///     3 Button „START“   AutomationId=btnStart   FrameworkId=WinForm
    ///     4 Button „Zero“
    ///   5 MenuBar
    ///     6 MenuItem „&amp;File“
    /// </code>
    /// </remarks>
    private static UiaTreeNodeInfo Drvo()
        => Cvor(
            Elem(1, "Window", "CableConnector V3.12.17", frameworkId: "Win32"), 0,
            Cvor(
                Elem(2, "Pane", "", className: "Static"), 1,
                Cvor(Elem(3, "Button", "START", automationId: "btnStart", frameworkId: "WinForm"), 2),
                Cvor(Elem(4, "Button", "Zero"), 2)),
            Cvor(
                Elem(5, "MenuBar", ""), 1,
                Cvor(Elem(6, "MenuItem", "&File"), 2)));

    [Fact]
    public void TreeSearch_Count_BrojiCeloDrvo()
    {
        Assert.Equal(6, UiaTreeSearch.Count(Drvo()));
        Assert.Equal(0, UiaTreeSearch.Count(null));
        Assert.Equal(1, UiaTreeSearch.Count(Cvor(Elem(1, "Window", "sam"), 0)));
    }

    [Fact]
    public void TreeSearch_MaxDepth_JeNajdubljiDostignutiCvor()
    {
        Assert.Equal(2, UiaTreeSearch.MaxDepth(Drvo()));
        Assert.Equal(0, UiaTreeSearch.MaxDepth(Cvor(Elem(1, "Window", "sam"), 0)));
        Assert.Equal(0, UiaTreeSearch.MaxDepth(null));
    }

    [Fact]
    public void TreeSearch_ControlTypes_SamoTipoviIzDrveta_SaSvimaNaPocetku()
    {
        Assert.Equal(
            new[] { UiaTreeSearch.AllControlTypes, "Button", "MenuBar", "MenuItem", "Pane", "Window" },
            UiaTreeSearch.ControlTypes(Drvo()));

        Assert.Equal(new[] { UiaTreeSearch.AllControlTypes }, UiaTreeSearch.ControlTypes(null));
    }

    /// <summary>Traži se po svih pet polja, bez obzira na velika i mala slova.</summary>
    [Theory]
    [InlineData("start", true)]        // Name
    [InlineData("  START ", true)]     // Name, sa viškom razmaka
    [InlineData("btnstart", true)]     // AutomationId
    [InlineData("button", true)]       // ControlType
    [InlineData("winform", true)]      // FrameworkId
    [InlineData("zero", false)]
    public void TreeSearch_Matches_PretrazujeSvaPolja(string trazeno, bool ocekivano)
    {
        UiaElementInfo element = Elem(3, "Button", "START", automationId: "btnStart", frameworkId: "WinForm");

        Assert.Equal(ocekivano, UiaTreeSearch.Matches(element, trazeno, UiaTreeSearch.AllControlTypes));
    }

    [Fact]
    public void TreeSearch_Matches_PoKlasiIPoTipu()
    {
        UiaElementInfo pane = Elem(2, "Pane", "", className: "Static");

        Assert.True(UiaTreeSearch.Matches(pane, "static", UiaTreeSearch.AllControlTypes));

        // Tip mora da se poklopi tačno; tekst se tada traži samo među kontrolama tog tipa.
        Assert.True(UiaTreeSearch.Matches(pane, "static", "Pane"));
        Assert.False(UiaTreeSearch.Matches(pane, "static", "Button"));
        Assert.False(UiaTreeSearch.Matches(pane, "static", "pane"));
    }

    [Fact]
    public void TreeSearch_Matches_BezTeksta_TraziSamoPoTipu()
    {
        UiaElementInfo dugme = Elem(3, "Button", "START");

        Assert.True(UiaTreeSearch.Matches(dugme, "", "Button"));
        Assert.True(UiaTreeSearch.Matches(dugme, "   ", "Button"));
        Assert.False(UiaTreeSearch.Matches(dugme, null, "MenuItem"));
    }

    [Theory]
    [InlineData("", UiaTreeSearch.AllControlTypes, true)]
    [InlineData("   ", null, true)]
    [InlineData("start", UiaTreeSearch.AllControlTypes, false)]
    [InlineData("", "Button", false)]
    public void TreeSearch_IsEmpty_PrepoznajeDaSeNistaNeTrazi(string? tekst, string? tip, bool ocekivano)
        => Assert.Equal(ocekivano, UiaTreeSearch.IsEmpty(tekst, tip));

    /// <summary>U režimu „istakni" vidi se celo drvo, a poklapanja su samo obeležena.</summary>
    [Fact]
    public void TreeSearch_Apply_Istakni_PrikazujeCeloDrvo()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            Drvo(), "start", UiaTreeSearch.AllControlTypes, UiaTreeFilterMode.Highlight);

        Assert.Equal(new[] { 3 }, ishod.Matched.OrderBy(id => id));
        Assert.Equal(6, ishod.Total);
        Assert.Equal(6, ishod.Shown);
        Assert.Equal(1, ishod.Matches);
    }

    /// <summary>U režimu „samo poklapanja" ostaje pogodak i put do njega.</summary>
    [Fact]
    public void TreeSearch_Apply_SamoPoklapanja_ZadrzavaRoditelje()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            Drvo(), "start", UiaTreeSearch.AllControlTypes, UiaTreeFilterMode.MatchesOnly);

        Assert.Equal(new[] { 3 }, ishod.Matched.OrderBy(id => id));
        Assert.Equal(new[] { 1, 2, 3 }, ishod.Visible.OrderBy(id => id));
        Assert.Equal(6, ishod.Total);
        Assert.Equal(3, ishod.Shown);
    }

    /// <summary>Dete koje se ne poklapa ne ostaje na ekranu samo zato što mu se roditelj poklopio.</summary>
    [Fact]
    public void TreeSearch_Apply_SamoPoklapanja_NePustaDecuPogotka()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            Drvo(), "pane", UiaTreeSearch.AllControlTypes, UiaTreeFilterMode.MatchesOnly);

        Assert.Equal(new[] { 2 }, ishod.Matched.OrderBy(id => id));
        Assert.Equal(new[] { 1, 2 }, ishod.Visible.OrderBy(id => id));
    }

    [Fact]
    public void TreeSearch_Apply_PoTipu_PogadjaSveKontroleTogTipa()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            Drvo(), string.Empty, "Button", UiaTreeFilterMode.MatchesOnly);

        Assert.Equal(new[] { 3, 4 }, ishod.Matched.OrderBy(id => id));
        Assert.Equal(new[] { 1, 2, 3, 4 }, ishod.Visible.OrderBy(id => id));
        Assert.Equal(4, ishod.Shown);
        Assert.Equal(6, ishod.Total);
    }

    /// <summary>Kad se ništa ne traži, ništa nije ni pogođeno — a celo drvo se vidi, u oba režima.</summary>
    [Theory]
    [InlineData(UiaTreeFilterMode.Highlight)]
    [InlineData(UiaTreeFilterMode.MatchesOnly)]
    public void TreeSearch_Apply_BezTrazenog_SveSeVidi(UiaTreeFilterMode rezim)
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(Drvo(), "  ", UiaTreeSearch.AllControlTypes, rezim);

        Assert.Empty(ishod.Matched);
        Assert.Equal(6, ishod.Shown);
        Assert.Equal(6, ishod.Total);
    }

    [Fact]
    public void TreeSearch_Apply_BezPogotka_USamoPoklapanjima_NePrikazujeNista()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            Drvo(), "nema ovoga", UiaTreeSearch.AllControlTypes, UiaTreeFilterMode.MatchesOnly);

        Assert.Empty(ishod.Matched);
        Assert.Empty(ishod.Visible);
        Assert.Equal(6, ishod.Total);
    }

    [Fact]
    public void TreeSearch_Apply_BezDrveta_NeRusiSe()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            null, "start", UiaTreeSearch.AllControlTypes, UiaTreeFilterMode.Highlight);

        Assert.Empty(ishod.Matched);
        Assert.Empty(ishod.Visible);
        Assert.Equal(0, ishod.Total);
    }

    [Fact]
    public void TreeSearch_CountText_IspisujePrikazanoIUkupno()
        => Assert.Equal("Prikazano 3 od 6 čvorova.", UiaTreeSearch.CountText(3, 6));

    [Fact]
    public void TreeSearch_RezimSeCitaIzNatpisa()
    {
        Assert.Equal(UiaTreeFilterMode.Highlight, UiaTreeSearch.ModeFromName(UiaTreeSearch.HighlightModeName));
        Assert.Equal(UiaTreeFilterMode.MatchesOnly, UiaTreeSearch.ModeFromName(UiaTreeSearch.MatchesOnlyModeName));
        Assert.Equal(UiaTreeFilterMode.Highlight, UiaTreeSearch.ModeFromName("nepoznato"));
        Assert.Equal(UiaTreeFilterMode.Highlight, UiaTreeSearch.ModeFromName(null));

        Assert.Equal(UiaTreeSearch.MatchesOnlyModeName, UiaTreeSearch.NameOfMode(UiaTreeFilterMode.MatchesOnly));
        Assert.Equal(UiaTreeSearch.HighlightModeName, UiaTreeSearch.NameOfMode(UiaTreeFilterMode.Highlight));
    }

    // -------------------------------------------------------------------------------------
    // Dijagnostika filtera
    // -------------------------------------------------------------------------------------

    /// <summary>
    /// U logu mora da stoji i ono što je otkucano i ono čime se zaista poredilo: razmak na kraju
    /// i dvostruki razmak unutra inače izgledaju kao da su nestali sami od sebe.
    /// </summary>
    [Fact]
    public void Diagnostika_SadrziUpitNormalizovanOblikTipRezimIBrojeve()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            Drvo(), "  START  ", UiaTreeSearch.AllControlTypes, UiaTreeFilterMode.Highlight);

        string red = UiaTreeSearch.DiagnosticText(
            "  START  ", UiaTreeSearch.AllControlTypes, UiaTreeFilterMode.Highlight, ishod);

        Assert.Contains("upit=\"  START  \"", red);
        Assert.Contains("normalizovano \"start\"", red);
        Assert.Contains($"ControlType={UiaTreeSearch.AllControlTypes}", red);
        Assert.Contains($"režim={UiaTreeSearch.HighlightModeName}", red);
        Assert.Contains("poklapanja 1", red);
        Assert.Contains("prikazano 6 od 6 čvorova", red);
    }

    [Fact]
    public void Diagnostika_IspisujeIzabraniTipIRezimSamoPoklapanja()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(
            Drvo(), string.Empty, "Button", UiaTreeFilterMode.MatchesOnly);

        string red = UiaTreeSearch.DiagnosticText(
            string.Empty, "Button", UiaTreeFilterMode.MatchesOnly, ishod);

        Assert.Contains("ControlType=Button", red);
        Assert.Contains($"režim={UiaTreeSearch.MatchesOnlyModeName}", red);
        Assert.Contains("poklapanja 2", red);
        Assert.Contains("prikazano 4 od 6 čvorova", red);
    }

    /// <summary>Prazan tip se u logu vidi kao „svi", a ne kao prazno mesto.</summary>
    [Fact]
    public void Diagnostika_PrazanTipSeIspisujeKaoSvi()
    {
        UiaTreeFilterResult ishod = UiaTreeSearch.Apply(Drvo(), "zero", null, UiaTreeFilterMode.Highlight);

        Assert.Contains(
            $"ControlType={UiaTreeSearch.AllControlTypes}",
            UiaTreeSearch.DiagnosticText("zero", null, UiaTreeFilterMode.Highlight, ishod));
    }

    [Fact]
    public void FirstNames_VracaImenaRedomKojimSeDrvoObilazi()
    {
        Assert.Equal(
            new[] { "CableConnector V3.12.17", "", "START", "Zero", "", "&File" },
            UiaTreeSearch.FirstNames(Drvo()));
    }

    [Fact]
    public void FirstNames_StajeNaZadatomBroju()
    {
        Assert.Equal(new[] { "CableConnector V3.12.17", "" }, UiaTreeSearch.FirstNames(Drvo(), max: 2));
        Assert.Empty(UiaTreeSearch.FirstNames(Drvo(), max: 0));
        Assert.Empty(UiaTreeSearch.FirstNames(null));
    }

    /// <summary>
    /// Ispis mora da razlikuje kontrolu bez imena od kontrole sa drugačijim imenom — to su dva
    /// sasvim različita zaključka o tome zašto pretraga nije pogodila ništa.
    /// </summary>
    [Fact]
    public void FirstNamesText_PrazanNazivSeVidiKaoBezImena()
    {
        string red = UiaTreeSearch.FirstNamesText(Drvo(), max: 3);

        Assert.Contains("prvih 3 imena u stablu:", red);
        Assert.Contains("\"CableConnector V3.12.17\"", red);
        Assert.Contains("(bez imena)", red);
        Assert.Contains("\"START\"", red);
    }

    [Fact]
    public void FirstNamesText_BezDrveta_KazeDaCvorovaNema()
        => Assert.Contains("nema nijednog čvora", UiaTreeSearch.FirstNamesText(null));

    // -------------------------------------------------------------------------------------
    // Lanac roditelja
    // -------------------------------------------------------------------------------------

    /// <summary>Put do lista je ceo lanac: prozor, pa sve između, pa sam čvor.</summary>
    [Fact]
    public void Path_DoLista_VracaCeoLanacOdKorena()
    {
        int[] put = UiaTreeSearch.Path(Drvo(), 3).Select(n => n.Element.Id).ToArray();

        Assert.Equal(new[] { 1, 2, 3 }, put);
    }

    [Fact]
    public void Path_DoKorena_VracaSamoKoren()
        => Assert.Equal(new[] { 1 }, UiaTreeSearch.Path(Drvo(), 1).Select(n => n.Element.Id));

    [Fact]
    public void Path_DoCvoraUDrugojGrani_NeNosiBracuSaSobom()
        => Assert.Equal(new[] { 1, 5, 6 }, UiaTreeSearch.Path(Drvo(), 6).Select(n => n.Element.Id));

    [Fact]
    public void Path_ZaNepoznatCvor_VracaPrazno()
    {
        Assert.Empty(UiaTreeSearch.Path(Drvo(), 999));
        Assert.Empty(UiaTreeSearch.Path(null, 1));
    }

    /// <summary>Poslednji u lancu je sam čvor; sve pre njega su preci, redom od prozora.</summary>
    [Fact]
    public void Path_PosledenjiJeTrazeniCvor_OstaloSuPreci()
    {
        IReadOnlyList<UiaTreeNodeInfo> put = UiaTreeSearch.Path(Drvo(), 4);

        Assert.Equal(4, put[^1].Element.Id);
        Assert.Equal(new[] { 1, 2 }, put.Take(put.Count - 1).Select(n => n.Element.Id));
    }

    [Fact]
    public void FindByRuntimeId_PronalaziCvorIUDubini()
    {
        UiaTreeNodeInfo? cvor = UiaTreeSearch.FindByRuntimeId(Drvo(), "42,6");

        Assert.NotNull(cvor);
        Assert.Equal(6, cvor!.Element.Id);
    }

    [Fact]
    public void FindByRuntimeId_BezPogotka_VracaNull()
    {
        Assert.Null(UiaTreeSearch.FindByRuntimeId(Drvo(), "42,999"));
        Assert.Null(UiaTreeSearch.FindByRuntimeId(Drvo(), ""));
        Assert.Null(UiaTreeSearch.FindByRuntimeId(Drvo(), null));
        Assert.Null(UiaTreeSearch.FindByRuntimeId(null, "42,1"));
    }

    /// <summary>Lanac u tekstu ide od prozora nadole, svaki nivo uvučen za dva razmaka.</summary>
    [Fact]
    public void TreeText_Chain_UvlaciNivoeOdProzoraNadole()
    {
        UiaElementInfo[] lanac = UiaTreeSearch.Path(Drvo(), 3).Select(n => n.Element).ToArray();

        string[] redovi = UiaTreeText.Chain(lanac)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.TrimEnd('\r'))
            .ToArray();

        Assert.Equal(3, redovi.Length);
        Assert.StartsWith("Window — CableConnector V3.12.17", redovi[0]);
        Assert.StartsWith("  Pane — (bez imena)", redovi[1]);
        Assert.StartsWith("    Button — START", redovi[2]);
        Assert.Contains("RuntimeId=42,3", redovi[2]);
    }

    [Fact]
    public void TreeText_Chain_PrazanLanac_JePrazanTekst()
        => Assert.Equal(string.Empty, UiaTreeText.Chain(Array.Empty<UiaElementInfo>()));

    // -------------------------------------------------------------------------------------
    // Čvor i grana u obliku teksta
    // -------------------------------------------------------------------------------------

    [Fact]
    public void TreeText_Node_SadrziSvePodatkeOCvoru()
    {
        string tekst = UiaTreeText.Node(
            Elem(3, "Button", "START", automationId: "btnStart", className: "Button", frameworkId: "Win32")
                with { NativeWindowHandle = 0x1A2B, Patterns = UiaPatterns.Invoke });

        Assert.Contains("ControlType: Button", tekst);
        Assert.Contains("Name: START", tekst);
        Assert.Contains("AutomationId: btnStart", tekst);
        Assert.Contains("ClassName: Button", tekst);
        Assert.Contains("FrameworkId: Win32", tekst);
        Assert.Contains("NativeWindowHandle: 0x1A2B", tekst);
        Assert.Contains("RuntimeId: 42,3", tekst);
        Assert.Contains("IsEnabled: True", tekst);
        Assert.Contains("Obrasci: Invoke", tekst);
    }

    /// <summary>Grana je čvor sa svom decom, svaki nivo uvučen za dva razmaka.</summary>
    [Fact]
    public void TreeText_Branch_UvlaciDecuPoNivoima()
    {
        string[] redovi = UiaTreeText.Branch(Drvo())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(r => r.TrimEnd('\r'))
            .ToArray();

        Assert.Equal(6, redovi.Length);
        Assert.StartsWith("Window — CableConnector V3.12.17", redovi[0]);
        Assert.StartsWith("  Pane — (bez imena)", redovi[1]);
        Assert.StartsWith("    Button — START", redovi[2]);
        Assert.StartsWith("    Button — Zero", redovi[3]);
        Assert.StartsWith("  MenuBar — (bez imena)", redovi[4]);
        Assert.StartsWith("    MenuItem — &File", redovi[5]);
    }

    [Fact]
    public void TreeText_Branch_JednogCvora_JeJedanRed()
    {
        string tekst = UiaTreeText.Branch(Cvor(Elem(1, "Button", "Zero"), 0));

        Assert.Single(tekst.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("Button — Zero", tekst);
    }

    // -------------------------------------------------------------------------------------
    // Ispis obrazaca
    // -------------------------------------------------------------------------------------

    [Fact]
    public void PatternText_IspisujeSveObrasceIliCrticu()
    {
        Assert.Equal("—", UiaPatternText.Describe(UiaPatterns.None));
        Assert.Equal("Invoke", UiaPatternText.Describe(UiaPatterns.Invoke));
        Assert.Equal(
            "Invoke, Value, LegacyIAccessible",
            UiaPatternText.Describe(UiaPatterns.Invoke | UiaPatterns.Value | UiaPatterns.LegacyIAccessible));
    }

    // -------------------------------------------------------------------------------------
    // Pomoćno
    // -------------------------------------------------------------------------------------

    private static UiaControlTarget Dugme(string ime, UiaWindowKind prozor = UiaWindowKind.Main)
        => new($"{prozor}.{ime}", prozor, UiaControlKind.Button, ime);

    private static UiaControlTarget StavkaMenija(string ime)
        => new($"Main.{ime}", UiaWindowKind.Main, UiaControlKind.MenuItem, ime);

    private static UiaElementInfo Element(string name, string automationId = "")
        => new(
            Id: 1,
            ControlType: "Button",
            Name: name,
            AutomationId: automationId,
            ClassName: string.Empty,
            IsEnabled: true,
            IsOffscreen: false,
            Bounds: System.Drawing.Rectangle.Empty,
            Patterns: UiaPatterns.None);

    private static UiaElementInfo Elem(
        int id,
        string controlType,
        string name,
        string automationId = "",
        string className = "",
        string frameworkId = "",
        string? runtimeId = null)
        => new(
            Id: id,
            ControlType: controlType,
            Name: name,
            AutomationId: automationId,
            ClassName: className,
            IsEnabled: true,
            IsOffscreen: false,
            Bounds: System.Drawing.Rectangle.Empty,
            Patterns: UiaPatterns.None,
            FrameworkId: frameworkId,
            RuntimeId: runtimeId ?? $"42,{id}");

    private static UiaTreeNodeInfo Cvor(UiaElementInfo element, int depth, params UiaTreeNodeInfo[] deca)
    {
        var node = new UiaTreeNodeInfo(element, depth);

        foreach (UiaTreeNodeInfo dete in deca)
        {
            node.Children.Add(dete);
        }

        return node;
    }
}
