namespace CableTest.Core.Gateway;

/// <summary>
/// Poništavanje ponovljenih obaveštenja o istoj putanji.
/// </summary>
/// <remarks>
/// <para>
/// Jedan upis u fajl ume da podigne više događaja <see cref="FileSystemWatcher"/>-a: posebno za
/// veličinu, posebno za vreme izmene, a kod nekih programa i po nekoliko puta dok pišu u delovima.
/// Svaki od njih bi pokrenuo čitanje sa odlaganjima — posao bez ikakvog dobitka.
/// </para>
/// <para>
/// Zato se po putanji pamti kada je poslednji put propušten događaj: sve što stigne unutar
/// prozora se preskače. Tajmer u međuvremenu radi svoje, pa se preskakanjem ništa ne gubi — samo
/// se reakcija pomeri za najviše jedan prolaz.
/// </para>
/// <para>
/// Vreme se predaje spolja, pa se ponašanje može ispitati testom bez ijednog čekanja.
/// </para>
/// </remarks>
public sealed class PathDebounce
{
    private readonly object _sync = new();
    private readonly Dictionary<string, DateTime> _last = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _window;

    public PathDebounce(TimeSpan window) => _window = window;

    /// <summary>Koliko dugo se ponovljeni događaji za istu putanju preskaču.</summary>
    public TimeSpan Window => _window;

    /// <summary>
    /// Da li se događaj za zadatu putanju obrađuje.
    /// </summary>
    /// <remarks>
    /// Prvi događaj za putanju uvek prolazi. Kad prođe, vreme se pamti — pa sve u narednom
    /// prozoru otpada.
    /// </remarks>
    public bool ShouldHandle(string path, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        lock (_sync)
        {
            if (_last.TryGetValue(path, out DateTime last) && now - last < _window)
            {
                return false;
            }

            _last[path] = now;
            return true;
        }
    }

    /// <summary>Zaboravlja sve zapamćeno; poziva se pri pokretanju nadgledanja.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _last.Clear();
        }
    }
}
