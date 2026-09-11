# CableTest

Windows desktop aplikacija (demo) za automatizaciju ispitivanja kablovskih setova na testeru
**Microtest 8761NK** (512 ispitnih tačaka), uz proizvođačev softver **CableConnector V3.12.17**.

> Stanje: u izradi. Gotovi su model podataka, generator `.c61` fajla i čitanje CSV-a sa detekcijom
> kodiranja. Slede parser CSV rezultata, `ITesterGateway` sa nadgledanjem fajla, SQLite baza i GUI.

## Pokretanje

```
run.cmd      pokreće aplikaciju
test.cmd     pokreće sve testove nad CableTest.sln
```

### .NET SDK

Na ovoj mašini u `C:\Program Files\dotnet` postoji **samo runtime** (6.0 i 8.0), bez SDK-a, pa
`dotnet build` i `dotnet run` odatle ne rade. Zato je .NET 8 SDK instaliran **po korisniku**, u:

```
C:\Users\<ime>\.dotnet
```

Instalacija je urađena zvaničnom skriptom `https://dot.net/v1/dotnet-install.ps1` sa
`-Channel 8.0 -InstallDir "$env:USERPROFILE\.dotnet"`. Ne traži administratorska prava, ne dira
sistemsku instalaciju i **uklanja se prostim brisanjem foldera `C:\Users\<ime>\.dotnet`**.

Pošto se `dotnet` sa PATH-a i dalje razrešava na sistemski runtime bez SDK-a, `run.cmd` i `test.cmd`
namerno pozivaju pun put `%USERPROFILE%\.dotnet\dotnet.exe`. Ručno isto radi ovako:

```
"%USERPROFILE%\.dotnet\dotnet.exe" run --project CableTest.App
"%USERPROFILE%\.dotnet\dotnet.exe" test CableTest.sln
```

## Struktura

```
CableTest.sln
CableTest.Core\      model, generator .c61, čitanje/parsiranje CSV-a, baza, gateway
CableTest.App\       WPF aplikacija (net8.0-windows)
CableTest.Tests\     xunit testovi
templates\MASTER.c61 šablon test programa
run.cmd, test.cmd
```

## Tok rada

1. Aplikacija generiše `.c61` (test program) i upisuje ga u spec folder,
   podrazumevano `C:\Cable Linker8761\spec\`.
2. Operater u CableConnector-u izabere taj spec, pritisne **Download** i pokrene test.
3. CableConnector upisuje rezultat kao novi red u CSV fajl.
4. Aplikacija nadgleda taj CSV, čita nove redove, upisuje ih u bazu i pravi mernu kartu.

U ovoj fazi aplikacija **ne komunicira sa testerom** — razmena ide preko fajlova. Rad sa fajlovima
je izolovan iza `ITesterGateway`, pa se u fazi 2 dodaje `SerialTesterGateway` (RS-232) bez diranja
ostatka aplikacije.

## `templates\MASTER.c61`

**Ovaj fajl treba zameniti fajlom snimljenim iz samog CableConnector-a, sa stvarnim proizvodnim
parametrima.** Priloženi šablon je primer.

Aplikacija **nikada ne generiše zaglavlje `.c61` fajla**. Učita šablon i zameni isključivo blok
`OSNet=` / `OSNet:`; sve ostalo prolazi bajt po bajt nepromenjeno. Razlog: parametri u zaglavlju
(naponi, pragovi, vremena) moraju biti iz zatvorenog skupa vrednosti koje CableConnector mapira
nazad u svoje padajuće liste. Vrednost van tog skupa ruši CableConnector sa porukom
`Index was outside the bounds of the array`. Šablon snimljen samim CableConnector-om je jedini
pouzdan izvor tih vrednosti.

Prateći `.osn` fajl se ne generiše — nije obavezan, služi samo za prikazna imena netova u GUI-ju
proizvođačevog softvera.

## Imena spec fajlova

Jedan kabl — jedan `.c61` fajl, imenovan po `Cable.SpecFileName`. Ograničenje od 500 test programa
odnosi se na internu memoriju testera, ne na spec folder na disku, a u tester se učitava samo
program koji je trenutno potreban. Zahvaljujući tome kolona `Filename` u CSV rezultatu jednoznačno
određuje kabl.

Dozvoljeno ime: velika slova `A-Z`, cifre `0-9` i crtica, najviše **8 znakova**, bez ekstenzije
(dodaje je generator). Granica dužine je **pretpostavka** koju treba potvrditi kod proizvođača —
stoji kao konstanta `SpecFileNameValidator.MaxLength`. Neispravno ime se odbija pri unosu, umesto da
ga tester tiho odseče i time raskine vezu rezultata sa kablom.

## Kultura i kodiranje

- `.c61` se upisuje kao čist **ASCII**, bez BOM-a, sa **CRLF** prelomima; nigde `Environment.NewLine`.
- Svako formatiranje i parsiranje brojeva ide kroz `CultureInfo.InvariantCulture`. Windows je na
  srpskom, gde je decimalni separator zarez, a `.c61` i CSV su razdvojeni zarezima.
- CSV piše CableConnector i u kolonama `Operater` i `Barcode1` može imati srpska slova, pa se čita
  prvo kao UTF-8 sa strogom proverom, a zatim, ako to ne uspe, kao Windows-1250.
- CSV se otvara sa `FileShare.ReadWrite` jer ga CableConnector može držati otvorenim.

## OneDrive

Spec folder i CSV fajl ne smeju biti unutar OneDrive foldera — CableConnector tada javlja
`Could not find file`. Podešavanja upozoravaju na takvu putanju.
