using UnityEngine;

/// <summary>
/// Clase base para todas las armas del juego (ScriptableObject).
/// Hereda de ItemData (nombre, icono, descripción, tipo).
///
/// Jerarquía:
///   WeaponData  (esta clase — abstract, sin CreateAssetMenu)
///   ├─ ShortRangeWeaponData   (espadas, dagas)
///   ├─ MediumRangeWeaponData  (lanzas, bastones)
///   └─ LongRangeWeaponData    (arcos, ballestas)
///
/// IMPORTANTE — runtime vs. asset:
///   Los ScriptableObjects son assets compartidos. Para que cada arma
///   en el inventario tenga su propia durabilidad y nivel de mejora,
///   Inventory.AddItem() clona el SO con Instantiate() antes de guardarlo.
///   Así modificar currentDurability/upgradeLevel no afecta al asset original.
/// </summary>
public abstract class WeaponData : ItemData
{
    // ─────────────────────────────────────────────
    // STATS BASE
    // ─────────────────────────────────────────────

    [Header("Stats del arma")]
    [Tooltip("Daño base antes de aplicar nivel de mejora.")]
    public float baseDamage    = 10f;

    [Tooltip("Ataques por segundo.")]
    public float attackSpeed   = 1f;

    [Tooltip("Durabilidad máxima. Se reduce con cada uso.")]
    public float maxDurability = 100f;

    [Header("Rareza")]
    public Rarity rarity = Rarity.Common;

    // ─────────────────────────────────────────────
    // MODELO 3D EN MANO
    // ─────────────────────────────────────────────

    [Header("Modelo 3D (mano del jugador)")]
    [Tooltip("Prefab del modelo 3D que se muestra en la mano al equipar el arma.")]
    public GameObject weaponModelPrefab;

    [Tooltip("Posición local relativa al punto de anclaje (WeaponHolder). Ajustar por arma.")]
    public Vector3 modelLocalPosition = Vector3.zero;

    [Tooltip("Rotación local relativa al punto de anclaje. Ajustar por arma.")]
    public Vector3 modelLocalRotation = Vector3.zero;

    [Tooltip("Escala del modelo en mano (1 = tamaño original del prefab).")]
    public float modelLocalScale = 1f;

    // ─────────────────────────────────────────────
    // ESTADO RUNTIME (se modifica en la instancia clonada del inventario)
    // ─────────────────────────────────────────────

    /// <summary>Durabilidad actual. Se reduce al atacar; llegar a 0 rompe el arma.</summary>
    [HideInInspector] public float currentDurability;

    /// <summary>Nivel de mejora actual (0 = sin mejorar, máx 5).</summary>
    [HideInInspector] public int upgradeLevel = 0;

    public const int MAX_UPGRADE_LEVEL = 5;

    // ─────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────

    protected virtual void OnEnable()
    {
        // Forzar tipo Weapon para que Inventory lo coloque en slots morados
        itemType  = ItemType.Weapon;
        stackable = false;
        maxStack  = 1;

        // Inicializar durabilidad al valor máximo
        currentDurability = maxDurability;
    }

    // ─────────────────────────────────────────────
    // STATS EFECTIVOS (escalan con upgradeLevel)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Daño real: +10% por nivel de mejora, -30% si el arma está rota.
    /// </summary>
    public float GetEffectiveDamage()
    {
        float upgradeMult    = 1f + upgradeLevel * 0.10f;  // +10% por nivel
        float durabilityMult = IsBroken ? 0.70f : 1f;      // -30% si rota
        return baseDamage * upgradeMult * durabilityMult;
    }

    /// <summary>
    /// Velocidad de ataque real: +5% por nivel de mejora.
    /// </summary>
    public float GetEffectiveAttackSpeed()
    {
        return attackSpeed * (1f + upgradeLevel * 0.05f);   // +5% por nivel
    }

    /// <summary>
    /// Durabilidad máxima efectiva: +10% por nivel de mejora.
    /// Mejorar el arma también la hace más resistente.
    /// </summary>
    public float GetEffectiveMaxDurability()
    {
        return maxDurability * (1f + upgradeLevel * 0.10f); // +10% por nivel
    }

