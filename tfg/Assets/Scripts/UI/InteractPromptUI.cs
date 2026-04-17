using UnityEngine;
using TMPro;

/// <summary>
/// Prompt de interacción centrado en el HUD.
/// Cualquier objeto interactuable llama a Show/Hide.
///
/// Setup en Unity:
///   HUD Canvas
///   └─ InteractPrompt  (este script + Image fondo semitransparente, opcional)
///       └─ PromptText  (TMP_Text  "[ E ]  Abrir")
///
/// Posición recomendada: centro-inferior de la pantalla
/// Anchor: Bottom Center  |  Pos Y: 80
/// </summary>
public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance { get; private set; }

    [Header("Texto del prompt")]
    public TMP_Text promptText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
    }

    /// <summary>Muestra el prompt con el texto indicado.</summary>
    public void Show(string text)
    {
        if (promptText != null) promptText.text = text;
        gameObject.SetActive(true);
    }

    /// <summary>Oculta el prompt.</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
