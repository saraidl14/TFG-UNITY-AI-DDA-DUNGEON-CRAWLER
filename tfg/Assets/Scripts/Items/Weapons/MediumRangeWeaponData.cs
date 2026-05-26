/*  Nombre:      MediumRangeWeaponData.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/05/2026
 *  Descripcion: ScriptableObject para armas de medio alcance con knockback (lanzas).
 */
using UnityEngine;

/// <summary>
/// Arma de medio alcance: lanzas, bastones.
/// Equilibrio entre alcance y velocidad. Puede aplicar knockback.
///
/// Crear via: Assets > Create > TFG/Armas/Medio Alcance
/// </summary>
[CreateAssetMenu(menuName = "TFG/Armas/Medio Alcance", fileName = "NewMediumWeapon")]
public class MediumRangeWeaponData : WeaponData
{
    [Header("Medio Alcance")]
    [Tooltip("Fuerza de empuje aplicada al enemigo al golpear.")]
    public float knockbackForce = 3f;

    public override float  AttackRange    => 2.5f;
    public override string WeaponTypeName => "Medio alcance";
}
