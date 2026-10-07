# Quickstart / Manual Verification

1. `dotnet build` from the repo root; apply the migration (`dotnet ef database update --project MariCamiStore`).
2. **Config**: Configuración → "Meta mensual por defecto" shows 4 000 000; set 0 → rejected.
3. **Vendedores**: Catálogos → Vendedores → create two; deactivate one.
4. **Mes Actual**: Ventas → Metas → Mes Actual → first active salesperson preselected; one row per day of the current CR month; Σ "Monto Meta Control" = goal; Sat highest, Mon lowest.
5. Reload / reselect → no duplicate rows (check `SalesGoals` count).
6. Enter real amounts → header real, %, % al día update; reload → persisted. Check semáforo at 69.9 / 70 / 90 / 90.1.
7. Change header goal → Control column recomputed, manually edited days kept, reals untouched. Change currency → saved.
8. Negative amount / goal 0 → inline error, value reverted.
9. Histórico: previous months (insert one via SQL for testing) read-only; no editable cells.
10. Algorithm check: for goals {4 000 000, 3 999 999.99, 1 000, 123} × months of 28/29/30/31 days: Σ = goal, no negatives; Oct-2026 @ 4 000 000 reproduces the Sat > Fri > … > Mon ordering.
