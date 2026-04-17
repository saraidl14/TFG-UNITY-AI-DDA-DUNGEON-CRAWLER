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
/// Cada slot hijo necesita:
///   ├─ SelectionBorder  Image (dorado, desactivado)
///   ├─ Icon             Image, Raycast Target OFF
///   ├─ Quantity         TMP_Text, Raycast Target OFF
///   └─ KeyLabel         TMP_Text, Raycast Target OFF
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

    [Header("Iconos del ítem (6 slots)")]
    public Image[] icons = new Image[6];

    [Header("Textos de cantidad (6 slots)")]
    public TMP_Text[] quantities = new TMP_Text[6];

    [Header("Etiquetas de tecla (6 slots)")]
    public TMP_Text[] keyLabels = new TMP_Text[6];

    // ─────────────────────────────────────────────
    // COLORES
    // ─────────────────────────────────────────────
    [Header("Colores")]
    public Color colorWeapon    = new Color(0.45f, 0.10f, 0.65f, 1f);
    public Color colorGeneral   = new Color(0.18f, 0.18f, 0.18f, 1f);
    public Color colorIconEmpty = new Color(1f, 1f, 1f, 0.10f);

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

    private void Start()
    {
        _combat = FindObjectOfType<PlayerCombat>();
        _health = FindObjectOfType<PlayerHealth>();

        // Auto-configurar arrays desde los hijos si no están asignados en el Inspector
        AutoSetupFromChildren();

        if (Inventory.Instance != null)
            Inventory.Instance.OnSlotChanged += OnInventorySlotChanged;

        for (int i = 0; i < 6; i++)
        {
            if (backgrounds[i]      != null) backgrounds[i].color      = i < 3 ? colorWeapon : colorGeneral;
            if (selectionBorders[i] != null) selectionBorders[i].gameObject.SetActive(false);
            if (keyLabels[i]        != null) keyLabels[i].text         = (i + 1).ToString();
        }

        RefreshAll();
        EquipWeaponSlot(0);
    }

    /// <summary>
    /// Busca automáticamente los componentes hijos en orden.
    /// Cada hijo directo del HotbarRoot es un slot (0-5).
    /// Dentro de cada slot busca: Image (fondo), Image hijo "Icon",
    /// TMP_Text "Quantity", TMP_Text "KeyLabel", Image "SelectionBorder".
    /// </summary>
    private void AutoSetupFromChildren()
    {
        // Recoger los slots hijos directos (en orden)
        int childCount = Mathf.Min(transform.childCount, 6);

        for (int i = 0; i < childCount; i++)
        {
            Transform slot = transform.GetChild(i);

            // Fondo: Image en el propio slot
            if (backgrounds[i] == null)
                backgrounds[i] = slot.GetComponent<Image>();

            // Buscar hijos por nombre
            foreach (Transform child in slot)
            {
                string n = child.name.ToLower();

                if (icons[i] == null && n.Contains("icon"))
                    icons[i] = child.GetComponent<Image>();

                if (selectionBorders[i] == null && (n.Contains("border") || n.Contains("selection")))
                    selectionBorders[i] = child.GetComponent<Image>();

                if (quantities[i] == null && (n.Contains("quant") || n.Contains("qty") || n.Contains("cantidad")))
                    quantities[i] = child.GetComponent<TMP_Text>();

                if (keyLabels[i] == null && (n.Contains("key") || n.Contains("label")))
                    keyLabels[i] = child.GetComponent<TMP_Text>();
            }
        }
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnSlotChanged -= OnInventorySlotChanged;
    }

    private void Update()
    {
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
        int hotbarIdx = 3 + _nextGeneralPin;
        int prev      = _invMapping[hotbarIdx];
        _invMapping[hotbarIdx] = inventorySlot;
        _nextGeneralPin        = (_nextGeneralPin + 1) % 3;

        RefreshSlot(hotbarIdx);
        Debug.Log($"[HotbarUI] Tecla {hotbarIdx + 1} → inventario {inventorySlot} (antes: {prev})");
    }

    // ─────────────────────────────────────────────
    // ACTIVACIÓN
    // ─────────────────────────────────────────────
    private void ActivateHotbarSlot(int hotbarIndex)
    {
        if (hotbarIndex < 3)
            EquipWeaponSlot(hotbarIndex);
        else
            UseItemSlot(hotbarIndex);
    }

    public void EquipWeaponSlot(int hotbarSlot)
    {
        _activeWeaponSlot = hotbarSlot;

        for (int i = 0; i < 3; i++)
            if (selectionBorders[i] != null)
                selectionBorders[i].gameObject.SetActive(i == hotbarSlot);

        if (_combat == null || Inventory.Instance == null) return;
        int invSlot = _invMapping[hotbarSlot]; // siempre 0/1/2 para armas
        Inventory.Slot slot = Inventory.Instance.GetSlot(invSlot);
        _combat.SetActiveWeapon(slot.item as WeaponData);
    }

    private void UseItemSlot(int hotbarIndex)
    {
        if (Inventory.Instance == null) return;
        int invSlot = _invMapping[hotbarIndex];
        Inventory.Slot slot = Inventory.Instance.GetSlot(invSlot);
        if (slot.IsEmpty) return;

        ItemData used = Inventory.Instance.UseItem(invSlot);
        if (used == null) return;

        ApplyItemEffect(used);
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
        // Refrescar cualquier slot del hotbar que apunte a este slot de inventario
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
        int invSlot = _invMapping[hotbarIndex];
        Inventory.Slot slot = Inventory.Instance.GetSlot(invSlot);

        bool empty = slot.IsEmpty;

        if (icons[hotbarIndex] != null)
        {
            icons[hotbarIndex].enabled = !empty;
            icons[hotbarIndex].sprite  = empty ? null : slot.item.icon;
            icons[hotbarIndex].color   = empty ? colorIconEmpty : Color.white;
        }

        if (quantities[hotbarIndex] != null)
        {
            bool showQty = !empty && slot.item.stackable && slot.quantity > 1;
            quantities[hotbarIndex].enabled = showQty;
            if (showQty) quantities[hotbarIndex].text = $"x{slot.quantity}";
        }
    }
}
