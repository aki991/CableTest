-- Kablovi vozila „Miloš Veliki“ — grupe M26, M40, M52, M53, M77, M90, M96, verzija šeme 5.
--
-- IZVOR PODATAKA: crteži „Elektro dokumentacija BOV M16 Miloš Veliki“, radna verzija
-- „Prelazni“, po jedan PDF za svaku grupu. Uz svaki kabl stoji ime fajla (Cable.SourceDocument)
-- i strana (Cable.SourcePage), pa se svaki podatak može vratiti na izvor.
--
-- ŠTA SE OVDE MENJA
--
--   1. GRUPA 40 IZ MIGRACIJE 004 ODLAZI. Ti kablovi („40-W1.1“ … „40-W5“) prepisani su sa
--      starijeg crteža 40_grupa_2_0.pdf, na kome W1.x sa DIN 72585 idu na pin 10XF i papučicu.
--      Na novom crtežu isti kablovi idu u konektor XM7, a oznaka grupe je „M40“. To nije
--      preimenovanje nego drugi kabl, pa se stari zapisi ne ažuriraju — gase se i uvode novi.
--      Kabl nad kojim je nešto ispitano OSTAJE (samo IsActive = 0), jer je istorija dokaz
--      ispitivanja; kabl bez ijednog rezultata se briše.
--
--   2. UVODI SE 55 kablova iz 7 grupa: M26 (3), M40 (14), M52 (2), M53 (5), M77 (19), M90 (9), M96 (3).
--
-- PRAVILA PREPISA (ista za sve grupe)
--
--   • OZNAKA. Šifra kabla je „M<grupa>-W<n>“ prema grupi iz imena crteža. Na stranama W2
--     grupe 90 na crtežu piše „=90-W2A“, bez „M“; šifra je ipak „M90-W2A“, da sve iz tog
--     crteža stoji u istoj grupi, a doslovna oznaka sa crteža ostaje u koloni Designation.
--
--   • ŽICA je jedan red priključne tabele: red sa leve strane crteža (strana A) uz red sa
--     desne (strana B). Da to zaista važi, provereno je da se boja sa obe strane poklapa na
--     svakoj strani crteža — žica ima jednu boju, pa boje koje se ne slažu znače da redovi
--     nisu par. Jedini kabl gde se ne slažu je =M52-W1; tamo su krajevi spojeni po boji i to
--     piše u njegovoj napomeni.
--
--   • OZNAKE PRIKLJUČAKA moraju biti jedinstvene u okviru kabla, a crtež ih ponavlja
--     („1/m6“ pet puta, pinovi A..D na oba kraja). Ponovljena oznaka na istoj strani dobija
--     redni broj („1/m6 (2)“), a ista oznaka na drugoj strani crticu („A“ i „A'“).
--
--   • TAČKE TESTERA SU PRIVREMENE (IsProvisional = 1), kao i u migraciji 004: adapter još
--     nije napravljen. Strana A dobija A01…, strana B B01…, redom sa crteža. Ekran to i
--     kaže operateru — „Priključna tabela je privremena“.
--
-- OTVORENA PITANJA
--   TODO: =M52-W1 — desna tabela crteža ima boje drugim redom nego leva; dve žice su PL, a
--         oba kraja su iste buksne 1/6,3, pa je raspored te dve žice proizvoljan.
--   TODO: =M90-W2A…D — provodnici su koaksijalni („koaksjalac“, „širm koaks.“) i crtež im ne
--         daje presek; upisana je nula, što znači „nije poznato“.
--   TODO: konačan raspored tačaka na adapteru, kad adapter bude napravljen.


-- =============================================================================
-- 1. Grupa 40 sa starog crteža odlazi
-- =============================================================================

DELETE FROM Cable
 WHERE SpecFileName LIKE '40W%'
   AND NOT EXISTS (SELECT 1 FROM TestRun r WHERE r.CableId = Cable.Id);

UPDATE Cable
   SET IsActive = 0
 WHERE SpecFileName LIKE '40W%';

-- =============================================================================
-- 2. Kablovi sa novih crteža
-- =============================================================================


-- -----------------------------------------------------------------------------
-- Grupa M26 — Gr.26 Miloš Veliki  Prelazni.pdf
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M26-W1', 'Snop FLRY 6x1mm2', 'M26W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M26-W1', 'Snop FLRY 6x1mm2', 7.2, 'krimpovati', 'Gr.26 Miloš Veliki  Prelazni.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M26W1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '9', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '9');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '5', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '5');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '6', 'A', '', 'A06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'E', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'E');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'B', '', 'B06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'RZ', 1, NULL, '1', 'F', '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'SV', 1, NULL, '2', 'E', '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'ZU', 1, NULL, '9', 'D', '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'ZE', 1, NULL, '10', 'C', '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'BE', 1, NULL, '5', 'B', '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 6, 'BR', 1, NULL, '6', 'A', '' FROM Cable c
WHERE c.SpecFileName = 'M26W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 6);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M26-W2', 'Konektor BDK', 'M26W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M26-W2', 'Konektor BDK', 1.2, 'krimpovati', 'Gr.26 Miloš Veliki  Prelazni.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M26W2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '14', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '14');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '17', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '17');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'LJ', 0, NULL, '14', '2', '' FROM Cable c
WHERE c.SpecFileName = 'M26W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CV', 0, NULL, '17', '1', '' FROM Cable c
WHERE c.SpecFileName = 'M26W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M26-W3', 'FLRY 2x1mm2', 'M26W3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M26-W3', 'FLRY 2x1mm2', 0.8, '', 'Gr.26 Miloš Veliki  Prelazni.pdf', 3, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M26W3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '11', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C40', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C40');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M26W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'LJ', 1, NULL, '11', '2', '' FROM Cable c
WHERE c.SpecFileName = 'M26W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CV', 1, NULL, 'C40', '1', '' FROM Cable c
WHERE c.SpecFileName = 'M26W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

