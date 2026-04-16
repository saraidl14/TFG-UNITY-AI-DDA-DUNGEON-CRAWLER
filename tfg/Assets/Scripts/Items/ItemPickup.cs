using UnityEngine;

/// <summary>
/// Objeto recogible del mundo. Al entrar en contacto con el jugador
/// intenta añadirse al inventario y se destruye.
///
/// Uso:
///   1. Crea un GameObject en escena (esfera/cubo o sprite)
///   2. Añade este script + un Collider con IsTrigger = true
///   3. Asigna el ItemData en el Inspector
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("Ítem")]
    public ItemData item;
    public int quantity = 1;

    [Header("Feedback")]
    [Tooltip("Texto que flota al recoger el ítem (opcional).")]
    public string pickupMessage = "";

    private bool _pickedUp = false; // evita doble recogida en el mismo frame

    private void OnTriggerEnter(Collider other)
    {
        if (_pickedUp) return;
        if (!other.CompareTag("Player")) return;
        if (item == null) return;
        if (Inventory.Instance == null) return;

        bool added = Inventory.Instance.AddItem(item, quantity);

        if (added)
        {
            _pickedUp = true;
            string msg = string.IsNullOrEmpty(pickupMessage) ? item.itemName : pickupMessage;
            Debug.Log($"[ItemPickup] Recogido: {msg}");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"[ItemPickup] Inventario lleno, no se pudo recoger: {item.itemName}");
            // El objeto NO se destruye: el jugador puede intentarlo más tarde
        }
    }
}
