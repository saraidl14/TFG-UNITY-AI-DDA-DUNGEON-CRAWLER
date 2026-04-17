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

    // Registro de posiciones que ya han generado una sala (evita doble spawn)
    private static HashSet<Vector2Int> _spawnedPositions = new HashSet<Vector2Int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRegistry()
    {
        _spawnedPositions.Clear();
    }

    private RoomGenerateTemplates templates;
    public bool spawned = false;

    void Start()
    {
        templates = GameObject.FindGameObjectWithTag("Rooms").GetComponent<RoomGenerateTemplates>();
        Invoke("Spawn", 0.1f);
    }

    void Spawn()
    {
        if (spawned) return;

        Vector2Int pos = GetGridPos();

        // Ya hay una sala spawneada aquí: este SpawnPoint sobra, se destruye sin poner muro
        if (_spawnedPositions.Contains(pos))
        {
            spawned = true;
            Destroy(gameObject);
            return;
        }

        // Límite de salas alcanzado: poner muro
        if (templates.rooms.Count >= templates.maxRooms)
        {
            SpawnClosed();
            return;
        }

        // Spawnar sala según la dirección
        spawned = true;
        _spawnedPositions.Add(pos);

        if (openSides == 1)
        {
            int r = Random.Range(0, templates.bottomRooms.Length);
            Instantiate(templates.bottomRooms[r], transform.position, templates.bottomRooms[r].transform.rotation);
        }
        else if (openSides == 2)
        {
            int r = Random.Range(0, templates.topRooms.Length);
            Instantiate(templates.topRooms[r], transform.position, templates.topRooms[r].transform.rotation);
        }
        else if (openSides == 3)
        {
            int r = Random.Range(0, templates.leftRooms.Length);
            Instantiate(templates.leftRooms[r], transform.position, templates.leftRooms[r].transform.rotation);
        }
        else if (openSides == 4)
        {
            int r = Random.Range(0, templates.rightRooms.Length);
            Instantiate(templates.rightRooms[r], transform.position, templates.rightRooms[r].transform.rotation);
        }
    }

    private void SpawnClosed()
    {
        spawned = true;
        if (templates != null && templates.closedRoom != null)
            Instantiate(templates.closedRoom, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private Vector2Int GetGridPos()
    {
        return new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z));
    }
}
