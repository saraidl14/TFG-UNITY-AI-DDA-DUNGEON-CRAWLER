/*  Nombre:      AddRooms.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/05/2026
 *  Descripcion: Registra las salas instanciadas en el generador procedural del dungeon.
 */
using System.Collections.Generic;
using UnityEngine;

public class AddRooms : MonoBehaviour
{
    // Registro estático de posiciones donde ya existe una sala generada
    public static HashSet<Vector2Int> OccupiedPositions = new HashSet<Vector2Int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRegistry() => OccupiedPositions.Clear();

    void Start()
    {
        RoomGenerateTemplates templates = GameObject.FindGameObjectWithTag("Rooms")
            .GetComponent<RoomGenerateTemplates>();
        templates.rooms.Add(this.gameObject);

        Vector2Int pos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z));

        OccupiedPositions.Add(pos);
    }
}
