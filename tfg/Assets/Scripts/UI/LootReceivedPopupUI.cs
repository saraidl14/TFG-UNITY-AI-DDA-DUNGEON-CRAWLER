/*  Nombre:      LootReceivedPopupUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       12/05/2026
 *  Descripcion: Popup temporal que muestra los ítems recibidos al abrir un cofre.
 *               Aparece unos segundos y se oculta solo.
 *
 *  SETUP EN UNITY:
 *    Canvas
 *    └─ LootReceivedRoot  (este script, SIEMPRE ACTIVO)
 *        └─ LootPanel     (Image fondo semi-transparente) → asignar a 'panelRoot'
 *            ├─ Title     (TMP_Text "¡Loot obtenido!")   → asignar a 'titleText'
 *            └─ Items     (TMP_Text lista de ítems)      → asignar a 'itemsText'
 *
 *  El panel hijo 'LootPanel' empieza DESACTIVADO.
 *  Al llamar a Show(...), se activa y se oculta automaticamente tras 'displayDuration' segundos.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Singleton UI: muestra un popup con la lista de ítems obtenidos al abrir un cofre.
/// Se auto-oculta tras unos segundos.
/// </summary>
public class LootReceivedPopupUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static LootReceivedPopupUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────
    [Header("Panel hijo (empieza desactivado)")]
    public GameObject panelRoot;

    [Header("Textos")]
    public TMP_Text titleText;
    public TMP_Text itemsText;

    [Header("Duracion del popup (segundos)")]
    [Tooltip("Tiempo en segundos antes de que el popup se oculte automaticamente.")]
    public float displayDuration = 3f;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private Coroutine _hideCoroutine;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Muestra el popup con la lista de ítems recibidos.
    /// Si ya hay un popup visible, lo reinicia con los nuevos datos.
    /// </summary>
    /// <param name="receivedItems">Lista de (ItemData, cantidad) obtenidos.</param>
    public void Show(List<(ItemData item, int qty)> receivedItems)
    {
        if (receivedItems == null || receivedItems.Count == 0) return;

        // Construir texto de ítems
        if (titleText != null)
            titleText.text = "¡Objeto obtenido!";

        if (itemsText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach ((ItemData item, int qty) in receivedItems)
            {
                if (item == null) continue;
                if (qty > 1)
                    sb.AppendLine($"  + <b>{item.itemName}</b>  x{qty}");
                else
                    sb.AppendLine($"  + <b>{item.itemName}</b>");
            }
            itemsText.text = sb.ToString().TrimEnd();
        }

        // Mostrar panel
        if (panelRoot != null) panelRoot.SetActive(true);

        // Reiniciar temporizador de ocultado
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    // ─────────────────────────────────────────────
    // INTERNOS
    // ─────────────────────────────────────────────

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration); // funciona aunque timeScale = 0
        Hide();
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _hideCoroutine = null;
    }
}