-- -----------------------------------------------------------------------------
-- Grupa M40 — Gr.40 Miloš Veliki  Prelazni.pdf
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W1', 'Snop FLRY 12 x 1,5mm2', 'M40W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W1', 'Snop FLRY 12 x 1,5mm2', 1.2, 'papučice, pinovi i', 'Gr.40 Miloš Veliki  Prelazni.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'E', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'E');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F', 'A', '', 'A06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'G', 'A', '', 'A07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'G');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'H', 'A', '', 'A08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'H');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'J', 'A', '', 'A09', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'J');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'K', 'A', '', 'A10', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'K');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'L', 'A', '', 'A11', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'L');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'M', 'A', '', 'A12', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'M');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'N', 'A', '', 'A13', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'N');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'P', 'A', '', 'A14', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'P');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'R', 'A', '', 'A15', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'R');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'S', 'A', '', 'A16', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'S');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F02', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F02');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F08', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F08');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (malo)', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F11', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (4)', 'B', '', 'B06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (4)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F14', 'B', '', 'B07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F14');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (6)', 'B', '', 'B08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (6)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F15', 'B', '', 'B09', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F15');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (8)', 'B', '', 'B10', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (8)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F07', 'B', '', 'B11', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F07');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (10)', 'B', '', 'B12', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (10)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3', 'B', '', 'B13', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (2)', 'B', '', 'B14', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (2)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (3)', 'B', '', 'B15', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (3)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (12)', 'B', '', 'B16', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (12)');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BE', 1.5, NULL, 'A', 'F02', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, 'B', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'BR', 1.5, NULL, 'C', 'F08', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'PL', 1.5, NULL, 'D', '1/m6 (malo)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'ZE', 1.5, NULL, 'E', 'F11', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 6, 'PL', 1.5, NULL, 'F', '1/m6 (4)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 6);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 7, 'SV', 1.5, NULL, 'G', 'F14', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 7);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 8, 'PL', 1.5, NULL, 'H', '1/m6 (6)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 8);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 9, 'ŽU', 1.5, NULL, 'J', 'F15', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 9);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 10, 'PL', 1.5, NULL, 'K', '1/m6 (8)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 10);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 11, 'CV', 1.5, NULL, 'L', 'F07', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 11);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 12, 'PL', 1.5, NULL, 'M', '1/m6 (10)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 12);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 13, 'BR', 1, NULL, 'N', '1/6,3', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 13);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 14, 'ZE', 1, NULL, 'P', '1/6,3 (2)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 14);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 15, 'CV', 1, NULL, 'R', '1/6,3 (3)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 15);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 16, 'PL', 1, NULL, 'S', '1/m6 (12)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 16);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W1.1', 'Wabco 2x1,5mm2', 'M40W1-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W1.1', 'Wabco 2x1,5mm2', 3.5, 'fabrički kabl', 'Gr.40 Miloš Veliki  Prelazni.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W1-1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, '1', 'A', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, '2', 'B', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W1.2', 'Wabco 2x1,5mm2', 'M40W1-2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W1.2', 'Wabco 2x1,5mm2', 3.5, 'fabrički kabl', 'Gr.40 Miloš Veliki  Prelazni.pdf', 3, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W1-2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, '1', 'C', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, '2', 'D', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W1.3', 'Wabco 2x1,5mm2', 'M40W1-3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W1.3', 'Wabco 2x1,5mm2', 3.5, 'fabrički kabl', 'Gr.40 Miloš Veliki  Prelazni.pdf', 4, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W1-3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'E', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'E');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, '1', 'E', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, '2', 'F', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W1.4', 'Wabco 2x1,5mm2', 'M40W1-4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W1.4', 'Wabco 2x1,5mm2', 3.6, 'fabrički kabl', 'Gr.40 Miloš Veliki  Prelazni.pdf', 5, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W1-4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'G', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'G');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'H', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'H');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, '1', 'G', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, '2', 'H', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W1.5', 'Wabco 2x1,5mm2', 'M40W1-5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W1.5', 'Wabco 2x1,5mm2', 3.6, 'fabrički kabl', 'Gr.40 Miloš Veliki  Prelazni.pdf', 6, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W1-5');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'J', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'J');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'K', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'K');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, '1', 'J', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, '2', 'K', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W1.6', 'Wabco 2x1,5mm2', 'M40W1-6', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W1.6', 'Wabco 2x1,5mm2', 3.6, 'fabrički kabl', 'Gr.40 Miloš Veliki  Prelazni.pdf', 7, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W1-6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'L', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'L');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'M', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'M');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, '1', 'L', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, '2', 'M', '' FROM Cable c
WHERE c.SpecFileName = 'M40W1-6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W2', 'Snop FLRY 2x0,75mm2', 'M40W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W2', 'Snop FLRY 2x0,75mm2', 5.1, '', 'Gr.40 Miloš Veliki  Prelazni.pdf', 8, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'P', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'P');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'R', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'R');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', 'P', '' FROM Cable c
WHERE c.SpecFileName = 'M40W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', 'R', '' FROM Cable c
WHERE c.SpecFileName = 'M40W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W3', 'Snop FLRY 2x0,75mm2', 'M40W3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W3', 'Snop FLRY 2x0,75mm2', 3.4, '', 'Gr.40 Miloš Veliki  Prelazni.pdf', 9, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'S', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'S');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'T', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'T');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', 'S', '' FROM Cable c
WHERE c.SpecFileName = 'M40W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', 'T', '' FROM Cable c
WHERE c.SpecFileName = 'M40W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W4.1', 'Snop FLRY 2x0,75mm2', 'M40W4-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W4.1', 'Snop FLRY 2x0,75mm2', 3.8, '', 'Gr.40 Miloš Veliki  Prelazni.pdf', 10, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W4-1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'U', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'U');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'V', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'V');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BE', 0.75, NULL, '2', 'U', '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', 'V', '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W4.2', 'Snop FLRY 2x0,75mm2', 'M40W4-2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W4.2', 'Snop FLRY 2x0,75mm2', 3.8, '', 'Gr.40 Miloš Veliki  Prelazni.pdf', 11, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W4-2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'W', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'W');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'j', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'j');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BE', 0.75, NULL, '2', 'W', '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', 'j', '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W4.3', 'Snop FLRY 2x0,75mm2', 'M40W4-3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W4.3', 'Snop FLRY 2x0,75mm2', 4.2, '', 'Gr.40 Miloš Veliki  Prelazni.pdf', 12, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W4-3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'i', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'i');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'h', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'h');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BE', 0.75, NULL, '2', 'i', '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', 'h', '' FROM Cable c
WHERE c.SpecFileName = 'M40W4-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W5', 'Snop FLRY 9 x 0,75mm2', 'M40W5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W5', 'Snop FLRY 9 x 0,75mm2', 0.8, '', 'Gr.40 Miloš Veliki  Prelazni.pdf', 13, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W5');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '3', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '4', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '5', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '5');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '6', 'A', '', 'A06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '7', 'A', '', 'A07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '7');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '8', 'A', '', 'A08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '8');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '9', 'A', '', 'A09', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '9');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10', 'A', '', 'A10', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '11', 'A', '', 'A11', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '12', 'A', '', 'A12', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '12');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B09', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B09');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B10', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B10');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B11', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B13', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B13');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C17', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C17');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C18', 'B', '', 'B06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C18');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C19', 'B', '', 'B07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C19');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C30', 'B', '', 'B08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C30');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C31', 'B', '', 'B09', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C31');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C32', 'B', '', 'B10', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C32');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C33', 'B', '', 'B11', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C33');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C34', 'B', '', 'B12', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C34');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '1', 'B09', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'SV', 0.75, NULL, '2', 'B10', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'ZE', 0.75, NULL, '3', 'B11', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'ŽU', 0.75, NULL, '4', 'B13', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'LJ', 0.75, NULL, '5', 'C17', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 6, 'CV', 0.75, NULL, '6', 'C18', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 6);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 7, 'BE', 0.75, NULL, '7', 'C19', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 7);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 8, 'RZ', 0.75, NULL, '8', 'C30', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 8);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 9, 'CN', 0.75, NULL, '9', 'C31', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 9);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 10, 'BR', 0.5, NULL, '10', 'C32', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 10);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 11, 'ŽU', 0.5, NULL, '11', 'C33', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 11);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 12, 'SV', 0.5, NULL, '12', 'C34', '' FROM Cable c
WHERE c.SpecFileName = 'M40W5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 12);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M40-W6', 'Snop FLRY 28x0,75mm2', 'M40W6', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M40-W6', 'Snop FLRY 28x0,75mm2', 1.2, 'papučice, pinovi i', 'Gr.40 Miloš Veliki  Prelazni.pdf', 14, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M40W6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'E', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'E');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F', 'A', '', 'A06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'G', 'A', '', 'A07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'G');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'H', 'A', '', 'A08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'H');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'J', 'A', '', 'A09', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'J');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'K', 'A', '', 'A10', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'K');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'L', 'A', '', 'A11', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'L');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'P', 'A', '', 'A12', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'P');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'R', 'A', '', 'A13', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'R');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'S', 'A', '', 'A14', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'S');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'T', 'A', '', 'A15', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'T');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'U', 'A', '', 'A16', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'U');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'V', 'A', '', 'A17', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'V');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'W', 'A', '', 'A18', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'W');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'X', 'A', '', 'A19', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'X');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y', 'A', '', 'A20', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Z', 'A', '', 'A21', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Z');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'a (malo)', 'A', '', 'A22', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'a (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'b (malo)', 'A', '', 'A23', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'b (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'c (malo)', 'A', '', 'A24', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'c (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'd (malo)', 'A', '', 'A25', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'd (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'f (malo)', 'A', '', 'A26', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'f (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'g (malo)', 'A', '', 'A27', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'g (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'h (malo)', 'A', '', 'A28', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'h (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'i', 'A', '', 'A29', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'i');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'j (malo)', 'A', '', 'A30', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'j (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'r (malo)', 'A', '', 'A31', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'r (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D11', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D12', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D12');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D09', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D09');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D33', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D33');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (2)', 'B', '', 'B06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (2)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D13', 'B', '', 'B08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D13');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (malo)', 'B', '', 'B09', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D37', 'B', '', 'B10', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D37');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (4)', 'B', '', 'B11', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (4)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B16', 'B', '', 'B12', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B16');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (6)', 'B', '', 'B13', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (6)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B15', 'B', '', 'B14', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B15');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (8)', 'B', '', 'B15', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (8)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B17', 'B', '', 'B16', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B17');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (10)', 'B', '', 'B17', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (10)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B20', 'B', '', 'B18', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B20');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C36', 'B', '', 'B19', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C36');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (12)', 'B', '', 'B20', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (12)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (14)', 'B', '', 'B21', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (14)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (16)', 'B', '', 'B22', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (16)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D15', 'B', '', 'B23', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D15');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (3)', 'B', '', 'B24', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (3)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D32', 'B', '', 'B25', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D32');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D34', 'B', '', 'B26', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D34');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C50', 'B', '', 'B27', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C50');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (18)', 'B', '', 'B28', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (18)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C05', 'B', '', 'B29', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C05');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (20)', 'B', '', 'B30', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (20)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (22)', 'B', '', 'B31', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (22)');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, 'A', 'D11', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.75, NULL, 'B', 'D12', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'BR', 0.75, NULL, 'C', 'D09', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'BR', 0.75, NULL, 'D', 'D33', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'ZE', 0.75, NULL, 'E', '1/6,3', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 6, 'LJ', 0.75, NULL, 'F', '1/6,3 (2)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 6);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 7, 'PL', 0.75, NULL, 'G', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 7);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 8, 'BE', 0.75, NULL, 'H', 'D13', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 8);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 9, 'PL', 0.75, NULL, 'J', '1/m6 (malo)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 9);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 10, 'SV', 0.5, NULL, 'K', 'D37', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 10);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 11, 'PL', 0.75, NULL, 'L', '1/m6 (4)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 11);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 12, 'CN', 0.75, NULL, 'P', 'B16', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 12);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 13, 'PL', 0.75, NULL, 'R', '1/m6 (6)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 13);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 14, 'CV', 0.75, NULL, 'S', 'B15', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 14);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 15, 'PL', 0.75, NULL, 'T', '1/m6 (8)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 15);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 16, 'SV', 0.75, NULL, 'U', 'B17', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 16);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 17, 'PL', 0.75, NULL, 'V', '1/m6 (10)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 17);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 18, 'RZ', 0.75, NULL, 'W', 'B20', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 18);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 19, 'BR', 0.75, NULL, 'X', 'C36', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 19);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 20, 'PL', 0.75, NULL, 'Y', '1/m6 (12)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 20);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 21, 'PL', 0.75, NULL, 'Z', '1/m6 (14)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 21);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 22, 'PL', 0.75, NULL, 'a (malo)', '1/m6 (16)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 22);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 23, 'ZE', 0.5, NULL, 'b (malo)', 'D15', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 23);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 24, 'CV', 0.5, NULL, 'c (malo)', '1/6,3 (3)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 24);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 25, 'BE', 0.75, NULL, 'd (malo)', 'D32', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 25);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 26, 'BE', 0.75, NULL, 'f (malo)', 'D34', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 26);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 27, 'BE', 0.75, NULL, 'g (malo)', 'C50', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 27);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 28, 'PL', 0.75, NULL, 'h (malo)', '1/m6 (18)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 28);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 29, 'ŽU', 0.75, NULL, 'i', 'C05', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 29);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 30, 'PL', 0.75, NULL, 'j (malo)', '1/m6 (20)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 30);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 31, 'PL', 0.75, NULL, 'r (malo)', '1/m6 (22)', '' FROM Cable c
WHERE c.SpecFileName = 'M40W6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 31);

