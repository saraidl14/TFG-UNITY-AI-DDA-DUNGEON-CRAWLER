/*  Nombre:      LongRangeWeaponData.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: ScriptableObject para armas de largo alcance con proyectil y munición.
 */
using UnityEngine;

/// <summary>
/// Arma de largo alcance: arcos, ballestas.
/// Dispara proyectiles. Tiene sistema de munición.
///
/// Crear via: Assets > Create > TFG/Armas/Largo Alcance
/// </summary>
[CreateAssetMenu(menuName = "TFG/Armas/Largo Alcance", fileName = "NewLongWeapon")]
public class LongRangeWeaponData : WeaponData
{
    [Header("Largo Alcance")]
    [Tooltip("Prefab del proyectil que se instancia al disparar.")]
    public GameObject projectilePrefab;

    [Tooltip("Velocidad del proyectil en m/s.")]
    public float projectileSpeed = 15f;

    [Tooltip("Munición máxima por carga.")]
    public int maxAmmo = 20;

    /// <summary>Munición actual (runtime, se resetea al recargar).</summary>
    [HideInInspector] public int currentAmmo;

    protected override void OnEnable()
    {
        base.OnEnable();
        currentAmmo = maxAmmo;
    }

    public bool HasAmmo    => currentAmmo > 0;
    public void ConsumeAmmo() { if (currentAmmo > 0) currentAmmo--; }
    public void Reload()      { currentAmmo = maxAmmo; }

    public override float  AttackRange    => 10f;
    public override string WeaponTypeName => "Largo alcance";
}
