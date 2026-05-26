/*  Nombre:      CreditsUI.cs
 *  Autor:       Sara Iglesias
 *  Fecha:       26/05/2026
 *  Descripcion: Pantalla de créditos con páginas navegables desde el menú principal.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla de créditos con páginas navegables.
/// Mismo sistema que InstructionsUI.
/// </summary>
public class CreditsUI : MonoBehaviour
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
    public string mainMenuScene = "Start";

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
            // ── 1. DESARROLLO ──────────────────────────────────────────────
            new Page(
                "Desarrollo",
                "<b>RE-LΤVED</b>\n" +
                "Trabajo Fin de Grado — Diseño de Videjuegos\n\n" +
                "<b>Autora</b>\n" +
                "  Sara Iglesias de Lucas\n\n" +
                "<b>Motor</b>\n" +
                "  Unity 2022.3.63f LTS\n\n" +
                "<b>Assets propios</b>\n" +
                "  • Bola de fuego (modelo y efectos)\n" +
                "  • Espadas y escudo (modelado 3D)\n" +
                "  • Sistema DDA (Dificultad Dinámica Adaptativa)\n" +
                "  • Generación procedural de mazmorras\n" +
                "  • Todos los scripts de juego\n\n" +
                "<color=#AAAAAA>Todos los assets de terceros se usan bajo sus respectivas licencias.</color>"
            ),

            // ── 2. ENTORNO ─────────────────────────────────────────────────
            new Page(
                "Entorno — Assets 3D",
                "<b>Elementary Dungeon Pack Lite</b>\n" +
                "  Gridness Studios\n" +
                "  <color=#AAAAAA>Unity Asset Store (gratuito)</color>\n" +
                "  Paredes, suelos, techos, puertas, cofres,\n" +
                "  antorchas, barriles, columnas, skybox y texturas\n\n" +
                "<b>Low Poly Dungeons Lite</b>\n" +
                "  JustCreate\n" +
                "  <color=#AAAAAA>Unity Asset Store (gratuito)</color>\n" +
                "  Props decorativos: velas, botellas, columnas\n\n" +
                "<color=#FFD700>Unity Asset Store: assetstore.unity.com</color>"
            ),

            // ── 3. PERSONAJES Y ENEMIGOS ───────────────────────────────────
            new Page(
                "Personajes y Enemigos",
                "<b>POLYGON Adventure / Knights</b>\n" +
                "  Synty Studios\n" +
                "  <color=#AAAAAA>Unity Asset Store</color>\n" +
                "  Modelos de personaje, orcos, guerreros,\n" +
                "  magos y boss\n\n" +
                "<b>Dungeon Skeletons Demo</b>\n" +
                "  <color=#AAAAAA>Unity Asset Store (gratuito)</color>\n" +
                "  Modelo y animaciones del enemigo esqueleto\n" +
                "  (idle, caminar, ataque)\n\n" +
                "<b>POLYGON Starter / Prototype</b>\n" +
                "  Synty Studios\n" +
                "  <color=#AAAAAA>Unity Asset Store</color>\n" +
                "  Assets de prototipado y personajes adicionales\n\n" +
                "<color=#FFD700>syntystore.com</color>"
            ),

            // ── 4. MÚSICA ──────────────────────────────────────────────────
            new Page(
                "Música",
                "<b>Banner at the Ridge</b>\n" +
                "  <color=#AAAAAA>Música del menú principal</color>\n" +
                "  Descargado de Freesound.org\n" +
                "  Licencia Creative Commons (CC0)\n\n" +
                "<b>Against the Iron Gate</b>\n" +
                "  <color=#AAAAAA>Música de la mazmorra</color>\n" +
                "  Descargado de Freesound.org\n" +
                "  Licencia Creative Commons (CC0)\n\n" +
                "<b>Créditos</b>\n" +
                "  <color=#AAAAAA>Música de los créditos</color>\n" +
                "  Descargado de Freesound.org\n" +
                "  Licencia Creative Commons (CC0)\n\n" +
                "<color=#FFD700>freesound.org</color>"
            ),

            // ── 5. EFECTOS DE SONIDO ───────────────────────────────────────
            new Page(
                "Efectos de Sonido",
                "<b>Golpe de espada</b>  (sword.wav)\n" +
                "  Freesound.org — Licencia CC0\n\n" +
                "<b>Golpe de puño</b>  (punch.wav)\n" +
                "  Freesound.org — Licencia CC0\n\n" +
                "<b>Daño recibido</b>  (hurt1–4.mp3)\n" +
                "  Freesound.org — Licencia CC0\n" +
                "  <color=#AAAAAA>Character damage and other physical</color>\n\n" +
                "<b>Abrir cofre</b>  (chest.mp3)\n" +
                "  Freesound.org — Licencia CC0\n\n" +
                "<b>Pasos</b>  (walk.mp3)\n" +
                "  Freesound.org — Licencia CC0\n\n" +
                "<color=#FFD700>freesound.org</color>"
            ),

            // ── 6. HERRAMIENTAS Y AGRADECIMIENTOS ─────────────────────────
            new Page(
                "Herramientas y Agradecimientos",
                "<b>Motor de juego</b>\n" +
                "  Unity Technologies — unity.com\n\n" +
                "<b>Modelado 3D</b>\n" +
                "  Blender Foundation — blender.org\n\n" +
                "<b>Edición de audio</b>\n" +
                "  Audacity — audacityteam.org\n\n" +
                "<b>Control de versiones</b>\n" +
                "  Git + Unity Version Control\n\n" +
                "<b>Fuentes tipográficas</b>\n" +
                "  TextMesh Pro (Unity Technologies)\n\n\n" +
                "<color=#FFD700>Gracias por jugar  ♥</color>"
            ),
        };
    }
}