-- -----------------------------------------------------------------------------
-- Grupa M52 — Gr.52 Miloš Veliki  Prelazni.pdf
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M52-W1', 'Snop FLRY 5x0,75mm2', 'M52W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M52-W1', 'Snop FLRY 5x0,75mm2', 2.6, 'NEPOTVRĐENO: na crtežu su boje desne tabele date drugim redom nego leve, pa su krajevi spojeni po boji, a ne po redu. Kod dve žice iste boje raspored je proizvoljan — treba ga potvrditi na crtežu.', 'Gr.52 Miloš Veliki  Prelazni.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M52W1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'J', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'J');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'K', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'K');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'L', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'L');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'M', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'M');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'N', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'N');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (2)', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (2)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (3)', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (3)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (4)', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (4)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3 (5)', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3 (5)');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'ŽU', 0.75, NULL, 'J', '1/6,3', '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.75, NULL, 'K', '1/6,3 (2)', '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'BE', 0.75, NULL, 'L', '1/6,3 (3)', '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'PL', 0.75, NULL, 'M', '1/6,3 (4)', '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'PL', 0.75, NULL, 'N', '1/6,3 (5)', '' FROM Cable c
WHERE c.SpecFileName = 'M52W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M52-W2', 'Snop FLRY', 'M52W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M52-W2', 'Snop FLRY', 3.8, 'papučicu i pin', 'Gr.52 Miloš Veliki  Prelazni.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M52W2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '20XA:18', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M52W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '20XA:18');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 0, NULL, '1', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M52W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0, NULL, '2', '20XA:18', '' FROM Cable c
WHERE c.SpecFileName = 'M52W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

