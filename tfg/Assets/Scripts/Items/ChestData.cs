/*  Nombre:      ChestData.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: ScriptableObject que define el tipo de cofre, su rareza y tabla de loot.
 */
using UnityEngine;

/// <summary>
/// Tipo/rareza del cofre. Determina la calidad del loot y la probabilidad de aparición.
/// </summary>
public enum ChestRarity
{
    Common,     // Gris   — aparece frecuentemente, ítems básicos
    Uncommon,   // Verde  — aparece a menudo, ítems decentes
    Rare,       // Azul   — aparece a veces, ítems buenos
    Epic,       // Morado — aparece poco, ítems poderosos
    Legendary   // Dorado — aparece muy raramente, ítems únicos
}

/// <summary>
/// ScriptableObject que define un tipo de cofre: su rareza, peso de aparición y tabla de loot.
///
/// Crear via: Assets > Create > TFG/Cofre
///
/// El generador de salas usa los pesos de aparición para decidir qué tipo de cofre spawnear.
/// A mayor nivel de dificultad DDA, los cofres de mayor rareza tienen más peso.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Cofre", fileName = "NewChest")]
public class ChestData : ScriptableObject
{
    [Header("Identidad")]
    public ChestRarity rarity       = ChestRarity.Common;
    public string      chestName    = "Cofre Común";
    public Color       chestColor   = Color.white;

    [Header("Peso de aparición")]
    [Tooltip("Peso base en el sorteo de tipo de cofre. Se multiplica por el modificador de dificultad.")]
    public float spawnWeight = 10f;

    [Header("Tabla de Loot")]
    [Tooltip("Ítems que puede contener este cofre y su probabilidad relativa.")]
    public LootTable lootTable;

    [Header("Cofre vacío")]
    [Tooltip("Probabilidad de que el cofre esté vacío (0 = nunca, 0.1 = 10%).")]
    [Range(0f, 1f)]
    public float emptyChance = 0.05f;

    // ─────────────────────────────────────────────
    // LOOT
    // ─────────────────────────────────────────────

    /// <summary>
    /// Hace el sorteo y devuelve el ítem que contiene el cofre.
    /// Devuelve null si el cofre está vacío (emptyChance) o la tabla está vacía.
    /// </summary>
    public LootEntry RollLoot()
    {
        if (Random.value < emptyChance)
        {
            Debug.Log($"[ChestData] Cofre {chestName} vacío.");
            return null;
        }
        return lootTable?.Roll();
    }

    // ─────────────────────────────────────────────
    // MODIFICADOR DE DIFICULTAD
    // ─────────────────────────────────────────────

    /// <summary>
    /// Peso efectivo según el nivel de dificultad DDA actual.
    /// Los cofres de mayor rareza escalan mejor con la dificultad.
    /// </summary>
    public float GetEffectiveSpawnWeight(int difficultyLevel)
    {
        float rarityBonus = rarity switch
        {
            ChestRarity.Common    => 1.0f,
            ChestRarity.Uncommon  => 1.0f + difficultyLevel * 0.05f,
            ChestRarity.Rare      => 1.0f + difficultyLevel * 0.10f,
            ChestRarity.Epic      => 1.0f + difficultyLevel * 0.15f,
            ChestRarity.Legendary => 1.0f + difficultyLevel * 0.20f,
            _                     => 1.0f
        };
        return spawnWeight * rarityBonus;
    }
}
