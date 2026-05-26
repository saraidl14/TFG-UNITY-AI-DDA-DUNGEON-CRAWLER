/*  Nombre:      RoomTrigger.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       22/05/2026
 *  Descripcion: Gestiona el bloqueo/desbloqueo de puertas de una sala.
 *               Usa OverlapSphere desde el CENTRO de la sala (no desde la puerta)
 *               para evitar detectar enemigos de salas adyacentes.
 *               Detección dinámica: funciona aunque los enemigos se spawneen
 *               después de Start() (generación procedural).
 */

using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10)]   // Se ejecuta antes que DoorController.Start()
public class RoomTrigger : MonoBehaviour
{
    [Header("Puertas de esta sala")]
    [Tooltip("Dejar vacío → se auto-detectan los DoorController hijos del mismo padre.")]
    public DoorController[] doors;

    [Header("Opciones")]
    [Tooltip("Sala sin enemigos (central, tienda…). Puertas desbloqueadas al inicio.")]
    public bool noEnemyRoom = false;

    [Header("Detección de enemigos")]
    [Tooltip("Radio desde el CENTRO de la sala. Ajustar para que no llegue a salas adyacentes.")]
    public float roomRadius = 9.5f;
    public LayerMask enemyLayer;

    // ─────────────────────────────────────────────
    private bool _cleared        = false;
    private bool _hadEnemies     = false;   // true en cuanto detectamos ≥1 enemigo aquí
    private int  _peakEnemyCount = 0;       // máximo de enemigos vistos simultáneamente

    public bool IsCleared => _cleared;

    // ─────────────────────────────────────────────
    private void Start()
    {
        // Auto-detectar puertas del mismo prefab de sala
        if (doors == null || doors.Length == 0)
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            doors = root.GetComponentsInChildren<DoorController>();
        }

        // Registrarse en cada puerta (para que usen IsCleared en vez de OverlapSphere)
        foreach (DoorController door in doors)
            if (door != null) door.SetRoom(this);

        // Bloquear todas las puertas al inicio
        LockAll();

        if (noEnemyRoom)
        {
            _cleared = true;
            UnlockAll();
            Debug.Log($"[RoomTrigger] '{gameObject.name}' sala sin enemigos → desbloqueada.");
            return;
        }

        // Corrutina de seguridad: si tras X segundos no aparece ningún enemigo, desbloquear
        StartCoroutine(UnlockIfNoEnemiesAfterDelay(4f));

        Debug.Log($"[RoomTrigger] '{gameObject.name}' | Puertas: {doors.Length} | Esperando enemigos...");
    }

    private void Update()
    {
        if (_cleared) return;

        bool hasEnemies = CheckEnemiesInRoom();

        if (hasEnemies)
        {
            _hadEnemies = true;
            // Actualizar el pico de enemigos para el bonus de limpieza del DDA
            int count = CountEnemiesInRoom();
            if (count > _peakEnemyCount) _peakEnemyCount = count;
        }
        else if (_hadEnemies)
        {
            // Había enemigos y ya no quedan → sala limpia
            _cleared = true;
            UnlockAll();
            Debug.Log($"[RoomTrigger] '{gameObject.name}' limpia → puertas desbloqueadas.");

            // Notificar al sistema DDA para evaluar y resetear timer de sala
            EnemiesControllers.Instance?.NotifyRoomCleared(_peakEnemyCount);
        }
    }

    // Si tras 'delay' segundos nunca apareció ningún enemigo, la sala se considera limpia.
    // Cubre salas que el generador decide dejar sin enemigos en esa run.
    private IEnumerator UnlockIfNoEnemiesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!_cleared && !_hadEnemies)
        {
            _cleared = true;
            UnlockAll();
            Debug.Log($"[RoomTrigger] '{gameObject.name}' sin enemigos tras {delay}s → limpia.");
        }
    }

    // ─────────────────────────────────────────────
    private bool CheckEnemiesInRoom()
    {
        if (enemyLayer.value != 0)
            return Physics.OverlapSphere(transform.position, roomRadius, enemyLayer).Length > 0;

        Collider[] hits = Physics.OverlapSphere(transform.position, roomRadius);
        foreach (Collider c in hits)
            if (c.GetComponent<EnemyBase>() != null) return true;
        return false;
    }

    private int CountEnemiesInRoom()
    {
        int count = 0;
        Collider[] hits = Physics.OverlapSphere(transform.position, roomRadius,
                              enemyLayer.value != 0 ? (int)enemyLayer : ~0);
        foreach (Collider c in hits)
            if (c.GetComponent<EnemyBase>() != null) count++;
        return count;
    }

    // ─────────────────────────────────────────────
    private void LockAll()
    {
        foreach (DoorController door in doors)
            if (door != null) door.Lock();
    }

    private void UnlockAll()
    {
        foreach (DoorController door in doors)
            if (door != null) door.Unlock();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, roomRadius);
    }
}
