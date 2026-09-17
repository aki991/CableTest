-- Kablovi vozila „Miloš Veliki“, grupa =40, verzija šeme 4.
--
-- IZVOR PODATAKA: elektro dokumentacija „Elektro dokumentacija BOV M16 Miloš“,
-- crtež 40_grupa_2_0.pdf, 12 strana, datum 11.2.2021. Strana crteža je upisana uz svaki
-- kabl (Cable.SourcePage), pa se svaki podatak može vratiti na izvor.
--
-- ŠTA SE OVDE MENJA
-- Do sada je net lista unošena ručno, kao spisak tačaka testera. Crtež, međutim, opisuje
-- povezivanje sa strane vozila (pin 2 DIN konektora, pin 10XF:02, papučica), a tester poznaje
-- samo tačke A01–P32. Između to dvoje stoji priključni pribor (adapter) na stolu, koji još nije
-- napravljen. Zato se od ove verzije čuvaju dve odvojene stvari:
--
--   1. OŽIČENJE  — tabele CableTerminal i CableWire: koji je terminal sa kojim spojen, kojom
--                  žicom, koje boje i preseka. To je prepis crteža i ne zavisi od testera.
--   2. PRIKLJUČNA TABELA — kolona CableTerminal.TesterPoint: na koju tačku testera ide koji
--                  terminal. To pripada adapteru, ne kablu.
--
-- Net lista se iz toga IZVODI (CableTest.Core.Model.NetBuilder) i više se ne unosi. Kad se
-- napravi pravi adapter, menja se samo priključna tabela, a ožičenje ostaje netaknuto.
--
-- SVE DODELE TAČAKA SU PRIVREMENE (IsProvisional = 1): raspored je izabran tako da jedan
-- adapter posluži za svih 11 kablova sa DIN 72585 konektorom, a W5 ima zaseban adapter.
--
-- OTVORENA PITANJA (vidi i Notes pojedinih kablova):
--   TODO: x1/x2 na W1.x — da li je to pin 1 i pin 2 DIN konektora i zašto je raspored boja
--         obrnut u odnosu na W2–W4.
--   TODO: W4.3 — zaglavlje crteža kaže „PIN 10XB“, tabela kaže 10XC:05; uzeta je tabela.
--   TODO: da li nekorišćeni pinovi 3 i 4 DIN konektora treba da uđu u ispitivanje izolacije.
--   TODO: konačan raspored tačaka na adapteru, kad adapter bude napravljen.

-- =============================================================================
-- 1. Šema: podaci o kablu sa crteža, terminali i provodnici
-- =============================================================================

ALTER TABLE Cable ADD COLUMN Designation    TEXT    NOT NULL DEFAULT '';
ALTER TABLE Cable ADD COLUMN CableType      TEXT    NOT NULL DEFAULT '';
ALTER TABLE Cable ADD COLUMN LengthM        REAL    NOT NULL DEFAULT 0;
ALTER TABLE Cable ADD COLUMN Notes          TEXT    NOT NULL DEFAULT '';
ALTER TABLE Cable ADD COLUMN SourceDocument TEXT    NOT NULL DEFAULT '';
ALTER TABLE Cable ADD COLUMN SourcePage     INTEGER NOT NULL DEFAULT 0;

-- Kabl nad kojim je nešto već ispitano se ne briše ni kad izađe iz upotrebe — istorija je dokaz
-- da je ispitan. Umesto brisanja se gasi: nestaje iz padajuće liste, ostaje u istoriji.
ALTER TABLE Cable ADD COLUMN IsActive       INTEGER NOT NULL DEFAULT 1;

CREATE TABLE CableTerminal (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    CableId       INTEGER NOT NULL REFERENCES Cable (Id) ON DELETE CASCADE,
    -- Oznaka sa crteža: "DIN:1", "10XF:02", "PAP 1,5/M6", "BUK-BR".
    Label         TEXT NOT NULL COLLATE NOCASE,
    -- 'A' ili 'B' — strana kabla na crtežu.
    Side          TEXT NOT NULL,
    ContactType   TEXT NOT NULL DEFAULT '',
    -- Tačka testera preko adaptera; NULL dok nije dodeljena.
    TesterPoint   TEXT NULL COLLATE NOCASE,
    IsProvisional INTEGER NOT NULL DEFAULT 0,
    Notes         TEXT NOT NULL DEFAULT ''
);