-- -----------------------------------------------------------------------------
-- Grupa M53 — Gr.53 Miloš Veliki  Prelazni.pdf
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M53-W1', 'Snop FLRY 8x1mm2', 'M53W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M53-W1', 'Snop FLRY 8x1mm2', 4.3, '', 'Gr.53 Miloš Veliki  Prelazni.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M53W1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'K', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'K');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'L', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'L');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'M', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'M');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'N', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'N');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'P', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'P');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'R', 'A', '', 'A06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'R');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'S', 'A', '', 'A07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'S');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'T', 'A', '', 'A08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'T');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'G', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'G');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'E', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'E');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'H', 'B', '', 'B06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'H');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'B', '', 'B07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'B', '', 'B08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 1, NULL, 'K', 'C', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CN', 1, NULL, 'L', 'G', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'CN', 1, NULL, 'M', 'F', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'ZE', 1, NULL, 'N', 'E', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'ŽU', 1, NULL, 'P', 'D', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 6, 'BE', 1, NULL, 'R', 'H', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 6);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 7, 'BE', 1, NULL, 'S', 'A', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 7);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 8, 'PL', 1, NULL, 'T', 'B', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 8);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M53-W1.1', 'Snop FLRY 8x1mm2', 'M53W1-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M53-W1.1', 'Snop FLRY 8x1mm2', 0.6, '', 'Gr.53 Miloš Veliki  Prelazni.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M53W1-1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '3', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '4', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '5', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '5');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '6', 'A', '', 'A06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '8', 'A', '', 'A07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '8');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '9', 'A', '', 'A08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '9');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'K', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'K');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'L', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'L');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'M', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'M');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'N', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'N');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'P', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'P');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'R', 'B', '', 'B06', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'R');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'S', 'B', '', 'B07', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'S');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'T', 'B', '', 'B08', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'T');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 1, NULL, '1', 'K', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CN', 2, NULL, '2', 'L', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'CN', 3, NULL, '3', 'M', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'ZE', 1, NULL, '4', 'N', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'ŽU', 1, NULL, '5', 'P', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 6, 'BE', 6, NULL, '6', 'R', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 6);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 7, 'BE', 8, NULL, '8', 'S', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 7);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 8, 'PL', 1, NULL, '9', 'T', '' FROM Cable c
WHERE c.SpecFileName = 'M53W1-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 8);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M53-W2', 'Snop FLRY 2x1,5mm2', 'M53W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M53-W2', 'Snop FLRY 2x1,5mm2', 1.7, 'papučice krimpovati', 'Gr.53 Miloš Veliki  Prelazni.pdf', 3, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M53W2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/m4', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/m4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 1.5, NULL, '1', '1,5/m4', '' FROM Cable c
WHERE c.SpecFileName = 'M53W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1.5, NULL, '2', '1,5/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M53W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M53-W3', 'Snop FLRY 1x1,5mm2, 2x0,5mm2', 'M53W3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M53-W3', 'Snop FLRY 1x1,5mm2, 2x0,5mm2', 0.85, '', 'Gr.53 Miloš Veliki  Prelazni.pdf', 4, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M53W3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/6,3', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XC:C11', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XC:C11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '10XA:A06', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '10XA:A06');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'A', '1/6,3', '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.5, NULL, 'B', '10XC:C11', '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'CN', 0.5, NULL, 'C', '10XA:A06', '' FROM Cable c
WHERE c.SpecFileName = 'M53W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M53-W3.1', 'Snop FLRY 1x0,5mm2', 'M53W3-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M53-W3.1', 'Snop FLRY 1x0,5mm2', 0.75, 'krimpovati zajedno krimpovati', 'Gr.53 Miloš Veliki  Prelazni.pdf', 5, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M53W3-1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '5', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M53W3-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '5');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CN', 0.5, NULL, 'C', '5', '' FROM Cable c
WHERE c.SpecFileName = 'M53W3-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);

