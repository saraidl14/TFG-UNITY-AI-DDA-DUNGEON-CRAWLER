/*  Nombre:      ItemData.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: ScriptableObject base para todos los ítems del juego (nombre, icono, tipo).
 */
using UnityEngine;

/// <summary>
/// Tipo de item. Determina en qué slots puede colocarse.
///   Weapon  → solo en los 3 slots morados (índices 0-2)
///   General → en los 6 slots normales (índices 3-8)
/// </summary>
public enum ItemType
{
    Weapon,
    Potion,
    Ammo,           // Flechas, bolts, etc. — stackables, consumidos al disparar
    ManaShield,
    StaminaShield,
    Other
}

/// <summary>
/// ScriptableObject base para cualquier ítem del juego.
/// Crear via: Assets > Create > TFG > Item
///
/// Subclases concretas (WeaponData, PotionData, etc.) añaden
/// stats propios pero heredan este bloque de identidad.
/// </summary>
[CreateAssetMenu(menuName = "TFG/Item", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    [Header("Identidad")]
    public string itemName        = "Item";
    public Sprite icon            = null;
    [TextArea] public string description = "";

    [Header("Tipo")]
    public ItemType itemType = ItemType.Other;

    [Header("Cantidad")]
    [Tooltip("True si este ítem se puede apilar (pociones). False si ocupa un slot entero (armas).")]
    public bool stackable = false;
    [Tooltip("Máximo de unidades por slot. Solo relevante si stackable = true.")]
    public int  maxStack  = 1;
}