CREATE UNIQUE INDEX UX_CableTerminal_Label ON CableTerminal (CableId, Label);

-- Ista tačka testera ne sme da stoji uz dva terminala istog kabla: tester bi ih video kao
-- spojene i prijavio kratak spoj koga na kablu nema.
CREATE UNIQUE INDEX UX_CableTerminal_Point ON CableTerminal (CableId, TesterPoint)
    WHERE TesterPoint IS NOT NULL;

CREATE TABLE CableWire (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    CableId         INTEGER NOT NULL REFERENCES Cable (Id) ON DELETE CASCADE,
    WireNo          INTEGER NOT NULL,
    -- Skraćenica boje sa crteža: BR, PL, BE, ZE, ZU, SV; za mostove "most".
    Color           TEXT NOT NULL DEFAULT '',
    CrossSectionMm2 REAL NOT NULL DEFAULT 0,
    -- Dužina samo kad je posebno data (mostovi); inače NULL — važi dužina kabla.
    LengthM         REAL NULL,
    FromTerminal    TEXT NOT NULL COLLATE NOCASE,
    ToTerminal      TEXT NOT NULL COLLATE NOCASE,
    Notes           TEXT NOT NULL DEFAULT ''
);

CREATE UNIQUE INDEX UX_CableWire_No ON CableWire (CableId, WireNo);
CREATE INDEX IX_CableWire_Cable ON CableWire (CableId);

-- =============================================================================
-- 2. Vozilo: „Miloš“ postaje „Miloš Veliki“, uz zadržavanje istog Id
-- =============================================================================

UPDATE Vehicle
   SET Name = 'Miloš Veliki'
 WHERE Name = 'Miloš'
   AND NOT EXISTS (SELECT 1 FROM Vehicle v2 WHERE v2.Name = 'Miloš Veliki');

-- Ako oba vozila postoje (jedno iz migracije 002, drugo iz 003), kablovi prelaze na „Miloš
-- Veliki“, pa prazno vozilo odlazi. Kabl zadržava svoj Id, pa istorija ostaje vezana za njega.
UPDATE Cable
   SET VehicleId = (SELECT Id FROM Vehicle WHERE Name = 'Miloš Veliki')
 WHERE VehicleId IN (SELECT Id FROM Vehicle WHERE Name = 'Miloš')
   AND EXISTS (SELECT 1 FROM Vehicle WHERE Name = 'Miloš Veliki');

DELETE FROM Vehicle
 WHERE Name = 'Miloš'
   AND NOT EXISTS (SELECT 1 FROM Cable c WHERE c.VehicleId = Vehicle.Id);

INSERT INTO Vehicle (Name, CreatedAt)
SELECT 'Miloš Veliki', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
WHERE NOT EXISTS (SELECT 1 FROM Vehicle WHERE Name = 'Miloš Veliki');

-- =============================================================================
-- 3. Demo kablovi
-- =============================================================================
-- Ogledni katalog iz migracija 002 i 003 (M100W1, M30-W*, LM30-W*, KB1*P) je izmišljen.
-- Kabl bez ijednog zapisa u TestRun se briše zajedno sa svojim netovima; kabl nad kojim je
-- nešto ispitano OSTAJE i samo se gasi, jer je istorija dokaz ispitivanja.

DELETE FROM Cable
 WHERE (SpecFileName = 'M100W1'
        OR SpecFileName LIKE 'M30-W%'
        OR SpecFileName LIKE 'LM30-W%'
        OR SpecFileName LIKE 'KB1%P')
   AND NOT EXISTS (SELECT 1 FROM TestRun r WHERE r.CableId = Cable.Id);

