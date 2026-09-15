# CableTest

Windows desktop aplikacija (demo) za automatizaciju ispitivanja kablovskih setova na testeru
**Microtest 8761NK** (512 ispitnih tačaka), uz proizvođačev softver **CableConnector V3.12.17**.

> Stanje: u izradi. Gotovi su model podataka, generator `.c61` fajla, čitanje i parsiranje CSV-a,
> `ITesterGateway` sa nadgledanjem fajla, SQLite baza i glavni ekran „Ispitivanje" sa mernom
> kartom. Slede ekrani Istorija i Podešavanja.

## Pokretanje

```
run.cmd          pokreće aplikaciju
run.cmd --demo   pokreće aplikaciju u demo režimu, bez testera
test.cmd         pokreće sve testove nad CableTest.sln
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
CableTest.Core\
  Model\             Vehicle, Cable, Net, TestRun, TestDefect, TestRunKey
  Spec\              generator .c61, provera imena spec fajla
  Results\           čitanje i parsiranje CSV-a sa rezultatima
  Gateway\           ITesterGateway, FileBasedTesterGateway, FakeTesterGateway
  Data\              SQLite baza, migracije, repozitorijumi
  Configuration\     settings.json, provera putanja
  Reporting\         merna karta (HTML)
CableTest.App\
  Mvvm\              ObservableObject, RelayCommand
  Services\          zvučni signali, otvaranje dokumenta
  ViewModels\        TestingViewModel
  Views\             TestingView.xaml
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

## Merene vrednosti u CSV-u — pretpostavka

Parser razlikuje izmerenu vrednost (npr. provodnu otpornost) od poruke o grešci regularnim
izrazom `ResultCsvParser.MeasurementRegex`. **Taj izraz je pretpostavka, ne znanje.** Napravljen je
na osnovu jednog jedinog stvarnog CSV fajla sa dva reda, u kome je bio uključen samo Open/Short
test i u kome zato nema nijedne izmerene vrednosti.

Varijante koje trenutno pokrivamo nabrojane su u `MeasurementPatternTests`. Kad stigne stvarni fajl
sa uključenim Cond testom, njegov zapis se dodaje kao jedan red `[InlineData("...")]`; ako test
padne, izraz se dopunjuje na jednom mestu. Cilj nije da pogodimo format, nego da odmah vidimo šta
ne pokrivamo — izmerena vrednost koju ne prepoznamo ušla bi u istoriju kao lažna greška.

## Prirodni ključ rezultata

Zapis o testu nema svoj identifikator u CSV-u. Prirodni ključ je `SpecFileName` + `Seq` +
`TestedAt` (`Model.TestRunKey`), a sprovodi se na tri mesta:

- `ResultCsvParser.RemoveDuplicates` — nad listom pročitanih zapisa,
- `FileBasedTesterGateway` — isti rezultat se ne prijavljuje dvaput u jednom radu aplikacije,
- jedinstveni indeks `UX_TestRun_Natural` u bazi — poslednja i sigurna odbrana.

Potrebno je zato što parser, kada fajl postane kraći nego ranije, zaključuje da je zamenjen novim
i čita ga od početka. To je ispravno kod dnevne rotacije, ali ako CableConnector prepiše fajl istim
imenom, svi redovi dolaze ponovo. Istorija testova mora biti tačna — ona je smisao celog projekta.

## Nadgledanje CSV-a

`FileBasedTesterGateway` nadgleda fajl i `FileSystemWatcher`-om **i** tajmerom na 2 sekunde.
Watcher daje brzu reakciju ali poznato propušta događaje, pa se na njega ne oslanjamo sam; tajmer
poredi veličinu i vreme izmene. Fajl se otvara sa `FileShare.ReadWrite | FileShare.Delete` jer ga
CableConnector drži otvorenim, a čitaju se samo novi redovi.

Ako je podešena putanja foldera umesto konkretnog fajla, prati se najnoviji CSV u njemu (dnevna
rotacija po datumu u imenu). Fajl koji ne postoji, nestane ili bude zamenjen ne ruši nadgledanje —
stanje se vidi kroz `ITesterGateway.State`.

### Backfill — bezbednosno pravilo

Rezultati zatečeni u fajlu pri pokretanju nadgledanja se prijavljuju kao i svi ostali, da se ne
izgubi ono što je tester zapisao dok aplikacija nije radila. Ali dolaze označeni:
`TestRunReceivedEventArgs.IsBackfill == true`.

**Veliki prikaz ishoda sme da se pomeri isključivo na rezultat sa `IsBackfill == false`.** Bez
toga: operater ujutru pokrene aplikaciju, ona pročita jučerašnji CSV i na ekranu ostane PASS od
sinoć u 18:40; operater stavi kabl, vidi PASS i pusti ga dalje, a test nije ni pokrenut. Time bi
neispitan kabl otišao u vozilo. Pri pokretanju ekran sa rezultatom stoji prazan, sa tekstom
„Čeka se rezultat".

Baza i istorija primaju oba tipa bez razlike — zatečeni redovi su stvarni testovi.

`FakeTesterGateway` dozvoljava da rezultat „stigne" programski, pa se GUI razvija i testira bez
mašine. `ReceivePass` i `ReceiveCsvLine` podrazumevano šalju uživo rezultat, a
`ReceiveBackfillPass` zatečeni.

## Baza

SQLite (`Microsoft.Data.Sqlite`), bez ORM-a. Migracije su numerisane SQL skripte u
`CableTest.Core\Data\Migrations`, ugrađene u sklop i primenjene pri pokretanju, uz evidenciju u
tabeli `SchemaVersion`. Skripta koja je jednom primenjena se ne menja — izmena ide kao nova, sa
većim brojem.

Svi datumi se čuvaju kao ISO 8601 tekst u UTC (`2026-09-07T09:44:11.000Z`), a prikazuju u lokalnom
vremenu; pretvaranje je na jednom mestu, u `Data.IsoDate`. Pristup bazi ide kroz repozitorijume sa
interfejsima, da GUI ne zna za SQL.

Podrazumevana putanja baze: `%LOCALAPPDATA%\CableTest\CableTest.db`.

## GUI

WPF, MVVM, bez MVVM biblioteke i bez kontejnera — aplikacija se sastavlja na jednom mestu, u
`App.xaml.cs`. ViewModel-i su u `CableTest.App`, a testovi ih pokrivaju bez otvaranja prozora
(test projekat je zato `net8.0-windows`).

Ekran je pravljen za pogon, ne za sto: krupna slova, veliki kontrast, ishod zauzima najveći deo
ekrana. Uz boju uvek ide i tekst (PASS / FAIL, „KABL JE ISPRAVAN" / „KABL NIJE ISPRAVAN"), jer se
na boju samu ne sme oslanjati. Uz svaki rezultat ide i zvučni signal, različit za PASS i FAIL —
operater u tom trenutku gleda kabl, ne ekran.

Dve provere koje kompajler ne hvata pokrivene su testovima: da se XAML učitava sa svim
`StaticResource` ključevima i da svako `{Binding}` pokazuje na postojeće svojstvo ViewModel-a.
Pogrešno ime u vezi ne izaziva grešku — polje jednostavno ostane prazno, a ekran izgleda ispravno.

### Merna karta

HTML dokument koji se otvara u podrazumevanom pregledaču, sa stilom za štampu (A4, `@media print`).
Bez biblioteke za PDF — svaki pregledač u štampi ima „Sačuvaj kao PDF". Sadrži vozilo, kabl i opis,
net listu, vreme testa, operatera, ukupan ishod, spisak grešaka i mesto za potpis kontrolora.

### Demo režim

`run.cmd --demo` (ili prekidač u podešavanjima) pokreće aplikaciju sa `FakeTesterGateway` umesto
testera. Na ekranu stoji ljubičasta traka „DEMO REŽIM — rezultati su simulirani, tester nije
priključen", a u njoj dugmad **Simuliraj PASS** i **Simuliraj FAIL** koja ubacuju uživo rezultat
kroz pravi parser, sa tačkama iz net liste izabranog kabla.

Demo radi nad **zasebnim fajlom baze** (`CableTest.demo.db`). Simulirani rezultati ne smeju da se
nađu u stvarnoj istoriji — ona je dokaz da je kabl ispitan i sme da sadrži samo ono što je tester
zaista izmerio.

### Podešavanja

`%APPDATA%\CableTest\settings.json`. Ekran za izmenu dolazi u sledećem koraku; do tada se fajl
čita sa podrazumevanim vrednostima, a sve što nije u redu (OneDrive putanja, folder koji ne postoji
ili nije upisiv, šablon koji nedostaje) prikazuje se u traci pri dnu glavnog ekrana — ćutanje o
grešci je gore od greške.
