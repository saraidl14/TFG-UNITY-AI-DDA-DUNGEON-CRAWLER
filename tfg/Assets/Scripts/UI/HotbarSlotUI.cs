/*  Nombre:      HotbarSlotUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: Componente de cada slot del hotbar con referencias a sus elementos UI.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente que va en cada slot del hotbar (Slotbar0, Slotbar0(1)...).
/// Guarda las referencias de sus hijos. HotbarUI los encuentra con GetComponentsInChildren.
///
/// SETUP: Añadir este script a cada Slotbar0 y arrastrar sus hijos en el Inspector.
/// </summary>
public class HotbarSlotUI : MonoBehaviour
{
    [Header("Referencias del slot")]
    public Image    background;
    public Image    icon;
    public Image    selectionBorder;
    public TMP_Text quantity;
    public TMP_Text keyLabel;
}