UPDATE Cable
   SET IsActive = 0
 WHERE (SpecFileName = 'M100W1'
        OR SpecFileName LIKE 'M30-W%'
        OR SpecFileName LIKE 'LM30-W%'
        OR SpecFileName LIKE 'KB1%P');

-- Ogledna vozila koja su ostala bez ijednog kabla odlaze; „Miloš Veliki“ ostaje jer u njega
-- ulaze pravi kablovi.
DELETE FROM Vehicle
 WHERE Name IN ('Lazar 3M', 'Perun')
   AND NOT EXISTS (SELECT 1 FROM Cable c WHERE c.VehicleId = Vehicle.Id);

-- =============================================================================
-- 4. Kablovi sa crteža
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Grupa 1: =40-W1.1 … =40-W1.6 — Wabco 2x1,5 mm², strane 1–6
-- Terminali: DIN:1 (x1) i DIN:2 (x2) na strani A; pin 10XF i papučica na strani B.
-- Priključna tabela: DIN:1→A01, DIN:2→A02, pin→B01, papučica→B02
-- (A03 i A04 su rezervisani za pinove 3 i 4 DIN konektora i sada se ne koriste).
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W1.1', 'Fabrički Wabco kabl, DIN 72585 – 10XF:02 i papučica M6', '40W1-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W1.1', 'Wabco 2x1,5 mm²', 4.5, 'Fabrički kabl. DIN 72 585, dolazi sa kablom. Papučicu i pin postavljati na vozilu. NEPOTVRĐENO: crtež krajeve DIN konektora označava sa x1 i x2; ovde je protumačeno kao pin 1 i pin 2. Kod W2–W4 je raspored boja obrnut (PL na pinu 1) — razliku treba proveriti u dokumentaciji.', '40_grupa_2_0.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W1-1');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, 'na crtežu označen kao x1' FROM Cable c
WHERE c.SpecFileName = '40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, 'na crtežu označen kao x2' FROM Cable c
WHERE c.SpecFileName = '40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XF:02', 'B', 'pin 10XF (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XF:02');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1,5/M6', 'B', 'papučica 1,5/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1,5/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'DIN:1', '10XF:02', '' FROM Cable c
WHERE c.SpecFileName = '40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, 'DIN:2', 'PAP 1,5/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W1.2', 'Fabrički Wabco kabl, DIN 72585 – 10XF:08 i papučica M6', '40W1-2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W1.2', 'Wabco 2x1,5 mm²', 4.5, 'Fabrički kabl. DIN 72 585, dolazi sa kablom. Papučicu i pin postavljati na vozilu. NEPOTVRĐENO: crtež krajeve DIN konektora označava sa x1 i x2; ovde je protumačeno kao pin 1 i pin 2. Kod W2–W4 je raspored boja obrnut (PL na pinu 1) — razliku treba proveriti u dokumentaciji.', '40_grupa_2_0.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W1-2');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, 'na crtežu označen kao x1' FROM Cable c
WHERE c.SpecFileName = '40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, 'na crtežu označen kao x2' FROM Cable c
WHERE c.SpecFileName = '40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XF:08', 'B', 'pin 10XF (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XF:08');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1,5/M6', 'B', 'papučica 1,5/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1,5/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'DIN:1', '10XF:08', '' FROM Cable c
WHERE c.SpecFileName = '40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, 'DIN:2', 'PAP 1,5/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W1.3', 'Fabrički Wabco kabl, DIN 72585 – 10XF:11 i papučica M6', '40W1-3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W1.3', 'Wabco 2x1,5 mm²', 4.5, 'Fabrički kabl. DIN 72 585, dolazi sa kablom. Papučicu i pin postavljati na vozilu. NEPOTVRĐENO: crtež krajeve DIN konektora označava sa x1 i x2; ovde je protumačeno kao pin 1 i pin 2. Kod W2–W4 je raspored boja obrnut (PL na pinu 1) — razliku treba proveriti u dokumentaciji.', '40_grupa_2_0.pdf', 3, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W1-3');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, 'na crtežu označen kao x1' FROM Cable c
WHERE c.SpecFileName = '40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, 'na crtežu označen kao x2' FROM Cable c
WHERE c.SpecFileName = '40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XF:11', 'B', 'pin 10XF (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XF:11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1,5/M6', 'B', 'papučica 1,5/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1,5/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'DIN:1', '10XF:11', '' FROM Cable c
WHERE c.SpecFileName = '40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, 'DIN:2', 'PAP 1,5/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W1.4', 'Fabrički Wabco kabl, DIN 72585 – 10XF:14 i papučica M6', '40W1-4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W1.4', 'Wabco 2x1,5 mm²', 4.6, 'Fabrički kabl. DIN 72 585, dolazi sa kablom. Papučicu i pin postavljati na vozilu. NEPOTVRĐENO: crtež krajeve DIN konektora označava sa x1 i x2; ovde je protumačeno kao pin 1 i pin 2. Kod W2–W4 je raspored boja obrnut (PL na pinu 1) — razliku treba proveriti u dokumentaciji.', '40_grupa_2_0.pdf', 4, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W1-4');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, 'na crtežu označen kao x1' FROM Cable c
WHERE c.SpecFileName = '40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, 'na crtežu označen kao x2' FROM Cable c
WHERE c.SpecFileName = '40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XF:14', 'B', 'pin 10XF (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XF:14');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1,5/M6', 'B', 'papučica 1,5/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1,5/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'DIN:1', '10XF:14', '' FROM Cable c
WHERE c.SpecFileName = '40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, 'DIN:2', 'PAP 1,5/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W1.5', 'Fabrički Wabco kabl, DIN 72585 – 10XF:15 i papučica M6', '40W1-5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W1.5', 'Wabco 2x1,5 mm²', 4.6, 'Fabrički kabl. DIN 72 585, dolazi sa kablom. Papučicu i pin postavljati na vozilu. NEPOTVRĐENO: crtež krajeve DIN konektora označava sa x1 i x2; ovde je protumačeno kao pin 1 i pin 2. Kod W2–W4 je raspored boja obrnut (PL na pinu 1) — razliku treba proveriti u dokumentaciji.', '40_grupa_2_0.pdf', 5, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W1-5');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, 'na crtežu označen kao x1' FROM Cable c
WHERE c.SpecFileName = '40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, 'na crtežu označen kao x2' FROM Cable c
WHERE c.SpecFileName = '40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XF:15', 'B', 'pin 10XF (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XF:15');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1,5/M6', 'B', 'papučica 1,5/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1,5/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'DIN:1', '10XF:15', '' FROM Cable c
WHERE c.SpecFileName = '40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, 'DIN:2', 'PAP 1,5/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W1.6', 'Fabrički Wabco kabl, DIN 72585 – 10XF:07 i papučica M6', '40W1-6', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W1.6', 'Wabco 2x1,5 mm²', 4.6, 'Fabrički kabl. DIN 72 585, dolazi sa kablom. Papučicu i pin postavljati na vozilu. NEPOTVRĐENO: crtež krajeve DIN konektora označava sa x1 i x2; ovde je protumačeno kao pin 1 i pin 2. Kod W2–W4 je raspored boja obrnut (PL na pinu 1) — razliku treba proveriti u dokumentaciji.', '40_grupa_2_0.pdf', 6, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W1-6');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, 'na crtežu označen kao x1' FROM Cable c
WHERE c.SpecFileName = '40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, 'na crtežu označen kao x2' FROM Cable c
WHERE c.SpecFileName = '40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XF:07', 'B', 'pin 10XF (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XF:07');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1,5/M6', 'B', 'papučica 1,5/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1,5/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'DIN:1', '10XF:07', '' FROM Cable c
WHERE c.SpecFileName = '40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, 'DIN:2', 'PAP 1,5/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

