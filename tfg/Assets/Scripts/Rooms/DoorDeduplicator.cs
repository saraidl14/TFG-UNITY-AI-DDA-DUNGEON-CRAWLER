/*  Nombre:      DoorDeduplicator.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       22/05/2026
 *  Descripcion: Evita puertas duplicadas en dungeons procedurales.
 *               Al iniciar, busca todas las puertas de la escena y se destruye
 *               si hay otra más cercana que detectionRadius.
 *
 *  SETUP: añadir al mismo GameObject que DoorController.
 */

using UnityEngine;

public class DoorDeduplicator : MonoBehaviour
{
    [Tooltip("Distancia máxima para considerar dos puertas como duplicadas.")]
    public float detectionRadius = 3f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private void Start()
    {
        DoorDeduplicator[] allDoors = FindObjectsOfType<DoorDeduplicator>();

        foreach (DoorDeduplicator other in allDoors)
        {
            if (other == this) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < detectionRadius)
            {
                // Solo se destruye la que tiene el InstanceID más alto → siempre sobrevive la misma
                if (gameObject.GetInstanceID() > other.gameObject.GetInstanceID())
                {
                    Debug.Log($"[DoorDeduplicator] Duplicada a {dist:F2}m. Eliminando {gameObject.name}.");
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
