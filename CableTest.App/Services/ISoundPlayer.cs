using System.Media;

namespace CableTest.App.Services;

/// <summary>
/// Zvučni signal uz rezultat.
/// </summary>
/// <remarks>
/// Operater ne gleda ekran u trenutku kada test završi — ruke su mu na kablu. Zvuk je zato
/// ravnopravan sa bojom, a ne ukras. Iza interfejsa je zato da bi testovi mogli da provere da je
/// zvuk pušten, bez pravljenja zvuka.
/// </remarks>
public interface ISoundPlayer
{
    /// <summary>Signal za prošao test.</summary>
    void PlayPass();

    /// <summary>Signal za pao test. Mora zvučati drugačije od <see cref="PlayPass"/>.</summary>
    void PlayFail();
}

/// <summary>Zvuk preko sistemskih signala Windows-a.</summary>
/// <remarks>
/// <see cref="SystemSounds"/> je dovoljan: ne traži nijedan fajl uz aplikaciju, radi na svakoj
/// mašini i poštuje podešavanja zvuka korisnika. <c>Asterisk</c> je kratak i vedar,
/// <c>Hand</c> je oštar zvuk greške — razlika se čuje i kroz buku u pogonu.
/// </remarks>
public sealed class SystemSoundPlayer : ISoundPlayer
{
    public void PlayPass() => SystemSounds.Asterisk.Play();

    public void PlayFail() => SystemSounds.Hand.Play();
}

/// <summary>Ne pušta ništa — kad su zvučni signali isključeni u podešavanjima.</summary>
public sealed class SilentSoundPlayer : ISoundPlayer
{
    public void PlayPass()
    {
    }

    public void PlayFail()
    {
    }
}

/// <summary>Zvuk koji se uključuje i isključuje u toku rada, bez menjanja ViewModel-a.</summary>
public sealed class SwitchableSoundPlayer : ISoundPlayer
{
    private readonly ISoundPlayer _inner;

    public SwitchableSoundPlayer(ISoundPlayer inner, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        IsEnabled = enabled;
    }

    /// <summary>Da li se zvuk pušta.</summary>
    public bool IsEnabled { get; set; }

    public void PlayPass()
    {
        if (IsEnabled)
        {
            _inner.PlayPass();
        }
    }

    public void PlayFail()
    {
        if (IsEnabled)
        {
            _inner.PlayFail();
        }
    }
}
