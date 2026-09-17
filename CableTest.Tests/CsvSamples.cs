namespace CableTest.Tests;

/// <summary>
/// CSV uzorci onakvi kakve piše CableConnector.
/// </summary>
/// <remarks>
/// Na jednom mestu zato što ih koriste i testovi parsera i testovi veze sa testerom. Kad bi svaki
/// test pisao svoj red, provere bi se vremenom razišle od onoga što mašina zaista zapisuje — a
/// baš to poklapanje je ovde jedino što vredi proveravati.
/// </remarks>
internal static class CsvSamples
{
    public const string CrLf = "\r\n";

    /// <summary>Zaglavlje tabele rezultata, prepisano iz stvarnog fajla.</summary>
    public const string Header = "Seq.,Filename,Pass,Date,Time,Lots,Barcode1,Operater,STEP 1,O/S TEST,unit,";

    /// <summary>Stvarni fajl iz pogona: zaglavlje spec-a, pa dva testa — drugi sa tri kratka spoja.</summary>
    public const string RealFile =
        "File Name: PROBA1" + CrLf +
        "Model:8761NK" + CrLf +
        "O/S:5KOHM" + CrLf +
        "-------------------------------" + CrLf +
        "001 O01-O02-O31-O32" + CrLf +
        "-------------------------------" + CrLf +
        Header + CrLf +
        "1,PROBA1,pass,2026/09/07,11:44:11,,,Andreja,PASS,PASS," + CrLf +
        "2,PROBA1,fail,2026/09/07,11:47:34,,,Andreja,FAIL, SHORT O01-O02; SHORT O01-O31; SHORT O01-O32;FAIL," + CrLf;

    /// <summary>Fajl sa jednim prolaznim rezultatom.</summary>
    public const string PassFile =
        Header + CrLf +
        "1,M100W1,pass,2026/09/07,11:44:11,,,Miloš,PASS,PASS," + CrLf;

    /// <summary>Fajl sa jednim rezultatom koji je pao, sa kratkim spojem.</summary>
    public const string FailFile =
        Header + CrLf +
        "2,M100W1,fail,2026/09/07,11:47:34,,,Miloš,FAIL,SHORT O01-O02;FAIL," + CrLf;

    /// <summary>Jedan red rezultata, onakav kakav stoji u fajlu.</summary>
    public static string Row(int seq, string pass, string time, string osTest = "PASS", string date = "2026/09/07")
        => $"{seq},M100W1,{pass},{date},{time},,,Miloš,PASS,{osTest}," + CrLf;
}