-- -----------------------------------------------------------------------------
-- Grupa M77 — Gr.77 Miloš Veliki  Prelazni.pdf
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W2', 'Snop FLRY 5x0,75mm2', 'M77W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W2', 'Snop FLRY 5x0,75mm2', 3, 'krimpovati papučicu pinove', 'Gr.77 Miloš Veliki  Prelazni.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'G', 'A', '', 'A05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'G');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XB:21', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XB:21');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XB:05', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XB:05');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XB:08', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XB:08');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XB:11', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XB:11');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B05', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 0.75, NULL, 'A', '40XB:21', '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CN', 0.75, NULL, 'B', '40XB:05', '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'BR', 0.75, NULL, 'C', '40XB:08', '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'ZE', 0.75, NULL, 'D', '40XB:11', '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 5, 'PL', 0.75, NULL, 'G', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 5);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W2.1', 'Snop FLRY', 'M77W2-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W2.1', 'Snop FLRY', 3, 'most u kapi krimpovati', 'Gr.77 Miloš Veliki  Prelazni.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W2-1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'F', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'F');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'E', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'E');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y2:x2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y2:x2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y2:x1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W2-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y2:x1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 0, NULL, 'F', 'Y2:x2', '' FROM Cable c
WHERE c.SpecFileName = 'M77W2-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0, NULL, 'E', 'Y2:x1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W2-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W3', 'Snop FLRY 3x0,75mm2', 'M77W3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W3', 'Snop FLRY 3x0,75mm2', 1.2, 'pinove postavljati', 'Gr.77 Miloš Veliki  Prelazni.pdf', 3, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '3', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C03', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C03');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C05', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C05');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C02', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C02');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 0.75, NULL, '2', 'C03', '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.75, NULL, '1', 'C05', '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'BE', 0.75, NULL, '3', 'C02', '' FROM Cable c
WHERE c.SpecFileName = 'M77W3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W4', 'Snop FLRY 4x1mm2', 'M77W4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W4', 'Snop FLRY 4x1mm2', 1.2, 'PL, BR kabl W4.1I papučice i pinove', 'Gr.77 Miloš Veliki  Prelazni.pdf', 4, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XA:10', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XA:10');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XA:16', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XA:16');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (malo)', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (malo)');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 1, NULL, '1/m6', 'C', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 1, NULL, '40XA:10', 'A', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'BE', 1, NULL, '40XA:16', 'B', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'PL', 1, NULL, '1/m6 (malo)', 'D', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W4.1I', 'Snop FLRY 2x1mm2', 'M77W4-1I', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W4.1I', 'Snop FLRY 2x1mm2', 0.7, 'Krimpovati zajedno sa', 'Gr.77 Miloš Veliki  Prelazni.pdf', 5, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W4-1I');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1I'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1I'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y4:x2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1I'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y4:x2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y4:x1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1I'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y4:x1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 1, NULL, 'C', 'Y4:x2', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1I'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 1, NULL, 'A', 'Y4:x1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1I'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W4.1U', 'Snop FLRY 2x1mm2', 'M77W4-1U', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W4.1U', 'Snop FLRY 2x1mm2', 0.7, 'Krimpovati zajedno sa', 'Gr.77 Miloš Veliki  Prelazni.pdf', 6, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W4-1U');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1U'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1U'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y5:x2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1U'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y5:x2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y5:x1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1U'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y5:x1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 1, NULL, 'D', 'Y5:x2', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1U'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BE', 1, NULL, 'B', 'Y5:x1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W4-1U'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W5.1', 'Snop FLRY 2x0,75mm2', 'M77W5-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W5.1', 'Snop FLRY 2x0,75mm2', 1, 'papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 7, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W5-1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XB:17', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XB:17');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', '40XB:17', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W5.2', 'Snop FLRY 2x0,75mm2', 'M77W5-2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W5.2', 'Snop FLRY 2x0,75mm2', 1, 'papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 8, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W5-2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XB:14', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XB:14');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', '40XB:14', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W5.3', 'Snop FLRY 2x0,75mm2', 'M77W5-3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W5.3', 'Snop FLRY 2x0,75mm2', 2.6, 'papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 9, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W5-3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '4', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '3', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '30XB:05', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '30XB:05');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '4', '30XB:05', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '3', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W5.4', 'Snop FLRY 2x0,75mm2', 'M77W5-4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W5.4', 'Snop FLRY 2x0,75mm2', 1, 'papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 10, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W5-4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '30XB:17', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '30XB:17');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', '30XB:17', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W5.5', 'Snop FLRY 2x0,75mm2', 'M77W5-5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W5.5', 'Snop FLRY 2x0,75mm2', 1, 'papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 11, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W5-5');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '30XB:08', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '30XB:08');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', '30XB:08', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W5.6', 'Snop FLRY 2x0,75mm2', 'M77W5-6', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W5.6', 'Snop FLRY 2x0,75mm2', 2.9, '', 'Gr.77 Miloš Veliki  Prelazni.pdf', 12, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W5-6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '30XA:21', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '30XA:21');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-6'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', '30XA:21', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-6'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W5.7', 'Snop FLRY 2x0,75mm2', 'M77W5-7', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W5.7', 'Snop FLRY 2x0,75mm2', 5, '', 'Gr.77 Miloš Veliki  Prelazni.pdf', 13, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W5-7');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-7'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-7'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '30XA:18', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-7'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '30XA:18');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-7'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, '2', '30XA:18', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-7'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 0.75, NULL, '1', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W5-7'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W6.2', 'Snop FLRY 2x0,75mm2', 'M77W6-2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W6.2', 'Snop FLRY 2x0,75mm2', 1.4, 'most u kapi papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 14, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W6-2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XA:19', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XA:19');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y2:x2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y2:x2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y2:x1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y2:x1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 0.75, NULL, '1/m6', 'Y2:x2', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.75, NULL, '40XA:19', 'Y2:x1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W6.3', 'Snop FLRY 2x0,75mm2', 'M77W6-3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W6.3', 'Snop FLRY 2x0,75mm2', 1.4, 'most u kapi papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 15, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W6-3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XA:20', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XA:20');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y3:x2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y3:x2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y3:x1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-3'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y3:x1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 0.75, NULL, '1/m6', 'Y3:x2', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.75, NULL, '40XA:20', 'Y3:x1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-3'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W6.4', 'Snop FLRY 2x0,75mm2', 'M77W6-4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W6.4', 'Snop FLRY 2x0,75mm2', 1.4, 'most u kapi papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 16, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W6-4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XC:18', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XC:18');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y4:x2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y4:x2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y4:x1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y4:x1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 0.75, NULL, '1/m6', 'Y4:x2', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.75, NULL, '40XC:18', 'Y4:x1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W6.5', 'Snop FLRY 2x0,75mm2', 'M77W6-5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W6.5', 'Snop FLRY 2x0,75mm2', 1.4, 'most u kapi papučicu i pin', 'Gr.77 Miloš Veliki  Prelazni.pdf', 17, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W6-5');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '40XC:21', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '40XC:21');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y5:x2', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y5:x2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'Y5:x1', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-5'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'Y5:x1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 0.75, NULL, '1/m6', 'Y5:x2', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 0.75, NULL, '40XC:21', 'Y5:x1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W6-5'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W7', 'Snop FLRY 1x0,75mm2', 'M77W7', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W7', 'Snop FLRY 1x0,75mm2', 7, '', 'Gr.77 Miloš Veliki  Prelazni.pdf', 18, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W7');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'f', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W7'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'f');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W7'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 0.75, NULL, 'f', '1', '' FROM Cable c
WHERE c.SpecFileName = 'M77W7'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M77-W7.1', 'Snop FLRY 2x0,75mm2', 'M77W7-1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M77-W7.1', 'Snop FLRY 2x0,75mm2', 1.3, '', 'Gr.77 Miloš Veliki  Prelazni.pdf', 19, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M77W7-1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'r', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W7-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'r');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'g', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W7-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'g');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W7-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6 (malo)', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M77W7-1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6 (malo)');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'PL', 0.75, NULL, 'r', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M77W7-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CV', 0.75, NULL, 'g', '1/m6 (malo)', '' FROM Cable c
WHERE c.SpecFileName = 'M77W7-1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

