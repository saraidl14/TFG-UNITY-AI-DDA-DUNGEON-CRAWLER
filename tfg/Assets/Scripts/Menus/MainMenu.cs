/* Nombre: Menu principal del juego
 * Autor:  Sara Iglesias
 *
 * Fecha: 25/03/26
 * Fecha entrega: mayo 2026
 * Descripcion: Menu que aparecera al abrir el videojuego.
 *              v3.0 - Seleccion de dificultad inicial con DDA bandado.
 * Version: 1.0 primera version compilable
 *          2.0 con interfaz grafica y compilable
 *          3.0 panel de seleccion de dificultad + DDA bandado
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // PANELES DE UI
    // ─────────────────────────────────────────────

    [Header("Paneles")]
    [Tooltip("Panel principal del menu (MenuGroup).")]
    [SerializeField] private GameObject mainMenuPanel;

    [Tooltip("Panel de seleccion de dificultad.")]
    [SerializeField] private GameObject difficultyPanel;

    // ─────────────────────────────────────────────
    // ESCENA A CARGAR
    // ─────────────────────────────────────────────

    [Header("Escena de juego")]
    [Tooltip("Nombre exacto de la escena a cargar al pulsar cualquier dificultad.")]
    [SerializeField] private string gameSceneName = "GameScene";

    // ─────────────────────────────────────────────
    // UNITY CALLBACKS
    // ─────────────────────────────────────────────

    void Start()
    {
        // Aseguramos estado inicial correcto
        if (mainMenuPanel   != null) mainMenuPanel.SetActive(true);
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
    }

    void Update() { }

    // ─────────────────────────────────────────────
    // NAVEGACION ENTRE PANELES
    // ─────────────────────────────────────────────

    /// <summary>
    /// Boton "Jugar" del menu principal.
    /// Oculta el menu y muestra el panel de seleccion de dificultad.
    /// </summary>
    public void OnPlayPressed()
    {
        if (mainMenuPanel   != null) mainMenuPanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(true);
    }

    /// <summary>
    /// Boton "Volver" del panel de dificultad.
    /// Regresa al menu principal sin cargar ninguna escena.
    /// </summary>
    public void OnVolverAlMenu()
    {
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (mainMenuPanel   != null) mainMenuPanel.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // BOTONES DE DIFICULTAD
    // ─────────────────────────────────────────────

    /// <summary>Dificultad Facil: D inicial = 2, banda [1, 4].</summary>
    public void OnFacilPressed()     { StartGame(2); }

    /// <summary>Dificultad Normal: D inicial = 5, banda [3, 7].</summary>
    public void OnNormalPressed()    { StartGame(5); }

    /// <summary>Dificultad Dificil: D inicial = 7, banda [5, 10].</summary>
    public void OnDificilPressed()   { StartGame(7); }

    /// <summary>Dificultad Muy Dificil: D inicial = 10, banda [8, 10].</summary>
    public void OnMuyDificilPressed(){ StartGame(10); }

    // ─────────────────────────────────────────────
    // METODOS INTERNOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Configura la dificultad inicial en el DifficultyManager y carga la escena.
    /// </summary>
    private void StartGame(int chosenLevel)
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.SetStartingDifficulty(chosenLevel);
        else
            Debug.LogWarning("[MainMenu] DifficultyManager no encontrado. Se usara la dificultad por defecto.");

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    // ─────────────────────────────────────────────
    // METODOS HEREDADOS / COMPATIBILIDAD
    // ─────────────────────────────────────────────

    /// <summary>Carga una escena por nombre (uso generico, conservado por compatibilidad).</summary>
    public void irAlJuego(string NombreCiudad)
    {
        SceneManager.LoadScene(NombreCiudad);
        Time.timeScale = 1.0f;
    }

    public void Continuar()
    {
        // se llama desde el panel de guardado (pendiente de implementar)
    }

    public void CerrarJuego()
    {
        Debug.Log("HAS SALIDO DEL JUEGO");
        Application.Quit();
    }
}
