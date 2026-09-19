-- Priključna tabela kabla =M40-W1.1 — tačke umesto pinova.
--
-- ZAŠTO
--
--   Tester ima 16 konektora (A..P) sa po 32 ispitne tačke. Svaki konektor ima 64 pina,
--   označena A01..A32 i B01..B32, i oni rade isključivo u paru: A01 i B01 zajedno čine
--   tačku 01 TOG konektora. Kraj žice se zato ne stavlja na pin, nego na tačku — a operater
--   ga na adapteru ukrcava u oba pina para.
--
--   Migracija 005 je oba kraja svakog kabla smestila na konektore A i B: strana A na A01,
--   A02…, strana B na B01, B02… Net lista je onda izgledala „A01-B01", što je isti niz
--   znakova kao par pinova jedne tačke. Oznaka koja se čita na dva načina ispred testera je
--   opasna: operater koji je pročita kao par pinova ukrcava oba kraja žice u jedan konektor,
--   pa žica nije ni ispitana, a tester javlja „pass".
--
--   Zato krajevi ovog kabla idu na konektore C i D — kao u primeru sa stola. Sada je
--   „C01-D01" nedvosmisleno net između dve tačke, a „A01+B01" par pinova jedne tačke.
--
-- OBIM
--
--   Samo =M40-W1.1, da se raspored prvo proveri na jednom kablu. Ostali kablovi iz 005
--   ostaju na starim tačkama dok se ovaj ne potvrdi.
--
-- ŠTA SE NE MENJA
--
--   Ožičenje (CableWire) se ne dira — ono je prepis crteža i ne zna za tester. Menja se samo
--   CableTerminal.TesterPoint, tj. priključni pribor. IsProvisional ostaje 1: adapter još
--   nije napravljen, pa dodela nije potvrđena.

-- Strana A (DIN 72585, pinovi 1 i 2) → konektor C, tačke 01 i 02.
UPDATE CableTerminal
SET TesterPoint = 'C01'
WHERE Label = '1'
  AND CableId IN (SELECT Id FROM Cable WHERE SpecFileName = 'M40W1-1');

UPDATE CableTerminal
SET TesterPoint = 'C02'
WHERE Label = '2'
  AND CableId IN (SELECT Id FROM Cable WHERE SpecFileName = 'M40W1-1');

-- Strana B (konektor XM7, pinovi A i B) → konektor D, tačke 01 i 02.
UPDATE CableTerminal
SET TesterPoint = 'D01'
WHERE Label = 'A'
  AND CableId IN (SELECT Id FROM Cable WHERE SpecFileName = 'M40W1-1');

UPDATE CableTerminal
SET TesterPoint = 'D02'
WHERE Label = 'B'
  AND CableId IN (SELECT Id FROM Cable WHERE SpecFileName = 'M40W1-1');
