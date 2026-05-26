/*  Nombre:      Rarity.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/04/2026
 *  Descripcion: Enumeración de rareza de armas que afecta a costes y color en UI.
 */
/// <summary>
/// Rareza de un arma. Afecta al coste de mejora y reparación,
/// y al color del nombre en el panel de detalle.
/// </summary>
public enum Rarity
{
    Common,     // Blanco  — coste base
    Uncommon,   // Verde   — coste x1.5
    Rare,       // Azul    — coste x2.5
    Epic,       // Morado  — coste x4
    Legendary   // Dorado  — coste x7
}
