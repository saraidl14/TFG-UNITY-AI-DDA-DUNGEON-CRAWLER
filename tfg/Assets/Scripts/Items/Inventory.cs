/*  Nombre:      Inventory.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: Inventario singleton del jugador con 9 slots separados en armas y general.
 */
using UnityEngine;
using System;

/// <summary>
/// Inventario del jugador. Singleton persistente.
///
/// Layout de los 9 slots:
///   [0] [1] [2]  →  Slots de ARMA   (solo acepta ItemType.Weapon)
///   [3] [4] [5]  ┐
///   [6] [7] [8]  ┘  Slots GENERALES (pociones, escudos, etc.)
///
/// Los slots de arma (0-2) son los únicos que admiten ítems de tipo Weapon.
/// Los slots generales (3-8) admiten cualquier tipo excepto Weapon.
/// </summary>
public class Inventory : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────
    public static Inventory Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitSlots();
    }

    // ─────────────────────────────────────────────
    // CONSTANTES
    // ─────────────────────────────────────────────
    public const int TOTAL_SLOTS   = 9;
    public const int WEAPON_SLOTS  = 3;   // índices 0-2
    public const int GENERAL_SLOTS = 6;   // índices 3-8

    // ─────────────────────────────────────────────
    // DATOS DE SLOT
    // ─────────────────────────────────────────────

    [System.Serializable]
    public class Slot
    {
        public ItemData item     = null;
        public int      quantity = 0;

        public bool IsEmpty => item == null;
    }

    private Slot[] _slots;

    /// <summary>Acceso de solo lectura a los slots (para la UI).</summary>
    public Slot GetSlot(int index) => _slots[index];

    // ─────────────────────────────────────────────
    // EVENTOS
    // ─────────────────────────────────────────────

    /// <summary>Se dispara cuando cualquier slot cambia. La UI se suscribe para refrescarse.</summary>
    public event Action<int> OnSlotChanged;

    // ─────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────

    private void InitSlots()
    {
        _slots = new Slot[TOTAL_SLOTS];
        for (int i = 0; i < TOTAL_SLOTS; i++)
            _slots[i] = new Slot();
    }

    // ─────────────────────────────────────────────
    // CLASIFICACIÓN DE SLOTS
    // ─────────────────────────────────────────────

    /// <summary>True si el slot en ese índice es de tipo arma.</summary>
    public static bool IsWeaponSlot(int index) => index < WEAPON_SLOTS;

    /// <summary>True si este tipo de ítem puede ir en ese slot concreto.</summary>
    public static bool CanFitInSlot(ItemData item, int slotIndex)
    {
        if (item == null) return false;
        bool slotIsWeapon = IsWeaponSlot(slotIndex);
        bool itemIsWeapon = item.itemType == ItemType.Weapon;
        // Armas → solo slots 0-2 | Todo lo demás (pociones, ammo, etc.) → slots 3-8
        return slotIsWeapon == itemIsWeapon;
    }

    // ─────────────────────────────────────────────
    // AÑADIR ÍTEMS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Intenta añadir un ítem al inventario.
    /// Primero busca un slot del tipo correcto con espacio (si es stackable).
    /// Si no encuentra, busca el primer slot vacío del tipo correcto.
    /// Devuelve true si se añadió.
    /// </summary>
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;
        if (quantity <= 0) { Debug.LogWarning($"[Inventory] AddItem cantidad inválida ({quantity}) para {item.itemName}. Se ignora."); return false; }

        // Clonar SOs que tienen estado runtime propio (durabilidad, munición…)
        if (item is WeaponData || item is ManaShieldData)
            item = Instantiate(item);

        int start = item.itemType == ItemType.Weapon ? 0             : WEAPON_SLOTS;
        int end   = item.itemType == ItemType.Weapon ? WEAPON_SLOTS  : TOTAL_SLOTS;

        int remaining = quantity;

        // 1. Si es stackable, rellenar slots existentes parciales (con overflow a los siguientes)
        if (item.stackable)
        {
            for (int i = start; i < end && remaining > 0; i++)
            {
                if (_slots[i].item == item && _slots[i].quantity < item.maxStack)
                {
                    int canAdd = item.maxStack - _slots[i].quantity;
                    int toAdd  = Mathf.Min(canAdd, remaining);
                    _slots[i].quantity += toAdd;
                    remaining          -= toAdd;
                    OnSlotChanged?.Invoke(i);
                    Debug.Log($"[Inventory] +{toAdd} {item.itemName} apilado en slot {i} ({_slots[i].quantity}/{item.maxStack})");
                }
            }
            if (remaining <= 0) return true;
        }

        // 2. Buscar slots vacíos para el resto (puede necesitar varios si supera maxStack)
        for (int i = start; i < end && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int toPlace        = item.stackable ? Mathf.Min(remaining, item.maxStack) : 1;
            _slots[i].item     = item;
            _slots[i].quantity = toPlace;
            remaining         -= toPlace;
            OnSlotChanged?.Invoke(i);
            Debug.Log($"[Inventory] {item.itemName} añadido en slot {i} ({toPlace})");

            if (!item.stackable) break; // no apilable → 1 por slot
        }

        bool atLeastSome = remaining < quantity;
        if (!atLeastSome)
            Debug.Log($"[Inventory] Sin espacio para {item.itemName}.");
        else if (remaining > 0)
            Debug.Log($"[Inventory] {item.itemName}: solo {quantity - remaining}/{quantity} añadidos (inventario casi lleno).");
        return atLeastSome;
    }

    /// <summary>Dispara OnSlotChanged externamente (para refrescar UI tras cambios de durabilidad).</summary>
    public void NotifySlotChanged(int index) => OnSlotChanged?.Invoke(index);

    // ─────────────────────────────────────────────
    // RESET EN GAME OVER
    // ─────────────────────────────────────────────

    /// <summary>
    /// Vacía todos los slots al morir (game over).
    /// Las pociones persistentes se restauran por GameManager después de llamar esto.
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            if (_slots[i].IsEmpty) continue;
            _slots[i].item     = null;
            _slots[i].quantity = 0;
            OnSlotChanged?.Invoke(i);
        }
        Debug.Log("[Inventory] Inventario vaciado por game over.");
    }

    // ─────────────────────────────────────────────
    // ELIMINAR / USAR ÍTEMS
    // ─────────────────────────────────────────────

    /// <summary>Elimina un ítem del slot indicado (completo, independientemente de cantidad).</summary>
    public void RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return;
        if (_slots[slotIndex].IsEmpty) return;

        Debug.Log($"[Inventory] {_slots[slotIndex].item.itemName} eliminado del slot {slotIndex}");
        _slots[slotIndex].item     = null;
        _slots[slotIndex].quantity = 0;
        OnSlotChanged?.Invoke(slotIndex);
    }

    /// <summary>
    /// Consume una unidad del ítem en ese slot.
    /// Si quantity llega a 0 limpia el slot.
    /// Devuelve el ItemData consumido (para que el jugador aplique el efecto), o null.
    /// </summary>
    public ItemData UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return null;
        Slot slot = _slots[slotIndex];
        if (slot.IsEmpty) return null;

        ItemData used = slot.item;
        slot.quantity--;

        if (slot.quantity <= 0)
        {
            slot.item     = null;
            slot.quantity = 0;
        }

        OnSlotChanged?.Invoke(slotIndex);
        Debug.Log($"[Inventory] Usado: {used.itemName} | Quedan: {slot.quantity}");
        return used;
    }

    // ─────────────────────────────────────────────
    // REORDENAR
    // ─────────────────────────────────────────────

    /// <summary>
    /// Intercambia el contenido de dos slots.
    /// Respeta las restricciones de tipo: armas solo en slots 0-2, generales en 3-8.
    /// Si los ítems son incompatibles con el slot destino, no hace nada.
    /// </summary>
    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= TOTAL_SLOTS || b < 0 || b >= TOTAL_SLOTS || a == b) return;

        ItemData itemA = _slots[a].item;
        ItemData itemB = _slots[b].item;

        // Validar compatibilidad de tipo con el slot destino
        if (itemA != null && !CanFitInSlot(itemA, b)) return;
        if (itemB != null && !CanFitInSlot(itemB, a)) return;

        // Intercambiar
        (ItemData item, int qty) tempA = (itemA, _slots[a].quantity);
        _slots[a].item     = itemB;
        _slots[a].quantity = _slots[b].quantity;
        _slots[b].item     = tempA.item;
        _slots[b].quantity = tempA.qty;

        OnSlotChanged?.Invoke(a);
        OnSlotChanged?.Invoke(b);
        Debug.Log($"[Inventory] Swap slots {a} ↔ {b}");
    }

    // ─────────────────────────────────────────────
    // CONSULTAS
    // ─────────────────────────────────────────────

    /// <summary>True si hay al menos un ítem de ese tipo en el inventario.</summary>
    public bool HasItem(ItemType type)
    {
        for (int i = 0; i < TOTAL_SLOTS; i++)
            if (!_slots[i].IsEmpty && _slots[i].item.itemType == type)
                return true;
        return false;
    }

    /// <summary>Devuelve el primer slot que contenga ese ItemData concreto, o -1.</summary>
    public int FindItem(ItemData item)
    {
        for (int i = 0; i < TOTAL_SLOTS; i++)
            if (_slots[i].item == item)
                return i;
        return -1;
    }
}
