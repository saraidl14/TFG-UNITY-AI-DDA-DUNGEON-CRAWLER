using UnityEngine;
using TMPro;

/// <summary>
/// Panel de inventario del jugador.
/// Tecla I → abre/cierra. Pausa el tiempo mientras está abierto.
///
/// Al clicar un slot:
///   - Slot vacío   → deselecciona y cierra el panel de detalle
///   - Slot con ítem → selecciona (resalta en dorado) y abre ItemDetailPanel
///
/// Estructura del Canvas en Unity:
///   INVENTORY  (este script)
///   ├─ InvPanel  (Image fondo semitransparente) ← asignar a panelRoot
///   │   ├─ Title          (TMP_Text "INVENTARIO")
///   │   ├─ WeaponLabel    (TMP_Text "⚔ Armas")
///   │   ├─ GeneralLabel   (TMP_Text "🎒 Objetos")
///   │   └─ SlotsContainer (GridLayoutGroup, 3 col)
///   │       ├─ Slot0 … Slot2  (InventorySlotUI — morado)
///   │       └─ Slot3 … Slot8  (InventorySlotUI — gris)
///   └─ ItemDetailPanel  (ItemDetailPanel) ← asignar a detailPanel
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON  (solo lectura, para que PlayerCombat consulte IsOpen)
    // ─────────────────────────────────────────────
    public static InventoryUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────
    [Header("Panel raíz")]
    public GameObject panelRoot;

    [Header("Slots (arrastra los 9 en orden 0-8)")]
    public InventorySlotUI[] slotObjects = new InventorySlotUI[Inventory.TOTAL_SLOTS];

    [Header("Panel de detalle")]
    public ItemDetailPanel detailPanel;

    [Header("Tecla toggle")]
    public KeyCode toggleKey = KeyCode.I;

    [Header("Etiquetas opcionales")]
    public TMP_Text weaponLabel;
    public TMP_Text generalLabel;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private bool _isOpen        = false;
    private int  _selectedSlot  = -1;

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
        // Inicializar slots y suscribir clic
        for (int i = 0; i < slotObjects.Length; i++)
        {
            if (slotObjects[i] == null) continue;
            slotObjects[i].Init(i);
            int idx = i; // captura por valor para el lambda
            slotObjects[i].OnSlotClicked += HandleSlotClicked;
        }

        // Suscribirse al inventario para refrescarse cuando cambien los datos
        if (Inventory.Instance != null)
            Inventory.Instance.OnSlotChanged += RefreshSlot;

        // Etiquetas opcionales
        if (weaponLabel  != null) weaponLabel.text  = "⚔  Armas";
        if (generalLabel != null) generalLabel.text = "🎒  Objetos";

        SetPanelVisible(false);
        if (detailPanel != null) detailPanel.Hide();
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnSlotChanged -= RefreshSlot;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        // Cerrar con Escape
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // ─────────────────────────────────────────────
    // ABRIR / CERRAR
    // ─────────────────────────────────────────────
    public void Toggle() { if (_isOpen) Close(); else Open(); }

    public void Open()
    {
        _isOpen        = true;
        Time.timeScale = 0f;
        SetPanelVisible(true);
        RefreshAll();

        // Mostrar y desbloquear el cursor para poder clicar los slots
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("[InventoryUI] Abierto.");
    }

    public void Close()
    {
        _isOpen        = false;
        Time.timeScale = 1f;
        SetPanelVisible(false);
        Deselect();
        if (detailPanel != null) detailPanel.Hide();

        // Restaurar cursor al estado de juego (oculto y bloqueado en el centro)
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[InventoryUI] Cerrado.");
    }

    // ─────────────────────────────────────────────
    // SELECCIÓN DE SLOT
    // ─────────────────────────────────────────────
    private void HandleSlotClicked(int slotIndex)
    {
        if (Inventory.Instance == null) return;

        Inventory.Slot slot = Inventory.Instance.GetSlot(slotIndex);

        // Shift+clic en slot general con ítem → pinear al hotbar (teclas 4/5/6)
        if (Input.GetKey(KeyCode.LeftShift)
            && !Inventory.IsWeaponSlot(slotIndex)
            && !slot.IsEmpty
            && HotbarUI.Instance != null)
        {
            HotbarUI.Instance.AssignGeneralSlot(slotIndex);
            return;
        }

        // Clic en slot vacío → deseleccionar
        if (slot.IsEmpty)
        {
            Deselect();
            if (detailPanel != null) detailPanel.Hide();
            return;
        }

        // Clic en el slot ya seleccionado → deseleccionar (toggle)
        if (_selectedSlot == slotIndex)
        {
            Deselect();
            if (detailPanel != null) detailPanel.Hide();
            return;
        }

        // Seleccionar nuevo slot
        Deselect(); // quitar selección anterior
        _selectedSlot = slotIndex;
        if (slotObjects[slotIndex] != null)
            slotObjects[slotIndex].SetSelected(true);

        // Abrir panel de detalle
        if (detailPanel != null)
            detailPanel.Show(slotIndex);
    }

    private void Deselect()
    {
        if (_selectedSlot >= 0 && _selectedSlot < slotObjects.Length)
            if (slotObjects[_selectedSlot] != null)
                slotObjects[_selectedSlot].SetSelected(false);
        _selectedSlot = -1;
    }

    // ─────────────────────────────────────────────
    // REFRESCO
    // ─────────────────────────────────────────────
    private void RefreshAll()
    {
        for (int i = 0; i < slotObjects.Length; i++)
            if (slotObjects[i] != null) slotObjects[i].Refresh();
    }

    private void RefreshSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotObjects.Length)
            if (slotObjects[slotIndex] != null)
                slotObjects[slotIndex].Refresh();

        // Si el panel de detalle está mostrando este slot, actualizarlo también
        if (detailPanel != null && detailPanel.gameObject.activeSelf && _selectedSlot == slotIndex)
            detailPanel.Show(slotIndex);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null) panelRoot.SetActive(visible);
    }

    public bool IsOpen => _isOpen;
}
