/*  Nombre:      LootReceivedPopupUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       22/05/2026
 *  Descripcion: Popup que aparece al abrir un cofre mostrando el ítem obtenido
 *               con su icono grande en el centro, nombre y cantidad.
 *               Se auto-oculta tras 'displayDuration' segundos.
 *
 *  Si hay varios ítems, muestra el primero con imagen y el resto solo en texto.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LootReceivedPopupUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    public static LootReceivedPopupUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    [Header("Panel (empieza desactivado)")]
    public GameObject panelRoot;

    [Header("Imagen del ítem (grande, centro)")]
    public Image itemIcon;

    [Header("Textos")]
    public TMP_Text itemNameText;   // nombre del ítem principal
    public TMP_Text itemQtyText;    // "x2", "x1", etc.
    public TMP_Text extraItemsText; // ítems adicionales si hay más de uno (puede ser null)

    [Header("Botones (se conectan solos — no hace falta OnClick() en el Inspector)")]
    public Button closeBtn;           // cierra el popup
    public Button openInventoryBtn;   // abre el inventario y cierra el popup

    [Header("Duración del popup (si no se cierra manualmente)")]
    public float displayDuration = 3f;

    // ─────────────────────────────────────────────
    private Coroutine _hideCoroutine;

    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Conectar botones por código — no necesitas asignar nada en OnClick() del Inspector
        if (closeBtn         != null) closeBtn.onClick.AddListener(Hide);
        if (openInventoryBtn != null) openInventoryBtn.onClick.AddListener(OnOpenInventory);

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    public void Show(List<(ItemData item, int qty)> receivedItems)
    {
        if (receivedItems == null || receivedItems.Count == 0) return;

        // ── Ítem principal (el primero de la lista) ──
        (ItemData mainItem, int mainQty) = receivedItems[0];

        if (itemIcon != null)
        {
            itemIcon.sprite  = mainItem?.icon;
            itemIcon.enabled = mainItem?.icon != null;
            itemIcon.color   = Color.white;
        }

        if (itemNameText != null)
            itemNameText.text = mainItem != null ? mainItem.itemName : "Objeto";

        if (itemQtyText != null)
        {
            string desc = mainItem != null && !string.IsNullOrEmpty(mainItem.description)
                          ? mainItem.description : "";
            // Si hay más de 1 unidad, añadir la cantidad al final de la descripción
            itemQtyText.text = mainQty > 1 ? $"{desc}  (x{mainQty})" : desc;
        }

        // ── Ítems extra (si hay más de uno en el cofre) ──
        if (extraItemsText != null)
        {
            if (receivedItems.Count > 1)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 1; i < receivedItems.Count; i++)
                {
                    (ItemData extra, int qty) = receivedItems[i];
                    if (extra == null) continue;
                    sb.AppendLine(qty > 1 ? $"+ {extra.itemName}  x{qty}" : $"+ {extra.itemName}");
                }
                extraItemsText.text = sb.ToString().TrimEnd();
                extraItemsText.gameObject.SetActive(true);
            }
            else
            {
                extraItemsText.gameObject.SetActive(false);
            }
        }

        // ── Mostrar y arrancar timer de ocultado ──
        if (panelRoot != null) panelRoot.SetActive(true);

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    // ─────────────────────────────────────────────
    // BOTONES
    // ─────────────────────────────────────────────

    private void OnOpenInventory()
    {
        Hide();
        InventoryUI.Instance?.Open();
    }

    // ─────────────────────────────────────────────
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        Hide();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }
    }
}
