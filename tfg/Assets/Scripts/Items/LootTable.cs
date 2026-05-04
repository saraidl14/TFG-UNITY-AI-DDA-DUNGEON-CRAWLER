using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entrada individual de una tabla de loot.
/// Cada item tiene un peso relativo que determina su probabilidad de aparicion.
/// </summary>
[System.Serializable]
public class LootEntry
{
    public ItemData item;
    [Tooltip("Peso relativo. Mayor peso = mas probable. Ej: peso 10 vs peso 2 → 83% vs 17%.")]
    public float weight = 1f;
    [Min(1)] public int minQuantity = 1;
    [Min(1)] public int maxQuantity = 1;
}

/// <summary>
/// Tabla de loot reutilizable. Puede usarse en cofres, enemigos o cualquier drop.
/// Implementa seleccion aleatoria ponderada (weighted random).
///
/// v2.0 - Soporte para tabla premium (DDA compensatorio).
///   Si DifficultyManager.ShouldBoostLoot es true, se usara premiumEntries
///   en lugar de entries. Esto ocurre cuando el jugador lleva 2+ salas en el
///   minimo de banda de dificultad y sigue teniendo un score DDA muy negativo.
///   Asigna en premiumEntries items de mayor calidad (armas mejores, pociones, etc.)
/// </summary>
[System.Serializable]
public class LootTable
{
    [Tooltip("Tabla de loot normal (se usa siempre que ShouldBoostLoot sea false).")]
    public List<LootEntry> entries = new List<LootEntry>();

    [Tooltip("Tabla de loot premium (se usa cuando DDA activa ShouldBoostLoot). " +
             "Pon aqui items de mayor calidad para ayudar al jugador en dificultad.")]
    public List<LootEntry> premiumEntries = new List<LootEntry>();

    // ─────────────────────────────────────────────
    // ROLL PRINCIPAL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Selecciona un item aleatoriamente segun los pesos.
    /// Si DifficultyManager.ShouldBoostLoot es true y premiumEntries no esta vacia,
    /// usa la tabla premium; si no, usa la normal.
    /// Devuelve null si la tabla activa esta vacia.
    /// </summary>
    public LootEntry Roll()
    {
        // Determinar que tabla usar
        bool   boost        = DifficultyManager.Instance != null && DifficultyManager.Instance.ShouldBoostLoot;
        bool   hasPremium   = premiumEntries != null && premiumEntries.Count > 0;
        List<LootEntry> activeTable = (boost && hasPremium) ? premiumEntries : entries;

        if (boost && hasPremium)
            Debug.Log("[LootTable] Usando tabla PREMIUM (DDA compensatorio activo).");

        return RollFromTable(activeTable);
    }

    // ─────────────────────────────────────────────
    // METODO INTERNO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Realiza el sorteo ponderado sobre la tabla indicada.
    /// </summary>
    private LootEntry RollFromTable(List<LootEntry> table)
    {
        if (table == null || table.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var e in table)
            if (e.item != null) totalWeight += e.weight;

        if (totalWeight <= 0f) return null;

        float roll       = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var e in table)
        {
            if (e.item == null) continue;
            cumulative += e.weight;
            if (roll <= cumulative)
                return e;
        }

        return table[table.Count - 1];
    }

    // ─────────────────────────────────────────────
    // UTILIDADES DE EDITOR
    // ─────────────────────────────────────────────

    /// <summary>Devuelve la probabilidad porcentual de un item en la tabla normal.</summary>
    public float GetProbability(LootEntry entry)
    {
        float total = 0f;
        foreach (var e in entries)
            if (e.item != null) total += e.weight;
        return total > 0f ? (entry.weight / total) * 100f : 0f;
    }

    /// <summary>Devuelve la probabilidad porcentual de un item en la tabla premium.</summary>
    public float GetPremiumProbability(LootEntry entry)
    {
        float total = 0f;
        foreach (var e in premiumEntries)
            if (e.item != null) total += e.weight;
        return total > 0f ? (entry.weight / total) * 100f : 0f;
    }
}
