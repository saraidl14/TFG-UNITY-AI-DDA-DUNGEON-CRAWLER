/*  Nombre:      MouseSensitivity.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       04/05/2026
 *  Entrega:     16/05/2026
 *  Descripcion: Slider de sensibilidad del raton. Guarda el valor en
 *               PlayerPrefs y lo aplica al PlayerController en tiempo real.
 *  Version:     1.0
 */

using UnityEngine;
using UnityEngine.UI;

public class MouseSensitivity : MonoBehaviour
{
    [Tooltip("Slider de sensibilidad del menu de pausa/opciones.")]
    public Slider slider;

    // Rango del slider: min=5, max=150, default=50
    private const float DefaultSensitivity = 50f;

    void Start()
    {
        float saved = PlayerPrefs.GetFloat("mouseSensitivity", DefaultSensitivity);

        // Configurar rango del slider por codigo por si no esta ajustado en el Inspector
        if (slider != null)
        {
            slider.minValue = 5f;
            slider.maxValue = 150f;
            slider.value    = saved;
        }

        AplicarSensibilidad(saved);
    }

    /// <summary>
    /// Llamado por el evento OnValueChanged del Slider.
    /// Guarda el valor en PlayerPrefs y lo aplica al PlayerController activo.
    /// </summary>
    public void CambiarSensibilidad(float valor)
    {
        PlayerPrefs.SetFloat("mouseSensitivity", valor);
        AplicarSensibilidad(valor);
    }

    private void AplicarSensibilidad(float valor)
    {
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
            pc.mouseSensitivity = valor;
    }
}
