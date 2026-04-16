using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entrada individual de una tabla de loot.
/// Cada ítem tiene un peso relativo que determina su probabilidad de aparición.
/// </summary>
[System.Serializable]
public class LootEntry
{
    public ItemData item;
    [Tooltip("Peso relativo. Mayor peso = más probable. Ej: peso 10 vs peso 2 → 83% vs 17%.")]
    public float weight = 1f;
    [Min(1)] public int minQuantity = 1;
    [Min(1)] public int maxQuantity = 1;
}

/// <summary>
/// Tabla de loot reutilizable. Puede usarse en cofres, enemigos o cualquier drop.
/// Implementa selección aleatoria ponderada (weighted random).
/// </summary>
[System.Serializable]
public class LootTable
{
    public List<LootEntry> entries = new List<LootEntry>();

    /// <summary>
    /// Selecciona un ítem aleatoriamente según los pesos.
    /// Devuelve null si la tabla está vacía.
    /// </summary>
    public LootEntry Roll()
    {
        if (entries == null || entries.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var e in entries)
            if (e.item != null) totalWeight += e.weight;

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var e in entries)
        {
            if (e.item == null) continue;
            cumulative += e.weight;
            if (roll <= cumulative)
                return e;
        }

        return entries[entries.Count - 1];
    }

    /// <summary>Devuelve la probabilidad porcentual de cada ítem para mostrar en editor.</summary>
    public float GetProbability(LootEntry entry)
    {
        float total = 0f;
        foreach (var e in entries)
            if (e.item != null) total += e.weight;
        return total > 0f ? (entry.weight / total) * 100f : 0f;
    }
}
