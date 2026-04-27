using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel modal que aparece cuando el jugador intenta abrir un cofre con el inventario lleno.
/// Muestra qué ítems contiene el cofre y da opciones para liberar espacio.
///
/// SETUP EN UNITY (hijo del HUD Canvas, desactivado por defecto):
///   InventoryFullPrompt  (Image fondo oscuro semitransparente, este script)
///   ├─ TitleText         (TMP_Text)  ← "¡Inventario lleno!"
///   ├─ ItemsText         (TMP_Text)  ← lista de ítems del cofre
///   ├─ OpenInventoryBtn  (Button)    ← "Abrir inventario"
///   └─ CloseBtn          (Button)    ← "Ignorar loot"
/// </summary>
public class InventoryFullPromptUI : MonoBehaviour
{
    public static InventoryFullPromptUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────
    [Header("Textos")]
    public TMP_Text titleText;
    public TMP_Text itemsText;

    [Header("Botones")]
    public Button openInventoryBtn;
    public Button closeBtn;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────

    // Callback que se ejecuta cuando el jugador confirma haber hecho espacio.
    // ChestController lo usa para re-intentar dar el loot.
    private System.Action _onInventoryFreed;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (openInventoryBtn != null) openInventoryBtn.onClick.AddListener(OnOpenInventory);
        if (closeBtn         != null) closeBtn.onClick.AddListener(OnIgnore);

        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Muestra el aviso con la lista de ítems que el cofre quería dar.
    /// <param name="pendingItems">Pares (ItemData, cantidad) que no cupieron.</param>
    /// <param name="onFreed">Llamado si el jugador libera espacio y cierra el inventario.</param>
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

        gameObject.SetActive(true);
        Time.timeScale = 0f; // Pausar el juego mientras el panel está abierto
    }

    // ─────────────────────────────────────────────
    // BOTONES
    // ─────────────────────────────────────────────

    private void OnOpenInventory()
    {
        // Abrir el inventario sin cerrar este panel (el jugador descarta algo)
        InventoryUI.Instance?.Open();

        // Suscribirse al cierre del inventario para intentar dar el loot de nuevo
        StartCoroutine(WaitForInventoryClose());
    }

    private System.Collections.IEnumerator WaitForInventoryClose()
    {
        // Esperar a que el inventario se cierre
        yield return new WaitUntil(() =>
            InventoryUI.Instance == null || !InventoryUI.Instance.IsOpen);

        // Intentar dar el loot de nuevo
        Hide();
        _onInventoryFreed?.Invoke();
    }

    private void OnIgnore()
    {
        // El jugador no quiere el loot
        Hide();
        Debug.Log("[InventoryFullPromptUI] Jugador ignoró el loot del cofre.");
    }

    private void Hide()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        _onInventoryFreed = null;
    }
}
