using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestController : MonoBehaviour
{
    [Header("Datos del cofre")]
    public ChestData chestData;

    [Header("Solo primera mazmorra")]
    [Tooltip("Si está activo, este cofre se destruye automáticamente en mazmorras 2+.")]
    public bool firstDungeonOnly = false;

    [Header("Loot garantizado (siempre se da al abrir)")]
    [Tooltip("Ítems que se entregan siempre, además del sorteo de ChestData.")]
    public List<LootEntry> guaranteedLoot = new();

    [Header("Interacción")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Blend Shape (Shape Key)")]
    public SkinnedMeshRenderer chestMesh;
    public int blendShapeIndex = 0;
    public float openAnimDuration = 1f;

    [Header("Visual (opcional)")]
    public Renderer chestRenderer;

    private bool _isOpen       = false;
    private bool _playerNearby = false;

    // Loot rodado al abrir el cofre (pendiente de entregar)
    private List<(ItemData item, int qty)> _pendingLoot = new();

    // ─────────────────────────────────────────────
    private void Start()
    {
        if (firstDungeonOnly && GameManager.Instance != null && GameManager.Instance.DungeonNumber > 1)
        {
            Destroy(gameObject);
            return;
        }

        if (chestMesh == null)
            chestMesh = GetComponentInChildren<SkinnedMeshRenderer>();

        if (chestMesh != null)
            chestMesh.SetBlendShapeWeight(blendShapeIndex, 0f);

        if (chestRenderer != null && chestData != null)
            chestRenderer.material.color = chestData.chestColor;
    }

    private void Update()
    {
        if (_isOpen || !_playerNearby) return;
        if (Input.GetKeyDown(interactKey)) Open();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNearby = true;
        if (!_isOpen) InteractPromptUI.Instance?.Show("[ E ]  Abrir cofre");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNearby = false;
        InteractPromptUI.Instance?.Hide();
    }

    // ─────────────────────────────────────────────
    // ABRIR
    // ─────────────────────────────────────────────

    private void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        InteractPromptUI.Instance?.Hide();

        // ── 1. Rodar TODO el loot al abrir (guardado para darlo tras la animación) ──
        _pendingLoot.Clear();

        foreach (LootEntry entry in guaranteedLoot)
        {
            if (entry == null || entry.item == null) continue;
            int qty = Mathf.Max(1, Random.Range(entry.minQuantity, entry.maxQuantity + 1));
            _pendingLoot.Add((entry.item, qty));
        }

        if (chestData != null)
        {
            LootEntry result = chestData.RollLoot();
            if (result != null && result.item != null)
            {
                int qty = Mathf.Max(1, Random.Range(result.minQuantity, result.maxQuantity + 1));
                _pendingLoot.Add((result.item, qty));
            }
        }

        // ── 2. Animar apertura y luego intentar entregar el loot ──
        StartCoroutine(AnimateOpen());
        StartCoroutine(GiveLootAfterAnim());
    }

    // ─────────────────────────────────────────────
    // ENTREGA DE LOOT
    // ─────────────────────────────────────────────

    private IEnumerator GiveLootAfterAnim()
    {
        yield return new WaitForSeconds(openAnimDuration);
        TryGivePendingLoot();
    }

    /// <summary>
    /// Intenta añadir todos los ítems pendientes al inventario.
    /// Si alguno no cabe, muestra el panel de inventario lleno.
    /// </summary>
    private void TryGivePendingLoot()
    {
        if (_pendingLoot.Count == 0) return;
        if (Inventory.Instance == null) return;

        List<(ItemData item, int qty)> failed  = new();
        List<(ItemData item, int qty)> success = new();

        foreach ((ItemData item, int qty) in _pendingLoot)
        {
            if (item == null) continue;
            int safeQty = Mathf.Max(1, qty);          // nunca dar 0 unidades
            bool added = Inventory.Instance.AddItem(item, safeQty);
            if (added)
            {
                success.Add((item, safeQty));
                Debug.Log($"[ChestController] {item.itemName} x{safeQty} añadido al inventario.");
            }
            else
                failed.Add((item, safeQty));
        }

        _pendingLoot.Clear();

        // Mostrar popup con los ítems recibidos correctamente
        if (success.Count > 0 && LootReceivedPopupUI.Instance != null)
            LootReceivedPopupUI.Instance.Show(success);

        if (failed.Count > 0)
        {
            // Guardar los ítems que no cupieron y avisar al jugador
            _pendingLoot.AddRange(failed);
            Debug.Log($"[ChestController] {failed.Count} ítem(s) no caben en el inventario. Mostrando aviso.");

            if (InventoryFullPromptUI.Instance != null)
                InventoryFullPromptUI.Instance.Show(_pendingLoot, TryGivePendingLoot);
            else
                Debug.LogWarning("[ChestController] InventoryFullPromptUI no encontrado en escena.");
        }
    }

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────

    private IEnumerator AnimateOpen()
    {
        if (chestMesh == null) yield break;

        float elapsed = 0f;
        while (elapsed < openAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t       = Mathf.Clamp01(elapsed / openAnimDuration);
            float smoothT = t * t * (3f - 2f * t);
            chestMesh.SetBlendShapeWeight(blendShapeIndex, smoothT * 100f);
            yield return null;
        }
        chestMesh.SetBlendShapeWeight(blendShapeIndex, 100f);
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    public void SetChestData(ChestData data)
    {
        chestData = data;
        if (chestRenderer != null && data != null)
            chestRenderer.material.color = data.chestColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.5f);
    }
}
