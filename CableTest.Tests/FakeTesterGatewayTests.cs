using CableTest.Core.Gateway;
using CableTest.Core.Model;
using Xunit;

namespace CableTest.Tests;

/// <summary>
/// Lažni gateway — njime se razvija i proverava GUI dok se ne sedne za sto pored testera.
/// Mora da se ponaša kao pravi: bez pokrenutog nadgledanja ništa ne stiže, a isti rezultat
/// ne stiže dvaput.
/// </summary>
public class FakeTesterGatewayTests
{
    private static Cable Kabl()
    {
        var cable = new Cable { Id = 1, Code = "M100-W1", SpecFileName = "M100W1" };
        cable.Nets.Add(new CableNet { Ordinal = 1, Points = "O01-O02-O31-O32" });
        return cable;
    }

    [Fact]
    public void Receive_DokJeNadgledanjeAktivno_OkidaDogadjaj()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.TestRunReceived += (_, e) => received.Add(e.Run);

        gateway.StartMonitoring();
        TestRun run = gateway.ReceivePass(Kabl());

        Assert.Same(run, Assert.Single(received));
        Assert.True(run.Passed);
        Assert.Equal("M100W1", run.SpecFileName);
        Assert.Equal(1, gateway.State.ProcessedRunCount);
    }

    [Fact]
    public void Receive_BezPokrenutogNadgledanja_NeOkidaNista()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.TestRunReceived += (_, e) => received.Add(e.Run);

        gateway.ReceivePass(Kabl());

        Assert.Empty(received);
        Assert.False(gateway.State.IsMonitoring);
    }

    [Fact]
    public void StopMonitoring_ZaustavljaDogadjaje()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.TestRunReceived += (_, e) => received.Add(e.Run);

        gateway.StartMonitoring();
        gateway.ReceivePass(Kabl());
        gateway.StopMonitoring();
        gateway.ReceivePass(Kabl());

        Assert.Single(received);
    }

    [Fact]
    public void ReceiveFail_PravIGreskeNaKojeGuiMozeDaSeVezuje()
    {
        var gateway = new FakeTesterGateway();
        gateway.StartMonitoring();

        TestRun run = gateway.ReceiveFail(Kabl(), "SHORT O01-O02", "OPEN O31-O32");

        Assert.False(run.Passed);
        Assert.Equal(2, run.Defects.Count);
        Assert.Equal(DefectKind.Short, run.Defects[0].Kind);
        Assert.Equal("O01-O02", run.Defects[0].Points);
        Assert.Equal(DefectKind.Open, run.Defects[1].Kind);
        Assert.Equal("Prekid između tačaka O31 i O32", run.Defects[1].Describe());
    }

    [Fact]
    public void ReceiveCsvLine_ProlaziKrozPraviParser()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.TestRunReceived += (_, e) => received.Add(e.Run);
        gateway.StartMonitoring();

        gateway.ReceiveCsvLine("1,M100W1,fail,2026/09/07,11:47:34,,,Miloš,FAIL,SHORT O01-O02;FAIL,");

        TestRun run = Assert.Single(received);
        Assert.Equal(1, run.Seq);
        Assert.False(run.Passed);
        Assert.Equal(new DateTime(2026, 9, 7, 11, 47, 34), run.TestedAt);
        Assert.Equal("Miloš", run.Operator);
        Assert.Equal("O01-O02", Assert.Single(run.Defects).Points);
    }

    [Fact]
    public void IstiRezultatDvaput_SePrijavljujeJednom()
    {
        var gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.TestRunReceived += (_, e) => received.Add(e.Run);
        gateway.StartMonitoring();

        const string line = "1,M100W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS,";
        gateway.ReceiveCsvLine(line);
        gateway.ReceiveCsvLine(line);

        Assert.Single(received);
        Assert.Equal(1, gateway.State.SkippedDuplicateCount);
    }

    [Fact]
    public void Receive_PodrazumevanoJeUzivo()
    {
        var gateway = new FakeTesterGateway();
        TestRunReceivedEventArgs? primljeno = null;
        gateway.TestRunReceived += (_, e) => primljeno = e;
        gateway.StartMonitoring();

        gateway.ReceivePass(Kabl());

        Assert.NotNull(primljeno);
        Assert.False(primljeno!.IsBackfill);
        Assert.True(primljeno.IsLive);
    }

    [Fact]
    public void ReceiveBackfillPass_JeOznacenKaoBackfill()
    {
        var gateway = new FakeTesterGateway();
        TestRunReceivedEventArgs? primljeno = null;
        gateway.TestRunReceived += (_, e) => primljeno = e;
        gateway.StartMonitoring();

        gateway.ReceiveBackfillPass(Kabl());

        Assert.NotNull(primljeno);
        Assert.True(primljeno!.IsBackfill);
        Assert.False(primljeno.IsLive);
    }

    [Fact]
    public async Task PrepareTestAsync_PamtiKablBezDiranjaDiska()
    {
        var gateway = new FakeTesterGateway();
        Cable cable = Kabl();

        await gateway.PrepareTestAsync(cable, CancellationToken.None);

        Assert.Same(cable, Assert.Single(gateway.PreparedCables));
        Assert.Equal("M100W1.c61", gateway.State.LastPreparedSpecPath);
    }

    [Fact]
    public async Task PrepareFailure_SePrenosiPozivaocu()
    {
        var gateway = new FakeTesterGateway
        {
            PrepareFailure = new DirectoryNotFoundException("Spec folder ne postoji.")
        };

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => gateway.PrepareTestAsync(Kabl(), CancellationToken.None));

        Assert.Empty(gateway.PreparedCables);
    }

    /// <summary>Ostatak aplikacije radi sa interfejsom — ovo je provera da je to moguće.</summary>
    [Fact]
    public void RadiKrozInterfejs()
    {
        ITesterGateway gateway = new FakeTesterGateway();
        var received = new List<TestRun>();
        gateway.TestRunReceived += (_, e) => received.Add(e.Run);

        gateway.StartMonitoring();
        Assert.True(gateway.State.IsMonitoring);

        ((FakeTesterGateway)gateway).ReceivePass(Kabl());

        Assert.Single(received);
    }
}