    /// <summary>
    /// Devuelve un resumen de lo que cambia al subir al siguiente nivel.
    /// Usado por el panel de detalle para mostrar el preview de mejora.
    /// </summary>
    public string GetUpgradePreview()
    {
        if (!CanUpgrade) return "Nivel máximo alcanzado";
        int    next     = upgradeLevel + 1;
        float  nextDmg  = baseDamage    * (1f + next * 0.10f);
        float  nextSpd  = attackSpeed   * (1f + next * 0.05f);
        float  nextDur  = maxDurability * (1f + next * 0.10f);
        return $"Daño {GetEffectiveDamage():F1} → {nextDmg:F1}  |  " +
               $"Vel. {GetEffectiveAttackSpeed():F2} → {nextSpd:F2}  |  " +
               $"Dur. {GetEffectiveMaxDurability():F0} → {nextDur:F0}";
    }

    // ─────────────────────────────────────────────
    // DURABILIDAD
    // ─────────────────────────────────────────────

    /// <summary>True si la durabilidad llegó a 0 (arma rota, daño reducido).</summary>
    public bool IsBroken => currentDurability <= 0f;

    /// <summary>Porcentaje de durabilidad restante [0, 1].</summary>
    public float DurabilityPercent => maxDurability > 0f
        ? Mathf.Clamp01(currentDurability / maxDurability)
        : 0f;

    /// <summary>
    /// Reduce la durabilidad con cada uso del arma.
    /// Llamar desde PlayerCombat al atacar.
    /// </summary>
    public void UseDurability(float amount = 1f)
    {
        currentDurability = Mathf.Max(0f, currentDurability - amount);
        if (IsBroken)
            Debug.Log($"[WeaponData] {itemName} se ha roto. Repárala para recuperar el daño completo.");
    }

    /// <summary>Restaura la durabilidad al máximo (al reparar).</summary>
    public void Repair()
    {
        currentDurability = maxDurability;
        Debug.Log($"[WeaponData] {itemName} reparada. Durabilidad: {currentDurability}/{maxDurability}");
    }

    // ─────────────────────────────────────────────
    // MEJORA
    // ─────────────────────────────────────────────

    /// <summary>True si el arma puede seguir mejorándose.</summary>
    public bool CanUpgrade => upgradeLevel < MAX_UPGRADE_LEVEL;

    /// <summary>
    /// Sube el nivel de mejora en 1 y escala el daño base un 10%.
    /// Llamar tras descontar el coste de monedas.
    /// </summary>
    public void Upgrade()
    {
        if (!CanUpgrade) return;
        upgradeLevel++;
        Debug.Log($"[WeaponData] {itemName} mejorada a nivel {upgradeLevel}. " +
                  $"Daño efectivo: {GetEffectiveDamage():F1}");
    }

    // ─────────────────────────────────────────────
    // COSTES (en monedas)
    // ─────────────────────────────────────────────

    /// <summary>Coste de mejorar = base × rareza × (nivel+1).</summary>
    public int GetUpgradeCost()
    {
        if (!CanUpgrade) return 0;
        return Mathf.RoundToInt(100 * RarityMultiplier() * (upgradeLevel + 1));
    }

    /// <summary>Coste de reparar = proporcional a la durabilidad perdida × rareza.</summary>
    public int GetRepairCost()
    {
        if (!IsBroken && currentDurability >= maxDurability) return 0;
        float missing = maxDurability - currentDurability;
        return Mathf.RoundToInt((missing / maxDurability) * 80f * RarityMultiplier());
    }

    private float RarityMultiplier() => rarity switch
    {
        Rarity.Common    => 1.0f,
        Rarity.Uncommon  => 1.5f,
        Rarity.Rare      => 2.5f,
        Rarity.Epic      => 4.0f,
        Rarity.Legendary => 7.0f,
        _                => 1.0f
    };

    // ─────────────────────────────────────────────
    // COLOR DE RAREZA (para la UI)
    // ─────────────────────────────────────────────

    public UnityEngine.Color GetRarityColor() => rarity switch
    {
        Rarity.Common    => UnityEngine.Color.white,
        Rarity.Uncommon  => new UnityEngine.Color(0.30f, 0.85f, 0.30f),
        Rarity.Rare      => new UnityEngine.Color(0.30f, 0.55f, 1.00f),
        Rarity.Epic      => new UnityEngine.Color(0.65f, 0.25f, 0.90f),
        Rarity.Legendary => new UnityEngine.Color(1.00f, 0.80f, 0.10f),
        _                => UnityEngine.Color.white
    };

    // ─────────────────────────────────────────────
    // ABSTRACTO — cada subclase define su tipo de alcance
    // ─────────────────────────────────────────────

    /// <summary>Rango de ataque en metros. Cada subclase define el suyo.</summary>
    public abstract float AttackRange { get; }

    /// <summary>Nombre del tipo de arma para mostrar en UI ("Corto", "Medio", "Largo").</summary>
    public abstract string WeaponTypeName { get; }
}
