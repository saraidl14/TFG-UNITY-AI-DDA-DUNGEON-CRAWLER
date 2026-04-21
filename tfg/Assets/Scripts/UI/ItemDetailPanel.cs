using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel lateral que aparece al seleccionar un ítem del inventario.
///
/// Weapon selected  → muestra imagen grande + stats + botones Mejorar / Reparar
/// General selected → muestra imagen grande + descripción + botón Usar
/// Vacío selected   → se oculta
///
/// Estructura del GameObject en Unity:
///   ItemDetailPanel  (este script, Image fondo)
///   ├─ ItemIcon         (Image)       ← imagen grande del ítem
///   ├─ ItemName         (TMP_Text)
///   ├─ ItemDescription  (TMP_Text)
///   ├─ CoinsText        (TMP_Text)    ← monedas actuales del jugador
///   ├─ StatsContainer   (GameObject)
///   │   ├─ StatLine1    (TMP_Text)
///   │   ├─ StatLine2    (TMP_Text)
///   │   └─ StatLine3    (TMP_Text)
///   ├─ WeaponSection    (GameObject)  ← solo visible para armas
///   │   ├─ DurabilityBar (Slider)
///   │   ├─ UpgradePreviewText (TMP_Text)
///   │   ├─ UpgradeBtn   (Button)
///   │   └─ RepairBtn    (Button)
///   ├─ GeneralSection   (GameObject)  ← solo visible para no-armas
///   │   └─ UseBtn       (Button)
///   └─ CloseBtn         (Button)
/// </summary>
public class ItemDetailPanel : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────
    [Header("Cabecera")]
    public Image    itemIcon;
    public TMP_Text itemName;
    public TMP_Text itemDescription;

    [Header("Monedas del jugador")]
    [Tooltip("TMP que muestra las monedas actuales. Se actualiza al abrir el panel y al gastar.")]
    public TMP_Text coinsText;

    [Header("Stats (líneas de texto libres)")]
    public TMP_Text statLine1;
    public TMP_Text statLine2;
    public TMP_Text statLine3;
    public TMP_Text upgradePreviewText;

    [Header("Sección Arma")]
    public GameObject weaponSection;
    public Slider     durabilityBar;
    public TMP_Text   durabilityText;
    public Button     upgradeBtn;
    public TMP_Text   upgradeBtnLabel;
    public Button     repairBtn;
    public TMP_Text   repairBtnLabel;

    [Header("Sección General")]
    public GameObject generalSection;
    public Button     useBtn;
    public TMP_Text   useBtnLabel;

    [Header("Cerrar")]
    public Button closeBtn;

    [Header("Costes de mejora/reparación")]
    public int upgradeCostBase = 150;
    public int repairCostBase  = 50;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private int      _currentSlot = -1;
    private ItemData _currentItem = null;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (closeBtn   != null) closeBtn.onClick.AddListener(Hide);
        if (upgradeBtn != null) upgradeBtn.onClick.AddListener(OnUpgrade);
        if (repairBtn  != null) repairBtn.onClick.AddListener(OnRepair);
        if (useBtn     != null) useBtn.onClick.AddListener(OnUse);
    }

    // ─────────────────────────────────────────────
    // MOSTRAR / OCULTAR
    // ─────────────────────────────────────────────

    public void Show(int slotIndex)
    {
        if (Inventory.Instance == null) return;

        Inventory.Slot slot = Inventory.Instance.GetSlot(slotIndex);
        if (slot.IsEmpty) { Hide(); return; }

        _currentSlot = slotIndex;
        _currentItem = slot.item;

        gameObject.SetActive(true);
        RefreshCoins();
        Populate(slot.item, slot.quantity);
    }

    public void Hide()
    {
        _currentSlot = -1;
        _currentItem = null;
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // MONEDAS
    // ─────────────────────────────────────────────

    private void RefreshCoins()
    {
        if (coinsText == null) return;
        int coins = GameManager.Instance != null ? GameManager.Instance.SessionCoins : 0;
        coinsText.text = $"{coins} monedas";
    }

    // ─────────────────────────────────────────────
    // RELLENAR DATOS
    // ─────────────────────────────────────────────

    private void Populate(ItemData item, int qty)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite  = item.icon;
            itemIcon.enabled = item.icon != null;
        }
        if (itemName        != null) itemName.text        = item.itemName;
        if (itemDescription != null) itemDescription.text = item.description;

        bool isWeapon = item.itemType == ItemType.Weapon;
        if (weaponSection  != null) weaponSection.SetActive(isWeapon);
        if (generalSection != null) generalSection.SetActive(!isWeapon);

        ClearStats();

        if (isWeapon) PopulateWeapon(item);
        else          PopulateGeneral(item, qty);
    }

    private void PopulateWeapon(ItemData item)
    {
        WeaponData weapon = item as WeaponData;

        if (weapon == null)
        {
            SetStat(statLine1, "Tipo", "Arma");
            SetStat(statLine2, "", "");
            SetStat(statLine3, "", "");
            return;
        }

        if (itemName != null)
            itemName.color = weapon.GetRarityColor();

        SetStat(statLine1, "Tipo",        weapon.WeaponTypeName);
        SetStat(statLine2, "Daño",        $"{weapon.GetEffectiveDamage():F1}");
        SetStat(statLine3, "Vel. ataque", $"{weapon.GetEffectiveAttackSpeed():F2} ataques/s");

        if (upgradePreviewText != null)
        {
            upgradePreviewText.text    = weapon.CanUpgrade ? weapon.GetUpgradePreview() : "";
            upgradePreviewText.enabled = weapon.CanUpgrade;
        }

        float durMax = weapon.GetEffectiveMaxDurability();
        float durPct = durMax > 0f ? Mathf.Clamp01(weapon.currentDurability / durMax) : 0f;
        if (durabilityBar  != null) durabilityBar.value = durPct;
        if (durabilityText != null)
        {
            string aviso = weapon.IsBroken ? "  ⚠ ROTA" : "";
            durabilityText.text  = $"Durabilidad: {weapon.currentDurability:F0}/{durMax:F0}{aviso}";
            durabilityText.color = weapon.IsBroken ? UnityEngine.Color.red : UnityEngine.Color.white;
        }

        int upgCost = weapon.GetUpgradeCost();
        if (upgradeBtnLabel != null)
            upgradeBtnLabel.text = weapon.CanUpgrade
                ? $"Mejorar  Nv.{weapon.upgradeLevel} → {weapon.upgradeLevel + 1}  ({upgCost} monedas)"
                : "Nivel máximo";
        if (upgradeBtn != null)
            upgradeBtn.interactable = weapon.CanUpgrade;

        int repCost = weapon.GetRepairCost();
        if (repairBtnLabel != null)
            repairBtnLabel.text = repCost > 0
                ? $"Reparar  ({repCost} monedas)"
                : "En perfecto estado";
        if (repairBtn != null)
            repairBtn.interactable = repCost > 0;
    }

    private void PopulateGeneral(ItemData item, int qty)
    {
        SetStat(statLine1, "Cantidad", qty.ToString());

        switch (item.itemType)
        {
            case ItemType.Potion:
                if (item is PotionData potion)
                {
                    SetStat(statLine2, "Recupera", $"{potion.healAmount:F0} HP");
                    string buffDesc = potion.GetBuffDescription();
                    if (!string.IsNullOrEmpty(buffDesc))
                        SetStat(statLine3, "Buff", buffDesc);
                    else
                        SetStat(statLine3, "", "");
                }
                else
                {
                    SetStat(statLine2, "Efecto", "Recupera salud");
                    SetStat(statLine3, "", "");
                }
                if (useBtnLabel != null) useBtnLabel.text = "Usar poción";
                break;
            case ItemType.ManaShield:
                SetStat(statLine2, "Efecto", "Escudo de maná");
                SetStat(statLine3, "", "");
                if (useBtnLabel != null) useBtnLabel.text = "Activar escudo";
                break;
            case ItemType.StaminaShield:
                SetStat(statLine2, "Efecto", "Escudo de stamina");
                SetStat(statLine3, "", "");
                if (useBtnLabel != null) useBtnLabel.text = "Activar escudo";
                break;
            default:
                SetStat(statLine2, "", "");
                SetStat(statLine3, "", "");
                if (useBtnLabel != null) useBtnLabel.text = "Usar";
                break;
        }
    }

    // ─────────────────────────────────────────────
    // ACCIONES DE BOTONES
    // ─────────────────────────────────────────────

    private void OnUse()
    {
        if (_currentSlot < 0) return;

        ItemData used = Inventory.Instance.UseItem(_currentSlot);
        if (used == null) return;

        Debug.Log($"[ItemDetailPanel] Usando: {used.itemName}");

        PlayerHealth ph = FindObjectOfType<PlayerHealth>();
        if (ph != null)
        {
            switch (used.itemType)
            {
                case ItemType.Potion:
                    float heal = used is PotionData pd ? pd.healAmount : 30f;
                    ph.Heal(heal);
                    Debug.Log($"[ItemDetailPanel] Poción usada: +{heal:F0} HP");
                    break;
                case ItemType.StaminaShield:
                    Debug.Log("[ItemDetailPanel] Escudo de stamina activado (pendiente implementación)");
                    break;
                case ItemType.ManaShield:
                    if (used is ManaShieldData shield)
                    {
                        // Revertir el UseItem — el escudo gestiona su propio consumo
                        // (UseItem ya decrementó quantity, pero el escudo usa durabilidad, no quantity)
                        // → Restaurar el slot y usar la lógica de durabilidad
                        Inventory.Instance.AddItem(used, 1);

                        bool activated = ph.TryActivateManaShield(shield.InvincibilityDuration, shield.Cooldown);
                        if (activated)
                        {
                            int slotIdx = Inventory.Instance.FindItem(used);
                            bool broken = shield.ConsumeDurability();
                            if (broken && slotIdx >= 0) Inventory.Instance.RemoveItem(slotIdx);
                            else if (slotIdx >= 0)      Inventory.Instance.NotifySlotChanged(slotIdx);
                            Debug.Log($"[ItemDetailPanel] Escudo activado | {shield.DurabilityText}");
                        }
                    }
                    break;
            }
        }

        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.RegisterItemUsed();

        Inventory.Slot slot = Inventory.Instance.GetSlot(_currentSlot);
        if (slot.IsEmpty) Hide();
        else              Show(_currentSlot);
    }

    private void OnUpgrade()
    {
        if (_currentSlot < 0 || GameManager.Instance == null) return;
        WeaponData weapon = _currentItem as WeaponData;
        if (weapon == null || !weapon.CanUpgrade) return;

        int cost = weapon.GetUpgradeCost();
        if (!GameManager.Instance.SpendCoins(cost))
        {
            Debug.Log("[ItemDetailPanel] Monedas insuficientes para mejorar.");
            return;
        }

        weapon.Upgrade();
        Debug.Log($"[ItemDetailPanel] {weapon.itemName} mejorada a Nv.{weapon.upgradeLevel}. -{cost} monedas");
        RefreshCoins();
        Show(_currentSlot);
    }

    private void OnRepair()
    {
        if (_currentSlot < 0 || GameManager.Instance == null) return;
        WeaponData weapon = _currentItem as WeaponData;
        if (weapon == null) return;

        int cost = weapon.GetRepairCost();
        if (cost <= 0) return;

        if (!GameManager.Instance.SpendCoins(cost))
        {
            Debug.Log("[ItemDetailPanel] Monedas insuficientes para reparar.");
            return;
        }

        weapon.Repair();
        Debug.Log($"[ItemDetailPanel] {weapon.itemName} reparada. -{cost} monedas");
        RefreshCoins();
        Show(_currentSlot);
    }

    // ─────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────

    private void ClearStats()
    {
        SetStat(statLine1, "", "");
        SetStat(statLine2, "", "");
        SetStat(statLine3, "", "");
    }

    private static void SetStat(TMP_Text label, string key, string value)
    {
        if (label == null) return;
        label.text    = string.IsNullOrEmpty(key) ? "" : $"{key}: {value}";
        label.enabled = !string.IsNullOrEmpty(key);
    }
}
