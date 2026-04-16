using UnityEngine;

/// <summary>
/// Script temporal de prueba. Añade ítems al inventario desde el Inspector.
/// Eliminar antes de la build final.
/// </summary>
public class InventoryTester : MonoBehaviour
{
    [Header("Ítems de prueba")]
    public ItemData[] itemsToAdd;

    private void Update()
    {
        // Pulsa T en juego para añadir todos los ítems del array
        if (Input.GetKeyDown(KeyCode.T))
            AddAll();
    }

    [ContextMenu("Añadir ítems al inventario")]
    public void AddAll()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogError("[InventoryTester] Inventory.Instance es null. ¿Está el componente Inventory en escena?");
            return;
        }

        foreach (ItemData item in itemsToAdd)
        {
            if (item == null) continue;
            bool ok = Inventory.Instance.AddItem(item);
            Debug.Log($"[InventoryTester] {item.itemName} → {(ok ? "AÑADIDO" : "SIN ESPACIO")}");
        }
    }
}