-- -----------------------------------------------------------------------------
-- Grupa 2: =40-W2, =40-W3, =40-W4.1 … =40-W4.3 — Snop FLRY 2x0,75 mm², strane 7–11
-- Raspored boja je obrnut u odnosu na grupu 1: signalna žica ide sa DIN:2, PL sa DIN:1.
-- Priključna tabela: DIN:1→A01, DIN:2→A02, pin→B01, papučica→B02
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W2', 'Snop FLRY, DIN 72585 pod uglom – 10XB:16 i papučica M6', '40W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W2', 'Snop FLRY 2x0,75 mm²', 5.9, 'DIN 72 585, pod uglom. Papučicu i pin postavljati na vozilu.', '40_grupa_2_0.pdf', 7, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W2');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XB:16', 'B', 'pin 10XB/10XC (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XB:16');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1/M6', 'B', 'papučica 1/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, 'DIN:2', '10XB:16', '' FROM Cable c
WHERE c.SpecFileName = '40W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, 'DIN:1', 'PAP 1/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W3', 'Snop FLRY, DIN 72585 pod uglom – 10XB:15 i papučica M6', '40W3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W3', 'Snop FLRY 2x0,75 mm²', 3.4, 'DIN 72 585, pod uglom. Papučicu i pin postavljati na vozilu.', '40_grupa_2_0.pdf', 8, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W3');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XB:15', 'B', 'pin 10XB/10XC (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XB:15');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1/M6', 'B', 'papučica 1/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, 'DIN:2', '10XB:15', '' FROM Cable c
WHERE c.SpecFileName = '40W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, 'DIN:1', 'PAP 1/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W4.1', 'Snop FLRY, DIN 72585 pod uglom – 10XB:17 i papučica M6', '40W4-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W4.1', 'Snop FLRY 2x0,75 mm²', 4.8, 'DIN 72 585, pod uglom. Papučicu i pin postavljati na vozilu.', '40_grupa_2_0.pdf', 9, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W4-1');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XB:17', 'B', 'pin 10XB/10XC (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XB:17');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1/M6', 'B', 'papučica 1/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BE', 0.75, NULL, 'DIN:2', '10XB:17', '' FROM Cable c
WHERE c.SpecFileName = '40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, 'DIN:1', 'PAP 1/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W4.2', 'Snop FLRY, DIN 72585 pod uglom – 10XB:20 i papučica M6', '40W4-2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W4.2', 'Snop FLRY 2x0,75 mm²', 4.5, 'DIN 72 585, pod uglom. Papučicu i pin postavljati na vozilu.', '40_grupa_2_0.pdf', 10, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W4-2');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XB:20', 'B', 'pin 10XB/10XC (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XB:20');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1/M6', 'B', 'papučica 1/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BE', 0.75, NULL, 'DIN:2', '10XB:20', '' FROM Cable c
WHERE c.SpecFileName = '40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, 'DIN:1', 'PAP 1/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W4.3', 'Snop FLRY, DIN 72585 pod uglom – 10XC:05 i papučica M6', '40W4-3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W4.3', 'Snop FLRY 2x0,75 mm²', 5.1, 'DIN 72 585, pod uglom. Papučicu i pin postavljati na vozilu. NEPOTVRĐENO: zaglavlje crteža kaže „PIN 10XB“, a tabela 10XC:05; uzeta je vrednost iz tabele.', '40_grupa_2_0.pdf', 11, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W4-3');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:1', 'A', 'DIN 72585 pin', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'DIN:2', 'A', 'DIN 72585 pin', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = '40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'DIN:2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XC:05', 'B', 'pin 10XB/10XC (postavlja se na vozilu)', 'B01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XC:05');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1/M6', 'B', 'papučica 1/M6 (postavlja se na vozilu)', 'B02', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1/M6');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BE', 0.75, NULL, 'DIN:2', '10XC:05', '' FROM Cable c
WHERE c.SpecFileName = '40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, 'DIN:1', 'PAP 1/M6', '' FROM Cable c
WHERE c.SpecFileName = '40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

