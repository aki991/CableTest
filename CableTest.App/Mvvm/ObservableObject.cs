using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CableTest.App.Mvvm;

/// <summary>
/// Osnova za ViewModel-e: obaveštavanje prikaza o izmeni svojstva.
/// </summary>
/// <remarks>
/// Namerno bez MVVM biblioteke. Aplikacija ima tri ekrana i ovoliko je dovoljno; jedna zavisnost
/// manje na proizvodnoj mašini, a i vidi se šta se dešava.
/// </remarks>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Postavlja polje i javlja izmenu ako se vrednost zaista promenila.</summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Raise(propertyName);
        return true;
    }

    /// <summary>Javlja da se svojstvo promenilo.</summary>
    protected void Raise([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Javlja izmenu više svojstava odjednom (izračunata svojstva).</summary>
    protected void RaiseAll(params string[] propertyNames)
    {
        foreach (string name in propertyNames)
        {
            Raise(name);
        }
    }
}
