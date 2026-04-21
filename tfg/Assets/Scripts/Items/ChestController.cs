using System.Collections;
using UnityEngine;

public class ChestController : MonoBehaviour
{
    [Header("Datos del cofre")]
    public ChestData chestData;

    [Header("Solo primera mazmorra")]
    [Tooltip("Si está activo, este cofre se destruye automáticamente en mazmorras 2+.")]
    public bool firstDungeonOnly = false;

    [Header("Loot garantizado (siempre se da al abrir)")]
    [Tooltip("Ítems que se entregan siempre, además del sorteo de ChestData. Usar para el cofre inicial (arco + flechas).")]
    public System.Collections.Generic.List<LootEntry> guaranteedLoot = new();

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

    private void Start()
    {
        // Destruir el cofre si solo debe aparecer en la primera mazmorra
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

    private void Open()
    {
        if (_isOpen) return;
        _isOpen = true;

        InteractPromptUI.Instance?.Hide();
        StartCoroutine(AnimateOpen());

        // ── Loot garantizado (siempre) ──
        foreach (LootEntry entry in guaranteedLoot)
        {
            if (entry == null || entry.item == null) continue;
            int qty = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
            StartCoroutine(AddToInventoryAfterAnim(entry.item, qty));
        }

        // ── Loot aleatorio de ChestData ──
        if (chestData != null)
        {
            LootEntry result = chestData.RollLoot();
            if (result != null && result.item != null)
            {
                int qty = Random.Range(result.minQuantity, result.maxQuantity + 1);
                StartCoroutine(AddToInventoryAfterAnim(result.item, qty));
            }
            else
            {
                Debug.Log("[ChestController] El cofre no tenía loot aleatorio.");
            }
        }
    }

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

    private IEnumerator AddToInventoryAfterAnim(ItemData item, int qty)
    {
        yield return new WaitForSeconds(openAnimDuration);

        if (Inventory.Instance != null)
        {
            bool added = Inventory.Instance.AddItem(item, qty);
            Debug.Log(added
                ? $"[ChestController] {item.itemName} x{qty} añadido al inventario."
                : $"[ChestController] Inventario lleno.");
        }
    }

    public void SetChestData(ChestData data)
    {
        chestData = data;
        if (chestRenderer != null && data != null)
            chestRenderer.material.color = data.chestColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
