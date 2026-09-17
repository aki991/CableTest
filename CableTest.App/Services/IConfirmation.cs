using System.Windows;

namespace CableTest.App.Services;

/// <summary>Pitanje operateru pre radnje koja se ne može opozvati.</summary>
/// <remarks>
/// Iza interfejsa je da bi ViewModel mogao da se ispita bez otvaranja prozora, i da bi se u
/// testu videlo da je potvrda zaista tražena — a ne samo da je radnja izvršena.
/// </remarks>
public interface IConfirmation
{
    /// <summary>Vraća <c>true</c> ako je operater potvrdio.</summary>
    /// <param name="title">Naslov prozorčeta.</param>
    /// <param name="message">Pitanje, napisano tako da se vidi šta će se zaista desiti.</param>
    bool Ask(string title, string message);
}

/// <summary>Potvrda kroz obično Windows prozorče.</summary>
/// <remarks>
/// Podrazumevano dugme je „Ne": ovo se pita pred radnjama koje pokreću stvarni hardver, pa
/// nehotičan <c>Enter</c> ne sme da znači „Da".
/// </remarks>
public sealed class MessageBoxConfirmation : IConfirmation
{
    public bool Ask(string title, string message)
        => MessageBox.Show(
               message,
               title,
               MessageBoxButton.YesNo,
               MessageBoxImage.Warning,
               MessageBoxResult.No)
           == MessageBoxResult.Yes;
}
