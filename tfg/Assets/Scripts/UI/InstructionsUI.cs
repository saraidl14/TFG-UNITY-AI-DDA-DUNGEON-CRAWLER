/*  Nombre:      InstructionsUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       17/05/2026
 *  Descripcion: Pantalla de instrucciones con páginas navegables desde el menú principal.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla de instrucciones con páginas navegables.
/// </summary>
public class InstructionsUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // REFERENCIAS UI
    // ─────────────────────────────────────────────
    [Header("Textos")]
    public TMP_Text pageTitle;
    public TMP_Text pageContent;
    public TMP_Text pageCounter;

    [Header("Botones")]
    public Button prevBtn;
    public Button nextBtn;
    public Button menuBtn;

    [Header("Escena del menú principal")]
    public string mainMenuScene = "MainMenu";

    // ─────────────────────────────────────────────
    // DATOS DE PÁGINAS
    // ─────────────────────────────────────────────

    private struct Page
    {
        public string title;
        public string content;
        public Page(string t, string c) { title = t; content = c; }
    }

    private Page[] _pages;
    private int _currentPage = 0;

    // ─────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────

    private void Awake()
    {
        BuildPages();

        if (prevBtn != null) prevBtn.onClick.AddListener(OnPrev);
        if (nextBtn != null) nextBtn.onClick.AddListener(OnNext);
        if (menuBtn != null) menuBtn.onClick.AddListener(OnMenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Start()
    {
        ShowPage(0);
    }

    // ─────────────────────────────────────────────
    // NAVEGACIÓN
    // ─────────────────────────────────────────────

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) OnNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) OnPrev();
        if (Input.GetKeyDown(KeyCode.Escape))                                    OnMenu();
    }

    private void OnNext()
    {
        if (_currentPage < _pages.Length - 1)
            ShowPage(_currentPage + 1);
    }

    private void OnPrev()
    {
        if (_currentPage > 0)
            ShowPage(_currentPage - 1);
    }

    private void OnMenu()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    private void ShowPage(int index)
    {
        _currentPage = index;
        Page p = _pages[index];

        if (pageTitle   != null) pageTitle.text   = p.title;
        if (pageContent != null) pageContent.text = p.content;
        if (pageCounter != null) pageCounter.text = $"{index + 1} / {_pages.Length}";

        if (prevBtn != null) prevBtn.interactable = index > 0;
        if (nextBtn != null) nextBtn.interactable = index < _pages.Length - 1;
    }

    // ─────────────────────────────────────────────
    // CONTENIDO DE LAS PÁGINAS
    // ─────────────────────────────────────────────

    private void BuildPages()
    {
        _pages = new Page[]
        {
            // ── 1. OBJETIVO ──────────────────────────────────────────────
            new Page(
                "Objetivo",
                "Tu misión es explorar la mazmorra y <b>derrotar al Jefe</b> que se encuentra en la sala más alejada del centro.\n\n" +
                "Cada mazmorra completada es más difícil que la anterior: el sistema <b>DDA (Dificultad Dinámica Adaptativa)</b> " +
                "ajusta el nivel automáticamente según tu rendimiento.\n\n" +
                "Si mueres, perderás tus armas y tendrás que empezar desde el nivel 1 de dificultad.\n\n" +
                "<color=#FFD700>¡Derrota al Jefe, recoge monedas y mejora tu equipo para sobrevivir más tiempo!</color>"
            ),

            // ── 2. MOVIMIENTO ────────────────────────────────────────────
            new Page(
                "Movimiento",
                "<b>Moverse</b>\n" +
                "  W / A / S / D  →  Avanzar, retroceder y desplazarse\n\n" +
                "<b>Saltar</b>\n" +
                "  Espacio  →  Saltar\n\n" +
                "<b>Cámara</b>\n" +
                "  Ratón  →  Girar la cámara / apuntar\n\n" +
                "<b>Bonificación</b>\n" +
                "  Cada boss derrotado aumenta ligeramente tu velocidad de movimiento " +
                "(hasta un máximo acumulado).\n\n" +
                "<color=#AAAAAA>El movimiento se bloquea automáticamente al abrir el inventario.</color>"
            ),

            // ── 3. COMBATE CUERPO A CUERPO ───────────────────────────────
            new Page(
                "Combate — Cuerpo a cuerpo",
                "<b>Ataque básico</b>\n" +
                "  Clic izquierdo  →  Golpe rápido, sin coste adicional.\n\n" +
                "<b>Ataque pesado</b>\n" +
                "  Clic derecho  →  Golpe lento pero más potente (x2,5 daño, mayor rango).\n" +
                "  <color=#88CCFF>Consume stamina.</color> Si no tienes suficiente, no se ejecuta.\n\n" +
                "<b>Equipar arma</b>\n" +
                "  Teclas  1 / 2 / 3  →  Equipar el arma del slot correspondiente.\n\n" +
                "<b>Durabilidad</b>\n" +
                "  Cada golpe desgasta el arma. Al romperse, el daño baja un 30%.\n" +
                "  Repárala en el inventario (mínimo 30% de desgaste) con <color=#FFD700>monedas</color>.\n" +
                "  <color=#AAAAAA>Cuanto más desgastada, más cara la reparación.</color>\n\n" +
                "<b>Mejoras</b>\n" +
                "  Hasta nivel 5. Cada nivel sube daño +10%, velocidad +5% y durabilidad máx. +10%.\n" +
                "  Al reparar tras mejorar, se restaura la durabilidad máxima ampliada."
            ),

            // ── 4. COMBATE A DISTANCIA ────────────────────────────────────
            new Page(
                "Combate — A distancia",
                "<b>Disparar</b>\n" +
                "  Clic izquierdo  →  Disparar una flecha (con el arco equipado)\n\n" +
                "<b>Requisito</b>\n" +
                "  Para usar el arco necesitas <color=#FFD700>flechas</color> en el inventario (slots generales 4-9).\n" +
                "  Cada disparo consume una flecha automáticamente." + "(Cuando recibes el arco te dan 10 flechas)\n\n" +
                "<b>Sin flechas</b>\n" +
                "  Si te quedas sin flechas, el arco no dispara.\n" +
                "  Encuéntralas en cofres o en el cofre inicial de la primera mazmorra.\n\n" +
                "<color=#AAAAAA>El arco ocupa uno de los tres slots de arma (1-3).</color>"
            ),

            // ── 5. INVENTARIO ─────────────────────────────────────────────
            new Page(
                "Inventario",
                "<b>Abrir / cerrar</b>\n" +
                "  I  →  Abrir o cerrar el inventario\n" +
                "<b>Distribución de slots</b>\n" +
                "  Slots  1-3  →  Solo armas\n" +
                "  Slots  4-9  →  Pociones, flechas, escudos, etc.\n\n" +
                "<b>Reordenar ítems</b>\n" +
                "  Arrastra un ítem sobre otro slot para intercambiarlos.\n\n" +
                "<b>Pinear a la barra rápida</b>\n" +
                "  Shift + clic  →  Asigna ese ítem a la barra rápida (teclas 4-6).\n\n" +
                "<b>Descartar ítem</b>\n" +
                "  Clic derecho sobre un slot  →  Elimina el ítem del inventario.\n" +
                "  También puedes pulsar <b>Descartar</b> en el panel de detalle del ítem.\n\n" +
                "<b>Ver detalles</b>\n" +
                "  Clic izquierdo en un ítem  →  Abre el panel lateral con stats, mejora y reparación."
            ),

            new Page(
                "Menu de pausa",
                "<b>Abrir / cerrar</b>\n" +
                "P →  Abrir o cerrar el menú de pausa\n\n" +
                "<b>Reanudar</b>\n" +
                "  Botón 'Volver al juego' o Escape  →  Cierra el menu de pausa y reanuda la partida\n\n" +
                "<b>Salir al menú principal</b>\n" +
                "  Botón 'Menu principal'  →  Carga la escena del menú principal" +
                "\n\n<color=#AAAAAA>El menú de pausa bloquea el movimiento y el combate, pero no la cámara.</color>" +
                "\n\n<color=#AAAAAA>El menú de pausa no se muestra en esta pantalla de instrucciones, pero puedes probarlo durante el juego.</color>" +
                "<b>Salir del juego </b>\n" +
                "  Botón 'Exit'  →  Cierra la aplicación"

            ),

            // ── 6. BARRA RÁPIDA ───────────────────────────────────────────
            new Page(
                "Barra Rápida (Hotbar)",
                "<b>Slots de arma  (izquierda)</b>\n" +
                "  Tecla 1  →  Equipar arma del slot 1\n" +
                "  Tecla 2  →  Equipar arma del slot 2\n" +
                "  Tecla 3  →  Equipar arma del slot 3\n\n" +
                "<b>Slots de ítem  (derecha)</b>\n" +
                "  Tecla 4  →  Usar ítem asignado al slot 4\n" +
                "  Tecla 5  →  Usar ítem asignado al slot 5\n" +
                "  Tecla 6  →  Usar ítem asignado al slot 6\n\n" +
                "<b>Asignar ítems</b>\n" +
                "  Con el inventario abierto, pulsa  Shift + clic  en cualquier ítem " +
                "general para asignarlo al siguiente hueco disponible de la barra rápida.\n\n" +
                "<color=#AAAAAA>El borde iluminado indica el arma o ítem activo.</color>"
            ),

            // ── 7. ÍTEMS ──────────────────────────────────────────────────
            new Page(
                "Ítems",
                "<b>Pociones de salud</b>\n" +
                "  Recuperan una cantidad de HP al usarlas.\n" +
                "  Son apilables (puedes llevar varias en el mismo slot).\n\n" +
                "<b>Escudo de Maná</b>\n" +
                "  Te vuelve invencible durante unos segundos. Tiene tiempo de recarga (cooldown).\n" +
                "  • Común:  5 s de invencibilidad  |  30 s de cooldown  |  4 usos\n" +
                "  • Épico:  20 s de invencibilidad  |  90 s de cooldown  |  10 usos\n" +
                "  Se rompe y desaparece al agotar sus usos.\n\n" +
                "<b>Flechas</b>\n" +
                "  Munición para el arco. Se consumen una a una al disparar.\n" +
                "  Son apilables.\n\n" +
                "<color=#AAAAAA>Los ítems pueden obtenerse en cofres repartidos por la mazmorra.</color>"
            ),

            // ── 8. COFRES ─────────────────────────────────────────────────
            new Page(
                "Cofres",
                "<b>Interactuar</b>\n" +
                "  E  →  Abrir cofre (al estar cerca)\n\n" +
                "<b>Rareza del cofre</b>\n" +
                "  La rareza determina la calidad del loot que contiene:\n\n" +
                "  <color=#AAAAAA>■</color> Común     — ítems básicos\n" +
                "  <color=#00FF00>■</color> Poco común  — ítems decentes\n" +
                "  <color=#4488FF>■</color> Raro      — ítems buenos\n" +
                "  <color=#CC44FF>■</color> Épico     — ítems poderosos\n" +
                "  <color=#FFD700>■</color> Legendario — ítems únicos\n\n" +
                "<b>A mayor dificultad DDA</b>, más probabilidad de que aparezcan cofres de mayor rareza.\n\n" +
                "<color=#AAAAAA>El cofre inicial (solo aparece en la primera mazmorra) siempre contiene un arma de partida: " +
                "arco con flechas, una daga o una espada.</color>"
            ),

            // ── 9. MONEDAS Y MEJORAS ──────────────────────────────────────
            new Page(
                "Monedas y Mejoras",
                "<b>Obtener monedas</b>\n" +
                "  Los enemigos sueltan monedas al morir.\n\n" +
                "<b>Mejorar arma</b>\n" +
                "  Abre el inventario → selecciona el arma → pulsa <b>Mejorar</b>.\n" +
                "  Cada mejora sube el nivel del arma, aumentando daño y velocidad.\n" +
                "  El coste en monedas aumenta con cada nivel.\n\n" +
                "<b>Reparar arma</b>\n" +
                "  Abre el inventario → selecciona el arma → pulsa <b>Reparar</b>.\n" +
                "  Restaura la durabilidad al máximo.\n" +
                "  El coste depende del daño acumulado.\n\n" +
                "<color=#AAAAAA>Las monedas se reinician al morir (game over). ¡Gástalas antes de que sea tarde!</color>"
            ),

            // ── 10. SISTEMA DDA ───────────────────────────────────────────
            new Page(
                "Sistema DDA — Dificultad Adaptativa",
                "El juego ajusta la dificultad automáticamente según tu rendimiento.\n\n" +
                "<b>¿Qué evalúa el sistema?</b>\n" +
                "  • Tiempo que tardas en limpiar las salas\n" +
                "  • Salud media que conservas\n" +
                "  • Número de muertes\n" +
                "  • Ítems usados\n\n" +
                "<b>¿Qué cambia con la dificultad?</b>\n" +
                "  • Vida y daño de los enemigos\n" +
                "  • Velocidad de los enemigos\n" +
                "  • Número de enemigos por sala\n" +
                "  • Tipos de enemigos (niveles altos: orcos y magos)\n" +
                "  • Calidad de los cofres\n\n" +
                "<color=#FFD700>Si juegas muy bien, el juego se vuelve más difícil. Si tienes problemas, se hace más fácil.</color>\n\n" +
                "<color=#AAAAAA>El nivel de dificultad alcanzado se muestra en la pantalla de Game Over.</color>"
            ),
        };
    }
}
