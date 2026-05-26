/*  Nombre:      FullScreen.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/05/2026
 *  Descripcion: Gestiona las opciones de pantalla completa, VSync y resolución.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class FullScreen : MonoBehaviour
{
    public Toggle toggle, vSyncTog;
    public TMP_Dropdown resolucionesDropDown;
    Resolution[] resoluciones;

    /// <summary>
    /// El m�todo Start se llama al inicio del juego.
    /// Dice que si la pantalla est� en modo pantalla completa, el toggle de pantalla completa estar� activado.
    /// Si no, estar� desactivado.
    /// En el caso de que la pantalla est� en modo pantalla completa, se revisan las resoluciones disponibles.
    /// si la calidad de la pantalla est� en 0, el toggle de VSync estar� desactivado.
    /// Si no, estar� activado.
    /// </summary>
    void Start()
    {

        if (Screen.fullScreen)
        {

            toggle.isOn = true;
        }
        else
        {

            toggle.isOn = false;

        }

        RevisarResoluciones();

        if (QualitySettings.vSyncCount == 0)
        {
            vSyncTog.isOn = false;
        }
        else
        {
            vSyncTog.isOn = true;
        }
    }
    void Update()

    {

    }

    public void ActivarPantallaCompleta(bool pantallaCompleta) //activa o desactiva la pantalla completa
    {

        Screen.fullScreen = pantallaCompleta; //la pantalla se pone en modo pantalla completa o no


    }
    public void RevisarResoluciones() //revisa las resoluciones disponibles
    {

        resoluciones = Screen.resolutions;//obtiene las resoluciones disponibles
        resolucionesDropDown.ClearOptions(); //Borra las opciones del dropdown
        List<string> opciones = new List<string>(); //crea una lista de opciones
        int resolucionActual = 0; //inicializa la resolucion actual a 0

        for (int i = 0; i < resoluciones.Length; i++)
        {
            string opcion = resoluciones[i].width + " x " + resoluciones[i].height;
            opciones.Add(opcion);

            if (Screen.fullScreen && resoluciones[i].width == Screen.currentResolution.width && //si la resolucion actual es igual a la resolucion de la pantalla
                resoluciones[i].height == Screen.currentResolution.height)
            {
                resolucionActual = i; //guarda la resolucion actual
            }
        }

        resolucionesDropDown.AddOptions(opciones); //a�ade las opciones al dropdown
        resolucionesDropDown.value = resolucionActual; //selecciona la resolucion actual
        resolucionesDropDown.RefreshShownValue(); //refresca el dropdown

        resolucionesDropDown.value = PlayerPrefs.GetInt("numResolucion", 0);

    }

    public void CambiarResolucion(int indiceResolucion) //cambia la resolucion
    {
        PlayerPrefs.SetInt("numResolucion", indiceResolucion); //guarda la resolucion seleccionada

        Resolution resolucion = resoluciones[indiceResolucion];  //obtiene la resolucion seleccionada
        Screen.SetResolution(resolucion.width, resolucion.height, Screen.fullScreen); //cambia la resolucion
    }

    public void CambiarVSync(bool vSync) //cambia el VSync
    {
        Screen.fullScreen = toggle.isOn;

        if (vSync)
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
        }
    }
}