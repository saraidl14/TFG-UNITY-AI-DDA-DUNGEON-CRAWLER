using UnityEngine;

/// <summary>
/// ScriptableObject para munición (flechas, bolts, etc.).
/// Es un ítem stackable que se consume automáticamente al disparar
/// con un arma de largo alcance (LongRangeWeaponData).
///
/// Crear via: Assets > Create > TFG > Munición
///
/// SETUP:
///   - itemType  = Ammo
///   - stackable = true
///   - maxStack  = 99
/// </summary>
[CreateAssetMenu(menuName = "TFG/Municion", fileName = "NewAmmo")]
public class AmmoData : ItemData
{
    private void OnEnable()
    {
        itemType  = ItemType.Ammo;
        stackable = true;
        if (maxStack < 2) maxStack = 99;
    }
}
