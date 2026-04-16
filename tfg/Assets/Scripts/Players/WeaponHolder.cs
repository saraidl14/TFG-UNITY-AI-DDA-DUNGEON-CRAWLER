using UnityEngine;

/// <summary>
/// Gestiona el modelo 3D del arma equipada en la mano del jugador.
/// Debe ser hijo de la Camera del jugador (se mueve con la vista).
///
/// ──────────────────────────────────────────────────────────────
/// SETUP EN UNITY:
///
///   Player
///   └─ PlayerCamera  (Camera)
///       └─ WeaponHolder  (GameObject vacío — este script)
///              Posición local recomendada: (0.3, -0.25, 0.6)
///              Rotación local: (0, 0, 0)
///
/// Luego arrastra este WeaponHolder al campo "Weapon Holder" de PlayerCombat.
///
/// Cada arma puede tener offsets propios (modelLocalPosition / modelLocalRotation)
/// definidos en su WeaponData ScriptableObject para ajustar la pose.
/// ──────────────────────────────────────────────────────────────
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private GameObject   _currentModel  = null;
    private WeaponData   _currentWeapon = null;

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Instancia el modelo 3D del arma y lo coloca en la mano.
    /// Destruye el modelo anterior si lo hubiera.
    /// </summary>
    public void ShowWeapon(WeaponData weapon)
    {
        // Si ya está mostrando exactamente este arma, no hacer nada
        if (weapon == _currentWeapon && _currentModel != null) return;

        ClearModel();
        _currentWeapon = weapon;

        if (weapon == null || weapon.weaponModelPrefab == null)
        {
            Debug.Log("[WeaponHolder] Sin modelo: arma nula o sin prefab asignado.");
            return;
        }

        // Instanciar como hijo de este transform (el WeaponHolder)
        _currentModel = Instantiate(weapon.weaponModelPrefab, transform);

        // Aplicar los offsets definidos en el ScriptableObject del arma
        _currentModel.transform.localPosition = weapon.modelLocalPosition;
        _currentModel.transform.localRotation = Quaternion.Euler(weapon.modelLocalRotation);
        _currentModel.transform.localScale    = Vector3.one * Mathf.Max(0.01f, weapon.modelLocalScale);

        // Eliminar cualquier Collider del modelo visible (para no interferir con la física)
        foreach (Collider col in _currentModel.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Eliminar cualquier Rigidbody (idem)
        foreach (Rigidbody rb in _currentModel.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        Debug.Log($"[WeaponHolder] Mostrando: {weapon.itemName}");
    }

    /// <summary>Oculta y destruye el modelo actual (jugador sin arma equipada).</summary>
    public void HideWeapon()
    {
        ClearModel();
        _currentWeapon = null;
        Debug.Log("[WeaponHolder] Sin arma equipada.");
    }

    // ─────────────────────────────────────────────
    // INTERNO
    // ─────────────────────────────────────────────
    private void ClearModel()
    {
        if (_currentModel != null)
        {
            Destroy(_currentModel);
            _currentModel = null;
        }
    }

    /// <summary>El arma cuyo modelo está mostrando actualmente.</summary>
    public WeaponData CurrentWeapon => _currentWeapon;
}
