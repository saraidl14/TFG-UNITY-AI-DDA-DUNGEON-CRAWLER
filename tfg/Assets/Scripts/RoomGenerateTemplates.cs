using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomGenerateTemplates : MonoBehaviour
{
    public GameObject[] topRooms; //Array de GameObjects de las salas con apertura en la parte superior.
    public GameObject[] bottomRooms; //Array de GameObjects de las salas con apertura en la parte inferior.
    public GameObject[] leftRooms; //Array de GameObjects de las salas con apertura en la parte izquierda.
    public GameObject[] rightRooms; //Array de GameObjects de las salas con apertura en la parte derecha.

    public GameObject closedRoom; //GameObject de la sala cerrada, se puede usar para rellenar los huecos que queden entre las salas generadas.
              //REVISAR.

    public List<GameObject> rooms; //Lista de GameObjects de las salas que se han generado, nos puede ayudar a controlar el número de salas que se generan y a spawnear enemigos y ayudas en ellas.

    public int maxRooms = 15; // Límite configurable desde el Inspector, nos puede ayudar a controlar la dificultad del juego cuando le metamos BT.


    public GameObject boss;
    public GameObject slime;
    public GameObject skeleton;
    public GameObject goblin;
    public GameObject health;

    public GameObject player;

    private void Start()
    {
        Invoke("SpawnEnemies", 1f);
        Invoke("SpawnHelps", 1f);
    }

    private void SpawnEnemies() //Aquí es donde se spawnean los enemigos, el boss se spawnea en la última sala y el resto de enemigos en las demás salas. Podemos hacer que el
                                //tipo de enemigo que se spawnee dependa del número de sala, por ejemplo, en las primeras salas que se spawneen slimes, luego skeletons,
                                //luego goblins y finalmente el boss. Esto nos puede ayudar a controlar la dificultad del juego.
    {
        Instantiate(boss, rooms[rooms.Count - 1].transform.position, Quaternion.identity);

        for(int i = 0; i < rooms.Count-1 ; i++)
        {
            Instantiate(slime, rooms[i].transform.position, Quaternion.identity);
        }
    }
    private void SpawnHelps()
    {

    }
}
