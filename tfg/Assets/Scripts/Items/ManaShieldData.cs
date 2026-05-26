/*  Nombre:      ManaShieldData.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       21/04/2026
 *  Descripcion: ScriptableObject para el escudo de maná que otorga invencibilidad temporal.
 */
using UnityEngine;

/// <summary>
/// Escudo de maná: otorga invencibilidad temporal al usarse.
/// La durabilidad baja en cada uso; al llegar a 0 el ítem se rompe.
///
/// Crear via: Assets > Create > TFG > Escudo de Maná
///
/// Stats según rareza:
///   Común : 5s invencibilidad  | CD 30s  | -25 durabilidad/uso (4 usos)
///   Épico  : 20s invencibilidad | CD 90s  | -10 durabilidad/uso (10 usos)
/// </summary>
[CreateAssetMenu(menuName = "TFG/Escudo de Maná", fileName = "NewManaShield")]
public class ManaShieldData : ItemData
{
    [Header("Rareza")]
    public Rarity rarity = Rarity.Common;

    // ── Stats derivados de la rareza ──────────────────
    public float InvincibilityDuration => rarity == Rarity.Epic ? 20f : 5f;
    public float Cooldown              => rarity == Rarity.Epic ? 90f : 30f;
    public int   DurabilityPerUse      => rarity == Rarity.Epic ? 10  : 25;
    public int   MaxDurability         => 100;

    /// <summary>Durabilidad actual (runtime). Se inicializa al clonar el SO.</summary>
    [HideInInspector] public int currentDurability;

    private void OnEnable()
    {
        itemType          = ItemType.ManaShield;
        stackable         = false;
        currentDurability = MaxDurability;
    }

    /// <summary>
    /// Consume una carga de durabilidad.
    /// Devuelve true si el escudo queda roto (durabilidad <= 0).
    /// </summary>
    public bool ConsumeDurability()
    {
        currentDurability -= DurabilityPerUse;
        return currentDurability <= 0;
    }

    /// <summary>Texto para mostrar en UI: "25 / 100".</summary>
    public string DurabilityText => $"{Mathf.Max(currentDurability, 0)} / {MaxDurability}";
}
