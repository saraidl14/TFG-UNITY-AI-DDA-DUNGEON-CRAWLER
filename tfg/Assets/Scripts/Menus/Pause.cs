/*  Nombre:      Pause.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       04/05/2026
 *  Entrega:     16/05/2026
 *  Descripcion: Menu de pausa del juego.
 *  Version:     1.0  primera version compilable
 *               2.0  con interfaz grafica
 *               3.0  integrado con el sistema completo
 */

using UnityEngine;

/// <summary>
/// Gestiona el menu de pausa. Se activa/desactiva con la tecla Escape.
/// Metodos publicos asignables a botones TMP desde el Inspector:
///   ResumeGame()    -> boton "Volver al juego"
///   irAlMenu()      -> boton "Menu principal"
///   SalirDelJuego() -> boton "Salir"
/// </summary>
public class Pause : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Panel raiz del menu de pausa.")]
    public GameObject panelPausa;

    [Tooltip("Subpanel de confirmacion de salida (opcional).")]
    public GameObject menuSalir;

    // Estado de pausa (visible en Inspector para depuracion)
    public bool pausa = false;

    // ─────────────────────────────────────────────
    // ENTRADA
    // ─────────────────────────────────────────────

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!pausa)
                AbrirPausa();
            else
                ResumeGame();
        }
    }

    // ─────────────────────────────────────────────
    // CONTROL DE PAUSA
    // ─────────────────────────────────────────────

    private void AbrirPausa()
    {
        pausa = true;

        if (panelPausa != null)
            panelPausa.SetActive(true);

        Time.timeScale   = 0f;
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Cierra el menu de pausa y reanuda el juego.
    /// Asignar este metodo al boton "Volver al juego" en el Inspector.
    /// NOTA: el nombre NO puede ser Reset() porque Unity reserva ese nombre
    ///       como mensaje de editor (se ejecutaria al hacer Reset del componente).
    /// </summary>
    public void ResumeGame()
    {
        pausa = false;

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (menuSalir != null)
            menuSalir.SetActive(false);

        Time.timeScale   = 1f;
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ─────────────────────────────────────────────
    // BOTONES
    // ─────────────────────────────────────────────

    /// <summary>
    /// Carga la escena indicada (menu principal).
    /// Restaura el estado del juego y limpia el inventario antes de cambiar de escena.
    /// </summary>
    public void irAlMenu(string nombreEscena)
    {
        // Limpiar inventario: el loot no debe sobrevivir si se abandona la partida
        Inventory.Instance?.ClearAll();

        // Reiniciar el estado completo de la sesión
        GameManager.Instance?.ResetSession();

        // Reiniciar puntuación y métricas DDA
        ScoreManager.Instance?.ResetAll();
        MetricsTracker.Instance?.ResetRoomMetrics();
        MetricsTracker.Instance?.ResetStreak();

        Time.timeScale   = 1f;
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscena);
    }

    /// <summary>
    /// Cierra la aplicacion. Solo funciona en build; ignorado en el editor.
    /// </summary>
    public void SalirDelJuego()
    {
        Debug.Log("[Pause] Saliendo del juego.");
        Application.Quit();
    }
}
