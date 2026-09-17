using CableTest.Core.Gateway;
using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Mašina stanja veze sa testerom i njeno ponašanje u oba gateway-a.
/// </summary>
/// <remarks>
/// Pravilo koje se ovde brani: <b>nedozvoljena radnja vraća odgovor, ne izuzetak</b>. Operater
/// koji dvaput pritisne dugme ne sme da sruši aplikaciju, a onaj ko poziva gateway mora da može
/// da obradi ishod umesto da ga hvata.
/// </remarks>
public sealed class TesterStateMachineTests
{
    // -------------------------------------------------------------------------------------
    // Čista mašina stanja
    // -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(TesterState.Idle, true)]
    [InlineData(TesterState.ProgramLoaded, true)]
    [InlineData(TesterState.Completed, true)]
    [InlineData(TesterState.Failed, true)]
    [InlineData(TesterState.WaitingForResult, false)]
    public void Priprema_JeDozvoljenaSvudaOsimUsredTesta(TesterState stanje, bool ocekivano)
        => Assert.Equal(ocekivano, TesterStateMachine.IsAllowed(stanje, TesterOperation.LoadProgram));

    [Theory]
    [InlineData(TesterState.ProgramLoaded, true)]
    [InlineData(TesterState.Idle, false)]
    [InlineData(TesterState.WaitingForResult, false)]
    [InlineData(TesterState.Completed, false)]
    [InlineData(TesterState.Failed, false)]
    public void Pokretanje_TraziPripremljenProgram(TesterState stanje, bool ocekivano)
        => Assert.Equal(ocekivano, TesterStateMachine.IsAllowed(stanje, TesterOperation.StartTest));

    [Theory]
    [InlineData(TesterState.ProgramLoaded, true)]
    [InlineData(TesterState.WaitingForResult, true)]
    [InlineData(TesterState.Idle, false)]
    [InlineData(TesterState.Completed, false)]
    public void Cekanje_TraziPripremljenProgramIliVecZapocetoCekanje(TesterState stanje, bool ocekivano)
        => Assert.Equal(ocekivano, TesterStateMachine.IsAllowed(stanje, TesterOperation.WaitForResult));

    [Theory]
    [InlineData(TesterState.Idle, TesterOperation.LoadProgram, TesterState.ProgramLoaded)]
    [InlineData(TesterState.ProgramLoaded, TesterOperation.StartTest, TesterState.WaitingForResult)]
    [InlineData(TesterState.ProgramLoaded, TesterOperation.WaitForResult, TesterState.WaitingForResult)]
    [InlineData(TesterState.WaitingForResult, TesterOperation.ReceiveResult, TesterState.Completed)]
    [InlineData(TesterState.WaitingForResult, TesterOperation.Fail, TesterState.Failed)]
    [InlineData(TesterState.Completed, TesterOperation.LoadProgram, TesterState.ProgramLoaded)]
    [InlineData(TesterState.Failed, TesterOperation.LoadProgram, TesterState.ProgramLoaded)]
    public void Next_VodiUOcekivanoStanje(TesterState iz, TesterOperation radnja, TesterState u)
        => Assert.Equal(u, TesterStateMachine.Next(iz, radnja));

    /// <summary>
    /// Rezultat sme da stigne i kad ga niko ne čeka — operater ume da pritisne START bez
    /// pripreme iz aplikacije. Stanje tada ostaje kakvo je bilo.
    /// </summary>
    [Theory]
    [InlineData(TesterState.Idle)]
    [InlineData(TesterState.ProgramLoaded)]
    [InlineData(TesterState.Completed)]
    public void Rezultat_VanCekanja_NePomeraStanje(TesterState stanje)
    {
        Assert.True(TesterStateMachine.IsAllowed(stanje, TesterOperation.ReceiveResult));
        Assert.Equal(stanje, TesterStateMachine.Next(stanje, TesterOperation.ReceiveResult));
    }

    [Fact]
    public void Next_ZaNedozvoljenuRadnju_NeMenjaStanje()
        => Assert.Equal(
            TesterState.Idle,
            TesterStateMachine.Next(TesterState.Idle, TesterOperation.StartTest));

    [Fact]
    public void Explain_ObjasnjavaZastoRadnjaNijeMoguca()
    {
        Assert.Equal(string.Empty, TesterStateMachine.Explain(TesterState.ProgramLoaded, TesterOperation.StartTest));

        Assert.Contains(
            "nije pripremljen",
            TesterStateMachine.Explain(TesterState.Idle, TesterOperation.StartTest),
            StringComparison.Ordinal);

        Assert.NotEqual(
            string.Empty,
            TesterStateMachine.Explain(TesterState.WaitingForResult, TesterOperation.LoadProgram));
    }

    // -------------------------------------------------------------------------------------
    // Ponašanje gateway-a: greška, ne izuzetak
    // -------------------------------------------------------------------------------------

    [Fact]
    public async Task Fake_PokretanjeBezPripreme_VracaGreskuAneIzuzetak()
    {
        await using var gateway = new FakeTesterGateway();

        GatewayResult result = await gateway.StartTestAsync(CancellationToken.None);

        Assert.Equal(GatewayStatus.InvalidState, result.Status);
        Assert.NotEqual(string.Empty, result.Message);
        Assert.Equal(TesterState.Idle, gateway.State);
    }

