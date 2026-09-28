/* =====================================================================
   Backfill de talla - segunda pasada (casos que el primer script dejó en 'N/A')
   Aplicar MANUALMENTE. Solo toca ítems cuya talla sigue en 'N/A', o cuya talla
   quedó con una frase de cambio (p. ej. "XS pero cambio a S") por el primer script;
   en ese último caso se analiza la talla guardada en lugar de la descripción.

   Casos cubiertos:
     A) Cambio de talla:  "... XS Pero se cambio a S"   -> S
        (texto después del último " a " que sigue a "cambio"/"cambió"/"cambia")
     B) Palabra "Talla":  "Ideal para Ocasiones Formales  Talla S" -> S
        (texto después de la última "talla ", sin necesidad de " - ")

   En ambos casos, lo inferido debe ser UNA sola palabra de 1 a 10 caracteres
   (evita tomar frases como "Talla S con manga larga"). Si hay cambio (A),
   gana sobre "Talla" (B), porque es la talla final.
   Si no se puede inferir, el registro queda como está.

   Pasos:
     1. Ejecutar solo el PASO 1 y revisar la vista previa.
     2. Ejecutar el PASO 2 completo; revisar el conteo y elegir COMMIT o ROLLBACK.
   ===================================================================== */

/* ---------- PASO 1: vista previa (no modifica nada) ---------- */
WITH Base AS (
    SELECT oi.Id, oi.ProductDescription, oi.Size AS TallaActual,
           TRIM(REPLACE(CASE WHEN oi.Size LIKE N'%cambi_ a %' THEN oi.Size ELSE oi.ProductDescription END, N':', N' ')) AS D
    FROM dbo.OrderItems oi
    WHERE oi.Size = N'N/A' OR oi.Size LIKE N'%cambi_ a %'
),
Inferidos AS (
    SELECT
        b.Id,
        b.ProductDescription,
        b.TallaActual,
        CASE WHEN b.D LIKE N'%cambi_ a %'
             THEN TRIM(RIGHT(b.D, CHARINDEX(N' a ', REVERSE(b.D)) - 1))
        END AS PorCambio,
        CASE WHEN N' ' + b.D LIKE N'% talla %'
             THEN TRIM(RIGHT(b.D, CHARINDEX(N' allat ', REVERSE(N' ' + b.D)) - 1))
        END AS PorTalla
    FROM Base b
),
Resultado AS (
    SELECT
        i.*,
        CASE
            WHEN LEN(i.PorCambio) BETWEEN 1 AND 10 AND CHARINDEX(N' ', i.PorCambio) = 0 THEN i.PorCambio
            WHEN LEN(i.PorTalla)  BETWEEN 1 AND 10 AND CHARINDEX(N' ', i.PorTalla)  = 0 THEN i.PorTalla
        END AS TallaInferida
    FROM Inferidos i
)
SELECT
    CASE WHEN TallaInferida IS NOT NULL THEN N'Se actualiza' ELSE N'Queda N/A' END AS Resultado,
    TallaActual,
    TallaInferida,
    ProductDescription,
    Id
FROM Resultado
ORDER BY Resultado DESC, TallaInferida, ProductDescription;


/* ---------- PASO 2: actualización dentro de una transacción ---------- */
/*
BEGIN TRANSACTION;

WITH Base AS (
    SELECT oi.Id,
           TRIM(REPLACE(CASE WHEN oi.Size LIKE N'%cambi_ a %' THEN oi.Size ELSE oi.ProductDescription END, N':', N' ')) AS D
    FROM dbo.OrderItems oi
    WHERE oi.Size = N'N/A' OR oi.Size LIKE N'%cambi_ a %'
),
Inferidos AS (
    SELECT
        b.Id,
        CASE WHEN b.D LIKE N'%cambi_ a %'
             THEN TRIM(RIGHT(b.D, CHARINDEX(N' a ', REVERSE(b.D)) - 1))
        END AS PorCambio,
        CASE WHEN N' ' + b.D LIKE N'% talla %'
             THEN TRIM(RIGHT(b.D, CHARINDEX(N' allat ', REVERSE(N' ' + b.D)) - 1))
        END AS PorTalla
    FROM Base b
),
Resultado AS (
    SELECT
        i.Id,
        CASE
            WHEN LEN(i.PorCambio) BETWEEN 1 AND 10 AND CHARINDEX(N' ', i.PorCambio) = 0 THEN i.PorCambio
            WHEN LEN(i.PorTalla)  BETWEEN 1 AND 10 AND CHARINDEX(N' ', i.PorTalla)  = 0 THEN i.PorTalla
        END AS TallaInferida
    FROM Inferidos i
)
UPDATE oi
SET    oi.Size = r.TallaInferida,
       oi.UpdatedAt = GETUTCDATE()
FROM   dbo.OrderItems oi
JOIN   Resultado r ON r.Id = oi.Id
WHERE  r.TallaInferida IS NOT NULL;

SELECT @@ROWCOUNT AS FilasActualizadas;

-- Revisar el resultado y ejecutar UNA de las dos:
-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
*/
