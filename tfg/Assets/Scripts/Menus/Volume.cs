/*Nombre: ControladorCanvas.cs
 * Autor:  Sara Iglesias 
 * 
 * Fecha: 25/03/26
 * Fecha entrega: 26 / 05 / 26
 * Descripcion: Script que se encarga de controlar el sonido del juego
 * Versión: 1.0 primera version compilable
 *          2.0 con interfaz grafica y compilable
 *          3.0 con todo el sistema integrado
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Volume : MonoBehaviour
{

    public Slider slider; // Referencia al slider de volumen

    public float sliderValue; // Valor del slider

    public Image imagenMute; // Referencia a la imagen de muteo

    // Start is called before the first frame update

    void Start()
    {
        slider.value = PlayerPrefs.GetFloat("volumenAudio", 0.5f); // Valor por defecto 0.5f

        AudioListener.volume = slider.value; // Asignar el valor del slider al volumen de AudioListener

        RevisarSiEstoyMute(); // Comprobar si el audio está en muteo
    }


    public void ChangeSlider(float valor)

    {

        sliderValue = valor; // Obtener el valor del slider
        PlayerPrefs.SetFloat("volumenAudio", sliderValue); // Guardar el valor del slider en PlayerPrefs
        AudioListener.volume = slider.value; // Asignar el valor del slider al volumen de AudioListener
        RevisarSiEstoyMute(); // Comprobar si el audio está en muteo
    }

    public void RevisarSiEstoyMute() // Comprobar si el audio está en muteo
    {

        if (sliderValue == 0) // Si el valor del slider es 0, significa que el audio está en muteo

        {

            imagenMute.enabled = true; // Mostrar la imagen de muteo

        }
        else // Si el valor del slider no es 0, significa que el audio no está en muteo
            imagenMute.enabled = false; // Ocultar la imagen de muteo


    }
}
