/*  Nombre:      PotionData.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/05/2026
 *  Descripcion: ScriptableObject para pociones que restauran vida y aplican buffs temporales.
 */
using UnityEngine;

/// <summary>
/// Tipo de buff temporal que puede otorgar una poción al usarla.
/// </summary>
public enum BuffType
{
    None,
    SpeedBoost,     // +% velocidad de movimiento durante buffDuration segundos
    DamageBoost,    // +% daño de ataques durante buffDuration segundos
    DefenseBoost    // -% daño recibido durante buffDuration segundos
}

/// <summary>
/// ScriptableObject para pociones.
/// Define cuánta vida restaura y si otorga un buff temporal.
///
/// Crear via: Assets > Create > TFG/Ítems/Poción
///
/// Ejemplos:
///   • Poción pequeña  → healAmount = 20, buffType = None
///   • Poción de poder → healAmount = 0,  buffType = DamageBoost, buffValue = 0.25, buffDuration = 10
///   • Poción grande   → healAmount = 50, buffType = SpeedBoost,  buffValue = 0.15, buffDuration = 8
/// </summary>
[CreateAssetMenu(menuName = "TFG/Ítems/Poción", fileName = "NewPotion")]
public class PotionData : ItemData
{
    [Header("Curación")]
    [Tooltip("Puntos de vida que recupera al usarla. Puede ser 0 si la poción solo da buff.")]
    public float healAmount = 30f;

    [Header("Buff temporal")]
    [Tooltip("Tipo de buff. None = solo cura.")]
    public BuffType buffType     = BuffType.None;
    [Tooltip("Magnitud del buff en tanto por uno (0.20 = +20%).")]
    [Range(0f, 2f)]
    public float    buffValue    = 0f;
    [Tooltip("Duración del buff en segundos. Ignorado si buffType = None.")]
    [Min(0f)]
    public float    buffDuration = 0f;

    // ─────────────────────────────────────────────
    // CONFIGURACIÓN AUTOMÁTICA
    // ─────────────────────────────────────────────
    private void OnEnable()
    {
        // Forzar tipo correcto (útil al crear el asset por primera vez)
        itemType  = ItemType.Potion;
        stackable = true;
        if (maxStack < 2) maxStack = 100;
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    /// <summary>Texto descriptivo del buff para mostrar en UI.</summary>
    public string GetBuffDescription()
    {
        if (buffType == BuffType.None || buffDuration <= 0f) return "";
        string typeName = buffType switch
        {
            BuffType.SpeedBoost  => "Velocidad",
            BuffType.DamageBoost => "Daño",
            BuffType.DefenseBoost => "Defensa",
            _                    => buffType.ToString()
        };
        return $"{typeName} +{buffValue * 100f:F0}% durante {buffDuration:F0}s";
    }
}