    [Fact]
    public async Task Fake_CekanjeBezPripreme_VracaGreskuAneIzuzetak()
    {
        await using var gateway = new FakeTesterGateway();

        GatewayResult result = await gateway.WaitForResultAsync(TimeSpan.FromMilliseconds(50), CancellationToken.None);

        Assert.Equal(GatewayStatus.InvalidState, result.Status);
        Assert.Equal(TesterState.Idle, gateway.State);
    }

    [Fact]
    public async Task Fake_PripremaUsredTesta_VracaGreskuAneIzuzetak()
    {
        await using var gateway = new FakeTesterGateway(scenario: FakeScenario.Timeout, startDelay: TimeSpan.Zero);
        gateway.StartMonitoring();

        Assert.True((await gateway.LoadProgramAsync(Kabl(), CancellationToken.None)).IsOk);
        Assert.True((await gateway.StartTestAsync(CancellationToken.None)).IsOk);
        Assert.Equal(TesterState.WaitingForResult, gateway.State);

        GatewayResult result = await gateway.LoadProgramAsync(Kabl(), CancellationToken.None);

        Assert.Equal(GatewayStatus.InvalidState, result.Status);
        Assert.Equal(TesterState.WaitingForResult, gateway.State);
    }

    [Fact]
    public async Task Fake_PripremaPaPokretanje_ProlaziKrozSvaStanja()
    {
        var stanja = new List<TesterState>();

        await using var gateway = new FakeTesterGateway(startDelay: TimeSpan.Zero);
        gateway.StateChanged += (_, e) => stanja.Add(e.Current);
        gateway.StartMonitoring();

        await gateway.LoadProgramAsync(Kabl(), CancellationToken.None);
        await gateway.StartTestAsync(CancellationToken.None);

        GatewayResult result = await gateway.WaitForResultAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.True(result.IsOk);
        Assert.NotNull(result.Run);
        Assert.Equal(TesterState.Completed, gateway.State);

        Assert.Equal(
            new[]
            {
                TesterState.ProgramLoaded,
                TesterState.WaitingForResult,
                TesterState.Completed
            },
            stanja);
    }

    /// <summary>
    /// Mašina ume da odgovori pre nego što aplikacija stigne da se postavi na čekanje. Takav
    /// rezultat se ne sme izgubiti — inače bi čekanje isteklo iako je test odavno gotov.
    /// </summary>
    [Fact]
    public async Task Rezultat_KojiStignePreCekanja_SeNeGubi()
    {
        await using var gateway = new FakeTesterGateway(startDelay: TimeSpan.Zero);
        gateway.StartMonitoring();

        await gateway.LoadProgramAsync(Kabl(), CancellationToken.None);
        await gateway.StartTestAsync(CancellationToken.None);

        // Rezultat stiže dok se još niko nije postavio na čekanje.
        await Task.Delay(150);
        Assert.Equal(TesterState.Completed, gateway.State);

        GatewayResult result = await gateway.WaitForResultAsync(
            TimeSpan.FromMilliseconds(200),
            CancellationToken.None);

        Assert.True(result.IsOk);
        Assert.NotNull(result.Run);
    }

    /// <summary>Zatečeni rezultat važi za jedan test; sledeća priprema ga briše.</summary>
    [Fact]
    public async Task ZatecenRezultat_NeVaziZaSledeciTest()
    {
        await using var gateway = new FakeTesterGateway(startDelay: TimeSpan.Zero);
        gateway.StartMonitoring();

        await gateway.LoadProgramAsync(Kabl(), CancellationToken.None);
        await gateway.StartTestAsync(CancellationToken.None);
        await Task.Delay(150);

        // Nov test: stari rezultat se ne sme ponuditi kao da je njegov.
        await gateway.LoadProgramAsync(Kabl(), CancellationToken.None);
        gateway.Scenario = FakeScenario.Timeout;

        GatewayResult result = await gateway.WaitForResultAsync(
            TimeSpan.FromMilliseconds(150),
            CancellationToken.None);

        Assert.Equal(GatewayStatus.Timeout, result.Status);
    }

    /// <summary>Rad preko fajlova ne može da pokrene test — i to kaže, a ne baca.</summary>
    [Fact]
    public async Task File_PokretanjeTesta_VracaNotSupported()
    {
        await using var gateway = new FileBasedTesterGateway(new TesterGatewayOptions { ResultPath = string.Empty });

        GatewayResult result = await gateway.StartTestAsync(CancellationToken.None);

        Assert.Equal(GatewayStatus.NotSupported, result.Status);
        Assert.False(gateway.Capabilities.CanStartTest);
        Assert.NotEqual(string.Empty, gateway.Capabilities.StartInstruction);
    }

    [Fact]
    public async Task File_CekanjeBezPripremljenogPrograma_VracaGresku()
    {
        await using var gateway = new FileBasedTesterGateway(new TesterGatewayOptions { ResultPath = string.Empty });

        GatewayResult result = await gateway.WaitForResultAsync(TimeSpan.FromMilliseconds(50), CancellationToken.None);

        Assert.Equal(GatewayStatus.InvalidState, result.Status);
    }

    private static Cable Kabl()
    {
        var cable = new Cable { Id = 1, Code = "M100-W1", SpecFileName = "M100W1" };
        cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02" });
        return cable;
    }
}