-- -----------------------------------------------------------------------------
-- Grupa M90 — Gr.90 Miloš Veliki  Prelazni.pdf
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W4', 'Orlaco 2x1mm2', 'M90W4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M90-W4', 'Orlaco 2x1mm2', 3, 'papučice postavljati', 'Gr.90 Miloš Veliki  Prelazni.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m4', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m4');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1/m6', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W4'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1/m6');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1, NULL, 'A', '1/m4', '' FROM Cable c
WHERE c.SpecFileName = 'M90W4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'PL', 1, NULL, 'C', '1/m6', '' FROM Cable c
WHERE c.SpecFileName = 'M90W4'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W1A', 'Snop FLRY 2x0,5mm2', 'M90W1A', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M90-W1A', 'Snop FLRY 2x0,5mm2', 1.5, 'papučicu i buksnu', 'Gr.90 Miloš Veliki  Prelazni.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W1A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/6,3', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/m6', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 0.5, NULL, '1,5/6,3', '1', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1A'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CN', 0.5, NULL, '1,5/m6', '2', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1A'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W1B', 'Snop FLRY 2x0,5mm2', 'M90W1B', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M90-W1B', 'Snop FLRY 2x0,5mm2', 3.3, 'papučicu i buksnu', 'Gr.90 Miloš Veliki  Prelazni.pdf', 3, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W1B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/6,3', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/m6', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 0.5, NULL, '1,5/6,3', '1', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1B'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CN', 0.5, NULL, '1,5/m6', '2', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1B'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W1C', 'Snop FLRY 2x0,5mm2', 'M90W1C', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M90-W1C', 'Snop FLRY 2x0,5mm2', 5.1, 'papučicu i buksnu', 'Gr.90 Miloš Veliki  Prelazni.pdf', 4, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W1C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/6,3', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/m6', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 0.5, NULL, '1,5/6,3', '1', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1C'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CN', 0.5, NULL, '1,5/m6', '2', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1C'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W1D', 'Snop FLRY 2x0,5mm2', 'M90W1D', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M90-W1D', 'Snop FLRY 2x0,5mm2', 7.1, 'papučicu i buksnu', 'Gr.90 Miloš Veliki  Prelazni.pdf', 5, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W1D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/6,3', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/6,3');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1,5/m6', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1,5/m6');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W1D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 0.5, NULL, '1,5/6,3', '1', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1D'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'CN', 0.5, NULL, '1,5/m6', '2', '' FROM Cable c
WHERE c.SpecFileName = 'M90W1D'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W2A', 'RG174 + 2x0,5', 'M90W2A', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=90-W2A', 'RG174 + 2x0,5', 3, '', 'Gr.90 Miloš Veliki  Prelazni.pdf', 6, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W2A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A''', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B''', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C''', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D''', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D''');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'koaksjalac', 0, NULL, 'A', 'A''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'širm koaks.', 0, NULL, 'B', 'B''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'CV', 0, NULL, 'C', 'C''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'CN', 0, NULL, 'D', 'D''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2A'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W2B', 'RG174 + 2x0,5', 'M90W2B', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=90-W2B', 'RG174 + 2x0,5', 5.1, '', 'Gr.90 Miloš Veliki  Prelazni.pdf', 7, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W2B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A''', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B''', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C''', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D''', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D''');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'koaksjalac', 0, NULL, 'A', 'A''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'širm koaks.', 0, NULL, 'B', 'B''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'CV', 0, NULL, 'C', 'C''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'CN', 0, NULL, 'D', 'D''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2B'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W2C', 'RG174 + 2x0,5', 'M90W2C', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=90-W2C', 'RG174 + 2x0,5', 4.9, '', 'Gr.90 Miloš Veliki  Prelazni.pdf', 8, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W2C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A''', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B''', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C''', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D''', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D''');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'koaksjalac', 0, NULL, 'A', 'A''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'širm koaks.', 0, NULL, 'B', 'B''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'CV', 0, NULL, 'C', 'C''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'CN', 0, NULL, 'D', 'D''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2C'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M90-W2D', 'RG174 + 2x0,5', 'M90W2D', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=90-W2D', 'RG174 + 2x0,5', 6.9, '', 'Gr.90 Miloš Veliki  Prelazni.pdf', 9, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M90W2D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'A', '', 'A03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D', 'A', '', 'A04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A''', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B''', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C''', 'B', '', 'B03', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C''');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'D''', 'B', '', 'B04', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'D''');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'koaksjalac', 0, NULL, 'A', 'A''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'širm koaks.', 0, NULL, 'B', 'B''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 3, 'CV', 0, NULL, 'C', 'C''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 3);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 4, 'CN', 0, NULL, 'D', 'D''', '' FROM Cable c
WHERE c.SpecFileName = 'M90W2D'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 4);

-- -----------------------------------------------------------------------------
-- Grupa M96 — Gr.96 Miloš Veliki  Prelazni.pdf
-- -----------------------------------------------------------------------------

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M96-W1', 'Snop FLRY 1x50mm2', 'M96W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M96-W1', 'Snop FLRY 1x50mm2', 4.1, '', 'Gr.96 Miloš Veliki  Prelazni.pdf', 1, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M96W1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '50/m12', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '50/m12');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '50/m12''', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W1'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '50/m12''');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 50, NULL, '50/m12', '50/m12''', '' FROM Cable c
WHERE c.SpecFileName = 'M96W1'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M96-W2', 'Snop FLRY 2x1,5mm2', 'M96W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M96-W2', 'Snop FLRY 2x1,5mm2', 0.9, '', 'Gr.96 Miloš Veliki  Prelazni.pdf', 2, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M96W2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '2', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, '1', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = '1');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'u', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'u');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'q', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'q');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'CV', 1.5, NULL, '2', 'u', '' FROM Cable c
WHERE c.SpecFileName = 'M96W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BR', 1.5, NULL, '1', 'q', '' FROM Cable c
WHERE c.SpecFileName = 'M96W2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt,
                   Designation, CableType, LengthM, Notes, SourceDocument, SourcePage, IsActive)
