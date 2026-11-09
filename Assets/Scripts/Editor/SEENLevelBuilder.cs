#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Constructores de escenas de SEEN (menú "SEEN"):
///   • Nivel 1 — Tutorial de Movimiento (escena Juego)
///   • Nivel 2 — Red de Datos (escena Nivel2)
///   • Nivel 3 — El Núcleo (escena Nivel3)
/// Cada uno limpia lo anterior, crea jugador/cámara, decoración, hazards
/// verdes, tabletas de hackeo con puertas y la info del nivel.
/// </summary>
public static class SEENLevelBuilder
{
    // Materiales
    static Material matGround, matJump, matDash, matStructure, matGoal;
    static Material matConcrete, matCyan, matMagenta, matHazard, matBoton;

    [MenuItem("SEEN/Construir Nivel Completo (Nivel 1)")]
    static void BuildFullLevel()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 1 — SEEN",
                "Se construirá el TUTORIAL completo:\n\n" +
                "• Plaza de spawn\n• Pasillo de Notificaciones\n" +
                "• Abismo de Datos (plataformeo suave)\n• Sala del Sistema\n" +
                "• 3 hackeos tutoriales (uno por tipo de puzzle)\n\n" +
                "¿Continuar?", "Sí, construir", "Cancelar")) return;

        CreateMaterials();
        CreateLevel1Materials();
        ClearOldLevel();
        SetupPlayer();
        SetupCamera();
        BuildLevel1();
        SetupAtmosphere();
        SetupPostProcessing();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] NIVEL 1 (tutorial) construido. Play para jugar.");
    }

    [MenuItem("SEEN/Construir Nivel 2 (Red de Datos)")]
    static void BuildNivel2()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 2 — SEEN",
                "Se creará una escena nueva <b>Nivel2</b>:\n\n" +
                "• Plaza de entrada\n• Plataformeo medio + plataforma móvil\n" +
                "• Pasillo con barras → HACKEO 1 (1 módulo)\n" +
                "• Torre de señal → HACKEO 2 (2 módulos)\n" +
                "• Sala final → HACKEO 3 (3 módulos) + Meta\n\n" +
                "Se reemplazará la escena abierta actual. ¿Continuar?",
                "Sí, construir", "Cancelar")) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMaterials();
        CreateLevel1Materials();
        ClearOldLevel();
        SetupPlayer();
        SetupCamera();

        var root = new GameObject("N2_Root");
        Undo.RegisterCreatedObjectUndo(root, "N2_Root");

        BuildNivel2_ZonaPlaza(root);
        BuildNivel2_ZonaTorre(root);
        BuildNivel2_ZonaFinal(root);

        RegistrarEscena("Nivel2");

        var kz = new GameObject("N2_KillZone");
        kz.transform.SetParent(root.transform, true);
        kz.transform.position = new Vector3(0f, -14f, 70f);
        var kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(400f, 4f, 500f);
        Undo.RegisterCreatedObjectUndo(kz, "KillZone");

        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(0f, 1.3f, 7f);

        L1LevelInfo(root, "Nivel 2 — Red de Datos", "Nivel3", 1);

        SetupAtmosphere();
        SetupPostProcessing();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Nivel2.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] NIVEL 2 construido: 3 hackeos (1, 2 y 3 módulos).");
    }

    // ══════════════════════════════════════════════════════════════
    //  NIVEL 4 — "PERSECUCIÓN"  (velocidad + antivirus)
    // ══════════════════════════════════════════════════════════════

    [MenuItem("SEEN/Construir Nivel 4 (Persecución)")]
    static void BuildNivel4()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 4 — SEEN",
                "Se creará una escena nueva <b>Nivel4</b>:\n\n" +
                "• Nivel de VELOCIDAD: ancho y directo\n" +
                "• Checkpoints detrás de cada puerta de hackeo\n" +
                "• ¡Un ANTIVIRUS te persigue! Si te alcanza: GAME OVER\n" +
                "• Agacharse conserva el momentum para correr\n\n" +
                "Se reemplazará la escena abierta actual. ¿Continuar?",
                "Sí, construir", "Cancelar")) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMaterials();
        CreateLevel1Materials();
        ClearOldLevel();
        SetupPlayer();
        SetupCamera();

        var root = new GameObject("N4_Root");
        Undo.RegisterCreatedObjectUndo(root, "N4_Root");

        var kz = new GameObject("N4_KillZone");
        kz.transform.SetParent(root.transform, true);
        kz.transform.position = new Vector3(0f, -14f, 55f);
        var kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(400f, 4f, 420f);
        Undo.RegisterCreatedObjectUndo(kz, "KillZone");

        BuildNivel4_Zona1(root);
        BuildNivel4_Zona2(root);
        BuildNivel4_Zona3(root);

        // ── ANTIVIRUS (persigue desde el inicio) ──
        var avGO = new GameObject("N4_Antivirus");
        avGO.transform.SetParent(root.transform, true);
        avGO.transform.position = new Vector3(0f, 2f, -12f);
        var av = avGO.AddComponent<Antivirus>();
        av.velocidad = 6.4f;
        av.retrasoInicial = 3f;
        av.radioAtrapar = 2.4f;
        Undo.RegisterCreatedObjectUndo(avGO, "Antivirus");

        RegistrarEscena("Nivel4");

        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(0f, 1.3f, -8f);

        L1LevelInfo(root, "Nivel 4 — Persecución", "Nivel5", 2);

        SetupAtmosphere();
        SetupPostProcessing();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Nivel4.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] NIVEL 4 construido: velocidad + antivirus (velocidad 8).");
    }

    static void BuildNivel4_Zona1(GameObject root)
    {
        // Zona amplia y recta (velocidad)
        PlataformaZ(root, "N4_Suelo1", -20f, 40f, 0f, 0.5f, matConcrete, 0f, 22f);
        Wall(root, "N4_W1_L", new Vector3(-11.3f, 4f, 10f), new Vector3(0.6f, 8f, 60f));
        Wall(root, "N4_W1_R", new Vector3(11.3f, 4f, 10f), new Vector3(0.6f, 8f, 60f));

        // Pantallas decorativas
        for (int i = 0; i < 8; i++)
            Platform(root, $"N4_Pantalla_{i}",
                new Vector3(-10.8f, 3f + (i % 3) * 1.6f, -14f + i * 8f),
                new Vector3(0.15f, 1.3f, 2.2f), i % 2 == 0 ? matCyan : matMagenta);
        for (int i = 0; i < 8; i++)
            Platform(root, $"N4_PantallaR_{i}",
                new Vector3(10.8f, 3f + (i % 3) * 1.6f, -14f + i * 8f),
                new Vector3(0.15f, 1.3f, 2.2f), i % 2 == 0 ? matMagenta : matCyan);

        // Farolas
        for (int i = 0; i < 6; i++)
        {
            float z = -14f + i * 10f;
            L1Cable(root, $"N4_FarolaC_{i}", new Vector3(-8f, 6.5f, z), new Vector3(-8f, 5.5f, z), 0.05f);
            Platform(root, $"N4_Farola_{i}", new Vector3(-8f, 5.35f, z), new Vector3(1.4f, 0.3f, 0.8f), matCyan);
        }

        // Primeros obstáculos: bloques verdes para saltar (a velocidad)
        L1Obstaculo(root, "N4_Obs_1", new Vector3(0f, 0.35f, 6f), 4f, 0.7f, 0.7f);
        L1Obstaculo(root, "N4_Obs_2", new Vector3(0f, 0.35f, 20f), 4f, 0.7f, 0.7f);

        L1Light(root, "N4_Light_Z1", new Vector3(0f, 5.5f, 10f), new Color(0.35f, 0.8f, 1f), 12f, 24f);

        // ── HACKEO 1 (2 módulos, sin tutorial) ──
        var p1 = L1Puerta(root, "N4_Puerta1", new Vector3(0f, 2.9f, 34f), 40f, 4.8f);
        L1Tableta(root, "N4_Tableta1", new Vector3(-3f, 0.25f, 31.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.Sincronizar }, "CANDADO 1", false, p1);
        // Checkpoint DETRÁS de la puerta
        L1Checkpoint(root, "N4_CP_1", new Vector3(0f, 0.5f, 37f), 1);
    }

    static void BuildNivel4_Zona2(GameObject root)
    {
        // Zona 2: plataformeo ancho con huecos (requiere saltos largos con momentum)
        PlataformaZ(root, "N4_Suelo2", 40f, 62f, 0f, 0.5f, matConcrete, 0f, 22f);
        Wall(root, "N4_W2_L", new Vector3(-11.3f, 4f, 51f), new Vector3(0.6f, 8f, 22f));
        Wall(root, "N4_W2_R", new Vector3(11.3f, 4f, 51f), new Vector3(0.6f, 8f, 22f));

        // Escalones para subir (plataformeo suave)
        Platform(root, "N4_Esc_1", new Vector3(0f, 1f, 46f), new Vector3(8f, 0.5f, 4f), matConcrete);
        Platform(root, "N4_Esc_2", new Vector3(0f, 2f, 51f), new Vector3(8f, 0.5f, 4f), matConcrete);
        Platform(root, "N4_Esc_3", new Vector3(0f, 3f, 56f), new Vector3(8f, 0.5f, 4f), matConcrete);

        // Hazard verde en el camino (para saltar)
        L1Obstaculo(root, "N4_Obs_3", new Vector3(0f, 3.35f, 60f), 6f, 0.7f, 0.7f);

        L1Light(root, "N4_Light_Z2", new Vector3(0f, 5.5f, 51f), new Color(0.35f, 0.8f, 1f), 10f, 22f);

        // ── HACKEO 2 (3 módulos) ──
        var p2 = L1Puerta(root, "N4_Puerta2", new Vector3(0f, 5.9f, 62f), 40f, 4.4f);
        L1Tableta(root, "N4_Tableta2", new Vector3(-3f, 3f, 59.5f),
                  new[] { HackableType.UnirCables, HackableType.Escanear, HackableType.Patron },
                  "CANDADO 2", false, p2);
        L1Checkpoint(root, "N4_CP_2", new Vector3(0f, 3.5f, 64.5f), 2);
    }

    static void BuildNivel4_Zona3(GameObject root)
    {
        // Recta final hacia la meta (pura velocidad)
        PlataformaZ(root, "N4_Suelo3", 62f, 100f, 3f, 0.5f, matConcrete, 0f, 22f);
        Wall(root, "N4_W3_L", new Vector3(-11.3f, 7f, 81f), new Vector3(0.6f, 8f, 38f));
        Wall(root, "N4_W3_R", new Vector3(11.3f, 7f, 81f), new Vector3(0.6f, 8f, 38f));

        // Guías emisivas de velocidad
        Platform(root, "N4_Guia_1", new Vector3(-3f, 3.22f, 81f), new Vector3(0.2f, 0.05f, 36f), matCyan);
        Platform(root, "N4_Guia_2", new Vector3(3f, 3.22f, 81f), new Vector3(0.2f, 0.05f, 36f), matCyan);

        // Obstáculos finales
        L1Obstaculo(root, "N4_Obs_4", new Vector3(0f, 3.35f, 72f), 6f, 0.7f, 0.7f);
        L1Obstaculo(root, "N4_Obs_5", new Vector3(0f, 3.35f, 84f), 6f, 0.7f, 0.7f);

        L1Light(root, "N4_Light_Z3", new Vector3(0f, 8.5f, 95f), new Color(0.3f, 1f, 0.5f), 14f, 24f);

        // Meta
        Platform(root, "N4_Meta", new Vector3(0f, 3.6f, 96f), new Vector3(6f, 0.4f, 6f), matGoal);
        var goal = new GameObject("N4_GoalTrigger");
        goal.transform.SetParent(root.transform, true);
        goal.transform.position = new Vector3(0f, 5.5f, 96f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(5f, 3f, 5f);
        goal.AddComponent<LevelGoal>();
        L1Beacon(root, "N4_MetaBeacon", new Vector3(0f, 5f, 96f), Color.green);

        // Torres decorativas laterales
        L1Tower(root, "N4_Torre_A", new Vector3(-14f, 3f, 90f), new Vector3(3f, 20f, 3f), false);
        L1Tower(root, "N4_Torre_B", new Vector3(14f, 3f, 90f), new Vector3(3f, 20f, 3f), true);
    }

    // ══════════════════════════════════════════════════════════════
    //  NIVEL 5 — "EVASIÓN"  (velocidad extrema + antivirus rápido)
    // ══════════════════════════════════════════════════════════════

    [MenuItem("SEEN/Construir Nivel 5 (Evasión)")]
    static void BuildNivel5()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 5 — SEEN",
                "Se creará una escena nueva <b>Nivel5</b>:\n\n" +
                "• Nivel de VELOCIDAD EXTREMA\n" +
                "• Más plataformeo, hazards y checkpoints tras puertas\n" +
                "• El antivirus es MÁS RÁPIDO: usá el momentum\n" +
                "• 3 candados de hackeo\n\n" +
                "Se reemplazará la escena abierta actual. ¿Continuar?",
                "Sí, construir", "Cancelar")) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMaterials();
        CreateLevel1Materials();
        ClearOldLevel();
        SetupPlayer();
        SetupCamera();

        var root = new GameObject("N5_Root");
        Undo.RegisterCreatedObjectUndo(root, "N5_Root");

        var kz = new GameObject("N5_KillZone");
        kz.transform.SetParent(root.transform, true);
        kz.transform.position = new Vector3(0f, -14f, 70f);
        var kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(400f, 4f, 460f);
        Undo.RegisterCreatedObjectUndo(kz, "KillZone");

        BuildNivel5_Zona1(root);
        BuildNivel5_Zona2(root);
        BuildNivel5_Zona3(root);
        BuildNivel5_ZonaFinal(root);

        // ── ANTIVIRUS (más rápido y arranca antes) ──
        var avGO = new GameObject("N5_Antivirus");
        avGO.transform.SetParent(root.transform, true);
        avGO.transform.position = new Vector3(0f, 2f, -12f);
        var av = avGO.AddComponent<Antivirus>();
        av.velocidad = 7.84f;
        av.retrasoInicial = 2f;
        av.radioAtrapar = 2.5f;
        Undo.RegisterCreatedObjectUndo(avGO, "Antivirus");

        RegistrarEscena("Nivel5");

        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(0f, 1.3f, -8f);

        L1LevelInfo(root, "Nivel 5 — Evasión", "", 2);

        SetupAtmosphere();
        SetupPostProcessing();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Nivel5.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] NIVEL 5 construido: evasión con antivirus rápido (9.8).");
    }

    static void BuildNivel5_Zona1(GameObject root)
    {
        // Recta ancha con barras rotatorias y obstáculos
        PlataformaZ(root, "N5_Suelo1", -20f, 45f, 0f, 0.5f, matConcrete, 0f, 24f);
        Wall(root, "N5_W1_L", new Vector3(-12.3f, 4.5f, 12f), new Vector3(0.6f, 9f, 65f));
        Wall(root, "N5_W1_R", new Vector3(12.3f, 4.5f, 12f), new Vector3(0.6f, 9f, 65f));

        for (int i = 0; i < 10; i++)
            Platform(root, $"N5_Pantalla_{i}",
                new Vector3(-11.8f, 3f + (i % 3) * 1.7f, -16f + i * 7f),
                new Vector3(0.15f, 1.4f, 2.4f), i % 2 == 0 ? matCyan : matMagenta);
        for (int i = 0; i < 10; i++)
            Platform(root, $"N5_PantallaR_{i}",
                new Vector3(11.8f, 3f + (i % 3) * 1.7f, -16f + i * 7f),
                new Vector3(0.15f, 1.4f, 2.4f), i % 2 == 0 ? matMagenta : matCyan);

        L1RotatingBar(root, "N5_Bar_1", new Vector3(0f, 1.5f, 10f), 9f, 55f);
        L1RotatingBar(root, "N5_Bar_2", new Vector3(0f, 1.5f, 26f), 9f, 70f);
        L1Obstaculo(root, "N5_Obs_1", new Vector3(0f, 0.35f, 18f), 6f, 0.7f, 0.7f);
        L1Obstaculo(root, "N5_Obs_2", new Vector3(0f, 0.35f, 36f), 6f, 0.7f, 0.7f);

        L1Light(root, "N5_Light_Z1", new Vector3(0f, 6f, 12f), new Color(0.35f, 0.8f, 1f), 14f, 26f);

        // ── HACKEO 1 (3 módulos) ──
        var p1 = L1Puerta(root, "N5_Puerta1", new Vector3(0f, 2.9f, 42f), 40f, 4.8f);
        L1Tableta(root, "N5_Tableta1", new Vector3(-3.5f, 0.25f, 39.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.Escanear, HackableType.Sincronizar },
                  "CANDADO 1", false, p1);
        L1Checkpoint(root, "N5_CP_1", new Vector3(0f, 0.5f, 43.5f), 1);
    }

    static void BuildNivel5_Zona2(GameObject root)
    {
        // Escaleras amplias sobre el vacío
        PlataformaZ(root, "N5_Suelo2", 45f, 70f, 0f, 0.5f, matConcrete, 0f, 24f);
        Wall(root, "N5_W2_L", new Vector3(-12.3f, 4f, 57f), new Vector3(0.6f, 8f, 25f));
        Wall(root, "N5_W2_R", new Vector3(12.3f, 4f, 57f), new Vector3(0.6f, 8f, 25f));

        Platform(root, "N5_Esc_1", new Vector3(0f, 1.5f, 50f), new Vector3(10f, 0.5f, 4f), matConcrete);
        Platform(root, "N5_Esc_2", new Vector3(0f, 3f, 55f), new Vector3(10f, 0.5f, 4f), matConcrete);
        Platform(root, "N5_Esc_3", new Vector3(0f, 4.5f, 60f), new Vector3(10f, 0.5f, 4f), matConcrete);
        Platform(root, "N5_Esc_4", new Vector3(0f, 6f, 65f), new Vector3(10f, 0.5f, 4f), matConcrete);

        L1RotatingBar(root, "N5_Bar_3", new Vector3(0f, 8.5f, 62f), 9f, 60f);
        L1Vent(root, "N5_Vent_Z2", new Vector3(0f, 0.28f, 48f), new Vector3(10f, 0.05f, 3f));

        L1Light(root, "N5_Light_Z2", new Vector3(0f, 8f, 57f), new Color(0.35f, 0.8f, 1f), 12f, 24f);

        // ── HACKEO 2 (4 módulos) ──
        var p2 = L1Puerta(root, "N5_Puerta2", new Vector3(0f, 8.9f, 70f), 40f, 4.4f);
        L1Tableta(root, "N5_Tableta2", new Vector3(-4f, 6f, 67.5f),
                  new[] { HackableType.UnirCables, HackableType.Patron, HackableType.Secuencia, HackableType.Sincronizar },
                  "CANDADO 2", false, p2);
        L1Checkpoint(root, "N5_CP_2", new Vector3(0f, 6.5f, 72f), 2);
    }

    static void BuildNivel5_Zona3(GameObject root)
    {
        // Zona de dash con plataformas separadas
        PlataformaZ(root, "N5_Suelo3", 70f, 100f, 6f, 0.5f, matConcrete, 0f, 24f);
        Wall(root, "N5_W3_L", new Vector3(-12.3f, 10f, 85f), new Vector3(0.6f, 8f, 30f));
        Wall(root, "N5_W3_R", new Vector3(12.3f, 10f, 85f), new Vector3(0.6f, 8f, 30f));

        // Plataformas de dash (tocan el suelo en sus bordes)
        PlataformaZ(root, "N5_Dash_1", 76f, 80f, 6.4f, 0.4f, matDash, 0f, 5f);
        PlataformaZ(root, "N5_Dash_2", 85f, 89f, 7.4f, 0.4f, matDash, 0f, 5f);
        PlataformaZ(root, "N5_Dash_3", 94f, 98f, 6.4f, 0.4f, matDash, 0f, 5f);

        L1Obstaculo(root, "N5_Obs_3", new Vector3(0f, 6.35f, 82f), 8f, 0.7f, 0.7f);
        L1Vent(root, "N5_Vent_Z3", new Vector3(0f, 6.28f, 92f), new Vector3(8f, 0.05f, 3f));

        L1Light(root, "N5_Light_Z3", new Vector3(0f, 12f, 85f), new Color(0.35f, 0.8f, 1f), 14f, 26f);

        // ── HACKEO 3 (5 módulos) ──
        var p3 = L1Puerta(root, "N5_Puerta3", new Vector3(0f, 9.4f, 99f), 40f, 4.6f);
        L1Tableta(root, "N5_Tableta3", new Vector3(-4f, 6.2f, 96f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Patron,
                          HackableType.Secuencia, HackableType.Sincronizar }, "CANDADO 3", false, p3);
        L1Checkpoint(root, "N5_CP_3", new Vector3(0f, 6.5f, 101f), 3);
    }

    static void BuildNivel5_ZonaFinal(GameObject root)
    {
        // Recta final
        PlataformaZ(root, "N5_Suelo4", 100f, 132f, 6f, 0.5f, matConcrete, 0f, 24f);
        Wall(root, "N5_W4_L", new Vector3(-12.3f, 10f, 116f), new Vector3(0.6f, 8f, 32f));
        Wall(root, "N5_W4_R", new Vector3(12.3f, 10f, 116f), new Vector3(0.6f, 8f, 32f));

        Platform(root, "N5_Guia_1", new Vector3(-3f, 6.22f, 116f), new Vector3(0.2f, 0.05f, 32f), matCyan);
        Platform(root, "N5_Guia_2", new Vector3(3f, 6.22f, 116f), new Vector2(0.2f, 0.05f).x > 0 ? new Vector3(0.2f, 0.05f, 32f) : new Vector3(0.2f, 0.05f, 32f), matCyan);

        L1Obstaculo(root, "N5_Obs_4", new Vector3(0f, 6.35f, 110f), 8f, 0.7f, 0.7f);
        L1Obstaculo(root, "N5_Obs_5", new Vector3(0f, 6.35f, 124f), 8f, 0.7f, 0.7f);

        L1Light(root, "N5_Light_Final", new Vector3(0f, 12f, 128f), new Color(0.3f, 1f, 0.5f), 16f, 26f);

        Platform(root, "N5_Meta", new Vector3(0f, 6.6f, 128f), new Vector3(6f, 0.4f, 6f), matGoal);
        var goal = new GameObject("N5_GoalTrigger");
        goal.transform.SetParent(root.transform, true);
        goal.transform.position = new Vector3(0f, 8.5f, 128f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(5f, 3f, 5f);
        goal.AddComponent<LevelGoal>();
        L1Beacon(root, "N5_MetaBeacon", new Vector3(0f, 8f, 128f), Color.green);

        L1Tower(root, "N5_Torre_A", new Vector3(-15f, 6f, 122f), new Vector3(3f, 22f, 3f), false);
        L1Tower(root, "N5_Torre_B", new Vector3(15f, 6f, 122f), new Vector3(3f, 22f, 3f), true);
    }

    [MenuItem("SEEN/Construir Nivel 3 (El Núcleo)")]
    static void BuildNivel3()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 3 — SEEN",
                "Se creará una escena nueva <b>Nivel3</b>:\n\n" +
                "• 5 hackeos progresivos: 1, 2, 3, 4 y 5 módulos\n" +
                "• Plataformeo exigente (gaps de 4.5 a 5.5)\n" +
                "• Más hazards y patrullas\n" +
                "• Sala del Núcleo con la Meta final\n\n" +
                "Se reemplazará la escena abierta actual. ¿Continuar?",
                "Sí, construir", "Cancelar")) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMaterials();
        CreateLevel1Materials();
        ClearOldLevel();
        SetupPlayer();
        SetupCamera();

        var root = new GameObject("N3_Root");
        Undo.RegisterCreatedObjectUndo(root, "N3_Root");

        var kz = new GameObject("N3_KillZone");
        kz.transform.SetParent(root.transform, true);
        kz.transform.position = new Vector3(0f, -14f, 80f);
        var kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(400f, 4f, 520f);
        Undo.RegisterCreatedObjectUndo(kz, "KillZone");

        BuildNivel3_Plaza(root);
        BuildNivel3_Sector1(root);
        BuildNivel3_Sector2(root);
        BuildNivel3_Sector3(root);
        BuildNivel3_Nucleo(root);

        RegistrarEscena("Nivel3");

        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(0f, 1.3f, 8f);

        L1LevelInfo(root, "Nivel 3 — El Núcleo", "Nivel4", 2);

        SetupAtmosphere();
        SetupPostProcessing();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Nivel3.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] NIVEL 3 construido: 5 hackeos (hasta 5 módulos).");
    }

    // ── Materiales ────────────────────────────────────────────────

    static void CreateMaterials()
    {
        matGround    = GetOrCreateMat("Mat_Ground",    new Color(0.22f, 0.23f, 0.26f));
        matJump      = GetOrCreateMat("Mat_Jump",      new Color(0.45f, 0.47f, 0.52f));
        matDash      = GetOrCreateMat("Mat_Dash",      new Color(0.25f, 0.45f, 0.85f));
        matStructure = GetOrCreateMat("Mat_Structure", new Color(0.10f, 0.11f, 0.14f));
        matGoal      = GetOrCreateMat("Mat_Goal",      new Color(0.20f, 0.85f, 0.30f));
    }

    static void CreateLevel1Materials()
    {
        matConcrete = GetOrCreateMat("Mat_Concrete",   new Color(0.16f, 0.17f, 0.19f));
        matCyan    = GetOrCreateEmissiveMat("Mat_ScreenCyan",    new Color(0.05f, 0.12f, 0.18f), new Color(0.1f, 0.8f, 1f), 2.2f);
        matMagenta = GetOrCreateEmissiveMat("Mat_ScreenMagenta", new Color(0.14f, 0.04f, 0.12f), new Color(1f, 0.15f, 0.6f), 2.2f);
        // Objetos dañinos: VERDE que emana luz (energía tóxica)
        matHazard  = GetOrCreateEmissiveMat("Mat_Hazard", new Color(0.02f, 0.10f, 0.04f), new Color(0.12f, 1f, 0.28f), 2.6f);
        // Botón de hackeo: cuadrado NARANJA bien visible
        matBoton   = GetOrCreateEmissiveMat("Mat_BotonNaranja", new Color(0.25f, 0.10f, 0.02f), new Color(1f, 0.45f, 0.05f), 3.2f);
    }

    static Material GetOrCreateMat(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            EnsureAssetFolder("Assets/Materials");
            Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material GetOrCreateEmissiveMat(string name, Color baseColor, Color emissionColor, float intensity)
    {
        var mat = GetOrCreateMat(name, baseColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor * intensity);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Crea las carpetas dentro de Assets si no existen.</summary>
    static void EnsureAssetFolder(string assetPath)
    {
        string[] partes = assetPath.Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(siguiente))
                AssetDatabase.CreateFolder(actual, partes[i]);
            actual = siguiente;
        }
    }

    // ── Limpieza y setup ──────────────────────────────────────────

    static void ClearOldLevel()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            // Limpia CUALQUIER objeto de cualquier nivel (evita mezclas entre escenas)
            if (go.name.StartsWith("NIVEL_") || go.name.StartsWith("L1_") ||
                go.name.StartsWith("N2_")   || go.name.StartsWith("N3_") ||
                go.name == "Plane" || go.name == "TestLevelHints" || go.name == "L1_UI")
                Undo.DestroyObjectImmediate(go);
        }
    }

    static void SetupPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }

        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);

        var col = player.GetComponent<CapsuleCollider>();
        if (col == null) col = player.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.5f;
        col.center = Vector3.zero;

        var rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // El yaw (Y) lo maneja el PlayerController con el mouse
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Controller
        var pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = player.AddComponent<PlayerController>();

        // Mirilla en primera persona
        if (player.GetComponent<Crosshair>() == null)
            player.AddComponent<Crosshair>();

        // Efectos de cámara (bob, viento del dash, onda del doble salto)
        if (player.GetComponent<HeadBob>() == null) player.AddComponent<HeadBob>();
        if (player.GetComponent<SpeedLines>() == null) player.AddComponent<SpeedLines>();
        if (player.GetComponent<JumpRing>() == null) player.AddComponent<JumpRing>();

        // Indicadores de habilidades (esquina inferior izquierda)
        if (player.GetComponent<SkillIndicators>() == null) player.AddComponent<SkillIndicators>();

        // Menú de pausa
        if (player.GetComponent<PauseMenu>() == null)
            player.AddComponent<PauseMenu>();

        // Tabla César (se abre con TAB)
        if (player.GetComponent<CipherWheel>() == null)
            player.AddComponent<CipherWheel>();

        // Respawn al caer
        if (player.GetComponent<RespawnOnFall>() == null)
            player.AddComponent<RespawnOnFall>();

        Debug.Log("[SEENLevelBuilder] Jugador listo (cápsula + Rigidbody + Controller + Mirilla + Pausa).");
    }

    static void SetupCamera()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                cam = c;
                break;
            }
        }
        if (cam == null)
        {
            var go = new GameObject("MainCamera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            Undo.RegisterCreatedObjectUndo(go, "Create Camera");
        }

        // Enganchar la cámara al jugador (primera persona)
        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        cam.tag = "MainCamera";

        // Un solo AudioListener
        var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var l in listeners)
            if (l.GetComponent<Camera>() != cam)
                Undo.DestroyObjectImmediate(l);

        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    static void SetupAtmosphere()
    {
        // Niebla distópica
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.06f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.ambientLight = new Color(0.14f, 0.14f, 0.20f);

        // Luz direccional más tenue y azulada
        var light = Object.FindFirstObjectByType<Light>();
        if (light != null && light.type == LightType.Directional)
        {
            light.color = new Color(0.75f, 0.80f, 1f);
            light.intensity = 0.9f;
        }
    }

    static void SetupPostProcessing()
    {
        string path = "Assets/Settings/L1_PostProcessing.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            EnsureAssetFolder("Assets/Settings");
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(0.8f);
        bloom.threshold.Override(0.9f);
        bloom.tint.Override(Color.white);
        bloom.scatter.Override(0.7f);

        if (!profile.TryGet(out Tonemapping tone)) tone = profile.Add<Tonemapping>();
        tone.active = true;
        tone.mode.Override(TonemappingMode.ACES);

        if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.32f);
        vignette.smoothness.Override(0.5f);
        vignette.color.Override(Color.black);

        EditorUtility.SetDirty(profile);

        // Volumen global en la escena (reemplaza si ya existe)
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (v.name == "L1_GlobalVolume")
                Undo.DestroyObjectImmediate(v.gameObject);

        var volGO = new GameObject("L1_GlobalVolume");
        var vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.profile = profile;
        vol.weight = 1f;
        vol.priority = 1;
        Undo.RegisterCreatedObjectUndo(volGO, "GlobalVolume");
    }

    static void EnsureManagers()
    {
        if (Object.FindFirstObjectByType<TestLevelHints>() == null)
        {
            var go = new GameObject("TestLevelHints");
            go.AddComponent<TestLevelHints>();
            Undo.RegisterCreatedObjectUndo(go, "Hints");
        }

        // UI de feedback en pantalla (checkpoints, pantalla de nivel completado)
        if (Object.FindFirstObjectByType<GameFeedback>() == null)
        {
            var ui = new GameObject("L1_UI");
            ui.AddComponent<GameFeedback>();
            Undo.RegisterCreatedObjectUndo(ui, "Feedback");
        }


        AsegurarPlayDesdeMenu();
    }

    /// <summary>Hace que el Play del editor arranque siempre en el menú principal.</summary>
    static void AsegurarPlayDesdeMenu()
    {
        var menuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
        if (menuAsset != null)
            EditorSceneManager.playModeStartScene = menuAsset;
    }
    // ── Helpers de geometría ──────────────────────────────────────

    static GameObject PlataformaZ(GameObject parent, string name, float zInicio, float zFin,
                                  float y, float alto, Material mat, float xCentro = 0f, float ancho = 3f)
    {
        float centro = (zInicio + zFin) / 2f;
        float profundidad = Mathf.Max(0.1f, zFin - zInicio);
        return Platform(parent, name, new Vector3(xCentro, y, centro),
                        new Vector3(ancho, alto, profundidad), mat);
    }

    static GameObject Platform(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.transform.SetParent(parent.transform);
        Undo.RegisterCreatedObjectUndo(go, name);
        return go;
    }

    static GameObject Wall(GameObject parent, string name, Vector3 pos, Vector3 scale)
        => Platform(parent, name, pos, scale, matStructure);

    /// <summary>Riel/relieve emisivo sobre una plataforma (detalle estético).</summary>
    static void L1Riel(GameObject parent, string name, Vector3 pos, Vector3 scale)
    {
        Platform(parent, name, pos, scale, matCyan);
    }

    // ══════════════════════════════════════════════════════════════
    //  NIVEL 1 — TUTORIAL DE MOVIMIENTO (escena Juego)
    // ══════════════════════════════════════════════════════════════

    static void BuildLevel1()
    {
        var root = new GameObject("L1_Root");
        Undo.RegisterCreatedObjectUndo(root, "L1_Root");

        // Zona de muerte (caídas)
        var kz = new GameObject("L1_KillZone");
        kz.transform.SetParent(root.transform, true);
        kz.transform.position = new Vector3(0f, -14f, 45f);
        var kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(400f, 4f, 400f);
        Undo.RegisterCreatedObjectUndo(kz, "KillZone");

        BuildZona0(root);
        BuildZona1(root);
        BuildZona2(root);
        BuildZona3(root);

        // Spawn del jugador
        var player = GameObject.Find("Player");
        if (player != null)
            player.transform.position = new Vector3(0f, 1.3f, 6f);

        L1LevelInfo(root, "Nivel 1 — Tutorial de Movimiento", "Nivel2", 0);

        Debug.Log("[SEENLevelBuilder] Zonas: 0-Plaza | 1-Pasillo | 2-Abismo | 3-Sala del Sistema.");
    }

    /// <summary>ZONA 0 — Punto de Infección: plaza de spawn decorada.</summary>
    static void BuildZona0(GameObject root)
    {
        // Plaza más ABIERTA (32 x 34)
        Platform(root, "L1_Plaza", new Vector3(0f, 0f, 0f), new Vector3(32f, 0.5f, 34f), matConcrete);

        Wall(root, "L1_Wall_Back", new Vector3(0f, 3.5f, -17.3f), new Vector3(32f, 7f, 0.6f));
        Wall(root, "L1_Wall_L", new Vector3(-16.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 34f));
        Wall(root, "L1_Wall_R", new Vector3(16.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 34f));
        Wall(root, "L1_Wall_FL", new Vector3(-10.3f, 3.5f, 17.3f), new Vector3(12f, 7f, 0.6f));
        Wall(root, "L1_Wall_FR", new Vector3(10.3f, 3.5f, 17.3f), new Vector3(12f, 7f, 0.6f));

        // Vigas del techo
        Wall(root, "L1_Viga_A", new Vector3(-7f, 7.2f, 0f), new Vector3(0.5f, 0.5f, 34f));
        Wall(root, "L1_Viga_B", new Vector3(0f, 7.2f, 0f), new Vector3(0.5f, 0.5f, 34f));
        Wall(root, "L1_Viga_C", new Vector3(7f, 7.2f, 0f), new Vector3(0.5f, 0.5f, 34f));

        // Arco de entrada al pasillo
        Wall(root, "L1_Arco_L", new Vector3(-4.6f, 3f, 17.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "L1_Arco_R", new Vector3(4.6f, 3f, 17.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "L1_Arco_Top", new Vector3(0f, 6.4f, 17.3f), new Vector3(10.1f, 0.7f, 0.9f));
        Platform(root, "L1_Arco_Luz", new Vector3(0f, 5.9f, 17.3f), new Vector3(9f, 0.12f, 0.4f), matCyan);

        // Pilares
        Platform(root, "L1_Pilar_A", new Vector3(-8f, 3f, 6f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        Platform(root, "L1_Pilar_B", new Vector3(8f, 3f, 6f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        Platform(root, "L1_Pilar_C", new Vector3(-8f, 3f, -10f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        Platform(root, "L1_Pilar_D", new Vector3(8f, 3f, -10f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        // Molduras emisivas en los pilares
        Platform(root, "L1_Pilar_A_Luz", new Vector3(-8f, 5.6f, 6f), new Vector3(1.7f, 0.12f, 1.7f), matCyan);
        Platform(root, "L1_Pilar_B_Luz", new Vector3(8f, 5.6f, 6f), new Vector3(1.7f, 0.12f, 1.7f), matCyan);
        Platform(root, "L1_Pilar_C_Luz", new Vector3(-8f, 5.6f, -10f), new Vector3(1.7f, 0.12f, 1.7f), matMagenta);
        Platform(root, "L1_Pilar_D_Luz", new Vector3(8f, 5.6f, -10f), new Vector3(1.7f, 0.12f, 1.7f), matMagenta);

        // Muro de publicaciones (rejilla de pantallas emisivas, más ancho)
        for (int col = 0; col < 8; col++)
        {
            for (int row = 0; row < 3; row++)
            {
                var mat = (col + row) % 2 == 0 ? matCyan : matMagenta;
                Platform(root, $"L1_Post_{col}_{row}",
                    new Vector3(-7f + col * 2f, 1.8f + row * 1.4f, -17.0f),
                    new Vector3(1.3f, 1.3f, 0.15f), mat);
            }
        }

        // Racks de servidores en ambas esquinas
        Platform(root, "L1_Rack", new Vector3(-14.5f, 2.2f, -8f), new Vector3(1.6f, 4.4f, 1.6f), matStructure);
        Platform(root, "L1_Rack_B", new Vector3(14.5f, 2.2f, -8f), new Vector3(1.6f, 4.4f, 1.6f), matStructure);
        for (int i = 0; i < 4; i++)
        {
            Platform(root, $"L1_Rack_Luz_{i}", new Vector3(-14.1f, 0.8f + i * 1.1f, -8f),
                     new Vector3(0.08f, 0.12f, 1.2f), matCyan);
            Platform(root, $"L1_RackB_Luz_{i}", new Vector3(14.1f, 0.8f + i * 1.1f, -8f),
                     new Vector3(0.08f, 0.12f, 1.2f), matMagenta);
        }

        // Cajas / escombros apilados
        Platform(root, "L1_Caja_A", new Vector3(-11f, 0.5f, 10f), new Vector3(1.2f, 1f, 1.2f), matJump);
        Platform(root, "L1_Caja_B", new Vector3(-11f, 1.5f, 10f), new Vector3(0.8f, 1f, 0.8f), matJump);
        Platform(root, "L1_Caja_C", new Vector3(12f, 0.55f, 8f), new Vector3(1.1f, 1.1f, 1.1f), matJump);
        Platform(root, "L1_Caja_D", new Vector3(12f, 1.65f, 8f), new Vector3(0.9f, 0.9f, 0.9f), matJump);

        // Guías emisivas en el piso (marcan la ruta)
        Platform(root, "L1_Guia_1", new Vector3(-3f, 0.28f, 8f), new Vector3(0.5f, 0.06f, 14f), matCyan);
        Platform(root, "L1_Guia_2", new Vector3(3f, 0.28f, 8f), new Vector3(0.5f, 0.06f, 14f), matCyan);

        // Escombros decorativos
        var rng = new System.Random(7);
        L1Debris(root, rng, 10, new Bounds(new Vector3(0f, 0.6f, 4f), new Vector3(26f, 1.2f, 22f)));

        // Caños a lo largo de las paredes laterales
        L1Cable(root, "L1_Cano_L", new Vector3(-15.9f, 6.5f, -16f), new Vector3(-15.9f, 6.5f, 16f), 0.18f);
        L1Cable(root, "L1_Cano_R", new Vector3(15.9f, 6.5f, -16f), new Vector3(15.9f, 6.5f, 16f), 0.18f);

        // Farolas de techo
        for (int i = 0; i < 3; i++)
        {
            float x = -10f + i * 10f;
            L1Cable(root, $"L1_FarolaCable_{i}", new Vector3(x, 7.0f, -8f), new Vector3(x, 6.0f, -8f), 0.05f);
            Platform(root, $"L1_Farola_{i}", new Vector3(x, 5.85f, -8f), new Vector3(1.6f, 0.3f, 0.9f), matCyan);
        }

        L1Light(root, "L1_Light_Plaza", new Vector3(0f, 5.5f, 0f),
            new Color(0.5f, 0.85f, 1f), 16f, 22f);
        L1Light(root, "L1_Light_Plaza_B", new Vector3(0f, 5.5f, -12f),
            new Color(1f, 0.2f, 0.65f), 8f, 16f);
    }

    /// <summary>ZONA 1 — Pasillo de Notificaciones: pasillo con barras rotatorias.</summary>
    static void BuildZona1(GameObject root)
    {
        // Piso CONTINUO y más ANCHO (8 unidades)
        PlataformaZ(root, "L1_Corridor", 15f, 53f, 0f, 0.5f, matConcrete, 0f, 8f);
        Wall(root, "L1_CorWall_L", new Vector3(-4.8f, 3.5f, 34f), new Vector3(0.6f, 7f, 38f));
        Wall(root, "L1_CorWall_R", new Vector3(4.8f, 3.5f, 34f), new Vector3(0.6f, 7f, 38f));

        // Tiras emisivas en lo alto de las paredes
        Platform(root, "L1_Strip_L", new Vector3(-4.4f, 6.3f, 34f), new Vector3(0.12f, 0.06f, 37f), matCyan);
        Platform(root, "L1_Strip_R", new Vector3(4.4f, 6.3f, 34f), new Vector3(0.12f, 0.06f, 37f), matCyan);

        // Paneles de techo con luces
        for (int i = 0; i < 6; i++)
        {
            float z = 18f + i * 7f;
            Wall(root, $"L1_Techo_{i}", new Vector3(0f, 7.2f, z), new Vector3(8f, 0.3f, 2.6f));
            Platform(root, $"L1_LuzTecho_{i}", new Vector3(0f, 7.0f, z), new Vector3(1.6f, 0.08f, 0.7f), matCyan);
        }

        // Rejillas de energía verde en el piso (antes de cada barrera)
        L1Vent(root, "L1_Vent_1", new Vector3(0f, 0.28f, 27f), new Vector3(6f, 0.05f, 2.2f));
        L1Vent(root, "L1_Vent_2", new Vector3(0f, 0.28f, 35f), new Vector3(6f, 0.05f, 2.2f));
        L1Vent(root, "L1_Vent_3", new Vector3(0f, 0.28f, 43f), new Vector3(6f, 0.05f, 2.2f));

        // 4 barras rotatorias ("notificaciones") a distintas velocidades
        L1RotatingBar(root, "L1_Bar_1", new Vector3(0f, 1.5f, 23f), 6.6f, 40f);
        L1RotatingBar(root, "L1_Bar_2", new Vector3(0f, 1.5f, 31f), 6.6f, 55f);
        L1RotatingBar(root, "L1_Bar_3", new Vector3(0f, 1.5f, 39f), 6.6f, 70f);
        L1RotatingBar(root, "L1_Bar_4", new Vector3(0f, 1.5f, 47f), 6.6f, 45f);

        // Obstáculo suave para saltar (tutorial: solo uno y bajito)
        L1Obstaculo(root, "L1_Obs_1", new Vector3(0f, 0.25f, 37f), 6f, 0.5f, 0.7f);

        // Cables/caños decorativos en el techo
        L1Cable(root, "L1_Cable_1", new Vector3(-4.6f, 6.8f, 18f), new Vector3(-4.6f, 6.8f, 52f), 0.07f);
        L1Cable(root, "L1_Cable_2", new Vector3(4.6f, 6.8f, 18f), new Vector3(4.6f, 6.8f, 52f), 0.07f);

        // Marco de salida hacia el abismo
        Wall(root, "L1_Puerta_L", new Vector3(-4.6f, 2.6f, 53f), new Vector3(0.5f, 5.2f, 0.5f));
        Wall(root, "L1_Puerta_R", new Vector3(4.6f, 2.6f, 53f), new Vector3(0.5f, 5.2f, 0.5f));
        Wall(root, "L1_Puerta_Top", new Vector3(0f, 5.4f, 53f), new Vector3(9.7f, 0.4f, 0.5f));
        Platform(root, "L1_Puerta_Luz", new Vector3(0f, 5.1f, 53f), new Vector3(8.4f, 0.1f, 0.3f), matMagenta);

        // ── HACKEO 1 (tutorial: DescifrarCodigo) ──
        var puerta1 = L1Puerta(root, "L1_Puerta1", new Vector3(0f, 2.4f, 50f), 54f, 4.8f);
        L1Tableta(root, "L1_Tableta1", new Vector3(-2.6f, 0.25f, 47.5f),
                  new[] { HackableType.DescifrarCodigo }, "PUERTA 1", true, puerta1);

        L1Checkpoint(root, "L1_CP_Pasillo", new Vector3(0f, 0.5f, 19f), 1);
        L1Light(root, "L1_Light_Pasillo", new Vector3(0f, 4.5f, 35f),
            new Color(0.4f, 0.8f, 1f), 12f, 20f);
        L1Light(root, "L1_Light_Pasillo_B", new Vector3(0f, 4.5f, 47f),
            new Color(0.35f, 1f, 0.5f), 8f, 14f);
    }

    /// <summary>ZONA 2 — El Abismo de Datos: plataformeo suave del tutorial.</summary>
    static void BuildZona2(GameObject root)
    {
        Platform(root, "L1_P1", new Vector3(0f, 2.5f, 56f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1Riel(root, "L1_P1_Riel", new Vector3(0f, 2.68f, 56f), new Vector3(2.5f, 0.12f, 0.3f));
        L1MovingPlatform(root, "L1_P2_MovH", new Vector3(0f, 3.8f, 61.5f), new Vector3(3f, 0.3f, 3f),
            new Vector3(7f, 0f, 0f), 1.2f);
        Platform(root, "L1_P3", new Vector3(0f, 5.5f, 67f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1Riel(root, "L1_P3_Riel", new Vector3(0f, 5.68f, 67f), new Vector3(2.5f, 0.12f, 0.3f));
        L1MovingPlatform(root, "L1_P4_MovV", new Vector3(-3.5f, 7.5f, 72f), new Vector3(3f, 0.3f, 3f),
            new Vector3(0f, 3.5f, 0f), 1f);
        Platform(root, "L1_P5", new Vector3(0f, 9.2f, 77.5f), new Vector3(4f, 0.4f, 4f), matConcrete);
        L1Riel(root, "L1_P5_Riel", new Vector3(0f, 9.43f, 77.5f), new Vector3(4f, 0.12f, 0.4f));
        L1Checkpoint(root, "L1_CP_Abismo", new Vector3(0f, 9.5f, 77.5f), 2);
        Platform(root, "L1_P6_Dash", new Vector3(0f, 9.2f, 84f), new Vector3(3.5f, 0.4f, 3.5f), matDash);
        L1Riel(root, "L1_P6_Riel", new Vector3(0f, 9.43f, 84f), new Vector3(3.5f, 0.12f, 0.4f));
        Platform(root, "L1_P7", new Vector3(0f, 9.2f, 90.5f), new Vector3(5f, 0.4f, 5f), matConcrete);
        L1Riel(root, "L1_P7_Riel", new Vector3(0f, 9.43f, 90.5f), new Vector3(5f, 0.12f, 0.5f));

        // Torres de servidores a los lados
        L1Tower(root, "L1_Tower_A", new Vector3(-10f, 4f, 56f), new Vector3(3f, 16f, 3f), false);
        L1Tower(root, "L1_Tower_B", new Vector3(10f, 4f, 56f), new Vector3(3f, 16f, 3f), true);
        L1Tower(root, "L1_Tower_C", new Vector3(-10f, 4f, 74f), new Vector3(3f, 16f, 3f), false);
        L1Tower(root, "L1_Tower_D", new Vector3(10f, 4f, 74f), new Vector3(3f, 16f, 3f), true);

        // Faros en las cimas de las torres
        Platform(root, "L1_TowerA_Faro", new Vector3(-10f, 12.3f, 56f), new Vector3(0.6f, 0.3f, 0.6f), matMagenta);
        Platform(root, "L1_TowerB_Faro", new Vector3(10f, 12.3f, 56f), new Vector3(0.6f, 0.3f, 0.6f), matMagenta);
        Platform(root, "L1_TowerC_Faro", new Vector3(-10f, 12.3f, 74f), new Vector3(0.6f, 0.3f, 0.6f), matMagenta);
        Platform(root, "L1_TowerD_Faro", new Vector3(10f, 12.3f, 74f), new Vector3(0.6f, 0.3f, 0.6f), matMagenta);

        // Barras de energía verde entre torres (decoración peligrosa)
        L1Vent(root, "L1_Energia_1", new Vector3(-10f, 6f, 65f), new Vector3(0.25f, 3.2f, 0.25f));
        L1Vent(root, "L1_Energia_2", new Vector3(10f, 6f, 65f), new Vector3(0.25f, 3.2f, 0.25f));
        L1Vent(root, "L1_Energia_3", new Vector3(0f, 2.2f, 66f), new Vector3(14f, 0.15f, 0.15f));

        // Cables entre torres
        L1Cable(root, "L1_Cable_T1", new Vector3(-10f, 12f, 56f), new Vector3(-10f, 12f, 74f), 0.08f);
        L1Cable(root, "L1_Cable_T2", new Vector3(10f, 12f, 56f), new Vector3(10f, 12f, 74f), 0.08f);
        L1Cable(root, "L1_Cable_T3", new Vector3(-10f, 12f, 56f), new Vector3(10f, 12f, 56f), 0.08f);

        // Estructuras lejanas para dar profundidad
        Platform(root, "L1_Fondo_A", new Vector3(-20f, 6f, 62f), new Vector3(6f, 22f, 6f), matStructure);
        Platform(root, "L1_Fondo_B", new Vector3(20f, 8f, 70f), new Vector3(6f, 26f, 6f), matStructure);
        Platform(root, "L1_Fondo_C", new Vector3(-18f, 4f, 88f), new Vector3(5f, 16f, 5f), matStructure);
        Platform(root, "L1_Fondo_D", new Vector3(18f, 5f, 88f), new Vector3(5f, 18f, 5f), matStructure);

        // Escombros flotantes sobre el vacío
        var rng = new System.Random(23);
        L1Debris(root, rng, 12, new Bounds(new Vector3(0f, 3.5f, 73f), new Vector3(18f, 5f, 38f)));

        L1Light(root, "L1_Light_Abismo", new Vector3(0f, 6f, 75f),
            new Color(0.3f, 0.7f, 1f), 12f, 24f);
    }

    /// <summary>ZONA 3 — Sala del Sistema: la Pantalla, el Comentario hostil y la Meta.</summary>
    static void BuildZona3(GameObject root)
    {
        // Sala más ABIERTA (34 x 30)
        Platform(root, "L1_Sala", new Vector3(0f, 8.95f, 111f), new Vector3(34f, 0.5f, 30f), matConcrete);
        Wall(root, "L1_SalaWall_Back", new Vector3(0f, 13f, 126.3f), new Vector3(34f, 9f, 0.6f));
        Wall(root, "L1_SalaWall_L", new Vector3(-17.3f, 13f, 111f), new Vector3(0.6f, 9f, 30f));
        Wall(root, "L1_SalaWall_R", new Vector3(17.3f, 13f, 111f), new Vector3(0.6f, 9f, 30f));

        // Rejilla de vigas en el techo
        for (int i = 0; i < 5; i++)
            Wall(root, $"L1_SalaViga_{i}", new Vector3(0f, 15.8f, 99f + i * 7f), new Vector3(28f, 0.4f, 0.6f));
        for (int i = 0; i < 3; i++)
            Wall(root, $"L1_SalaVigaL_{i}", new Vector3(-9f + i * 9f, 15.6f, 111f), new Vector3(0.5f, 0.5f, 30f));

        // Escenario elevado bajo la pantalla
        Platform(root, "L1_Escenario", new Vector3(0f, 9.55f, 123f), new Vector3(12f, 0.3f, 5f), matStructure);
        Platform(root, "L1_Escenario_Luz", new Vector3(0f, 9.75f, 120.8f), new Vector3(10f, 0.06f, 0.5f), matCyan);

        // La Pantalla del Sistema (monitor gigante)
        Platform(root, "L1_PantallaSistema", new Vector3(0f, 13f, 125.9f), new Vector3(9f, 4.5f, 0.4f), matMagenta);

        // Pantallas laterales
        Platform(root, "L1_Screen_A", new Vector3(13.9f, 10.5f, 101f), new Vector3(0.3f, 2.5f, 3f), matCyan);
        Platform(root, "L1_Screen_B", new Vector3(-13.9f, 10.5f, 101f), new Vector3(0.3f, 2.5f, 3f), matCyan);
        Platform(root, "L1_Screen_C", new Vector3(13.9f, 10.5f, 115f), new Vector3(0.3f, 2.5f, 3f), matCyan);
        Platform(root, "L1_Screen_D", new Vector3(-13.9f, 10.5f, 115f), new Vector3(0.3f, 2.5f, 3f), matCyan);

        // Racks de servidores a los costados
        for (int i = 0; i < 3; i++)
        {
            float z = 101f + i * 9f;
            Platform(root, $"L1_Rack_L_{i}", new Vector3(-13.2f, 10.4f, z), new Vector3(1.4f, 2.9f, 4f), matStructure);
            Platform(root, $"L1_Rack_R_{i}", new Vector3(13.2f, 10.4f, z), new Vector3(1.4f, 2.9f, 4f), matStructure);
            Platform(root, $"L1_Rack_L_Luz_{i}", new Vector3(-12.4f, 10.4f, z), new Vector3(0.08f, 2.4f, 0.5f), matCyan);
            Platform(root, $"L1_Rack_R_Luz_{i}", new Vector3(12.4f, 10.4f, z), new Vector3(0.08f, 2.4f, 0.5f), matCyan);
        }

        // Pilares
        Platform(root, "L1_SalaPilar_A", new Vector3(-5f, 12.5f, 100f), new Vector3(1.6f, 8f, 1.6f), matStructure);
        Platform(root, "L1_SalaPilar_B", new Vector3(5f, 12.5f, 100f), new Vector3(1.6f, 8f, 1.6f), matStructure);
        Platform(root, "L1_SalaPilar_C", new Vector3(-5f, 12.5f, 118f), new Vector3(1.6f, 8f, 1.6f), matStructure);
        Platform(root, "L1_SalaPilar_D", new Vector3(5f, 12.5f, 118f), new Vector3(1.6f, 8f, 1.6f), matStructure);
        Platform(root, "L1_SalaPilar_A_Luz", new Vector3(-5f, 15.6f, 100f), new Vector3(1.7f, 0.15f, 1.7f), matCyan);
        Platform(root, "L1_SalaPilar_B_Luz", new Vector3(5f, 15.6f, 100f), new Vector3(1.7f, 0.15f, 1.7f), matCyan);
        Platform(root, "L1_SalaPilar_C_Luz", new Vector3(-5f, 15.6f, 118f), new Vector3(1.7f, 0.15f, 1.7f), matMagenta);
        Platform(root, "L1_SalaPilar_D_Luz", new Vector3(5f, 15.6f, 118f), new Vector3(1.7f, 0.15f, 1.7f), matMagenta);

        // Guías emisivas en el piso hacia la meta
        Platform(root, "L1_SalaGuia_1", new Vector3(-3f, 9.22f, 108f), new Vector3(0.16f, 0.05f, 20f), matCyan);
        Platform(root, "L1_SalaGuia_2", new Vector3(3f, 9.22f, 108f), new Vector3(0.16f, 0.05f, 20f), matCyan);

        // Lámparas colgantes
        for (int i = 0; i < 3; i++)
        {
            float z = 100f + i * 10f;
            L1Cable(root, $"L1_LamparaCable_{i}", new Vector3(0f, 15.4f, z), new Vector3(0f, 13.6f, z), 0.05f);
            Platform(root, $"L1_Lampara_{i}", new Vector3(0f, 13.4f, z), new Vector3(1.4f, 0.25f, 0.8f), matCyan);
        }

        // Paneles flotantes decorativos
        Platform(root, "L1_PanelFloat_1", new Vector3(-8f, 12.5f, 110f), new Vector3(2f, 1.2f, 0.1f), matMagenta);
        Platform(root, "L1_PanelFloat_2", new Vector3(8f, 12.5f, 110f), new Vector3(2f, 1.2f, 0.1f), matCyan);
        Platform(root, "L1_PanelFloat_3", new Vector3(-8f, 13f, 121f), new Vector3(1.2f, 2f, 0.1f), matCyan);
        Platform(root, "L1_PanelFloat_4", new Vector3(8f, 13f, 121f), new Vector3(1.2f, 2f, 0.1f), matMagenta);

        // ── HACKEO 2 (tutorial: UnirCables) ──
        var puerta2 = L1Puerta(root, "L1_Puerta2", new Vector3(0f, 11.6f, 104f), 34f, 4.6f);
        L1Tableta(root, "L1_Tableta2", new Vector3(-5f, 9.2f, 101.5f),
                  new[] { HackableType.UnirCables }, "PUERTA 2", true, puerta2);

        // ── HACKEO 3 (tutorial: Patron) ──
        var puerta3 = L1Puerta(root, "L1_Puerta3", new Vector3(0f, 11.6f, 112f), 34f, 4.6f);
        L1Tableta(root, "L1_Tableta3", new Vector3(-5f, 9.2f, 109.5f),
                  new[] { HackableType.Patron }, "PUERTA 3", true, puerta3);

        // Entidad hostil: "Comentario" que patrulla
        L1Patrol(root, "L1_ENT_Comentario", new Vector3(-4f, 9.8f, 106f), new Vector3(4f, 9.8f, 106f), 2.5f);

        // Meta
        Platform(root, "L1_Meta", new Vector3(0f, 9.25f, 118f), new Vector3(4.5f, 0.4f, 4.5f), matGoal);
        var goal = new GameObject("L1_GoalTrigger");
        goal.transform.SetParent(root.transform, true);
        goal.transform.position = new Vector3(0f, 11f, 118f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(4f, 3f, 4f);
        goal.AddComponent<LevelGoal>();
        Undo.RegisterCreatedObjectUndo(goal, "GoalTrigger");

        // Marco decorativo alrededor de la meta
        Wall(root, "L1_MetaMarco_L", new Vector3(-3.4f, 10.6f, 118f), new Vector3(0.4f, 2.4f, 0.4f));
        Wall(root, "L1_MetaMarco_R", new Vector3(3.4f, 10.6f, 118f), new Vector3(0.4f, 2.4f, 0.4f));
        Wall(root, "L1_MetaMarco_T", new Vector3(0f, 11.9f, 118f), new Vector3(7.2f, 0.4f, 0.4f));
        L1Beacon(root, "L1_MetaBeacon", new Vector3(0f, 10.6f, 118f), Color.green);

        // Cables del techo
        L1Cable(root, "L1_Cable_S1", new Vector3(-14f, 15.5f, 97f), new Vector3(-14f, 15.5f, 124f), 0.07f);
        L1Cable(root, "L1_Cable_S2", new Vector3(14f, 15.5f, 97f), new Vector3(14f, 15.5f, 124f), 0.07f);

        L1Checkpoint(root, "L1_CP_Sala", new Vector3(0f, 9.6f, 99f), 3);
        L1Light(root, "L1_Light_Meta", new Vector3(0f, 13f, 118f),
            new Color(0.3f, 1f, 0.5f), 14f, 20f);
        L1Light(root, "L1_Light_Sala", new Vector3(0f, 11f, 101f),
            new Color(0.2f, 0.75f, 1f), 10f, 17f);
    }

    // ══════════════════════════════════════════════════════════════
    //  NIVEL 2 — "RED DE DATOS"  (hackeos: 1, 2 y 3 módulos)
    // ══════════════════════════════════════════════════════════════

    static void BuildNivel2_ZonaPlaza(GameObject root)
    {
        // Plaza más ABIERTA (30 x 26)
        Platform(root, "N2_Plaza", new Vector3(0f, 0f, 0f), new Vector3(30f, 0.5f, 26f), matConcrete);
        Wall(root, "N2_Wall_Back", new Vector3(0f, 3.5f, -13.3f), new Vector3(30f, 7f, 0.6f));
        Wall(root, "N2_Wall_L", new Vector3(-15.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 26f));
        Wall(root, "N2_Wall_R", new Vector3(15.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 26f));
        Wall(root, "N2_Wall_FL", new Vector3(-10.2f, 3.5f, 13.3f), new Vector3(10f, 7f, 0.6f));
        Wall(root, "N2_Wall_FR", new Vector3(10.2f, 3.5f, 13.3f), new Vector3(10f, 7f, 0.6f));

        // Muro de publicaciones
        for (int col = 0; col < 7; col++)
            for (int row = 0; row < 2; row++)
                Platform(root, $"N2_Post_{col}_{row}",
                    new Vector3(-6f + col * 2f, 2f + row * 1.5f, -13.05f),
                    new Vector3(1.4f, 1.2f, 0.15f), (col + row) % 2 == 0 ? matCyan : matMagenta);

        // Arco de salida
        Wall(root, "N2_Arco_L", new Vector3(-4.6f, 3f, 13.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "N2_Arco_R", new Vector3(4.6f, 3f, 13.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "N2_Arco_Top", new Vector3(0f, 6.4f, 13.3f), new Vector3(10.1f, 0.7f, 0.9f));
        Platform(root, "N2_Arco_Luz", new Vector3(0f, 5.9f, 13.3f), new Vector3(9f, 0.12f, 0.4f), matCyan);

        // Guías en el piso
        Platform(root, "N2_Guia", new Vector3(0f, 0.28f, 4f), new Vector3(0.5f, 0.06f, 18f), matCyan);
        L1Vent(root, "N2_VentPlaza", new Vector3(-8f, 0.28f, -4f), new Vector3(4f, 0.05f, 4f));
        L1Vent(root, "N2_VentPlaza_B", new Vector3(8f, 0.28f, -4f), new Vector3(4f, 0.05f, 4f));

        // Cajas
        Platform(root, "N2_Caja_A", new Vector3(-11f, 0.6f, 8f), new Vector3(1.4f, 1.2f, 1.4f), matJump);
        Platform(root, "N2_Caja_B", new Vector3(-11f, 1.8f, 8f), new Vector3(1f, 1.2f, 1f), matJump);
        Platform(root, "N2_Caja_C", new Vector3(11f, 0.55f, 6f), new Vector3(1.1f, 1.1f, 1.1f), matJump);

        var rng = new System.Random(13);
        L1Debris(root, rng, 10, new Bounds(new Vector3(0f, 0.7f, 2f), new Vector3(24f, 1.4f, 20f)));

        L1Light(root, "N2_Light_Plaza", new Vector3(0f, 5.5f, 0f), new Color(0.4f, 0.8f, 1f), 14f, 22f);
        L1Light(root, "N2_Light_Plaza_B", new Vector3(0f, 5.5f, -8f), new Color(1f, 0.25f, 0.65f), 8f, 16f);
    }

    static void BuildNivel2_ZonaTorre(GameObject root)
    {
        // Plataformeo medio: gaps de 3.5 a 4.5
        Platform(root, "N2_P1", new Vector3(0f, 1.5f, 17f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N2_P2", new Vector3(0f, 3.5f, 24f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N2_P3", new Vector3(2f, 5.5f, 31f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1Riel(root, "N2_P1_R", new Vector3(0f, 1.68f, 17f), new Vector3(2.5f, 0.12f, 0.3f));
        L1MovingPlatform(root, "N2_P4_MovH", new Vector3(0f, 6.5f, 38f), new Vector3(3f, 0.3f, 3f),
            new Vector3(7f, 0f, 0f), 1.2f);

        // Torres laterales
        L1Tower(root, "N2_Tower_A", new Vector3(-9f, 4f, 22f), new Vector3(3f, 16f, 3f), false);
        L1Tower(root, "N2_Tower_B", new Vector3(9f, 4f, 22f), new Vector3(3f, 16f, 3f), true);
        L1Cable(root, "N2_Cable_T", new Vector3(-9f, 12f, 22f), new Vector3(9f, 12f, 22f), 0.08f);
        var rng = new System.Random(41);
        L1Debris(root, rng, 8, new Bounds(new Vector3(0f, 3f, 26f), new Vector3(14f, 4f, 20f)));

        // Pasillo con barras (piso continuo, más ANCHO: 8.5)
        PlataformaZ(root, "N2_Corredor", 41f, 59f, 6.75f, 0.5f, matConcrete, 0f, 8.5f);
        Wall(root, "N2_CorWall_L", new Vector3(-5.1f, 10f, 50f), new Vector3(0.6f, 7f, 18f));
        Wall(root, "N2_CorWall_R", new Vector3(5.1f, 10f, 50f), new Vector3(0.6f, 7f, 18f));
        Platform(root, "N2_Strip_L", new Vector3(-4.7f, 13.2f, 50f), new Vector3(0.12f, 0.06f, 17f), matCyan);
        Platform(root, "N2_Strip_R", new Vector3(4.7f, 13.2f, 50f), new Vector3(0.12f, 0.06f, 17f), matCyan);
        for (int i = 0; i < 4; i++)
        {
            float z = 43f + i * 5f;
            Wall(root, $"N2_Techo_{i}", new Vector3(0f, 13.7f, z), new Vector3(8.5f, 0.3f, 2.2f));
            Platform(root, $"N2_LuzT_{i}", new Vector3(0f, 13.5f, z), new Vector3(1.6f, 0.08f, 0.7f), matCyan);
        }
        L1Vent(root, "N2_Vent_Cor", new Vector3(0f, 7f, 48f), new Vector3(7f, 0.05f, 2.2f));
        L1RotatingBar(root, "N2_Bar_1", new Vector3(0f, 8.2f, 44f), 7f, 45f);
        L1RotatingBar(root, "N2_Bar_2", new Vector3(0f, 8.2f, 51f), 7f, 60f);
        L1RotatingBar(root, "N2_Bar_3", new Vector3(0f, 8.2f, 56f), 7f, 52f);

        // Obstáculos verdes para saltar
        L1Obstaculo(root, "N2_Obs_1", new Vector3(0f, 7.25f, 46f), 7f, 0.5f, 0.7f);
        L1Obstaculo(root, "N2_Obs_2", new Vector3(0f, 7.25f, 54f), 7f, 0.7f, 0.7f);

        // Patrulla hostil en el pasillo
        L1Patrol(root, "N2_PatrullaCor", new Vector3(-2.5f, 8f, 57f), new Vector3(2.5f, 8f, 57f), 2.4f);

        // ── HACKEO 1 (1 módulo) ──
        var puertaA = L1Puerta(root, "N2_PuertaA", new Vector3(0f, 9f, 57f), 40f, 4.4f);
        L1Tableta(root, "N2_TabletaA", new Vector3(-2.3f, 7f, 55.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.Escanear }, "ARCHIVO 1", false, puertaA);
        L1Checkpoint(root, "N2_CP_A", new Vector3(0f, 7.5f, 60f), 1);

        // Torre de señal
        Platform(root, "N2_P5", new Vector3(0f, 8f, 64f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N2_P6", new Vector3(0f, 9.5f, 71f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "N2_P7_MovV", new Vector3(-3.5f, 9.5f, 77f), new Vector3(3f, 0.3f, 3f),
            new Vector3(0f, 3.5f, 0f), 1f);
        PlataformaZ(root, "N2_P8", 82f, 86f, 13.2f, 0.4f, matConcrete, 0f, 4f);
        L1Checkpoint(root, "N2_CP_B", new Vector3(0f, 13.5f, 84f), 2);

        // ── HACKEO 2 (2 módulos) ── plataforma continua 86..96 (tope 13.4 = P8)
        PlataformaZ(root, "N2_PuertaPlat", 86f, 96f, 13.15f, 0.5f, matConcrete, 0f, 10f);
        var puertaB = L1Puerta(root, "N2_PuertaB", new Vector3(0f, 15.6f, 90f), 30f, 4.4f);
        L1Tableta(root, "N2_TabletaB", new Vector3(-3.5f, 13.4f, 87.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Sincronizar }, "ARCHIVO 2", false, puertaB);

        // Hueco con dash: D1 toca la plataforma en 96; D2/D3 separados 4 unidades
        PlataformaZ(root, "N2_D1", 96f, 99f, 13.2f, 0.4f, matDash, 0f, 3f);
        PlataformaZ(root, "N2_D2", 103f, 106f, 13.2f, 0.4f, matDash, 0f, 3f);
        PlataformaZ(root, "N2_D3", 110f, 113f, 14f, 0.4f, matDash, 0f, 3.5f);

        L1Light(root, "N2_Light_Torre", new Vector3(0f, 11f, 72f), new Color(0.3f, 0.7f, 1f), 12f, 22f);
    }

    static void BuildNivel2_ZonaFinal(GameObject root)
    {
        // Sala más ABIERTA (32 x 22)
        PlataformaZ(root, "N2_Sala", 113f, 135f, 13.95f, 0.5f, matConcrete, 0f, 32f);
        Wall(root, "N2_SalaWall_Back", new Vector3(0f, 18f, 135.3f), new Vector3(32f, 9f, 0.6f));
        Wall(root, "N2_SalaWall_L", new Vector3(-16.3f, 18f, 124f), new Vector3(0.6f, 9f, 22f));
        Wall(root, "N2_SalaWall_R", new Vector3(16.3f, 18f, 124f), new Vector3(0.6f, 9f, 22f));

        // La Pantalla
        Platform(root, "N2_Pantalla", new Vector3(0f, 18f, 134.9f), new Vector3(9f, 4.5f, 0.4f), matMagenta);
        for (int i = 0; i < 3; i++)
        {
            Platform(root, $"N2_Rack_{i}", new Vector3(-12.2f, 15.4f, 116f + i * 8f),
                     new Vector3(1.4f, 2.9f, 4f), matStructure);
            Platform(root, $"N2_RackLuz_{i}", new Vector3(-11.4f, 15.4f, 116f + i * 8f),
                     new Vector3(0.08f, 2.4f, 0.5f), matCyan);
        }
        for (int i = 0; i < 4; i++)
            Wall(root, $"N2_SalaPilar_{i}",
                 new Vector3(i % 2 == 0 ? -5f : 5f, 17.5f, 117f + (i / 2) * 9f),
                 new Vector3(1.6f, 8f, 1.6f));

        // Guías a la meta
        Platform(root, "N2_SalaGuia_1", new Vector3(-2.5f, 14.22f, 124f), new Vector3(0.16f, 0.05f, 12f), matCyan);
        Platform(root, "N2_SalaGuia_2", new Vector3(2.5f, 14.22f, 124f), new Vector3(0.16f, 0.05f, 12f), matCyan);

        // ── HACKEO 3 (3 módulos) ──
        var puertaC = L1Puerta(root, "N2_PuertaC", new Vector3(0f, 16.4f, 119f), 32f, 4.4f);
        L1Tableta(root, "N2_TabletaC", new Vector3(-4.5f, 14.2f, 116.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Secuencia },
                  "ARCHIVO 3", false, puertaC);
        L1Checkpoint(root, "N2_CP_C", new Vector3(0f, 14.4f, 114f), 3);

        // Entidad hostil (sobre el piso, tope 14.2)
        L1Patrol(root, "N2_ENT", new Vector3(-4.5f, 14.8f, 126f), new Vector3(4.5f, 14.8f, 126f), 2.8f);

        // Meta
        Platform(root, "N2_Meta", new Vector3(0f, 14.6f, 131f), new Vector3(4.5f, 0.4f, 4.5f), matGoal);
        var goal = new GameObject("N2_GoalTrigger");
        goal.transform.SetParent(root.transform, true);
        goal.transform.position = new Vector3(0f, 16f, 131f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(4f, 3f, 4f);
        goal.AddComponent<LevelGoal>();
        L1Beacon(root, "N2_MetaBeacon", new Vector3(0f, 15.9f, 131f), Color.green);

        L1Light(root, "N2_Light_Meta", new Vector3(0f, 18f, 130f), new Color(0.3f, 1f, 0.5f), 14f, 20f);
        L1Light(root, "N2_Light_Sala", new Vector3(0f, 16f, 117f), new Color(0.2f, 0.75f, 1f), 10f, 18f);
    }

    // ══════════════════════════════════════════════════════════════
    //  NIVEL 3 — "EL NÚCLEO"  (hackeos: 1, 2, 3, 4 y 5 módulos)
    // ══════════════════════════════════════════════════════════════

    static void BuildNivel3_Plaza(GameObject root)
    {
        // Plaza más ABIERTA (34 x 30)
        Platform(root, "N3_Plaza", new Vector3(0f, 0f, 0f), new Vector3(34f, 0.5f, 30f), matConcrete);
        Wall(root, "N3_Wall_Back", new Vector3(0f, 4f, -15.3f), new Vector3(34f, 8f, 0.6f));
        Wall(root, "N3_Wall_L", new Vector3(-17.3f, 4f, 0f), new Vector3(0.6f, 8f, 30f));
        Wall(root, "N3_Wall_R", new Vector3(17.3f, 4f, 0f), new Vector3(0.6f, 8f, 30f));
        Wall(root, "N3_Wall_FL", new Vector3(-10.2f, 4f, 15.3f), new Vector3(12f, 8f, 0.6f));
        Wall(root, "N3_Wall_FR", new Vector3(10.2f, 4f, 15.3f), new Vector3(12f, 8f, 0.6f));

        // Muro de publicaciones más grande
        for (int col = 0; col < 8; col++)
            for (int row = 0; row < 3; row++)
                Platform(root, $"N3_Post_{col}_{row}",
                    new Vector3(-7f + col * 2f, 1.6f + row * 1.4f, -15.05f),
                    new Vector3(1.4f, 1.2f, 0.15f), (col + row) % 2 == 0 ? matCyan : matMagenta);

        L1Vent(root, "N3_Vent_Plaza", new Vector3(-9f, 0.28f, -6f), new Vector3(5f, 0.05f, 5f));
        L1Vent(root, "N3_Vent_Plaza_B", new Vector3(9f, 0.28f, -6f), new Vector3(5f, 0.05f, 5f));
        Platform(root, "N3_Guia", new Vector3(0f, 0.28f, 8f), new Vector3(0.5f, 0.06f, 16f), matMagenta);

        // Cajas y torres decorativas
        Platform(root, "N3_Caja_A", new Vector3(-13f, 0.6f, 9f), new Vector3(1.4f, 1.2f, 1.4f), matJump);
        Platform(root, "N3_Caja_B", new Vector3(13f, 0.55f, 7f), new Vector3(1.1f, 1.1f, 1.1f), matJump);
        L1Tower(root, "N3_PlazaTorre_A", new Vector3(-15f, 4f, 2f), new Vector3(3f, 18f, 3f), false);
        L1Tower(root, "N3_PlazaTorre_B", new Vector3(15f, 4f, 2f), new Vector3(3f, 18f, 3f), true);

        var rng = new System.Random(29);
        L1Debris(root, rng, 10, new Bounds(new Vector3(0f, 0.7f, 4f), new Vector3(26f, 1.4f, 20f)));

        L1Light(root, "N3_Light_Plaza", new Vector3(0f, 6f, 0f), new Color(0.4f, 0.8f, 1f), 16f, 24f);
        L1Light(root, "N3_Light_Plaza_B", new Vector3(0f, 6f, -10f), new Color(1f, 0.25f, 0.65f), 9f, 18f);
    }

    static void BuildNivel3_Sector1(GameObject root)
    {
        // Plataformeo exigente: gaps de 4.5 a 5.5
        Platform(root, "N3_P1", new Vector3(0f, 1.5f, 20f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N3_P2", new Vector3(0f, 3.2f, 28f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "N3_P3_MovH", new Vector3(0f, 4.5f, 36f), new Vector3(3f, 0.3f, 3f),
            new Vector3(8f, 0f, 0f), 1.3f);
        PlataformaZ(root, "N3_P4", 41.5f, 44.5f, 6f, 0.3f, matConcrete, 0f, 3f);
        L1Tower(root, "N3_Tower_A", new Vector3(-10f, 3f, 24f), new Vector3(3f, 14f, 3f), false);
        L1Tower(root, "N3_Tower_B", new Vector3(10f, 3f, 24f), new Vector3(3f, 14f, 3f), true);

        // Pasillo peligroso: PEGADO a P4 (empieza en 44.5), tope 6.25
        PlataformaZ(root, "N3_Pasillo", 44.5f, 60.5f, 6f, 0.5f, matConcrete, 0f, 8.5f);
        Wall(root, "N3_PasoWall_L", new Vector3(-5.1f, 9f, 52.5f), new Vector3(0.6f, 6f, 16f));
        Wall(root, "N3_PasoWall_R", new Vector3(5.1f, 9f, 52.5f), new Vector3(0.6f, 6f, 16f));
        Platform(root, "N3_PasoStrip", new Vector3(0f, 12.2f, 52.5f), new Vector3(5.5f, 0.1f, 15f), matCyan);
        L1RotatingBar(root, "N3_Bar_1", new Vector3(0f, 7.6f, 47f), 7.4f, 50f);
        L1RotatingBar(root, "N3_Bar_2", new Vector3(0f, 7.6f, 54f), 7.4f, 65f);
        L1RotatingBar(root, "N3_Bar_2b", new Vector3(0f, 7.6f, 59f), 7.4f, 55f);
        L1Vent(root, "N3_Vent_Paso", new Vector3(0f, 6.28f, 50f), new Vector3(7f, 0.05f, 2f));
        // Obstáculos verdes para saltar
        L1Obstaculo(root, "N3_Obs_1", new Vector3(0f, 6.25f, 45.5f), 7f, 0.5f, 0.7f);
        L1Obstaculo(root, "N3_Obs_2", new Vector3(0f, 6.25f, 51.5f), 7f, 0.5f, 0.7f);
        L1Patrol(root, "N3_Patrulla_1", new Vector3(-2.5f, 6.9f, 56f), new Vector3(2.5f, 6.9f, 56f), 2.2f);

        // ── HACKEO 1 (1 módulo) ──
        var p1 = L1Puerta(root, "N3_Puerta1", new Vector3(0f, 8.4f, 58.5f), 40f, 4.6f);
        L1Tableta(root, "N3_Tableta1", new Vector3(-2.3f, 6.25f, 56.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.Patron, HackableType.Sincronizar }, "NODO 1", false, p1);
        L1Checkpoint(root, "N3_CP_1", new Vector3(0f, 6.5f, 63f), 1);
    }

    static void BuildNivel3_Sector2(GameObject root)
    {
        // Torre con plataforma vertical
        Platform(root, "N3_P5", new Vector3(0f, 7.5f, 66f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N3_P6", new Vector3(0f, 9f, 74f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "N3_P7_MovV", new Vector3(-3.5f, 9f, 80f), new Vector3(3f, 0.3f, 3f),
            new Vector3(0f, 4f, 0f), 1.1f);
        PlataformaZ(root, "N3_P8", 85f, 89f, 13f, 0.4f, matConcrete, 0f, 4f);
        L1Checkpoint(root, "N3_CP_2", new Vector3(0f, 13.3f, 87f), 2);

        // ── HACKEO 2 (2 módulos) ── plataforma 89..99, mismo tope que P8 (13.2)
        PlataformaZ(root, "N3_Plat2", 89f, 99f, 12.95f, 0.5f, matConcrete, 0f, 10f);
        var p2 = L1Puerta(root, "N3_Puerta2", new Vector3(0f, 15.4f, 94f), 30f, 4.4f);
        L1Tableta(root, "N3_Tableta2", new Vector3(-3.5f, 13.2f, 91.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.Secuencia, HackableType.UnirCables }, "NODO 2", false, p2);

        // Plataforma de dash, pegada a Plat2 en 99
        PlataformaZ(root, "N3_D1", 99f, 102f, 13f, 0.4f, matDash, 0f, 3f);
        L1Vent(root, "N3_Vent_Gap", new Vector3(0f, 12f, 103.5f), new Vector3(0.2f, 3f, 3f));

        L1Light(root, "N3_Light_S2", new Vector3(0f, 10f, 78f), new Color(0.3f, 0.7f, 1f), 12f, 22f);
    }

    static void BuildNivel3_Sector3(GameObject root)
    {
        // Pasillo de hazards: separado de D1 por un hueco de 3 (salto)
        PlataformaZ(root, "N3_Pasillo2", 105f, 122f, 12.95f, 0.5f, matConcrete, 0f, 8.5f);
        Wall(root, "N3_Paso2Wall_L", new Vector3(-5.1f, 15.5f, 113.5f), new Vector3(0.6f, 6f, 17f));
        Wall(root, "N3_Paso2Wall_R", new Vector3(5.1f, 15.5f, 113.5f), new Vector3(0.6f, 6f, 17f));
        Platform(root, "N3_Paso2Strip", new Vector3(0f, 19f, 113.5f), new Vector3(5.5f, 0.1f, 16f), matMagenta);
        L1RotatingBar(root, "N3_Bar_3", new Vector3(0f, 14.8f, 107f), 7.4f, 55f);
        L1RotatingBar(root, "N3_Bar_4", new Vector3(0f, 14.8f, 113f), 7.4f, 70f);
        L1RotatingBar(root, "N3_Bar_5", new Vector3(0f, 14.8f, 119f), 7.4f, 60f);
        L1Vent(root, "N3_Vent_Paso2", new Vector3(0f, 13.2f, 110f), new Vector3(7f, 0.05f, 2f));
        // Obstáculos verdes
        L1Obstaculo(root, "N3_Obs_3", new Vector3(0f, 13.2f, 108.5f), 7f, 0.6f, 0.7f);
        L1Obstaculo(root, "N3_Obs_4", new Vector3(0f, 13.2f, 116f), 7f, 0.6f, 0.7f);
        L1Patrol(root, "N3_Patrulla_2", new Vector3(-2.5f, 13.8f, 121f), new Vector3(2.5f, 13.8f, 121f), 2.6f);

        // ── HACKEO 3 (3 módulos) ──
        var p3 = L1Puerta(root, "N3_Puerta3", new Vector3(0f, 15.6f, 120.5f), 40f, 4.4f);
        L1Tableta(root, "N3_Tableta3", new Vector3(-2.3f, 13.2f, 106.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.Patron, HackableType.Secuencia },
                  "NODO 3", false, p3);
        L1Checkpoint(root, "N3_CP_3", new Vector3(0f, 13.5f, 104f), 3);

        // Plataformas de aproximación al núcleo (gaps de 4)
        PlataformaZ(root, "N3_P9", 126f, 129f, 13f, 0.4f, matConcrete, 0f, 3f);
        PlataformaZ(root, "N3_P10", 133f, 136.5f, 13f, 0.4f, matConcrete, 0f, 3.5f);

        // ── HACKEO 4 (4 módulos) ── plataforma 136.5..146, pegada a P10
        PlataformaZ(root, "N3_Plat4", 136.5f, 146f, 12.95f, 0.5f, matConcrete, 0f, 9f);
        var p4 = L1Puerta(root, "N3_Puerta4", new Vector3(0f, 15.4f, 141.5f), 30f, 4.4f);
        L1Tableta(root, "N3_Tableta4", new Vector3(-3.5f, 13.2f, 138.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Patron, HackableType.Sincronizar }, "NODO 4", false, p4);

        L1Checkpoint(root, "N3_CP_4", new Vector3(0f, 13.4f, 138f), 4);
    }

    static void BuildNivel3_Nucleo(GameObject root)
    {
        // Núcleo: piso continuo 146..168, mismo tope que Plat4 (13.2)
        PlataformaZ(root, "N3_Nucleo", 146f, 168f, 12.95f, 0.5f, matConcrete, 0f, 32f);
        Wall(root, "N3_NucWall_Back", new Vector3(0f, 19f, 168.3f), new Vector3(32f, 9f, 0.6f));
        Wall(root, "N3_NucWall_L", new Vector3(-16.3f, 19f, 157f), new Vector3(0.6f, 9f, 22f));
        Wall(root, "N3_NucWall_R", new Vector3(16.3f, 19f, 157f), new Vector3(0.6f, 9f, 22f));

        // Pantalla del Núcleo
        Platform(root, "N3_Pantalla", new Vector3(0f, 19f, 167.9f), new Vector3(10f, 5f, 0.4f), matMagenta);
        for (int i = 0; i < 4; i++)
            Platform(root, $"N3_NucRack_{i}",
                     new Vector3(i % 2 == 0 ? -12.2f : 12.2f, 16.4f, 148f + (i / 2) * 10f),
                     new Vector3(1.4f, 2.9f, 4f), matStructure);
        for (int i = 0; i < 4; i++)
            Wall(root, $"N3_NucPilar_{i}",
                 new Vector3(i % 2 == 0 ? -5f : 5f, 18.5f, 148f + (i / 2) * 10f),
                 new Vector3(1.6f, 8f, 1.6f));

        Platform(root, "N3_NucGuia_1", new Vector3(-2.5f, 13.22f, 157f), new Vector3(0.16f, 0.05f, 16f), matMagenta);
        Platform(root, "N3_NucGuia_2", new Vector3(2.5f, 13.22f, 157f), new Vector3(0.16f, 0.05f, 16f), matMagenta);

        // ── HACKEO 5 (5 módulos) ──
        var p5 = L1Puerta(root, "N3_Puerta5", new Vector3(0f, 15.5f, 152f), 32f, 4.6f);
        L1Tableta(root, "N3_Tableta5", new Vector3(-4.5f, 13.2f, 149f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Patron,
                          HackableType.Sincronizar, HackableType.Secuencia }, "Núcleo", false, p5);
        L1Checkpoint(root, "N3_CP_5", new Vector3(0f, 13.4f, 148f), 5);

        L1Patrol(root, "N3_Patrulla_3", new Vector3(-4.5f, 13.8f, 160f), new Vector3(4.5f, 13.8f, 160f), 3f);

        // Meta final
        Platform(root, "N3_Meta", new Vector3(0f, 13.6f, 164f), new Vector3(5f, 0.4f, 5f), matGoal);
        var goal = new GameObject("N3_GoalTrigger");
        goal.transform.SetParent(root.transform, true);
        goal.transform.position = new Vector3(0f, 15.4f, 164f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(4f, 3f, 4f);
        goal.AddComponent<LevelGoal>();
        L1Beacon(root, "N3_MetaBeacon", new Vector3(0f, 15f, 164f), Color.green);

        L1Light(root, "N3_Light_Nucleo", new Vector3(0f, 18f, 150f), new Color(0.3f, 1f, 0.5f), 16f, 22f);
        L1Light(root, "N3_Light_Meta", new Vector3(0f, 17f, 164f), new Color(0.3f, 1f, 0.5f), 14f, 20f);
    }

    // ── Hackeo: puertas y tabletas ───────────────────────────────

    /// <summary>
    /// Crea una puerta de dos hojas con marco emisivo y muros laterales que
    /// bloquean todo el ancho indicado (no se puede rodear la puerta).
    /// </summary>
    static GameObject L1Puerta(GameObject parent, string name, Vector3 centro, float anchoTotal, float alto,
                               float anchoHueco = 5.5f)
    {
        var grupo = new GameObject(name);
        grupo.transform.SetParent(parent.transform, true);
        grupo.transform.position = centro;

        float mitad = anchoTotal / 2f;
        float huecoMitad = anchoHueco / 2f;

        // Hojas (en el hueco central)
        var izq = Platform(grupo, name + "_HojaL", centro + new Vector3(-huecoMitad / 2f, 0f, 0f),
                           new Vector3(huecoMitad, alto, 0.5f), matDash);
        var der = Platform(grupo, name + "_HojaR", centro + new Vector3(huecoMitad / 2f, 0f, 0f),
                           new Vector3(huecoMitad, alto, 0.5f), matDash);
        izq.AddComponent<DoorController>();
        der.AddComponent<DoorController>();

        // Muros laterales: bloquean el paso por fuera del hueco
        float anchoMuro = Mathf.Max(0.5f, mitad - huecoMitad);
        float yBase = centro.y - alto / 2f;
        float altoMuro = alto + 1.5f;
        Wall(grupo, name + "_MuroL",
             centro + new Vector3(-huecoMitad - anchoMuro / 2f, yBase + altoMuro / 2f, 0f),
             new Vector3(anchoMuro, altoMuro, 0.6f));
        Wall(grupo, name + "_MuroR",
             centro + new Vector3(huecoMitad + anchoMuro / 2f, yBase + altoMuro / 2f, 0f),
             new Vector3(anchoMuro, altoMuro, 0.6f));

        // Marco decorativo
        Wall(grupo, name + "_MarcoSup", centro + new Vector3(0f, alto / 2f + 0.3f, 0f),
             new Vector3(anchoHueco + 1.2f, 0.6f, 0.7f));
        Wall(grupo, name + "_MarcoL", centro + new Vector3(-anchoHueco / 2f - 0.35f, 0f, 0f),
             new Vector3(0.7f, alto, 0.7f));
        Wall(grupo, name + "_MarcoR", centro + new Vector3(anchoHueco / 2f + 0.35f, 0f, 0f),
             new Vector3(0.7f, alto, 0.7f));
        Platform(grupo, name + "_Luz", centro + new Vector3(0f, alto / 2f + 0.05f, 0f),
                 new Vector3(anchoHueco + 1f, 0.08f, 0.4f), matMagenta);

        return grupo;
    }

    /// <summary>
    /// Crea la tableta de hackeo: un CUADRADO NARANJA emisivo (el "botón" del
    /// puzzle) sobre un pedestal, con su luz y prompt flotante. Se conecta a las
    /// puertas dadas. Si tutorial=true, muestra instrucciones antes de cada módulo.
    /// </summary>
    static void L1Tableta(GameObject parent, string name, Vector3 pos, HackableType[] modulos,
                          string nombreAcceso, bool tutorial = false, params GameObject[] puertas)
    {
        // Pedestal
        Platform(parent, name + "_Base", pos + new Vector3(0f, 0.45f, 0f),
                 new Vector3(0.6f, 0.9f, 0.6f), matStructure);

        // Poste
        Platform(parent, name + "_Poste", pos + new Vector3(0f, 1.05f, 0f),
                 new Vector3(0.18f, 0.35f, 0.18f), matStructure);

        // EL BOTÓN: cuadrado naranja emisivo, mirando hacia el jugador
        var panel = Platform(parent, name, pos + new Vector3(0f, 1.85f, 0f),
                             new Vector3(1.5f, 1.5f, 0.18f), matBoton);

        // Luz naranja del botón
        var luzGO = new GameObject(name + "_Luz");
        luzGO.transform.SetParent(panel.transform, false);
        luzGO.transform.localPosition = new Vector3(0f, 0f, -0.6f);
        var luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.45f, 0.05f);
        luz.range = 7f;
        luz.intensity = 3.5f;

        var term = panel.AddComponent<HackTerminal>();
        term.modulos = modulos;
        term.nombreAcceso = nombreAcceso;
        term.esTutorial = tutorial;

        // Conectar TODAS las hojas de puerta del grupo
        var lista = new System.Collections.Generic.List<DoorController>();
        foreach (var p in puertas)
        {
            if (p == null) continue;
            foreach (var dc in p.GetComponentsInChildren<DoorController>(true))
                if (dc != null) lista.Add(dc);
        }
        if (lista.Count > 0) term.puertas = lista.ToArray();
    }

    static void L1Checkpoint(GameObject parent, string name, Vector3 pos, int numero)
    {
        // Faro visual (cilindro emisivo sin collider)
        var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = name + "_Beacon";
        beacon.transform.SetParent(parent.transform, true);
        beacon.transform.position = pos + Vector3.up * 1.3f;
        beacon.transform.localScale = new Vector3(0.4f, 1.3f, 0.4f);
        var bmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        bmat.SetColor("_BaseColor", Color.green * 0.6f);
        bmat.color = Color.green * 0.6f;
        bmat.EnableKeyword("_EMISSION");
        bmat.SetColor("_EmissionColor", Color.green * 1.5f);
        beacon.GetComponent<MeshRenderer>().sharedMaterial = bmat;
        Object.DestroyImmediate(beacon.GetComponent<Collider>());
        Undo.RegisterCreatedObjectUndo(beacon, name);

        // Trigger de activación
        var trig = new GameObject(name);
        trig.transform.SetParent(parent.transform, true);
        trig.transform.position = pos + Vector3.up * 1.2f;
        var col = trig.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2f, 2.5f, 2f);
        var cp = trig.AddComponent<Checkpoint>();
        cp.numero = numero;
        cp.beaconRenderer = beacon.GetComponent<MeshRenderer>();
        Undo.RegisterCreatedObjectUndo(trig, name);
    }

    static void L1Beacon(GameObject parent, string name, Vector3 pos, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent.transform, true);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.35f, 1.3f, 0.35f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.SetColor("_BaseColor", color * 0.6f);
        mat.color = color * 0.6f;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 1.5f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        Undo.RegisterCreatedObjectUndo(go, name);
    }

    // ── Hazards verdes pulsantes ─────────────────────────────────

    static void L1RotatingBar(GameObject parent, string name, Vector3 pos, float length, float speed)
    {
        var go = Platform(parent, name, pos, new Vector3(length, 0.4f, 0.4f), matHazard);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var rot = go.AddComponent<RotatingHazard>();
        rot.rotationSpeed = new Vector3(0f, 0f, speed);
       
    }

    /// <summary>Rejilla/ventilación de energía verde peligrosa (no se mueve).</summary>
    static void L1Vent(GameObject parent, string name, Vector3 pos, Vector3 scale)
    {
        var go = Platform(parent, name, pos, scale, matHazard);
       
    }

    /// <summary>
    /// Obstáculo de energía verde SÓLIDO: hay que saltarlo (no mata, bloquea).
    /// Se usa para hacer más difícil el plataformeo.
    /// </summary>
    static void L1Obstaculo(GameObject parent, string name, Vector3 pos, float ancho, float alto, float profundidad)
    {
        var go = Platform(parent, name, pos, new Vector3(ancho, alto, profundidad), matHazard);
       
    }

    static void L1Patrol(GameObject parent, string name, Vector3 a, Vector3 b, float speed)
    {
        var go = Platform(parent, name, a, new Vector3(1.2f, 1.2f, 1.2f), matHazard);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var p = go.AddComponent<PatrolHazard>();
        p.pointA = a;
        p.pointB = b;
        p.speed = speed;
    }

    static void L1MovingPlatform(GameObject parent, string name, Vector3 pos, Vector3 scale, Vector3 move, float speed)
    {
        var go = Platform(parent, name, pos, scale, matConcrete);
        var mp = go.AddComponent<MovingPlatform>();
        mp.moveAmount = move;
        mp.speed = speed;
    }

    static void L1Tower(GameObject parent, string name, Vector3 pos, Vector3 scale, bool stripOnPositiveX)
    {
        var t = Platform(parent, name, pos, scale, matStructure);
        float side = stripOnPositiveX ? 1f : -1f;
        Platform(t, name + "_Strip",
            pos + new Vector3(side * (scale.x * 0.5f + 0.05f), 0f, 0f),
            new Vector3(0.08f, scale.y * 0.75f, 0.5f), matCyan);
    }

    static void L1Cable(GameObject parent, string name, Vector3 from, Vector3 to, float thickness)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, true);
        Vector3 dir = to - from;
        go.transform.position = (from + to) * 0.5f;
        go.transform.rotation = Quaternion.LookRotation(dir);
        go.transform.localScale = new Vector3(thickness, thickness, dir.magnitude);
        go.GetComponent<MeshRenderer>().sharedMaterial = matStructure;
        Undo.RegisterCreatedObjectUndo(go, name);
    }

    static void L1Light(GameObject parent, string name, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, true);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        Undo.RegisterCreatedObjectUndo(go, name);
    }

    static void L1Debris(GameObject parent, System.Random rng, int count, Bounds area)
    {
        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * area.size.x + area.min.x);
            float y = (float)(rng.NextDouble() * area.size.y + area.min.y);
            float z = (float)(rng.NextDouble() * area.size.z + area.min.z);
            float s = 0.4f + (float)rng.NextDouble() * 1.1f;
            var go = Platform(parent, $"L1_Debris_{i}", new Vector3(x, y, z), new Vector3(s, s, s), matJump);
            go.transform.rotation = Quaternion.Euler(
                (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f);
        }
    }

    /// <summary>Objeto con la info del nivel (nombre, escena siguiente y dificultad).</summary>
    static void L1LevelInfo(GameObject root, string nombre, string siguiente, int dificultad)
    {
        var go = new GameObject("L1_LevelInfo");
        go.transform.SetParent(root.transform, true);
        var info = go.AddComponent<LevelInfo>();
        info.nombreNivel = nombre;
        info.escenaSiguiente = siguiente;
        info.dificultad = dificultad;
        Undo.RegisterCreatedObjectUndo(go, "LevelInfo");
    }

    [MenuItem("SEEN/Registrar los 5 niveles en Build Settings")]
    static void RepararBuildSettings()
    {
        RegistrarTodasLasEscenas();
        Debug.Log("[SEENLevelBuilder] Build Settings reparado: MainMenu, Juego, Nivel2, Nivel3, Nivel4, Nivel5");
    }

    /// <summary>
    /// Registra TODAS las escenas del juego en el orden correcto.
    /// Lo llaman los builders para no perder ninguna al reconstruir.
    /// </summary>
    static void RegistrarTodasLasEscenas()
    {
        var escenas = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        string[] orden = { "MainMenu", "Juego", "Nivel2", "Nivel3", "Nivel4", "Nivel5" };

        foreach (var nombre in orden)
        {
            string ruta = $"Assets/Scenes/{nombre}.unity";
            if (AssetDatabase.LoadAssetAtPath<Object>(ruta) != null)
                escenas.Add(new EditorBuildSettingsScene(ruta, true));
        }

        if (escenas.Count > 0)
            EditorBuildSettings.scenes = escenas.ToArray();
    }

    /// <summary>Registra una escena en Build Settings preservando las demás.</summary>
    static void RegistrarEscena(string nombre)
    {
        // Registrar TODAS garantiza que ninguna se pierda al reconstruir
        RegistrarTodasLasEscenas();
    }
}
#endif
