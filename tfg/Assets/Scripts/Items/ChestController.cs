using UnityEngine;

/// <summary>
/// Controlador del cofre en el mundo. El jugador pulsa E cerca para abrirlo.
/// Al abrirse hace el sorteo de loot y spawnea el ítem en el suelo.
/// Solo puede abrirse una vez.
///
/// Setup en Unity:
///   - Añade este script al prefab del cofre
///   - Añade un Collider (SphereCollider radio ~1.5) con Is Trigger = true
///   - Asigna ChestData en el Inspector o desde el generador de salas
///   - Asigna itemPickupPrefab (prefab con ItemPickup + Collider trigger)
/// </summary>
public class ChestController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("Datos del cofre")]
    public ChestData chestData;

    [Header("Prefab del ítem en el suelo")]
    [Tooltip("Prefab que se instancia al abrir. Debe tener ItemPickup + Collider trigger.")]
    public GameObject itemPickupPrefab;

    [Header("Interacción")]
    public KeyCode interactKey  = KeyCode.E;
    public float   interactRange = 2f;

    [Header("Visual (opcional)")]
    [Tooltip("Renderer del cofre para cambiar color según rareza.")]
    public Renderer chestRenderer;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private bool      _isOpen          = false;
    private bool      _playerNearby    = false;
    private Transform _player;

    // ─────────────────────────────────────────────
    // CICLO DE VIDA
    // ─────────────────────────────────────────────
    private void Start()
    {
        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        // Aplicar color de rareza al renderer
        if (chestRenderer != null && chestData != null)
            chestRenderer.material.color = chestData.chestColor;
    }

    private void Update()
    {
        if (_isOpen || !_playerNearby) return;

        if (Input.GetKeyDown(interactKey))
            Open();
    }

    // ─────────────────────────────────────────────
    // DETECCIÓN DE PROXIMIDAD
    // ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerNearby = true;
            Debug.Log($"[ChestController] Jugador cerca de {chestData?.chestName}. Pulsa E para abrir.");
            // TODO: mostrar prompt UI "Pulsa E para abrir"
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _playerNearby = false;
    }

    // ─────────────────────────────────────────────
    // ABRIR COFRE
    // ─────────────────────────────────────────────
    private void Open()
    {
        if (_isOpen || chestData == null) return;
        _isOpen = true;

        Debug.Log($"[ChestController] Abriendo {chestData.chestName} ({chestData.rarity})...");

        // Sorteo de loot
        LootEntry result = chestData.RollLoot();

        if (result == null || result.item == null)
        {
            Debug.Log("[ChestController] El cofre estaba vacío.");
            // TODO: animación cofre vacío
            return;
        }

        int qty = Random.Range(result.minQuantity, result.maxQuantity + 1);
        SpawnItem(result.item, qty);
    }

    // ─────────────────────────────────────────────
    // SPAWNEAR ÍTEM
    // ─────────────────────────────────────────────
    private void SpawnItem(ItemData item, int qty)
    {
        if (itemPickupPrefab == null)
        {
            // Sin prefab: añadir directamente al inventario
            if (Inventory.Instance != null)
            {
                Inventory.Instance.AddItem(item, qty);
                Debug.Log($"[ChestController] {item.itemName} x{qty} añadido directamente al inventario.");
            }
            return;
        }

        // Spawnear prefab ligeramente encima del cofre
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject go = Instantiate(itemPickupPrefab, spawnPos, Quaternion.identity);

        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.item     = item;
            pickup.quantity = qty;
        }

        Debug.Log($"[ChestController] {item.itemName} x{qty} spawneado en {spawnPos}.");
    }

    // ─────────────────────────────────────────────
    // ASIGNACIÓN DESDE EL GENERADOR
    // ─────────────────────────────────────────────

    /// <summary>
    /// Llamado por el generador de salas al instanciar el cofre.
    /// Asigna el ChestData según el sorteo de rareza.
    /// </summary>
    public void SetChestData(ChestData data)
    {
        chestData = data;
        if (chestRenderer != null && data != null)
            chestRenderer.material.color = data.chestColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
