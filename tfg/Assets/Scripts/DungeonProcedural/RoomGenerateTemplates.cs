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

    public GameObject centralRoom; //GameObject de la sala central.

    public GameObject closedRoom; //GameObject de la sala cerrada, se puede usar para rellenar los huecos que queden entre las salas generadas.
              //REVISAR.

    public List<GameObject> rooms; //Lista de GameObjects de las salas que se han generado, nos puede ayudar a controlar el n�mero de salas que se generan y a spawnear enemigos y ayudas en ellas.

    public int maxRooms = 15; // L�mite configurable desde el Inspector, nos puede ayudar a controlar la dificultad del juego cuando le metamos BT.


    public GameObject boss;
    public GameObject slime;
    public GameObject skeleton;
    public GameObject goblin;
    public GameObject orc;
    public GameObject spider;
    public GameObject mage;

    //AYUDAS
    public GameObject chest;
    public GameObject health;
    //JUGADOR
    public GameObject player;

    private void Start()
    {
        StartCoroutine(WaitForGenerationAndSpawn());
    }

    private IEnumerator WaitForGenerationAndSpawn()
    {
        // Espera hasta que no se añadan más salas durante 0.5s seguidos
        int lastCount = 0;
        float timer = 0f;

        while (timer < 0.5f)
        {
            if (rooms.Count == lastCount)
                timer += Time.deltaTime;
            else
            {
                timer = 0f;
                lastCount = rooms.Count;
            }
            yield return null;
        }

        SpawnPlayer();
        SpawnEnemies();
        SpawnHelps();

        // Arrancar el timer de sala para MetricsTracker
        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.StartRoomTimer();
    }

    private void SpawnPlayer()
    {
        Instantiate(player, centralRoom.transform.position, Quaternion.identity);
    }
    private GameObject GetFurthestRoom()
    {
        GameObject furthest = rooms[0];
        float maxDistance = 0f;

        foreach (GameObject room in rooms)
        {
            float dist = Vector3.Distance(centralRoom.transform.position, room.transform.position);
            if (dist > maxDistance)
            {
                maxDistance = dist;
                furthest = room;
            }
        }
        return furthest;
    }

    private void SpawnEnemies() //Aquí es donde se spawnean los enemigos, el boss se spawnea en la sala más lejana a la central y el resto de enemigos en las demás salas.
                                //Podemos hacer que el tipo de enemigo que se spawnee dependa del nº de sala, por ejemplo, en las primeras salas que se spawneen slimes,
                                //luego skeletons, luego goblins y finalmente el boss. Esto nos puede ayudar a controlar la dificultad del juego.
    {
        GameObject bossRoom = GetFurthestRoom();
        Instantiate(boss, bossRoom.transform.position, Quaternion.identity);

        foreach (GameObject room in rooms)
        {
            if (room != bossRoom)
                Instantiate(slime, room.transform.position, Quaternion.identity);
        }
    }
    private void SpawnHelps()
    {

    }
  
}
