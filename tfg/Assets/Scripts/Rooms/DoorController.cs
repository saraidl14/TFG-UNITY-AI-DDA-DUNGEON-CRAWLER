/*  Nombre:      DoorController.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       22/05/2026
 *  Descripcion: Puerta que se abre con E cuando no hay enemigos cerca.
 *               No depende de RoomTrigger ni eventos globales.
 *               Comprueba ella misma si hay enemigos en su radio de sala.
 */

using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Colisiones")]
    public Collider blockCollider;

    [Header("Animación")]
    public Animator doorAnimator;

    [Header("Detección de enemigos")]
    [Tooltip("Radio en el que busca enemigos. Debe cubrir la sala entera.")]
    public float roomRadius = 5f;
    public LayerMask enemyLayer;

    [Header("Mensajes")]
    public string promptUnlocked = "[ E ]  Abrir";
    public string promptLocked   = "Derrota a todos los enemigos";

    [Header("Interacción")]
    public KeyCode interactKey = KeyCode.E;

    // ─────────────────────────────────────────────
    private bool          _isOpen          = false;
    private bool          _playerNearby    = false;
    private RoomTrigger[] _allRooms;           // todas las salas de la escena (cacheadas)
    private Transform     _playerTf;           // transform del jugador (cacheado)
    private RoomTrigger   _playerEntryRoom;    // sala del jugador en el momento de acercarse

    // Llamado desde RoomTrigger (ya no necesitamos almacenar la referencia aquí)
    public void SetRoom(RoomTrigger room) { /* reservado para compatibilidad */ }

    private static readonly int HashOpen  = Animator.StringToHash("Open");
    private static readonly int HashClose = Animator.StringToHash("Close");
    private static readonly int HashIdle  = Animator.StringToHash("Idle");

    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (doorAnimator  == null) doorAnimator  = GetComponent<Animator>();
        if (blockCollider == null) blockCollider = GetComponent<Collider>();

        SetBlockCollider(true);
    }

    private void Start()
    {
        // Cachear todas las salas y el jugador (barato hacerlo una sola vez)
        _allRooms = FindObjectsOfType<RoomTrigger>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTf = player.transform;
        else Debug.LogWarning($"[DoorController] {gameObject.name}: no se encontró 'Player'.");
    }

    private void Update()
    {
        if (!_playerNearby || _isOpen) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (HasEnemiesNearby())
                InteractPromptUI.Instance?.Show(promptLocked);
            else
                Open();
        }
    }

    // ─────────────────────────────────────────────
    // DETECCIÓN DE PROXIMIDAD (desde DoorInteractZone)
    // ─────────────────────────────────────────────
    public void OnPlayerEnter()
    {
        _playerNearby    = true;
        _playerEntryRoom = GetNearestRoomToPlayer();   // captura la sala de origen
        RefreshPrompt();
    }

    public void OnPlayerExit()
    {
        _playerNearby = false;
        InteractPromptUI.Instance?.Hide();
    }

    // ─────────────────────────────────────────────
    // LÓGICA
    // ─────────────────────────────────────────────
    // Devuelve la sala más cercana al jugador en este momento
    private RoomTrigger GetNearestRoomToPlayer()
    {
        if (_playerTf == null || _allRooms == null) return null;
        RoomTrigger nearest = null;
        float best = float.MaxValue;
        foreach (RoomTrigger rt in _allRooms)
        {
            float d = Vector3.Distance(_playerTf.position, rt.transform.position);
            if (d < best) { best = d; nearest = rt; }
        }
        return nearest;
    }

    private bool HasEnemiesNearby()
    {
        // Usar la sala en la que estaba el jugador cuando se acercó (no la actual,
        // que puede cambiar al cruzar el umbral entre dos salas)
        if (_playerEntryRoom != null) return !_playerEntryRoom.IsCleared;

        // Fallback (sin RoomTrigger): OverlapSphere por componente
        Collider[] hits = Physics.OverlapSphere(transform.position, roomRadius,
                              enemyLayer.value != 0 ? (int)enemyLayer : ~0);
        foreach (Collider c in hits)
            if (c.GetComponent<EnemyBase>() != null) return true;
        return false;
    }

    private void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        SetBlockCollider(false);
        if (doorAnimator != null) doorAnimator.SetTrigger(HashOpen);
        InteractPromptUI.Instance?.Hide();
        Debug.Log($"[DoorController] {gameObject.name} abierta.");

        // Abrir también cualquier puerta adyacente (duplicado del prefab de sala vecina)
        OpenAdjacentDoors();
    }

    // Forzar apertura sin animación ni prompt (llamado desde puerta vecina)
    public void ForceOpen()
    {
        if (_isOpen) return;
        _isOpen = true;
        SetBlockCollider(false);
        if (doorAnimator != null) doorAnimator.SetTrigger(HashOpen);
    }

    private void OpenAdjacentDoors()
    {
        // Mismo radio que DoorDeduplicator para cubrir puertas en la misma posición de pared
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        foreach (Collider c in hits)
        {
            DoorController other = c.GetComponent<DoorController>();
            if (other != null && other != this)
                other.ForceOpen();
        }
    }

    // ─────────────────────────────────────────────
    // Llamados desde RoomTrigger
    // ─────────────────────────────────────────────
    public void Lock(bool animate = false)
    {
        _isOpen = false;
        SetBlockCollider(true);
        if (animate && doorAnimator != null) doorAnimator.SetTrigger(HashClose);
        if (_playerNearby) RefreshPrompt();
    }

    public void Unlock()
    {
        // El collider físico permanece activo hasta que el jugador pulse E.
        // Solo refrescamos el mensaje para que diga "[ E ] Abrir".
        if (_playerNearby) RefreshPrompt();
    }

    private void SetBlockCollider(bool active)
    {
        if (blockCollider != null)
            blockCollider.enabled = active;
    }

    private void RefreshPrompt()
    {
        if (InteractPromptUI.Instance == null || _isOpen) return;
        InteractPromptUI.Instance.Show(
            HasEnemiesNearby() ? promptLocked : promptUnlocked);
    }

    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, roomRadius);
    }
}
