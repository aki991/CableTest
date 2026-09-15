-- Sadrzaj kataloga, verzija 3: tri vozila sa svojim kablovima i net listama.
--
-- Migracija 002 je pri prvom pokretanju upisivala jedno ogledno vozilo ("Milos" sa kablom
-- M100-W1). Ovde ga zamenjuje stvarni katalog. Ogledno vozilo se brise samo ako je
-- netaknuto - ako je nad njegovim kablom vec izvrsen neki test, ostaje u bazi, jer je
-- istorija testova dokaz da je kabl ispitan i ne sme da nestane.

DELETE FROM Vehicle
WHERE Name = 'Miloš'
  AND NOT EXISTS (
      SELECT 1 FROM Cable c
      WHERE c.VehicleId = Vehicle.Id
        AND (c.SpecFileName <> 'M100W1'
             OR EXISTS (SELECT 1 FROM TestRun r WHERE r.CableId = c.Id))
  );

-- =============================================================================
-- Miloš Veliki
-- =============================================================================

INSERT INTO Vehicle (Name, CreatedAt)
SELECT 'Miloš Veliki', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
WHERE NOT EXISTS (SELECT 1 FROM Vehicle WHERE Name = 'Miloš Veliki');

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W1', 'Glavni snop', 'M30-W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W1');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-I02-O01-F01' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-K01-B01-G01' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-K02' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F03-L01-C02-K03' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I03-N01-F04-L02' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J01-O02' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J02-B02-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'L03-C03-J03-O03' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'N02-D02-J04-B03' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N03-F05' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'A02-G02-N04' FROM Cable c
WHERE c.SpecFileName = 'M30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W2', 'Snop instrument table', 'M30-W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W2');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-J01' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-J02-B01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'G01-M01' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G02-N01' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H01-N02-G03-M02' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'J03-A02-H02-N03' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L01-A03-I02' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M03-E02-L02-B02' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N04-G04-M04-E03' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O01-H03-N05' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'A04-H04-O02' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B03-J04' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'B04-J05-A05' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'E04-L03-J06-B05' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'H05-N06-E05' FROM Cable c
WHERE c.SpecFileName = 'M30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W3', 'Zadnja svetla', 'M30-W3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W3');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-J01' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C02-L01-F01-N01' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-L02-F02-A01' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F03-A02-G01' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'J02-C03' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K01-C04-L03-D02' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L04-D03' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'N02-G02-K02' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N03-G03-A03' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'C05-K03' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'C06-L05-D04' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'F04-L06' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'F05-N04-G04-C07' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'G05-A04-J03' FROM Cable c
WHERE c.SpecFileName = 'M30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W4', 'Prednja svetla', 'M30-W4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W4');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-M01-G01' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-N01' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'G02-B02-L01' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'K01-C01' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'K02-D02' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'L02-G03-N02-K03' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'N03-K04-B03' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'B04-L03-C02-N04' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'C03-M02-G04-N05' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'C04-N06-K05' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'D03-B05' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'K06-B06-L04-D04' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'L05-C05-M03-G05' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'L06-D05-N07-K07' FROM Cable c
WHERE c.SpecFileName = 'M30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W5', 'Snop motora', 'M30-W5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W5');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-O01-K01' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-O02' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-B02-N01-A01' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'D02-B03-O03' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'K02-D03' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'O04-D04-B04-K03' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'A02-N02' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'A03-N03-K04-B05' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'B06-O05' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'D05-A04-O06-B07' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'K05-B08-A05' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'K06-C02-B09' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'O07-K07-C03' FROM Cable c
WHERE c.SpecFileName = 'M30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W6', 'Snop menjaca', 'M30-W6', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W6');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-O01' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-A01-O02-F01' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D02-A02-O03-C02' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E01-C03-D03' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F02-E02-D04' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'O04-E03-D05-F03' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'C04-O05-F04-E04' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'C05-A03-F05-O06' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'D06-A04-O07-C06' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'F06-D07-C07' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'F07-E05-D08-C08' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'A05-O08-F08-D09' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'C09-A06-F09-E06' FROM Cable c
WHERE c.SpecFileName = 'M30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W7', 'Napajanje kabine', 'M30-W7', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W7');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-I02-O01' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E02-K01' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F01-K02-A02-H01' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G01-M01' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H02-O02' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J01-O03-F02-K03' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'K04-P01-H03' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M02-A03-I03' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'M03-B01-J02-P02' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N01-E03-J03' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'P03-G02-K05' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'P04-H04' FROM Cable c
WHERE c.SpecFileName = 'M30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W8', 'Snop vrata', 'M30-W8', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W8');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-H01-P01' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-L01-B03-I02' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-L02-C01-K01' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H02-M01-C02' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I03-P02' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'K02-P03-H03' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L03-B04-H04-P04' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M02-C03-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'N01-C04-L04' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'P05-E03-M03' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'P06-G01-N02-E04' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'B05-H05' FROM Cable c
WHERE c.SpecFileName = 'M30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W9', 'Snop krova', 'M30-W9', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W9');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-K01-P01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C02-K02' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F01-L01-C03-B01' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H01-M01-F02' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'I02-N01-H02-F03' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K03-B02' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L02-C04-K04-B03' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M02-F04-L03' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N02-F05' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'N03-I03-M03' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'P02-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B04-J01-P03-N04' FROM Cable c
WHERE c.SpecFileName = 'M30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W10', 'ABS senzori', 'M30-W10', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W10');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-P01' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'G01-B02' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'I01-D01' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'K01-G02-B03-D02' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'O01-G03-B04' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'P02-I02-G04-K02' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'P03-O02-I03' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'B05-P04-K03-D03' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'G05-B06-O03' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'G06-B07' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'K04-G07-P05' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'O04-I04-D04-G08' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'O05-I05-G09' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'P06-K05' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'B08-O06-I06-D05' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'D06-B09' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'I07-B10-O07-D07' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'K06-D08-B11' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'O08-G10-D09' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'O09-I08-G11' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'B12-K07' FROM Cable c
WHERE c.SpecFileName = 'M30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W11', 'Snop rezervoara', 'M30-W11', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W11');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-M01-B01-P01' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-M02' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D02-O01-G01' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G02-O02-D03' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'K01-A02-H01' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K02-B02-M03' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'M04-D04-K03-H02' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'N01-G03-M05-K04' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'P02-G04' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P03-H03-O03-N02' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'B03-K05' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'D05-M06-A03' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'D06-N03-B04-A04' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'H04-O04' FROM Cable c
WHERE c.SpecFileName = 'M30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W12', 'Snop prikolice', 'M30-W12', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W12');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-G01' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'C02-I02' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F01-M01-B02' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G02-N01-C03-I03' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H01-O01' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I04-P01-E01' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'I05-A01-G03' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'K01-A02-H02-N02' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'M02-C04' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'O02-E02-K02-P02' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'O03-F02' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'A03-G04-M03' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'B03-G05' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'B04-H03-O04-F03' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'E03-I06-A04' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'E04-M04-A05' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'F04-N03-C05-I07' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'H04-O05' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'H05-P03' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'I08-A06-F05-M05' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'M06-B05-H06-O06' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 23, 'N04-C06-I09' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 23);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 24, 'N05-E05' FROM Cable c
WHERE c.SpecFileName = 'M30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 24);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W13', 'Snop grejaca', 'M30-W13', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W13');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-J01-I01-B01' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-A02-I02-G01' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'G02-A03' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G03-E01-A04' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'J02-E02' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'M01-G04' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'M02-I03' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'B03-J03-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'E03-A05-I05-G05' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'E04-A06-M03' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'I06-E05-A07' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'I07-E06-B04-M04' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'M05-G06-B05' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'A08-J04' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'B06-J05-I08-E07' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'E08-M06-J06-A09' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'G07-A10-M07-I09' FROM Cable c
WHERE c.SpecFileName = 'M30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W14', 'Snop klime', 'M30-W14', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W14');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-L01-C01' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'C02-L02-F01-B03' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-O01-C03' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'I02-A01' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I03-A02-J01' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L03-C04-J02' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M01-C05-J03-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M02-G01-L04' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O02-I05-A03-G02' FROM Cable c
WHERE c.SpecFileName = 'M30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W15', 'Razvodna kutija', 'M30-W15', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W15');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B01-I02-N01-D01' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D02-J01' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E01-L01-P01' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G01-L02-A02-I03' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H01-N02' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J02-N03-E02' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J03-O01' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'L03-A03-G02-J04' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'L04-B02-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N04-B03-J05' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'O02-D03-K01' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'P02-E03' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'A04-G03-M01-B04' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'A05-H02' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'B05-I05-N05' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'E04-J06' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'G04-K02-A06-H03' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'G05-L05-A07-H04' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'H05-M02' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'I06-O03-D04' FROM Cable c
WHERE c.SpecFileName = 'M30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W16', 'Snop akumulatora', 'M30-W16', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W16');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-P01-M01-F01' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'H01-B02-M02' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'H02-B03-O01' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'M03-H03-P02' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'M04-H04' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'P03-J01-H05-M05' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'B04-O02' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'F02-O03-M06' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'F03-B05' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'J02-F04' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'J03-F05' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'M07-H06' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'P04-M08' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'P05-O04-H07-F06' FROM Cable c
WHERE c.SpecFileName = 'M30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W17', 'Snop pumpe', 'M30-W17', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W17');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C02-J01-B01-P01' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-J02-D01-N01' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-N02' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G01-P02' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H01-B02' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J03-D02' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J04-E03' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N03-E04-J05' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'B03-H02-G02-N04' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'C03-I02-P03' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'D03-J06-B04-P04' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'D04-N05' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'G03-P05-D05' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'G04-B05' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'H03-B06-I03' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'J07-D06-C04-H04' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'J08-E05' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'P06-G05' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'P07-H05-N06-J09' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'B07-H06-C05' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'D07-I04-J10-B08' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 23, 'E06-J11' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 23);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 24, 'E07-P08-G06-D08' FROM Cable c
WHERE c.SpecFileName = 'M30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 24);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W18', 'Snop kocnica', 'M30-W18', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W18');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-F01-L01-B01' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-G01' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'C02-I01' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E01-K01-O01-F02' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F03-K02' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'F04-L02-B02-I02' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I03-M01-C03-F05' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J01-O02-D01-K03' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'K04-A02' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'K05-B03-F06' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'L03-B04-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'N01-D02-J02-L04' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'N02-D03-J03' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'A03-F07-K06' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'B05-F08' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'C04-G02' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'C05-I05' FROM Cable c
WHERE c.SpecFileName = 'M30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W19', 'Snop upravljaca', 'M30-W19', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W19');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'I01-O01' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'I02-A01' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'J01-I03' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'K01-I04' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'O02-K02-I05' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'P01-K03-I06' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'A02-L01' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'A03-O03-L02' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'J02-A04-O04' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'K04-I07-P02-L03' FROM Cable c
WHERE c.SpecFileName = 'M30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'M30-W20', 'Snop alternatora', 'M30-W20', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Miloš Veliki'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'M30-W20');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-L01' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B01-N01-H01-A02' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F01-P01-L02' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'J01-B02-L03-F02' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'J02-B03' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'L04-F03-P02' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'P03-H02' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'P04-J03-F04' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'A03-N02-F05' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'B04-P05-H03' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'H04-A04-J04' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'J05-A05-N03' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'L05-B05-N04-J06' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'L06-F06' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'N05-H05' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'P06-J07' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'B06-N06-F07' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'F08-P07-H06' FROM Cable c
WHERE c.SpecFileName = 'M30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);

