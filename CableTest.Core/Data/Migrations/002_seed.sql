-- Početni sadržaj, pri prvom pokretanju: vozilo "Miloš" i njegov kabl "M100-W1".
--
-- Upisuje se samo ako je baza prazna, da se pri kasnijem dodavanju migracija u već popunjenu
-- bazu ne pojavi vozilo koje je korisnik u međuvremenu obrisao.

INSERT INTO Vehicle (Name, CreatedAt)
SELECT 'Miloš', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
WHERE NOT EXISTS (SELECT 1 FROM Vehicle);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M100-W1', '', 'M100W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš'
  AND NOT EXISTS (SELECT 1 FROM Cable);

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'O01-O02-O31-O32'
FROM Cable c
WHERE c.SpecFileName = 'M100W1'
  AND NOT EXISTS (SELECT 1 FROM Net);
