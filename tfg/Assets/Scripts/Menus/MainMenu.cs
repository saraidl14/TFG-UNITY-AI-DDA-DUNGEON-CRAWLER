/* Nombre: Menu principal del juego
 * Autor:  Sara Iglesias
 * 
 * Fecha: 25/03/26
 * Fecha entrega: mayo 2026
 * Descripción: Menú que aparecerá al abrir el videojuego
 * Versión: 1.0 primera versión compilable
 *          2.0 con interfaz gráfica y compilable
 * 
 * @param:
 * 
 * return: No devuelve nada. Muestra los resultados directamente al usuario.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    /* public void EscenaJuego()
     {
         SceneManager.LoadScene("Ciudad");

     }*/

    public void irAlJuego(string NombreCiudad) //Te lleva a donde le digas
    {
        SceneManager.LoadScene(NombreCiudad);
        Time.timeScale = 1.0f;
    }
    public void Continuar()
    { //se llama en el de guardado

    }
    public void CerrarJuego()
    {
        Debug.Log("HAS SALIDO DEL JUEGO");
        Application.Quit();
    }

    ////public void cargar()
    ////{
    ////    GameManager.instancia.LoadGame(); // Carga el juego guardado
    ////    SceneManager.LoadScene(GameManager.instancia.currentScene); // Cambia a la escena guardada
    ////}
}