-- =============================================================================
-- Lazar 3M
-- =============================================================================

INSERT INTO Vehicle (Name, CreatedAt)
SELECT 'Lazar 3M', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
WHERE NOT EXISTS (SELECT 1 FROM Vehicle WHERE Name = 'Lazar 3M');

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W1', 'Glavni snop', 'LM30-W1', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W1');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-G01-M01-D01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B01-H01-M02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-I01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-J01-P01-F01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F02-K01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H02-L01-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'H03-M03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J02-O01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'K02-A02-F03-L02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'K03-A03-H04-M04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'L03-B02-H05-M05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'O02-D03-J03-L04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'P02-F04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'A04-F05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W1'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W2', 'Snop instrument table', 'LM30-W2', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W2');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-M01-K01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B01-O01-L01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'K02-A02-L02-B02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'L03-B03-M02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'M03-F01-O02-K03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'M04-F02-A03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'N01-K04-A04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'O03-L04-B04-N02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'B05-M05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'B06-N03-K05-A05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'F03-A06-M06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'K06-B07-M07-F04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'M08-F05-O04-K07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'M09-F06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'O05-K08-B08-M10' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'A07-M11-B09-O06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'B10-M12' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'F07-N04-L05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'K09-O07-L06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'K10-A08-M13' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'M14-B11-N05-L07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'N06-K11-O08-L08' FROM Cable c
WHERE c.SpecFileName = 'LM30-W2'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W3', 'Zadnja svetla', 'LM30-W3', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W3');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'D01-K01-O01-I01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D02-K02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'H01-M01-B01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H02-M02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'J01-N01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'J02-P01-I02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'K03-B02-I03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L01-B03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M03-F01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O02-F02-L02-B04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'O03-H03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B05-I04-N02-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'D03-K04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'D04-K05-P02-I05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'F03-L03-B06-J03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'H05-N03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W3'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W4', 'Prednja svetla', 'LM30-W4', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W4');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-I01-N01-D01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-I02-O01-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D03-J01-O02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E01-L01-P01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G01-L02-C02-E02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'G02-N02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J02-O03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'K01-O04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'L03-B02-F01-M01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'L04-C03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N03-D04-J03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'O05-E03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'P02-E04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'P03-G03-M02-B03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W4'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W5', 'Snop motora', 'LM30-W5', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W5');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-I01-D01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-N01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-N02-H01-B03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-A01-I02-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H02-D03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H03-D04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I03-E03-B04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'N03-G01-D05-I04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'B05-H04-D06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'B06-I05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'D07-N04-G02-B07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'E04-B08-H05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'H06-B09' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'I06-D08-N05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'N06-E05-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'A03-G03-B10-N07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'B11-H07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'B12-I07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'E06-N08-G04-D09' FROM Cable c
WHERE c.SpecFileName = 'LM30-W5'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W6', 'Snop menjaca', 'LM30-W6', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W6');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-K01-P01-I01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-L01-A01-J01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-M01-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'I02-M02-D02-L02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'J02-O01-H01-L03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K02-O02-H02-N01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'K03-A03-I03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L04-B03-K04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M03-D03-L05-P02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O03-H03-M04-B04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'O04-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'A04-I04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W6'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W7', 'Napajanje kabine', 'LM30-W7', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W7');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-G01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-H01-O01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-J01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-K01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G02-K02-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'G03-N01-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'H02-N02-D03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'I01-O02-F01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'J02-P01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'K03-B01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'L01-B02-H03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'N03-E03-I02-O03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'O04-F02-J03-A03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'P02-G04-K04-A04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'A05-G05-L02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'B03-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'E04-I03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'F03-J04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'F04-L03-A06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'H05-N04-B04-F05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'H06-N05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'I04-O05-F06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W7'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W8', 'Snop vrata', 'LM30-W8', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W8');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-J01-D01-A01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C02-L01-H01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F01-P01-J02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-C03-L02-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'J03-C04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'J04-D03-P02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'P03-H02-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'A03-J05-C05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'A04-L03-F03-P04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'C06-P05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'F04-A05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'H03-C07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'J06-D04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'L04-D05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'P06-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W8'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W9', 'Snop krova', 'LM30-W9', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W9');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-G01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-H01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-L01-A02-H02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'D02-L02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G02-N01-C02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'G03-N02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J01-P01-G04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L03-B01-H03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M01-B02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'M02-C03-L04-A03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'P02-E01-L05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'P03-E02-M03-C04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W9'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W10', 'ABS senzori', 'LM30-W10', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W10');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-M01-I01-D01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-A02-K01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'I02-A03-N01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'I03-D02-N02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'K02-I04-A04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'M02-I05-E02-N03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'N04-K03-I06-A05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'D03-M03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W10'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W11', 'Snop rezervoara', 'LM30-W11', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W11');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-F01-M01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-I01-O01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'C01-I02-O02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E01-K01-P01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'E02-M02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H01-M03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I03-N01-C02-K02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'I04-O03-E03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'K03-A01-F02-L01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'M04-A02-H02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N02-B03-H03-O04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'N03-C03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'O05-D01-K04-P02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'A03-F03-M05-O06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W11'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W12', 'Snop prikolice', 'LM30-W12', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W12');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-F01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-H01-P01-E01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-H02-P02-F02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'D02-M01-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F03-N01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'G01-O01-C02-L01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'H03-P03-D03-M02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L02-A03-E02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M03-A04-G02-O02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'M04-C03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N02-C04-L03-A05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'P04-D04-M05-A06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'A07-E03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'B01-G03-N03-D05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'C05-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'C06-H05-P05-F04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W12'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W13', 'Snop grejaca', 'LM30-W13', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W13');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'E01-K01-C01-H01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E02-L01-C02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'G01-L02-E03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G02-O01-F01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H02-P01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K02-P02-I01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'K03-E04-I02-P03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M01-F02-L03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O02-G03-L04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P04-H03-M02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'P05-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'E05-K04-C03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'E06-L05-C04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'G04-L06-F03-K05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'H05-M03-G05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'H06-P06-G06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'I03-C05-O03-H07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'L07-E07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'M04-E08-L08-C06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W13'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W14', 'Snop klime', 'LM30-W14', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W14');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-J01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C02-J02-D01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'G01-N01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H01-A01-I01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H02-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'J03-C03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J04-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'N02-G02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O01-H03-C04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'C05-I02-J05-O02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'D03-J06-G03-O03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'D04-O04-G04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'G05-A03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'I03-A04-G06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'I04-D05-N03-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'J07-G07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'O05-G08-A05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'A06-H05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'C06-I05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'D06-N04-A07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'D07-N05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'G09-A08' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 23, 'H06-A09-J08' FROM Cable c
WHERE c.SpecFileName = 'LM30-W14'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 23);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W15', 'Razvodna kutija', 'LM30-W15', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W15');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-N01-J01-G01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'G02-B02-J02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'H01-D01-L01-J03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H02-G03-B03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'L02-H03-B04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'N02-J04-G04-H04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'N03-L03-H05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'D02-N04-H06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'G05-B05-J05-H07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'G06-B06-N05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W15'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W16', 'Snop akumulatora', 'LM30-W16', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W16');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-I01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-B02-A01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D02-B03-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G01-D03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'I02-G02-F01-B04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I03-G03-D04-F02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'A03-I04-G04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'B05-A04-I05-G05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'D05-A05-B06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'F03-D06-B07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'I06-G06-F04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'A06-G07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'A07-I07-G08' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'B08-A08-I08-G09' FROM Cable c
WHERE c.SpecFileName = 'LM30-W16'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W17', 'Snop pumpe', 'LM30-W17', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W17');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-G01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-I01-O01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-L01-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E01-L02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G02-N01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I02-N02-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I03-P01-E02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J01-A03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M01-A04-I04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'M02-D03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N03-D04-L03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'P02-F01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'A05-F02-M03-D05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'A06-I05-N04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'D06-J02-P03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'E03-J03-A07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'F03-M04-A08' FROM Cable c
WHERE c.SpecFileName = 'LM30-W17'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W18', 'Snop kocnica', 'LM30-W18', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W18');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-H01-O01-E01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C02-J01-P01-H02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-L01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-L02-D02-J02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H03-O02-E03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H04-P02-G01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J03-P03-G02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L03-C03-I01-P04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M01-D03-J04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'M02-E04-J05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'P05-E05-M03-D04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B01-H05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'B02-I02-P06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'C04-J06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'D05-L04-B03-I03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'G03-M04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'G04-O03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'H06-P07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'J07-B04-H07-M05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'J08-C05-I04-O04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'M06-D06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W18'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W19', 'Snop upravljaca', 'LM30-W19', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W19');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-H01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B01-M01-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-O01-D01-M02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-O02-D02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G01-P01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H02-A03-B02-F01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'H03-B03-G02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'O03-D03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O04-E03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P02-G03-F02-O05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'A04-G04-B04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B05-H04-D04-A05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'E04-O06-D05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'F03-O07-D06-B06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'F04-P03-G05-A06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'G06-B07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'H05-B08-G07-F05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'O08-E05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'O09-F06-P04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'P05-F07-A07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'B09-H06-A08-P06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'D07-M03-A09-P07' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 23, 'E06-O10' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 23);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 24, 'E07-P08' FROM Cable c
WHERE c.SpecFileName = 'LM30-W19'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 24);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'LM30-W20', 'Snop alternatora', 'LM30-W20', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Lazar 3M'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'LM30-W20');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-H01-O01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-L01' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-L02-A01-B03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-O02-H02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F01-A02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H03-C01-L03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J01-C02-O03-F02' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L04-E03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'A03-H04-B04-L05' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'B05-J02-C03' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'C04-L06-E04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'E05-L07-F03-O04' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'F04-A04-C05-B06' FROM Cable c
WHERE c.SpecFileName = 'LM30-W20'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);

