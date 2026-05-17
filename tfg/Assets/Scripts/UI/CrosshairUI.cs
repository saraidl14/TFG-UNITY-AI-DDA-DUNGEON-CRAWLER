/*  Nombre:      CrosshairUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       12/05/2026
 *  Descripcion: Mirilla que aparece al apuntar con el arco (RMB).
 *               Se muestra/oculta desde PlayerCombat.HandleBowAim().
 *
 *  SETUP EN UNITY:
 *    1. Crea un Image en el Canvas (nombre sugerido: "Crosshair").
 *    2. Asigna a este script el campo 'crosshairImage'.
 *    3. Arrastra el GameObject (con este script) a la escena.
 *       Puede ser el mismo objeto del Image o un padre.
 *    4. El objeto debe estar ACTIVO para que Awake() registre el singleton,
 *       pero el Image empieza invisible (alpha 0 / desactivado segun 'useSetActive').
 *
 *  Si 'useSetActive' = true  → muestra/oculta con SetActive (mas eficiente).
 *  Si 'useSetActive' = false → cambia el alpha del Image (animacion suave).
 */

using UnityEngine;
using TMPro;

/// <summary>
/// Singleton UI: muestra una mirilla centrada en pantalla cuando el jugador
/// apunta con el arco. Controlado desde PlayerCombat.HandleBowAim().
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static CrosshairUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────
    [Header("Texto TMP de la mirilla (ej: '+')")]
    [Tooltip("TMP_Text del Canvas que representa la mirilla.")]
    public TMP_Text crosshairText;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SetVisible(false);
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>Muestra u oculta la mirilla.</summary>
    public void SetVisible(bool visible)
    {
        if (crosshairText == null) return;
        crosshairText.gameObject.SetActive(visible);
    }
}
