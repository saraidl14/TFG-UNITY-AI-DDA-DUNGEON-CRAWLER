using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera el dungeon procedimental y spawnea enemigos segun el nivel de dificultad DDA.
///
/// TABLA DE SPAWN POR NIVEL:
/// ┌────────┬──────────────────────────────────┬──────────────┐
/// │ Nivel  │ Tipos de enemigo                 │ Por sala     │
/// ├────────┼──────────────────────────────────┼──────────────┤
/// │   1    │ Slime                            │ 1 enemigo    │
/// │   2    │ Slime                            │ 2 enemigos   │
/// │   3    │ Slime, Skeleton                  │ 2 enemigos   │
/// │   4    │ Skeleton, Goblin                 │ 2 enemigos   │
/// │   5    │ Skeleton, Goblin, Spider         │ 3 enemigos   │
/// │   6    │ Goblin, Orc, Spider              │ 3 enemigos   │
/// │   7    │ Goblin, Orc, Mage                │ 3 enemigos   │
/// │   8    │ Orc, Spider, Mage                │ 4 enemigos   │
/// │   9    │ Orc, Spider, Mage                │ 4 enemigos   │
/// │  10    │ Orc, Spider, Mage                │ 5 enemigos   │
/// └────────┴──────────────────────────────────┴──────────────┘
///
/// Notas:
/// - El boss SIEMPRE aparece en la sala mas lejana a la central.
/// - Los enemigos se spawnean con un offset aleatorio dentro de la sala
///   para que no se apilen en el mismo punto.
/// - Si un prefab no esta asignado en el Inspector se omite ese slot
/// 
/// </summary>
public class RoomGenerateTemplates : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SALAS
    // ─────────────────────────────────────────────

    public GameObject[] topRooms;      // Array de GameObjects de las salas con apertura en la parte superior.
    public GameObject[] bottomRooms;   // Array de GameObjects de las salas con apertura en la parte inferior.
    public GameObject[] leftRooms;     // Array de GameObjects de las salas con apertura en la parte izquierda.
    public GameObject[] rightRooms;    // Array de GameObjects de las salas con apertura en la parte derecha.

    public GameObject centralRoom;     // GameObject de la sala central.
    public GameObject closedRoom;      // GameObject de la sala cerrada, se puede usar para rellenar los huecos que queden entre las salas generadas.
                                       // REVISAR.

    public List<GameObject> rooms;     // Lista de GameObjects de las salas que se han generado, nos puede ayudar a controlar el número de salas que se generan y a spawnear enemigos y ayudas en ellas.

    [Tooltip("Limite configurable desde el Inspector, nos puede ayudar a controlar la dificultad del juego cuando le metamos BT.")]
    public int maxRooms = 15;

    // ─────────────────────────────────────────────
    // PREFABS DE ENEMIGOS
    // ─────────────────────────────────────────────

    [Header("Enemigos")]
    public GameObject boss;
    public GameObject slime;
    public GameObject skeleton;
    public GameObject goblin;
    public GameObject orc;
    public GameObject spider;
    public GameObject mage;

    // ─────────────────────────────────────────────
    // AYUDAS Y JUGADOR
    // ─────────────────────────────────────────────

    [Header("Ayudas")]
    public GameObject chest;
    public GameObject health;

    [Header("Jugador")]
    public GameObject player;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Start()
    {
        StartCoroutine(WaitForGenerationAndSpawn());
    }

    private IEnumerator WaitForGenerationAndSpawn()
    {
        // Espera hasta que no se añadan mas salas durante 0.5s seguidos
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

        if (MetricsTracker.Instance != null)
            MetricsTracker.Instance.StartRoomTimer();
    }

    // ─────────────────────────────────────────────
    // SPAWN JUGADOR
    // ─────────────────────────────────────────────

    private void SpawnPlayer()
    {
        Instantiate(player, centralRoom.transform.position, Quaternion.identity);
    }

    // ─────────────────────────────────────────────
    // SALA MAS LEJANA (para el boss)
    // ─────────────────────────────────────────────

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

    // ─────────────────────────────────────────────
    // SPAWN ENEMIGOS
    // ─────────────────────────────────────────────

    private void SpawnEnemies() // Aquí es donde se spawnean los enemigos, el boss se spawnea en la sala más lejana a la central y el resto de enemigos en las demás salas.
                                // El tipo de enemigo depende del nivel DDA: en niveles bajos salen slimes, en niveles altos orcos, arañas y magos.
    {
        // Nivel DDA actual (1-10)
        int level = 1;
        if (DifficultyManager.Instance != null)
            level = Mathf.Clamp(DifficultyManager.Instance.currentLevel, 1, 10);

        // Boss en la sala mas lejana
        GameObject bossRoom = GetFurthestRoom();
        if (boss != null)
            Instantiate(boss, bossRoom.transform.position, Quaternion.identity);

        // Config de spawn para este nivel
        int            enemiesPerRoom = GetEnemiesPerRoom(level);
        GameObject[]   allowedTypes   = GetAllowedEnemyTypes(level);

        // Filtrar prefabs nulos (controladores aun no implementados)
        List<GameObject> validTypes = new List<GameObject>();
        foreach (GameObject t in allowedTypes)
            if (t != null) validTypes.Add(t);

        // Si no hay ningun tipo valido, usar slime como fallback
        if (validTypes.Count == 0 && slime != null)
            validTypes.Add(slime);

        if (validTypes.Count == 0)
        {
            Debug.LogWarning("[RoomGenerateTemplates] No hay prefabs de enemigos asignados.");
            return;
        }

        // Spawnear enemigos en cada sala (excepto la del boss)
        foreach (GameObject room in rooms)
        {
            if (room == bossRoom) continue;

            for (int i = 0; i < enemiesPerRoom; i++)
            {
                // Offset aleatorio para que no se apilen
                Vector3 offset = new Vector3(
                    Random.Range(-3f, 3f),
                    0f,
                    Random.Range(-3f, 3f)
                );

                GameObject prefab = validTypes[Random.Range(0, validTypes.Count)];
                Instantiate(prefab, room.transform.position + offset, Quaternion.identity);
            }
        }

        Debug.Log($"[RoomGenerateTemplates] Nivel {level} | " +
                  $"{enemiesPerRoom} enemigos/sala | " +
                  $"Tipos: {string.Join(", ", GetTypeNames(validTypes))}");
    }

    // ─────────────────────────────────────────────
    // TABLA: ENEMIGOS POR SALA SEGUN NIVEL
    // ─────────────────────────────────────────────

    /// <summary>Devuelve cuantos enemigos se spawnean por sala en este nivel.</summary>
    private int GetEnemiesPerRoom(int level)
    {
        // nivel:        1  2  3  4  5  6  7  8  9  10
        int[] counts = { 1, 2, 2, 2, 3, 3, 3, 4, 4,  5 };
        return counts[level - 1];
    }

    /// <summary>
    /// Devuelve los prefabs de enemigos permitidos en este nivel.
    /// Los prefabs nulos (controlador aun no creado) se filtran en SpawnEnemies.
    /// </summary>
    private GameObject[] GetAllowedEnemyTypes(int level)
    {
        switch (level)
        {
            case 1:  return new[] { slime };
            case 2:  return new[] { slime };
            case 3:  return new[] { slime,    skeleton };
            case 4:  return new[] { skeleton, goblin };
            case 5:  return new[] { skeleton, goblin,  spider };
            case 6:  return new[] { goblin,   orc,     spider };
            case 7:  return new[] { goblin,   orc,     mage };
            case 8:  return new[] { orc,      spider,  mage };
            case 9:  return new[] { orc,      spider,  mage };
            case 10: return new[] { orc,      spider,  mage };
            default: return new[] { slime };
        }
    }

    // ─────────────────────────────────────────────
    // AYUDAS (pendiente fase 3)
    // ─────────────────────────────────────────────

    private void SpawnHelps()
    {
        // TODO Fase 3: cofres y pociones de vida
    }

    // ─────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────

    private List<string> GetTypeNames(List<GameObject> types)
    {
        List<string> names = new List<string>();
        foreach (GameObject t in types) names.Add(t.name);
        return names;
    }
}