-- =============================================================================
-- Perun
-- =============================================================================

INSERT INTO Vehicle (Name, CreatedAt)
SELECT 'Perun', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
WHERE NOT EXISTS (SELECT 1 FROM Vehicle WHERE Name = 'Perun');

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB100P', 'Glavni snop', 'KB100P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB100P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'D01-K01-A01-I01' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D02-M01-L01-A02' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'H01-N01-D03-L02' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'I02-N02-H02-M02' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'K02-P01-I03-O01' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K03-A03-I04' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'M03-A04-L03-P02' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'N03-D04-M04' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N04-H03-M05' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O02-I05-G01-N05' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'A05-K04' FROM Cable c
WHERE c.SpecFileName = 'KB100P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB101P', 'Snop instrument table', 'KB101P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB101P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'D01-H01-P01-M01' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-K01-D02-H02' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F01-L01-E02' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G01-L02-F02-E03' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H03-P02-F03' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I01-C01-G02-P03' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I02-C02' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L03-D03-K02-C03' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'L04-F04-D04-K03' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P04-G03' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'C04-H04' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'D05-I03-P05-G04' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'E04-K04' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'E05-L05' FROM Cable c
WHERE c.SpecFileName = 'KB101P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB102P', 'Zadnja svetla', 'KB102P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB102P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-H01' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-L01-A02-I01' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-L02' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E02-M01-D02' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H02-P01-E03-M02' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H03-A03-N01-F01' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I02-C01-H04' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L03-D03-I03' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M03-D04-L04' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P02-F02' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'P03-F03-N02' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'A04-H05-P04' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'C02-I04' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'D05-M04' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'E04-M05-D06-L05' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'H06-N03-E05' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'H07-A05' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'L06-A06-H08' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'L07-C03' FROM Cable c
WHERE c.SpecFileName = 'KB102P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB103P', 'Prednja svetla', 'KB103P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB103P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'E01-K01-O01-N01' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E02-K02' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'H01-L01-E03' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H02-O02' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'J01-D01-O03-H03' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K03-E04' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'K04-G01-J02-I01' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L02-H04-N02' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O04-I02' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O05-J03-D02' FROM Cable c
WHERE c.SpecFileName = 'KB103P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB104P', 'Snop motora', 'KB104P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB104P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-J01-D01-I01' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-J02' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E02-M01-L01-D02' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H01-M02-F01' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H02-O01' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I02-C02-J03' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L02-D03-J04' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M03-E03-L03' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O02-H03' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'C03-H04-I03-M04' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'D04-J05-O03' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'E04-L04-C04' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'F02-L05' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'H05-O04' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'H06-C05-O05-F03' FROM Cable c
WHERE c.SpecFileName = 'KB104P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB105P', 'Snop menjaca', 'KB105P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB105P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'D01-J01' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D02-J02' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F01-N01-E01' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-O01-G01' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G02-A01' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'J03-A02-H01' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J04-E02-M01-H02' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M02-E03-J05-H03' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O02-G03' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O03-H04' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'D03-J06-A03' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'D04-J07' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'F03-M03' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'F04-N02-G04-E04' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'G05-O04' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'H05-A04-J08' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'J09-D05-M04' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'M05-F05' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'N03-F06-O05-M06' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'O06-H06-G06-N04' FROM Cable c
WHERE c.SpecFileName = 'KB105P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB106P', 'Napajanje kabine', 'KB106P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB106P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-M01-O01-N01' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-N02-H01' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E02-N03-K01' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'K02-O02' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'K03-C02' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'M02-C03-N04-H02' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'N05-E03-H03-L01' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'O03-K04-B01' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'B02-K05-M03-N06' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'B03-M04' FROM Cable c
WHERE c.SpecFileName = 'KB106P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB107P', 'Snop vrata', 'KB107P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB107P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-J01-F01-L01' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-L02' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'H01-L03' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H02-M01-J02' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'I01-C02-J03-E02' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'J04-E03-K01' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L04-F02-M02' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L05-H03-C03' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M03-J05-E04' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'C04-K02-E05' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'E06-L06-H04' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'F03-L07-H05' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'I02-C05-E07-F04' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'J06-E08-K03' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'K04-E09-L08-F05' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'L09-F06-M04' FROM Cable c
WHERE c.SpecFileName = 'KB107P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB108P', 'Snop krova', 'KB108P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB108P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'D01-M01' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'G01-P01-K01-B01' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'G02-B02-M02' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'K02-D02-M03-J01' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'M04-D03-P02-K03' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'M05-J02-B03' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'P03-J03' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'P04-M06' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'B04-M07-J04' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'D04-P05-K04-B05' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'J05-P06' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'J06-D05' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'K05-D06' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'N01-J07-P07' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'N02-J08-D07' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'P08-K06' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'B06-M08-G03-P09' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'D08-P10-J09' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'G04-P11-K07-D09' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'J10-D10-N03-G05' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'M09-D11-N04-J11' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'N05-J12-P12-K08' FROM Cable c
WHERE c.SpecFileName = 'KB108P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB109P', 'ABS senzori', 'KB109P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB109P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-J01' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'E01-M01-F01' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F02-M02-J02' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F03-B02' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H01-C01' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'J03-E02-M03' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'M04-F04' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M05-H02-E03' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'B03-K01-E04-M06' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'E05-K02-F05' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'F06-M07' FROM Cable c
WHERE c.SpecFileName = 'KB109P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB110P', 'Snop rezervoara', 'KB110P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB110P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-G01-M01' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-I01-M02-F01' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-I02-C02' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-K01' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F03-K02' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H01-M03' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'H02-B01' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J01-B02-G02' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'J02-D02-H03' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'K03-D03-I03-B03' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'L01-E02' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B04-F04' FROM Cable c
WHERE c.SpecFileName = 'KB110P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB111P', 'Snop prikolice', 'KB111P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB111P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-I01-P01' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-J01-B02' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F01-K01-D02-I02' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'H01-N01' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H02-O01-E01' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I03-O02-F02' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'K02-B03-H03-N02' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M01-B04-I04-O03' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N03-E02-K03-P02' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O04-F03' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'P03-H04-N04' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B05-I05' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'D03-I06' FROM Cable c
WHERE c.SpecFileName = 'KB111P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB112P', 'Snop grejaca', 'KB112P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB112P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-H01-N01-F01' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C02-J01-O01' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-K01-B01' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-L01' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F03-N02' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I01-N03-F04-K02' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J02-B02' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J03-C03-H02-N04' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'K03-C04' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'N05-E01-J04-B03' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'O02-E02-K04-C05' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B04-H03-N06-D02' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'C06-I02-O03' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'D03-J05-B05-H04' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'D04-K05-B06' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'F05-L02-C07' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'H05-L03-D05-J06' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'H06-O04-F06' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'I03-O05-F07' FROM Cable c
WHERE c.SpecFileName = 'KB112P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB113P', 'Snop klime', 'KB113P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB113P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'C01-J01-O01-H01' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'F01-L01' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F02-M01' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G01-N01' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'I01-O02-G02' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'I02-O03-H02-N02' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J02-C02' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M02-C03-I03-O04' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N03-E01' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'N04-F03-L02-C04' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'O05-G03' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'C05-H03-O06' FROM Cable c
WHERE c.SpecFileName = 'KB113P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB114P', 'Razvodna kutija', 'KB114P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB114P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-H01-P01' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-H02-P02-F01' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-K01-C01' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-K02-C02-I01' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F03-N01' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'G01-P03-F04' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I02-B02-F05' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'K03-C03' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'K04-D02-H03' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'N02-D03-K05' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'N03-F06-M01-D04' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B03-G02' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'B04-H04-P04-E02' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'C04-I03-P05' FROM Cable c
WHERE c.SpecFileName = 'KB114P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB115P', 'Snop akumulatora', 'KB115P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB115P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'E01-K01-C01-I01' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'F01-K02-C02' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'G01-L01-F02-K03' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'G02-N01' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'I02-N02' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K04-P01-J01-I03' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L02-E02-J02' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'L03-F03-K05' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'M01-G03' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P02-G04-M02-F04' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'C03-I04-N03-G05' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'C04-J03-P03-I05' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'E03-L04-C05-K06' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'F05-L05-E04-C06' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'I06-N04' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'I07-P04-G06-M03' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'K07-C07-J04-N05' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'K08-C08-J05-I08' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'M04-E05-L06-K09' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'M05-G07-L07' FROM Cable c
WHERE c.SpecFileName = 'KB115P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB116P', 'Snop pumpe', 'KB116P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB116P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-I01-P01-O01' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'D01-K01' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'E01-M01-D02-B02' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F01-O02-G01' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'I02-P02-G02' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K02-D03-I03' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'M02-E02-K03' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'M03-E03-F02-K04' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'P03-F03' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P04-G03' FROM Cable c
WHERE c.SpecFileName = 'KB116P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB117P', 'Snop kocnica', 'KB117P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB117P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'B01-H01-A01' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-K01' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'D01-K02-B03' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'E01-P01-N01-D02' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'G01-A02-E02-D03' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'H02-B04-A03-G02' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'J01-B05' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'N02-D04-K03-E03' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'N03-E04-P02-K04' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'P03-H03-G03-N04' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'B06-H04-A04-J02' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B07-J03-A05-P04' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'E05-N05' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'E06-P05-G04-N06' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'G05-A06-H05-E07' FROM Cable c
WHERE c.SpecFileName = 'KB117P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB118P', 'Snop upravljaca', 'KB118P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB118P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-F01-B01' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B02-I01' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'C01-N01-D01-J01' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'D02-N02' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F02-O01-E01' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'F03-B03-E02-D03' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I02-C02' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'J02-D04-C03-I03' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O02-D05-N03-E03' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O03-E04-A02-N04' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'A03-I04' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'B04-J03-C04' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'C05-N05' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'D06-N06-C06-B05' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'F04-A04-E05-O04' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'I05-A05' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'I06-C07-J04-F05' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'J05-D07-N07' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'O05-E06' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'A06-F06-N08' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'A07-I07' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'C08-J06-I08-A08' FROM Cable c
WHERE c.SpecFileName = 'KB118P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB119P', 'Snop alternatora', 'KB119P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB119P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-K01-B01-O01' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'C01-K02-B02' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'F01-N01-B03' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'F02-N02-C02' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'H01-O02-F03-C03' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'K03-B04-H02-G01' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'L01-B05' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'N03-C04-K04' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'O03-F04-N04' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'O04-G02-H03-N05' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'A02-H04-B06' FROM Cable c
WHERE c.SpecFileName = 'KB119P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);