-- -----------------------------------------------------------------------------
-- =40-W5 — Snop FLRY 6x1 mm², strana 12
-- Pet pinova 10XC i papučica na strani A; devet buksni 1,5/6,3 na strani B.
-- PL žica se preko tri mosta (5 cm) nastavlja na još tri buksne — električno je to jedan
-- čvor sa pet tačaka. Priključna tabela: 10XC:30…34→C01…C05, papučica→C06,
-- buksne→D01…D09.
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, '40-W5', 'Snop FLRY 6x1, 10XC:30–34 i papučica – 9 buksni 1,5/6,3', '40W5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=40-W5', 'Snop FLRY 6x1 mm²', 2, 'Ceo bez širma. Papučicu i pin postavljati na vozilu. PL žica se nastavlja na 3 mosta sa još 3 buksne 1,5/6,3, mostovi su dužine 5 cm. NEPOTVRĐENO: zaglavlje crteža kaže „Buksne 6x“, a sa mostovima ih ukupno ima 9; presek mostova 1,5 mm² je pretpostavka prema buksni.', '40_grupa_2_0.pdf', 12, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = '40W5');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XC:30', 'A', 'pin 10XC (postavlja se na vozilu)', 'C01', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XC:30');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XC:31', 'A', 'pin 10XC (postavlja se na vozilu)', 'C02', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XC:31');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XC:32', 'A', 'pin 10XC (postavlja se na vozilu)', 'C03', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XC:32');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XC:33', 'A', 'pin 10XC (postavlja se na vozilu)', 'C04', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XC:33');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XC:34', 'A', 'pin 10XC (postavlja se na vozilu)', 'C05', 1, 'pin se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XC:34');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'PAP 1/M6', 'A', 'papučica 1/M6 (postavlja se na vozilu)', 'C06', 1, 'papučica se postavlja na vozilu; pri ispitivanju je to gola žica' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'PAP 1/M6');

INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-BR', 'B', 'buksna 1,5/6,3', 'D01', 1, 'kraj BR žice' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-BR');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-BE', 'B', 'buksna 1,5/6,3', 'D02', 1, 'kraj BE žice' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-BE');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-ZE', 'B', 'buksna 1,5/6,3', 'D03', 1, 'kraj ZE žice' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-ZE');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-ZU', 'B', 'buksna 1,5/6,3', 'D04', 1, 'kraj ZU žice' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-ZU');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-SV', 'B', 'buksna 1,5/6,3', 'D05', 1, 'kraj SV žice' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-SV');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-PL', 'B', 'buksna 1,5/6,3', 'D06', 1, 'kraj PL žice' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-PL');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-M1', 'B', 'buksna 1,5/6,3', 'D07', 1, 'prvi most na PL žici' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-M1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-M2', 'B', 'buksna 1,5/6,3', 'D08', 1, 'drugi most' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-M2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'BUK-M3', 'B', 'buksna 1,5/6,3', 'D09', 1, 'treći most' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'BUK-M3');

INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1, NULL, '10XC:30', 'BUK-BR', '' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BE', 1, NULL, '10XC:31', 'BUK-BE', '' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'ZE', 1, NULL, '10XC:32', 'BUK-ZE', '' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'ZU', 1, NULL, '10XC:33', 'BUK-ZU', '' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'SV', 1, NULL, '10XC:34', 'BUK-SV', '' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 6, 'PL', 1, NULL, 'PAP 1/M6', 'BUK-PL', '' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 6);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 7, 'most', 1.5, 0.05, 'BUK-PL', 'BUK-M1', 'most dužine 5 cm; presek 1,5 mm² je pretpostavka prema buksni' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 7);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 8, 'most', 1.5, 0.05, 'BUK-M1', 'BUK-M2', 'most dužine 5 cm; presek 1,5 mm² je pretpostavka prema buksni' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 8);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 9, 'most', 1.5, 0.05, 'BUK-M2', 'BUK-M3', 'most dužine 5 cm; presek 1,5 mm² je pretpostavka prema buksni' FROM Cable c
WHERE c.SpecFileName = '40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 9);