SELECT v.Id, 'M96-W2.2', 'Snop FLRY 1x1,5mm2', 'M96W2-2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z',
       '=M96-W2.2', 'Snop FLRY 1x1,5mm2', 0.5, 'krimpovati zajedno', 'Gr.96 Miloš Veliki  Prelazni.pdf', 3, 1
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M96W2-2');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'A', 'A', '', 'A01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'A');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'B', 'A', '', 'A02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'B');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'q', 'B', '', 'B01', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'q');
INSERT INTO CableTerminal (CableId, Label, Side, ContactType, TesterPoint, IsProvisional, Notes)
SELECT c.Id, 'C', 'B', '', 'B02', 1, '' FROM Cable c
WHERE c.SpecFileName = 'M96W2-2'
  AND NOT EXISTS (SELECT 1 FROM CableTerminal t WHERE t.CableId = c.Id AND t.Label = 'C');
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 1, 'BR', 1.5, NULL, 'A', 'q', '' FROM Cable c
WHERE c.SpecFileName = 'M96W2-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 1);
INSERT INTO CableWire (CableId, WireNo, Color, CrossSectionMm2, LengthM, FromTerminal, ToTerminal, Notes)
SELECT c.Id, 2, 'BE', 0.5, NULL, 'B', 'C', '' FROM Cable c
WHERE c.SpecFileName = 'M96W2-2'
  AND NOT EXISTS (SELECT 1 FROM CableWire w WHERE w.CableId = c.Id AND w.WireNo = 2);
