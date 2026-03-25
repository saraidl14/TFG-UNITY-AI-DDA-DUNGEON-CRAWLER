using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomGenerateTemplates : MonoBehaviour
{
    public GameObject[] topRooms;
    public GameObject[] bottomRooms;
    public GameObject[] leftRooms;
    public GameObject[] rightRooms;

    public GameObject closedRoom;

    public List<GameObject> rooms;

    public GameObject boss;
    public GameObject slime;
    public GameObject skeleton;
    public GameObject goblin;
    public GameObject health;

    public GameObject player;

    private void Start()
    {
        Invoke("SpawnEnemies", 0.5f);
        Invoke("SpawnHelps", 0.5f);
    }

    private void SpawnEnemies()
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
