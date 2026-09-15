-- Šema baze, verzija 1.
--
-- Svi datumi su ISO 8601 tekst u UTC, npr. "2026-09-07T09:44:11.000Z". U UTC zato što se
-- vreme čuva, poredi i sortira nezavisno od letnjeg računanja vremena; u lokalnom vremenu se
-- samo prikazuje. Pretvaranje je na jednom mestu — CableTest.Core.Data.IsoDate.

CREATE TABLE Vehicle (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Name      TEXT NOT NULL COLLATE NOCASE,
    CreatedAt TEXT NOT NULL
);

CREATE UNIQUE INDEX UX_Vehicle_Name ON Vehicle (Name);

CREATE TABLE Cable (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    VehicleId    INTEGER NOT NULL REFERENCES Vehicle (Id) ON DELETE CASCADE,
    Code         TEXT NOT NULL COLLATE NOCASE,
    Description  TEXT NOT NULL DEFAULT '',
    SpecFileName TEXT NOT NULL COLLATE NOCASE,
    CreatedAt    TEXT NOT NULL
);

-- Jedan kabl — jedan .c61 fajl. Kolona "Filename" u CSV rezultatu je jedina veza rezultata sa
-- kablom, pa dva kabla ne smeju da dele ime spec fajla. Poređenje je bez obzira na velika i
-- mala slova (COLLATE NOCASE na koloni), jer tester ime piše velikim slovima.
CREATE UNIQUE INDEX UX_Cable_SpecFileName ON Cable (SpecFileName);
CREATE UNIQUE INDEX UX_Cable_VehicleCode ON Cable (VehicleId, Code);

CREATE TABLE Net (
    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
    CableId INTEGER NOT NULL REFERENCES Cable (Id) ON DELETE CASCADE,
    Ordinal INTEGER NOT NULL,
    Points  TEXT NOT NULL
);

CREATE UNIQUE INDEX UX_Net_CableOrdinal ON Net (CableId, Ordinal);

CREATE TABLE TestRun (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    -- NULL kada ime spec fajla iz CSV-a ne odgovara nijednom kablu. Rezultat se svejedno čuva:
    -- operater je možda pokrenuo test iz drugog spec fajla i to treba da se vidi u istoriji.
    CableId      INTEGER NULL REFERENCES Cable (Id) ON DELETE SET NULL,
    Seq          INTEGER NOT NULL,
    SpecFileName TEXT NOT NULL COLLATE NOCASE,
    Passed       INTEGER NOT NULL,
    TestedAt     TEXT NOT NULL,
    Operator     TEXT NOT NULL DEFAULT '',
    Barcode      TEXT NOT NULL DEFAULT '',
    RawRow       TEXT NOT NULL DEFAULT '',
    CreatedAt    TEXT NOT NULL
);

-- Prirodni ključ zapisa. Sprovodi pravilo da isti rezultat ne može da uđe u istoriju dvaput:
-- parser, kad fajl postane kraći nego ranije, čita ga od početka, pa se već upisani redovi
-- ponovo pojave. Ovaj indeks je poslednja i sigurna odbrana — istorija testova mora biti tačna.
CREATE UNIQUE INDEX UX_TestRun_Natural ON TestRun (SpecFileName, Seq, TestedAt);

CREATE INDEX IX_TestRun_Cable ON TestRun (CableId, TestedAt);
CREATE INDEX IX_TestRun_TestedAt ON TestRun (TestedAt);

CREATE TABLE TestDefect (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    TestRunId INTEGER NOT NULL REFERENCES TestRun (Id) ON DELETE CASCADE,
    -- "Short", "Open" ili "Unknown" — tekst, da se u upitu vidi šta piše.
    Kind      TEXT NOT NULL,
    Points    TEXT NOT NULL DEFAULT '',
    RawText   TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IX_TestDefect_TestRun ON TestDefect (TestRunId);
