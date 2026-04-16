using UnityEngine;

/// <summary>
/// Arma de corto alcance: espadas, dagas.
/// Alcance reducido pero alta velocidad de ataque.
///
/// Crear via: Assets > Create > TFG/Armas/Corto Alcance
/// </summary>
[CreateAssetMenu(menuName = "TFG/Armas/Corto Alcance", fileName = "NewShortWeapon")]
public class ShortRangeWeaponData : WeaponData
{
    [Header("Corto Alcance")]
    [Tooltip("Número de golpes del combo antes de resetear.")]
    public int comboHits = 2;

    // Alcance fijo para armas cortas
    public override float  AttackRange    => 1.5f;
    public override string WeaponTypeName => "Corto alcance";
}
