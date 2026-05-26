/*  Nombre:      DoorInteractZone.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       22/05/2026
 *  Descripcion: Script en InteractDoor. Detecta al jugador y avisa al DoorController.
 *               Arrastra manualmente el DoorController (Door_Middle) en el Inspector.
 */

using UnityEngine;

public class DoorInteractZone : MonoBehaviour
{
    [Tooltip("Arrastra aquí el DoorController (Door_Middle) de esta puerta.")]
    public DoorController door;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        door?.OnPlayerEnter();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        door?.OnPlayerExit();
    }
}
