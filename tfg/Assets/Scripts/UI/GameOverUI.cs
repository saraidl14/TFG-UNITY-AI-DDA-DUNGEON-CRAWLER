/*  Nombre:      GameOverUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/05/2026
 *  Descripcion: Pantalla de Game Over que muestra la puntuación y estadísticas de la partida.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la pantalla de Game Over.
/// Lee los datos de ScoreManager y los muestra en pantalla.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────

    [Header("Puntuacion")]
    public TMP_Text scoreText;
    public TMP_Text rankText;

    [Header("Estadisticas")]
    public TMP_Text roomsText;
    public TMP_Text killsText;
    public TMP_Text timeText;
    public TMP_Text deathsText;
    public TMP_Text difficultyText;
    public TMP_Text difficultyChangeText;

    [Header("Botones")]
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Nombres de Escenas")]
    public string gameSceneName     = "SampleScene";
    public string mainMenuSceneName = "MainMenu";

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Evaluar y aplicar el ajuste DDA ahora (incluye la muerte),
        // así PopulateUI muestra el valor correcto y Retry ya arranca con el nivel ajustado
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.EvaluateAndApplyImmediate();

        PopulateUI();
        SetupButtons();
    }

    // ─────────────────────────────────────────────
    // RELLENAR UI CON DATOS DE SCOREMANAGER
    // ─────────────────────────────────────────────

    private void PopulateUI()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[GameOverUI] ScoreManager no encontrado.");
            return;
        }

        ScoreManager sm = ScoreManager.Instance;

        if (scoreText      != null) scoreText.text      = $"{sm.FinalScore:N0} pts";
        if (rankText       != null) rankText.text       = $"Rango: {sm.GetRank()}";
        if (rankText       != null) rankText.color      = GetRankColor(sm.GetRank());
        if (roomsText      != null) roomsText.text      = $"Salas: {sm.TotalRooms}";
        if (killsText      != null) killsText.text      = $"Enemigos: {sm.TotalEnemiesKilled}";
        if (deathsText     != null) deathsText.text     = $"Muertes: {sm.TotalDeaths}";
        if (difficultyText != null) difficultyText.text = $"Dificultad maxima: {DifficultyManager.Instance?.currentLevel ?? 1}";

        if (difficultyChangeText != null && DifficultyManager.Instance != null)
        {
            int adj = DifficultyManager.Instance.LastAdjustment;
            if      (adj > 0) { difficultyChangeText.text  = $"↑ Sube {adj} nivel{(adj > 1 ? "es" : "")}";  difficultyChangeText.color = Color.green; }
            else if (adj < 0) { difficultyChangeText.text  = $"↓ Baja {Mathf.Abs(adj)} nivel{(Mathf.Abs(adj) > 1 ? "es" : "")}"; difficultyChangeText.color = Color.red;   }
            else              { difficultyChangeText.text  = "→ Mantiene nivel"; difficultyChangeText.color = Color.yellow; }
        }

        if (timeText != null)
        {
            int mins = Mathf.FloorToInt(sm.TotalTime / 60f);
            int secs = Mathf.FloorToInt(sm.TotalTime % 60f);
            timeText.text = $"Tiempo: {mins:00}:{secs:00}";
        }
    }

    // ─────────────────────────────────────────────
    // BOTONES
    // ─────────────────────────────────────────────

    private void SetupButtons()
    {
        if (retryButton    != null) retryButton.onClick.AddListener(OnRetry);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
    }

    private void OnRetry()
    {
        // ResetForRetry conserva las muertes acumuladas entre intentos
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetForRetry();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        SceneLoader.LoadScene(gameSceneName);
    }

    private void OnMainMenu()
    {
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetAll();

        // No tocamos DifficultyManager aqui — el menu llamara a SetStartingDifficulty
        // cuando el jugador elija dificultad de nuevo

        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    // ─────────────────────────────────────────────
    // COLOR DEL RANGO
    // ─────────────────────────────────────────────

    private Color GetRankColor(string rank)
    {
        switch (rank)
        {
            case "Legendario": return new Color(1.0f, 0.84f, 0.0f);
            case "Maestro":    return new Color(0.6f,  0.0f, 1.0f);
            case "Diamante":   return new Color(0.0f,  0.8f, 1.0f);
            case "Platino":    return new Color(0.7f,  0.9f, 1.0f);
            case "Oro":        return new Color(1.0f, 0.75f, 0.0f);
            case "Plata":      return new Color(0.75f,0.75f,0.75f);
            default:           return new Color(0.8f,  0.5f, 0.2f);
        }
    }
}
