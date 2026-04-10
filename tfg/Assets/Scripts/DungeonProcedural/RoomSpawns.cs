using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSpawns : MonoBehaviour
{
    public int openSides;
    // 0 = no open sides
    // 1 = bottom
    // 2 = top
    // 3 = left
    // 4 = right

    private RoomGenerateTemplates templates;
    private int rand;
    private bool spawned = false;

    void Start()
    {
      templates = GameObject.FindGameObjectWithTag("Rooms").GetComponent<RoomGenerateTemplates>();
       Invoke("Spawn", 0.1f);
    }

    void Spawn()
    {
        if (spawned == false)
        {
            if (openSides == 1) // bottom
            {
                rand = Random.Range(0, templates.bottomRooms.Length);
                Instantiate(templates.bottomRooms[rand], transform.position, templates.bottomRooms[rand].transform.rotation);
            }
            else if (openSides == 2) // top
            {
                rand = Random.Range(0, templates.topRooms.Length);
                Instantiate(templates.topRooms[rand], transform.position, templates.topRooms[rand].transform.rotation);
            }
            else if (openSides == 3) //left
            {
                rand = Random.Range(0, templates.leftRooms.Length);
                Instantiate(templates.leftRooms[rand], transform.position, templates.leftRooms[rand].transform.rotation);
            }
            else if (openSides == 4) // right
            {
                rand = Random.Range(0, templates.rightRooms.Length);
                Instantiate(templates.rightRooms[rand], transform.position, templates.rightRooms[rand].transform.rotation);
            }
            spawned = true;
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SpawnPoint"))
        {
            RoomSpawns otherSpawn = other.GetComponent<RoomSpawns>();
            if (otherSpawn == null) return; // el SpawnPoint no tiene RoomSpawns, ignorar

            if (otherSpawn.spawned == false && spawned == false)
            {
                if (templates != null && templates.closedRoom != null)
                    Instantiate(templates.closedRoom, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
            spawned = true;
        }
    }

}
