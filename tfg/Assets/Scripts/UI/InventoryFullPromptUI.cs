/*  Nombre:      InventoryFullPromptUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: Panel modal que avisa al jugador cuando intenta abrir un cofre con el inventario lleno.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel modal que aparece cuando el jugador intenta abrir un cofre con el inventario lleno.
/// Muestra qué ítems contiene el cofre y da opciones para liberar espacio.
///
/// SETUP EN UNITY:
///   InventoryFull   (root, SIEMPRE ACTIVO, este script)
///   └─ InvFul       (panel visual hijo — arrastrar a panelContent — empieza DESACTIVADO)
///       ├─ Title    (TMP_Text)  → titleText
///       ├─ Items    (TMP_Text)  → itemsText
///       ├─ Open     (Button)   → openInventoryBtn
///       └─ Close    (Button)   → closeBtn
///
/// IMPORTANTE: el root "InventoryFull" debe estar ACTIVADO en la escena para que
/// Awake() registre el singleton. El panel hijo "InvFul" empieza desactivado.
/// </summary>
public class InventoryFullPromptUI : MonoBehaviour
{
    public static InventoryFullPromptUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────
    [Header("Panel hijo que se muestra/oculta (arrastrar InvFul aquí)")]
    public GameObject panelContent;

    [Header("Textos (dentro de InvFul)")]
    public TMP_Text titleText;
    public TMP_Text itemsText;

    [Header("Botones (dentro de InvFul)")]
    public Button openInventoryBtn;
    public Button closeBtn;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private System.Action _onInventoryFreed;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // El root SIEMPRE está activo → Awake se ejecuta → Instance queda registrado
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (openInventoryBtn != null) openInventoryBtn.onClick.AddListener(OnOpenInventory);
        if (closeBtn         != null) closeBtn.onClick.AddListener(OnIgnore);

        // Ocultar el panel hijo (no el root)
        if (panelContent != null) panelContent.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Muestra el aviso con la lista de ítems que el cofre quería dar.
    /// </summary>
    public void Show(List<(ItemData item, int qty)> pendingItems, System.Action onFreed)
    {
        _onInventoryFreed = onFreed;

        if (titleText != null)
            titleText.text = "¡Inventario lleno!";

        if (itemsText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("El cofre contiene:");
            sb.AppendLine();
            foreach ((ItemData item, int qty) in pendingItems)
                sb.AppendLine($"  • <b>{item.itemName}</b>  x{qty}");
            sb.AppendLine();
            sb.AppendLine("Descarta algo del inventario para recogerlo.");
            itemsText.text = sb.ToString();
        }

        // Mostrar panel: usa el hijo si está asignado; si no, activa todos los hijos directos
        if (panelContent != null)
        {
            panelContent.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[InventoryFullPromptUI] 'Panel Content' no asignado. Arrastra 'InvFul' al campo. Usando fallback.");
            foreach (Transform child in transform)
                child.gameObject.SetActive(true);
        }

        // Desbloquear cursor para poder clicar los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Time.timeScale = 0f;
    }

    // ─────────────────────────────────────────────
    // BOTONES
    // ─────────────────────────────────────────────

    private void OnOpenInventory()
    {
        InventoryUI.Instance?.Open(); // Open() ya desbloquea el cursor
        StartCoroutine(WaitForInventoryClose());
    }

    private System.Collections.IEnumerator WaitForInventoryClose()
    {
        // WaitUntil funciona aunque timeScale=0 (evalúa cada frame de Update)
        yield return new WaitUntil(() =>
            InventoryUI.Instance == null || !InventoryUI.Instance.IsOpen);

        // Guardar el callback ANTES de Hide() porque Hide() lo pone a null
        System.Action callback = _onInventoryFreed;
        Hide(); // oculta panel, bloquea cursor, timeScale=1
        callback?.Invoke(); // re-intenta dar el loot (puede llamar a Show() de nuevo si aún no cabe)
    }

    private void OnIgnore()
    {
        Hide();
        Debug.Log("[InventoryFullPromptUI] Jugador ignoró el loot del cofre.");
    }

    private void Hide()
    {
        Time.timeScale = 1f;

        if (panelContent != null)
        {
            panelContent.SetActive(false);
        }
        else
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }

        // Volver al estado de juego: cursor oculto y bloqueado
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _onInventoryFreed = null;
    }
}
