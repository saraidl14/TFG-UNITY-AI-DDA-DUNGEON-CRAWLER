using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Pause : MonoBehaviour
{
    public GameObject Objetopausa; //objeto que contiene el menu de pausa
    public bool pausa = false; //variable que indica si el juego esta pausado o no
    public GameObject MenuSalir; //objeto que contiene el menu de salir
    //public GameObject MenuPrincipal;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))  //si se pulsa la tecla escape
        {
            if (pausa == false)
            {
                Objetopausa.SetActive(true);
                pausa = true;

                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                //Vamos a pausar el Audio
                /*  AudioSource[] sonidos = FindObjectOfType<AudioSource>();
                  for(int i = 0; i < sonidos.Length; i++)
                  {
                      sonidos[i].Pause();
                  }*/
            }
            else if (pausa == true)
            {
                Reset();
            }
        }
    }
    /// <summary>
    /// Metodo que reinicia el juego
    /// Se utiliza para reiniciar el juego cuando se pulsa el boton de volver al juego
    /// </summary>

    public void Reset()
    {
        Objetopausa.SetActive(false);  //desactivamos el menu de pausa
        MenuSalir.SetActive(false); //desactivamos el menu de salir
        pausa = false; //cambiamos el estado de la pausa a false

        Time.timeScale = 1.0f; //reiniciamos el tiempo
        Cursor.visible = false; //desactivamos el cursor
        Cursor.lockState = CursorLockMode.Locked; //bloqueamos el cursor en el centro de la pantalla

        //Reanudamos el sonido

        /* AudioSource[] sonidos = FindObjectOfType<AudioSource>();
         for (int i = 0; i < sonidos.Length; i++)
         {
             sonidos[i].Play();
         }*/
    }
    public void irAlMenu(string NombreMenu)
    {
        SceneManager.LoadScene(NombreMenu);
        Time.timeScale = 1.0f;
    }

    public void SalirDelJuego()
    {
        Debug.Log("Has salido del juego");
        Application.Quit();
    }

    //public void GuardarPartida()
    //{
    //    GameManager.instancia.SaveGame(); // Guarda el juego actual
    //    Debug.Log("Partida guardada correctamente.");
    //    Reset(); // Resetea el menú de pausa
    //}
}