/*  Nombre:      RoomGenerateTemplates.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       24/03/2026
 *  Descripcion: Genera el dungeon procedural y spawnea enemigos según el nivel DDA.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

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
    public int maxRooms = 10;

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

    [Header("NavMesh")]
    [Tooltip("NavMeshSurface del GameController. Se hornea tras generar todas las salas.")]
    public NavMeshSurface navMeshSurface;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────

    private void Awake()
    {
        // Destruir las salas de la mazmorra anterior antes de generar la nueva
        foreach (GameObject room in rooms)
            if (room != null) Destroy(room);

        RoomSpawns.ResetStatic();
        AddRooms.OccupiedPositions.Clear();
        rooms.Clear();
    }

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

        // Hornear NavMesh con todas las salas ya generadas
        if (navMeshSurface != null)
        {
            navMeshSurface.RemoveData();   // eliminar el NavMesh de la mazmorra anterior
            navMeshSurface.BuildNavMesh(); // hornear sobre la nueva geometría
            Debug.Log("[RoomGenerateTemplates] NavMesh reseteado y horneado correctamente.");
        }
        else
        {
            Debug.LogWarning("[RoomGenerateTemplates] NavMeshSurface no asignado en el Inspector.");
        }

        // Esperar 2 frames para que el NavMeshAgent registre el NavMesh recien horneado.
        // Sin esta espera los agentes no encuentran NavMesh y se warpean a la esquina.
        yield return null;
        yield return null;

        SpawnPlayer();
        SpawnEnemies();
        SpawnHelps();

        if (MetricsTracker.Instance != null)
        {
            MetricsTracker.Instance.ResetRoomMetrics();    // limpiar metricas de la mazmorra anterior
            MetricsTracker.Instance.StartRoomTimer();
            MetricsTracker.Instance.StartDungeonTimer();   // cronómetro total hasta el jefe
        }
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

        // Boss en el centro de la sala mas lejana
        GameObject bossRoom = GetFurthestRoom();
        if (boss != null)
        {
            Vector3 bossCenter = GetRoomCenter(bossRoom);
            GameObject bossInstance = Instantiate(boss, bossCenter, Quaternion.identity);
            NavMeshAgent bossAgent = bossInstance.GetComponent<NavMeshAgent>();
            if (bossAgent != null)
                bossAgent.Warp(bossCenter);
        }
        else
            Debug.LogError("[RoomGenerateTemplates] El prefab BOSS no esta asignado en el Inspector.");

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
            Debug.LogError("[RoomGenerateTemplates] No hay prefabs validos para nivel " + level +
                           ". Comprueba que el prefab SLIME esta asignado en el Inspector.");
            return;
        }

        Debug.Log($"[RoomGenerateTemplates] Spawneando nivel {level} | " +
                  $"{enemiesPerRoom} enemigos/sala | Salas: {rooms.Count - 1}");

        // Spawnear enemigos en cada sala (excepto la del boss)
        foreach (GameObject room in rooms)
        {
            if (room == bossRoom) continue;

            Vector3 roomCenter = GetRoomCenter(room);

            for (int i = 0; i < enemiesPerRoom; i++)
            {
                // Warp coloca al NavMeshAgent exactamente en roomCenter dentro del NavMesh.
                // Sin Warp, el agente busca el punto mas cercano al (0,0,0) al activarse y todos
                // acaban en la misma esquina, independientemente de donde se instanciaron.
                GameObject prefab = validTypes[Random.Range(0, validTypes.Count)];
                GameObject enemy = Instantiate(prefab, roomCenter, Quaternion.identity);
                NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null)
                    agent.Warp(roomCenter);
            }
        }

        // Boss tambien en punto valido del NavMesh
        // (ya se instancio antes, pero si queremos reubicarlo podemos hacerlo aqui)

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
        // Enemigos activos: Slime, Skeleton, Orc, Mage
        switch (level)
        {
            case 1:  return new[] { slime };
            case 2:  return new[] { slime };
            case 3:  return new[] { slime,    skeleton };
            case 4:  return new[] { slime,    skeleton };
            case 5:  return new[] { skeleton, orc };
            case 6:  return new[] { skeleton, orc };
            case 7:  return new[] { orc,      mage };
            case 8:  return new[] { orc,      mage };
            case 9:  return new[] { orc,      mage };
            case 10: return new[] { orc,      mage };
            default: return new[] { slime };
        }
    }

    // ─────────────────────────────────────────────
    // AYUDAS (pendiente fase 3)
    // ─────────────────────────────────────────────

    // ─────────────────────────────────────────────
    // COFRES
    // ─────────────────────────────────────────────

    [Header("Cofres")]
    [Tooltip("Prefab del cofre. Debe tener ChestController.")]
    public GameObject chestPrefab;

    [Tooltip("Los 5 tipos de cofre en orden: Common, Uncommon, Rare, Epic, Legendary.")]
    public ChestData[] chestTypes;

    [Tooltip("Probabilidad de que aparezca un cofre en cada sala normal (0.0-1.0).")]
    [Range(0f, 1f)]
    public float chestSpawnChance = 0.6f;

    [Tooltip("Offset vertical del cofre sobre el suelo detectado. Ajusta si el pivot del prefab no está en la base.")]
    public float chestYOffset = 0.5f;

    private void SpawnHelps()
    {
        if (chestPrefab == null || chestTypes == null || chestTypes.Length == 0) return;

        int level = DifficultyManager.Instance != null ? DifficultyManager.Instance.currentLevel : 1;
        GameObject bossRoom = GetFurthestRoom();

        foreach (GameObject room in rooms)
        {
            if (room == centralRoom) continue; // No cofre en la sala inicial
            if (room == bossRoom)    continue; // No cofre en la sala del boss

            if (Random.value > chestSpawnChance) continue; // Probabilidad de aparición

            ChestData picked = RollChestType(level);
            if (picked == null) continue;

            // Posición XZ: esquina si hay enemigos, centro si no
            bool hasEnemies = room.GetComponentInChildren<EnemyBase>() != null;
            Vector3 spawnPos = hasEnemies
                ? GetRoomCorner(room)
                : GetRoomCenter(room);

            // ── Corrección de Y: Raycast hacia abajo desde arriba para encontrar el suelo real ──
            float castOriginY = room.transform.position.y + 20f;
            Vector3 rayOrigin = new Vector3(spawnPos.x, castOriginY, spawnPos.z);
            int floorMask = LayerMask.GetMask("Suelo");
            if (floorMask == 0) floorMask = ~0; // si no existe la layer "Suelo", usar todas

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rayHit, 40f, floorMask))
            {
                spawnPos = new Vector3(spawnPos.x, rayHit.point.y + chestYOffset, spawnPos.z);
                Debug.Log($"[SpawnHelps] {room.name} → Raycast Y={rayHit.point.y:F2} → cofre Y={spawnPos.y:F2}");
            }
            else
            {
                // Fallback: NavMesh
                NavMeshHit nmHit;
                Vector3 searchOrigin = new Vector3(spawnPos.x, room.transform.position.y + 10f, spawnPos.z);
                if (NavMesh.SamplePosition(searchOrigin, out nmHit, 15f, NavMesh.AllAreas))
                    spawnPos = new Vector3(spawnPos.x, nmHit.position.y + chestYOffset, spawnPos.z);
                else
                    spawnPos = new Vector3(spawnPos.x, room.transform.position.y + chestYOffset, spawnPos.z);

                Debug.LogWarning($"[SpawnHelps] {room.name} → Raycast no impactó suelo, Y fallback={spawnPos.y:F2}");
            }

            GameObject chest = Instantiate(chestPrefab, spawnPos, Quaternion.identity);
            ChestController cc = chest.GetComponent<ChestController>();
            if (cc != null) cc.SetChestData(picked);

            Debug.Log($"[RoomGenerateTemplates] Cofre {picked.chestName} en {room.name} pos={spawnPos}");
        }
    }

    /// <summary>
    /// Selecciona el tipo de cofre usando pesos ponderados ajustados al nivel DDA.
    /// A mayor dificultad, más probabilidad de cofres de mayor rareza.
    /// </summary>
    private ChestData RollChestType(int difficultyLevel)
    {
        float totalWeight = 0f;
        foreach (ChestData cd in chestTypes)
            if (cd != null) totalWeight += cd.GetEffectiveSpawnWeight(difficultyLevel);

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (ChestData cd in chestTypes)
        {
            if (cd == null) continue;
            cumulative += cd.GetEffectiveSpawnWeight(difficultyLevel);
            if (roll <= cumulative) return cd;
        }
        return chestTypes[0];
    }

    /// <summary>
    /// Devuelve una posición en la esquina de la sala (alejada del centro).
    /// Usada cuando hay enemigos para no bloquear el combate.
    /// </summary>
    private Vector3 GetRoomCorner(GameObject room)
    {
        Vector3 center = GetRoomCenter(room);

        // Elegir una de las 4 esquinas aleatoriamente con un offset del 35% del tamaño
        int floorLayer = LayerMask.NameToLayer("Suelo");
        Collider[] colliders = room.GetComponentsInChildren<Collider>();
        Bounds bounds = new Bounds(center, Vector3.one);

        foreach (Collider c in colliders)
            if (c.gameObject.layer == floorLayer)
                bounds.Encapsulate(c.bounds);

        float offsetX = bounds.extents.x * 0.6f;
        float offsetZ = bounds.extents.z * 0.6f;

        Vector2[] corners = {
            new Vector2( offsetX,  offsetZ),
            new Vector2(-offsetX,  offsetZ),
            new Vector2( offsetX, -offsetZ),
            new Vector2(-offsetX, -offsetZ)
        };

        Vector2 chosen = corners[Random.Range(0, corners.Length)];
        return new Vector3(center.x + chosen.x, center.y, center.z + chosen.y);
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

    /// <summary>
    /// Devuelve el centro del suelo de la sala usando el Collider de la capa "Suelo".
    /// NO usa los SpawnPoint existentes porque esos son puntos de conexion entre salas.
    /// </summary>
    private Vector3 GetRoomCenter(GameObject room)
    {
        int floorLayer = LayerMask.NameToLayer("Suelo");
        Collider[] colliders = room.GetComponentsInChildren<Collider>();

        // ── Intento 1: collider en capa "Suelo" ──
        Bounds floorBounds = new Bounds();
        bool found = false;
        foreach (Collider c in colliders)
        {
            if (c.gameObject.layer != floorLayer) continue;
            if (!found) { floorBounds = c.bounds; found = true; }
            else floorBounds.Encapsulate(c.bounds);
        }

        if (found)
        {
            Vector3 center = new Vector3(floorBounds.center.x,
                                         floorBounds.max.y + 0.05f,
                                         floorBounds.center.z);
            return center;
        }

        // ── Intento 2: sin capa "Suelo" → usar el collider con el centro más bajo (el suelo) ──
        Bounds allBounds   = new Bounds();
        bool   anyFound    = false;
        Collider lowestCol = null;
        float lowestCY     = float.MaxValue;

        foreach (Collider c in colliders)
        {
            if (c.isTrigger) continue;
            if (!anyFound) { allBounds = c.bounds; anyFound = true; }
            else           allBounds.Encapsulate(c.bounds);

            if (c.bounds.center.y < lowestCY)
            {
                lowestCY  = c.bounds.center.y;
                lowestCol = c;
            }
        }

        if (anyFound)
        {
            // El collider con el centro Y más bajo es el suelo → usar su cara superior
            float floorY = lowestCol != null
                ? lowestCol.bounds.max.y + 0.05f
                : allBounds.min.y + 0.5f;

            Vector3 center = new Vector3(allBounds.center.x, floorY, allBounds.center.z);
            Debug.LogWarning($"[RoomCenter] {room.name} → sin capa 'Suelo', estimando suelo por collider más bajo → Y={floorY:F2}. " +
                             "Asigna la capa 'Suelo' al suelo del prefab de sala para mayor precisión.");
            return center;
        }

        // ── Intento 3: posición del transform (último recurso) ──
        Debug.LogWarning($"[RoomCenter] {room.name} → sin colliders, usando transform.position.");
        return room.transform.position;
    }

    /// <summary>
    /// Busca el punto valido mas cercano en el NavMesh a partir de una posicion origen.
    /// Si no encuentra ningun punto en el radio dado, devuelve la posicion original.
    /// </summary>
    private Vector3 GetNavMeshPosition(Vector3 origin, float searchRadius)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origin, out hit, searchRadius, NavMesh.AllAreas))
            return hit.position;

        Debug.LogWarning($"[RoomGenerateTemplates] No se encontro punto NavMesh cerca de {origin}. " +
                          "Usando posicion original.");
        return origin;
    }
}
