/* =====================================================================
   Backfill de OrderItems.Size (talla) a partir de ProductDescription
   Feature 011 - aplicar MANUALMENTE, DESPUÉS de la migración AddOrderItemSize.

   Convención usada hasta ahora: la talla va al final de la descripción,
   separada por " - ", p. ej. "Blusa floral - M", "Jeans - 32",
   "Vestido niña - 10-12 años", "Camisa - TALLA XL", "Short - Talla: S".

   Regla:
     - Se toma el texto después del ÚLTIMO " - " de la descripción.
     - Si empieza con "Talla" (con o sin ":"), se quita esa palabra.
     - Se recortan espacios. Si queda entre 1 y 20 caracteres, esa es la talla.
     - Solo se actualizan ítems cuya talla sigue en 'N/A' (valor por defecto).
     - Si no se puede inferir, el registro queda como está.
     - La descripción NO se modifica.

   Pasos:
     1. Ejecutar solo el PASO 1 y revisar la vista previa: cada ítem en 'N/A'
        aparece como "Se actualiza" (con la talla inferida) o "Queda N/A".
        Ojo con falsos positivos, p. ej. "Vestido - Rojo" inferiría "Rojo".
     2. Ejecutar el PASO 2 completo; revisar el conteo y elegir COMMIT o ROLLBACK.
   ===================================================================== */

/* ---------- PASO 1: vista previa (no modifica nada) ---------- */
WITH Candidatos AS (
    SELECT
        oi.Id,
        oi.ProductDescription,
        CASE WHEN CHARINDEX(' - ', oi.ProductDescription) > 0
             THEN TRIM(RIGHT(oi.ProductDescription, CHARINDEX(' - ', REVERSE(oi.ProductDescription)) - 1))
        END AS Sufijo
    FROM dbo.OrderItems oi
    WHERE oi.Size = N'N/A'
),
Inferidos AS (
    SELECT
        c.*,
        TRIM(CASE
                 WHEN c.Sufijo LIKE N'talla:%' THEN STUFF(c.Sufijo, 1, 6, N'')
                 WHEN c.Sufijo LIKE N'talla %' THEN STUFF(c.Sufijo, 1, 6, N'')
                 ELSE c.Sufijo
             END) AS TallaInferida
    FROM Candidatos c
)
SELECT
    CASE WHEN LEN(TallaInferida) BETWEEN 1 AND 20 THEN N'Se actualiza' ELSE N'Queda N/A' END AS Resultado,
    TallaInferida,
    ProductDescription,
    Id
FROM Inferidos
ORDER BY Resultado DESC, TallaInferida, ProductDescription;


/* ---------- PASO 2: actualización dentro de una transacción ---------- */
/*
BEGIN TRANSACTION;

WITH Candidatos AS (
    SELECT
        oi.Id,
        CASE WHEN CHARINDEX(' - ', oi.ProductDescription) > 0
             THEN TRIM(RIGHT(oi.ProductDescription, CHARINDEX(' - ', REVERSE(oi.ProductDescription)) - 1))
        END AS Sufijo
    FROM dbo.OrderItems oi
    WHERE oi.Size = N'N/A'
),
Inferidos AS (
    SELECT
        c.Id,
        TRIM(CASE
                 WHEN c.Sufijo LIKE N'talla:%' THEN STUFF(c.Sufijo, 1, 6, N'')
                 WHEN c.Sufijo LIKE N'talla %' THEN STUFF(c.Sufijo, 1, 6, N'')
                 ELSE c.Sufijo
             END) AS TallaInferida
    FROM Candidatos c
)
UPDATE oi
SET    oi.Size = i.TallaInferida,
       oi.UpdatedAt = GETUTCDATE()
FROM   dbo.OrderItems oi
JOIN   Inferidos i ON i.Id = oi.Id
WHERE  LEN(i.TallaInferida) BETWEEN 1 AND 20;

SELECT @@ROWCOUNT AS FilasActualizadas;

-- Revisar el resultado y ejecutar UNA de las dos:
-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
*/
