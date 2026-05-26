/*  Nombre:      InventorySlotUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: Slot clicable y arrastrable del inventario con soporte para drag-and-drop.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

/// <summary>
/// Slot individual del inventario. Clicable y arrastrable.
///
/// CLIC IZQUIERDO  → selecciona el slot y avisa a InventoryUI (abre panel de detalle)
/// CLIC DERECHO    → descarta el ítem completo del slot
/// SHIFT+CLIC      → pinea este slot al siguiente hueco del hotbar general (teclas 4/5/6)
/// ARRASTRAR       → mueve el ítem a otro slot del inventario (swap)
///
/// Estructura del GameObject en Unity:
///   SlotX  (Image = background, Button, este script)
///   ├─ SelectionBorder  (Image) ← borde dorado, oculto por defecto
///   ├─ Icon             (Image) ← sprite del ítem,  Raycast Target OFF
///   └─ Quantity         (TMP_Text) ← "x3",          Raycast Target OFF
/// </summary>
[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────
    [Header("Referencias UI")]
    public Image    background;
    public Image    selectionBorder;
    public Image    icon;
    public TMP_Text quantityText;

    // ─────────────────────────────────────────────
    // COLORES
    // ─────────────────────────────────────────────
    [Header("Colores")]
    public Color colorWeaponSlot  = new Color(0.45f, 0.10f, 0.65f, 1f);
    public Color colorGeneralSlot = new Color(0.18f, 0.18f, 0.18f, 1f);
    public Color colorSelected    = new Color(1f,    0.85f, 0.20f, 1f);
    public Color colorIconEmpty   = new Color(1f, 1f, 1f, 0.10f);

    // ─────────────────────────────────────────────
    // EVENTO
    // ─────────────────────────────────────────────
    public event Action<int> OnSlotClicked;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private int    _slotIndex;
    private bool   _isWeaponSlot;
    private Button _button;
    private Canvas _canvas;

    // Drag — compartido entre todos los slots (solo un drag activo a la vez)
    private static GameObject _dragGhost;

    // ─────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────
    public void Init(int slotIndex)
    {
        _slotIndex    = slotIndex;
        _isWeaponSlot = Inventory.IsWeaponSlot(slotIndex);
        _canvas       = GetComponentInParent<Canvas>();

        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => OnSlotClicked?.Invoke(_slotIndex));
        }

        if (background != null)
            background.color = _isWeaponSlot ? colorWeaponSlot : colorGeneralSlot;

        SetSelected(false);
        Refresh();
    }

    // ─────────────────────────────────────────────
    // SELECCIÓN
    // ─────────────────────────────────────────────
    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(selected);
            selectionBorder.color = colorSelected;
        }
    }

    // ─────────────────────────────────────────────
    // CLIC DERECHO → DESCARTAR
    // ─────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (Inventory.Instance == null) return;

        Inventory.Slot slot = Inventory.Instance.GetSlot(_slotIndex);
        if (slot.IsEmpty) return;

        string itemName = slot.item.itemName;
        Inventory.Instance.RemoveItem(_slotIndex);

        // Si el panel de detalle mostraba este slot, cerrarlo
        ItemDetailPanel panel = FindObjectOfType<ItemDetailPanel>();
        if (panel != null && panel.gameObject.activeSelf)
            panel.Hide();

        Debug.Log($"[InventorySlotUI] {itemName} descartado desde slot {_slotIndex} (clic derecho).");
    }

    // ─────────────────────────────────────────────
    // REFRESCO VISUAL
    // ─────────────────────────────────────────────
    public void Refresh()
    {
        if (Inventory.Instance == null) return;
        Inventory.Slot slot = Inventory.Instance.GetSlot(_slotIndex);

        if (slot.IsEmpty) ShowEmpty();
        else              ShowItem(slot.item, slot.quantity);
    }

    private void ShowEmpty()
    {
        if (icon != null)
        {
            icon.sprite  = null;
            icon.color   = colorIconEmpty;
            icon.enabled = false;
        }
        if (quantityText != null)
            quantityText.enabled = false;
    }

    private void ShowItem(ItemData item, int qty)
    {
        if (icon != null)
        {
            icon.enabled = true;
            icon.sprite  = item.icon;
            icon.color   = Color.white;
        }
        if (quantityText != null)
        {
            // Mostrar cantidad para todos los stackables (incluso x1, para saber cuántos quedan)
            bool showQty         = item.stackable;
            quantityText.enabled = showQty;
            quantityText.color   = Color.white;
            if (showQty) quantityText.text = $"x{qty}";
        }
    }

    // ─────────────────────────────────────────────
    // DRAG & DROP  —  reordenar slots del inventario
    // ─────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Inventory.Instance == null) return;
        Inventory.Slot slot = Inventory.Instance.GetSlot(_slotIndex);
        if (slot.IsEmpty || slot.item.icon == null) return;

        // Crear imagen fantasma que sigue al cursor
        _dragGhost = new GameObject("DragGhost");
        _dragGhost.transform.SetParent(_canvas.transform, false);
        _dragGhost.transform.SetAsLastSibling(); // encima de todo

        RectTransform rt = _dragGhost.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 60f);

        Image img = _dragGhost.AddComponent<Image>();
        img.sprite       = slot.item.icon;
        img.raycastTarget = false; // no interceptar raycasts (deja pasar al slot destino)

        // Atenuar el icono original mientras se arrastra
        if (icon != null) icon.color = new Color(1f, 1f, 1f, 0.3f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragGhost == null) return;

        // Mover el fantasma con el cursor
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPos
        );
        (_dragGhost.transform as RectTransform).anchoredPosition = localPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Destruir fantasma y restaurar icono
        if (_dragGhost != null) { Destroy(_dragGhost); _dragGhost = null; }
        Refresh();

        // Buscar el slot destino bajo el cursor
        if (eventData.pointerEnter == null) return;
        InventorySlotUI target = eventData.pointerEnter.GetComponentInParent<InventorySlotUI>();
        if (target == null || target == this) return;

        // Intercambiar ítems (Inventory valida compatibilidad de tipo)
        Inventory.Instance?.SwapSlots(_slotIndex, target._slotIndex);
    }
}
