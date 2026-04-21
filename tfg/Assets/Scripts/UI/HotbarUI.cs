using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD hotbar de acceso rápido. Siempre visible durante el juego.
///
/// Slots 1–3 (hotbar 0-2) → slots de ARMA del inventario (siempre 0-2, fijos)
///   Tecla 1/2/3 → equipa el arma de ese slot en PlayerCombat
///
/// Slots 4–6 (hotbar 3-5) → slots GENERALES configurables (por defecto 3,4,5)
///   Tecla 4/5/6 → usa el ítem directamente
///   Shift+clic en inventario → reasigna ese slot al hotbar (cicla 4→5→6→4…)
///
/// SETUP EN UNITY (debajo del HUD Canvas):
///   HotbarRoot  (este script, HorizontalLayoutGroup opcional)
///   ├─ SlotHB_0 … SlotHB_2  (morado — armas)
///   └─ SlotHB_3 … SlotHB_5  (gris   — general)
///
/// Arrastra manualmente en el Inspector los 6 elementos de cada array (orden 0-5).
/// </summary>
public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // REFERENCIAS — arrastrar en Inspector (orden 0-5)
    // ─────────────────────────────────────────────
    [Header("Fondos (6 slots en orden 0-5)")]
    public Image[] backgrounds = new Image[6];

    [Header("Bordes de selección (6 slots, desactivados por defecto)")]
    public Image[] selectionBorders = new Image[6];

    [Header("Iconos del ítem (6 slots) — arrastrar la Image que muestra el sprite del item")]
    public Image[] icons = new Image[6];

    [Header("Textos de cantidad (6 slots)")]
    public TMP_Text[] quantities = new TMP_Text[6];

    [Header("Etiquetas de tecla (6 slots)")]
    public TMP_Text[] keyLabels = new TMP_Text[6];

    // ─────────────────────────────────────────────
    // COLORES
    // ─────────────────────────────────────────────
    [Header("Colores")]
    public Color colorWeapon  = new Color(0.45f, 0.10f, 0.65f, 1f);
    public Color colorGeneral = new Color(0.18f, 0.18f, 0.18f, 1f);

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────

    // Mapeado hotbarIndex → inventoryIndex
    // Slots 0-2 siempre fijos (armas). Slots 3-5 configurables con AssignGeneralSlot.
    private readonly int[] _invMapping = { 0, 1, 2, 3, 4, 5 };

    // Próximo slot general del hotbar que se sobreescribirá al pinear (cicla 0→1→2→0)
    private int _nextGeneralPin = 0;

    private int          _activeWeaponSlot = 0;
    private PlayerCombat _combat;
    private PlayerHealth _health;

    private static readonly KeyCode[] HotbarKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6
    };

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private bool _subscribed = false;

    private void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            if (backgrounds[i]      != null) backgrounds[i].color = i < 3 ? colorWeapon : colorGeneral;
            if (selectionBorders[i] != null) selectionBorders[i].gameObject.SetActive(false);
            if (keyLabels[i]        != null) keyLabels[i].text    = (i + 1).ToString();
            // Icono vacío al inicio → desactivar
            if (icons[i]            != null) icons[i].gameObject.SetActive(false);
            if (quantities[i]       != null) quantities[i].gameObject.SetActive(false);
        }

        TrySubscribeInventory();
    }

    private void TrySubscribeInventory()
    {
        if (_subscribed || Inventory.Instance == null) return;
        Inventory.Instance.OnSlotChanged += OnInventorySlotChanged;
        _subscribed = true;

        _combat = FindObjectOfType<PlayerCombat>();
        _health = FindObjectOfType<PlayerHealth>();

        RefreshAll();
        EquipWeaponSlot(0);
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnSlotChanged -= OnInventorySlotChanged;
    }

    private void Update()
    {
        // Suscribirse en cuanto el Inventory esté disponible (Player instanciado tarde)
        TrySubscribeInventory();

        if (_combat == null) _combat = FindObjectOfType<PlayerCombat>();
        if (_health == null) _health = FindObjectOfType<PlayerHealth>();

        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return;

        for (int i = 0; i < HotbarKeys.Length; i++)
        {
            if (Input.GetKeyDown(HotbarKeys[i]))
                ActivateHotbarSlot(i);
        }
    }

    // ─────────────────────────────────────────────
    // ASIGNACIÓN DINÁMICA (Shift+clic en inventario)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Pinea un slot de inventario (cualquier índice 3-8) al siguiente hueco
    /// general del hotbar (cicla teclas 4 → 5 → 6 → 4…).
    /// Llamado por InventoryUI cuando el jugador hace Shift+clic en un slot general.
    /// </summary>
    public void AssignGeneralSlot(int inventorySlot)
    {
        int hotbarIdx      = 3 + _nextGeneralPin;
        int prev           = _invMapping[hotbarIdx];
        _invMapping[hotbarIdx] = inventorySlot;
        _nextGeneralPin    = (_nextGeneralPin + 1) % 3;

        RefreshSlot(hotbarIdx);
        Debug.Log($"[HotbarUI] Tecla {hotbarIdx + 1} → inventario {inventorySlot} (antes: {prev})");
    }

    // ─────────────────────────────────────────────
    // ACTIVACIÓN
    // ─────────────────────────────────────────────
    private void ActivateHotbarSlot(int hotbarIndex)
    {
        if (hotbarIndex < 3)
        {
            // Al pulsar arma: limpiar los bordes generales (4-6)
            for (int i = 3; i < 6; i++)
                if (selectionBorders[i] != null)
                    selectionBorders[i].gameObject.SetActive(false);

            EquipWeaponSlot(hotbarIndex);
        }
        else
        {
            // Al pulsar general: actualizar solo los bordes generales, no tocar los de arma
            for (int i = 3; i < 6; i++)
                if (selectionBorders[i] != null)
                    selectionBorders[i].gameObject.SetActive(i == hotbarIndex);

            UseItemSlot(hotbarIndex);
        }
    }

    public void EquipWeaponSlot(int hotbarSlot)
    {
        if (_combat == null || Inventory.Instance == null) return;

        // Actualizar borde de selección SIEMPRE, aunque el slot esté vacío
        for (int i = 0; i < 3; i++)
            if (selectionBorders[i] != null)
                selectionBorders[i].gameObject.SetActive(i == hotbarSlot);

        int invSlot = _invMapping[hotbarSlot];
        Inventory.Slot slot = Inventory.Instance.GetSlot(invSlot);

        // No cambiar el arma equipada si el slot está vacío
        if (slot.IsEmpty) return;

        _activeWeaponSlot = hotbarSlot;
        _combat.SetActiveWeapon(slot.item as WeaponData);
    }

    private void UseItemSlot(int hotbarIndex)
    {
        if (Inventory.Instance == null) return;
        int invSlot = _invMapping[hotbarIndex];
        Inventory.Slot slot = Inventory.Instance.GetSlot(invSlot);
        if (slot.IsEmpty) return;

        // Escudo de maná: lógica especial (cooldown + durabilidad)
        if (slot.item is ManaShieldData shield)
        {
            UseManaShield(shield, invSlot);
            return;
        }

        ItemData used = Inventory.Instance.UseItem(invSlot);
        if (used == null) return;
        ApplyItemEffect(used);
    }

    private void UseManaShield(ManaShieldData shield, int invSlot)
    {
        if (_health == null) return;

        bool activated = _health.TryActivateManaShield(shield.InvincibilityDuration, shield.Cooldown);
        if (!activated) return; // En cooldown, no consumir durabilidad

        bool broken = shield.ConsumeDurability();
        if (broken)
            Inventory.Instance.RemoveItem(invSlot);
        else
            Inventory.Instance.NotifySlotChanged(invSlot);

        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.RegisterItemUsed();

        Debug.Log($"[HotbarUI] Escudo de maná activado | Durabilidad: {shield.DurabilityText}");
    }

    private void ApplyItemEffect(ItemData used)
    {
        if (_health == null) return;

        switch (used.itemType)
        {
            case ItemType.Potion:
                float heal = used is PotionData pd ? pd.healAmount : 30f;
                _health.Heal(heal);
                Debug.Log($"[HotbarUI] Poción usada: +{heal:F0} HP");
                break;
        }

        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.RegisterItemUsed();
    }

    // ─────────────────────────────────────────────
    // REFRESCO
    // ─────────────────────────────────────────────
    private void OnInventorySlotChanged(int inventoryIndex)
    {
        for (int i = 0; i < 6; i++)
        {
            if (_invMapping[i] != inventoryIndex) continue;
            RefreshSlot(i);
            // Si era el arma activa, re-equipar con los nuevos datos
            if (i < 3 && i == _activeWeaponSlot)
                EquipWeaponSlot(i);
        }
    }

    private void RefreshAll()
    {
        for (int i = 0; i < 6; i++) RefreshSlot(i);
    }

    private void RefreshSlot(int hotbarIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= 6 || Inventory.Instance == null) return;

        int invSlot         = _invMapping[hotbarIndex];
        Inventory.Slot slot = Inventory.Instance.GetSlot(invSlot);
        bool empty          = slot.IsEmpty;

        // ICONO: si está vacío, desactivar el GameObject entero (no deja imagen blanca)
        if (icons[hotbarIndex] != null)
        {
            icons[hotbarIndex].gameObject.SetActive(!empty);
            if (!empty)
            {
                icons[hotbarIndex].sprite  = slot.item.icon;
                icons[hotbarIndex].color   = Color.white;
                icons[hotbarIndex].enabled = true;
            }
        }

        // CANTIDAD: solo si stackable y > 1
        if (quantities[hotbarIndex] != null)
        {
            bool showQty = !empty && slot.item.stackable && slot.quantity > 1;
            quantities[hotbarIndex].gameObject.SetActive(showQty);
            if (showQty) quantities[hotbarIndex].text = $"x{slot.quantity}";
        }
    }
}