INSERT INTO Cable (VehicleId, Code, Description, SpecFileName, CreatedAt)
SELECT v.Id, 'KB120P', 'Snop radio-uredjaja', 'KB120P', strftime('%Y-%m-%dT%H:%M:%f', 'now') || 'Z'
FROM Vehicle v
WHERE v.Name = 'Perun'
  AND NOT EXISTS (SELECT 1 FROM Cable WHERE SpecFileName = 'KB120P');

INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 1, 'A01-F01-L01' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 1);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 2, 'B01-H01-N01' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 2);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 3, 'C01-J01' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 3);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 4, 'D01-J02-A02-F02' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 4);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 5, 'F03-L02' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 5);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 6, 'F04-N02-B02-H02' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 6);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 7, 'I01-O01-D02' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 7);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 8, 'I02-O02' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 8);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 9, 'K01-A03' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 9);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 10, 'L03-A04-H03-N03' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 10);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 11, 'L04-C02' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 11);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 12, 'O03-C03-I03' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 12);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 13, 'O04-E01-K02' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 13);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 14, 'P01-E02-K03-A05' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 14);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 15, 'A06-H04' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 15);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 16, 'B03-H05-O05' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 16);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 17, 'D03-J03' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 17);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 18, 'D04-J04-P02-E03' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 18);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 19, 'E04-K04' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 19);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 20, 'F05-N04-B04-I04' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 20);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 21, 'H06-O06-C04-I05' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 21);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 22, 'I06-O07' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 22);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 23, 'K05-P03-E05-L05' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 23);
INSERT INTO Net (CableId, Ordinal, Points)
SELECT c.Id, 24, 'K06-A07' FROM Cable c
WHERE c.SpecFileName = 'KB120P'
  AND NOT EXISTS (SELECT 1 FROM Net WHERE CableId = c.Id AND Ordinal = 24);

