#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Construye el nivel de pruebas de SEEN desde el menú <b>SEEN → Construir Nivel de Pruebas</b>.
///
/// Ejecutar UNA VEZ después de que Unity compile los scripts, con la escena Juego.abierta.
/// Crea: jugador en primera persona (cámara hija), ruta de doble salto, ruta de dash,
/// plataforma final con meta, killzone con respawn y niebla distópica.
/// Basado en la estructura del LevelBuilder del proyecto Jueguinho.
/// </summary>
public static class SEENLevelBuilder
{
    // Materiales reutilizables
    static Material matGround, matJump, matDash, matStructure, matGoal;

    [MenuItem("SEEN/Construir Nivel de Pruebas")]
    static void Build()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel de Pruebas — SEEN",
                "Esto eliminará el nivel anterior y construirá el nivel de pruebas:\n\n" +
                "• Jugador en primera persona (cámara hija)\n" +
                "• Ruta de doble salto\n• Ruta de dash\n• Meta final\n\n" +
                "¿Continuar?",
                "Sí, construir", "Cancelar")) return;

        CreateMaterials();
        ClearOldLevel();
        SetupPlayer();
        SetupCamera();
        BuildLevel();
        SetupAtmosphere();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] ¡Nivel de pruebas construido! Presioná Play y probá: " +
                  "WASD mover | Mouse mirar | Espacio doble salto | Shift dash.");
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

    // ── Limpieza ──────────────────────────────────────────────────
    static void ClearOldLevel()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            if (go.name.StartsWith("NIVEL_") || go.name.StartsWith("L1_") || go.name == "Plane")
                Undo.DestroyObjectImmediate(go);
        }
    }

    // ── Jugador ───────────────────────────────────────────────────
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

        // Collider y física
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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Controller
        var pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = player.AddComponent<PlayerController>();

        // Respawn al caer
        if (player.GetComponent<RespawnOnFall>() == null)
            player.AddComponent<RespawnOnFall>();

        Debug.Log("[SEENLevelBuilder] Jugador listo (cápsula + Rigidbody + PlayerController + RespawnOnFall).");
    }

    // ── Cámara ────────────────────────────────────────────────────
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

    // ── Nivel ─────────────────────────────────────────────────────
    static void BuildLevel()
    {
        var root = new GameObject("NIVEL_Root");
        Undo.RegisterCreatedObjectUndo(root, "NIVEL_Root");

        // Zona de muerte (todo lo que esté por debajo reaparece al jugador)
        var kz = new GameObject("NIVEL_KillZone");
        kz.transform.position = new Vector3(0f, -14f, 30f);
        var kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(300f, 4f, 400f);
        kz.transform.SetParent(root.transform);
        Undo.RegisterCreatedObjectUndo(kz, "KillZone");

        // Plataforma de inicio
        Platform(root, "NIVEL_Spawn", new Vector3(0f, 0f, 0f), new Vector3(8f, 0.5f, 8f), matGround);

        // Paredes de orientación alrededor del spawn (referencia distópica)
        Wall(root, "NIVEL_Wall_L", new Vector3(-6f, 3f, 0f), new Vector3(0.6f, 6f, 14f));
        Wall(root, "NIVEL_Wall_R", new Vector3(6f, 3f, 0f), new Vector3(0.6f, 6f, 14f));
        Wall(root, "NIVEL_Wall_B", new Vector3(0f, 3f, -7f), new Vector3(12f, 6f, 0.6f));

        // ── Ruta de DOBLE SALTO (gris) ──
        Platform(root, "NIVEL_J1", new Vector3(0f, 1.5f, 8f), new Vector3(2.5f, 0.3f, 2.5f), matJump);    // salto simple
        Platform(root, "NIVEL_J2", new Vector3(0f, 3.4f, 15.5f), new Vector3(2.2f, 0.3f, 2.2f), matJump); // doble salto
        Platform(root, "NIVEL_J3", new Vector3(2f, 5.2f, 23f), new Vector3(2.2f, 0.3f, 2.2f), matJump);    // doble salto + lateral

        // ── Ruta de DASH (azul) ──
        Platform(root, "NIVEL_D1", new Vector3(0f, 5f, 30f), new Vector3(2.5f, 0.3f, 2.5f), matDash);
        Platform(root, "NIVEL_D2", new Vector3(0f, 5f, 38f), new Vector3(2.5f, 0.3f, 2.5f), matDash);      // dash
        Platform(root, "NIVEL_D3", new Vector3(0f, 6.5f, 45.5f), new Vector3(2.5f, 0.3f, 2.5f), matDash);  // dash + subida
        Platform(root, "NIVEL_D4", new Vector3(0f, 6.5f, 53f), new Vector3(2.5f, 0.3f, 2.5f), matDash);    // dash largo

        // ── META (verde) ──
        Platform(root, "NIVEL_Meta", new Vector3(0f, 6.5f, 61f), new Vector3(6f, 0.4f, 6f), matGoal);
        var goal = new GameObject("NIVEL_GoalTrigger");
        goal.transform.position = new Vector3(0f, 8.2f, 61f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(5f, 3f, 5f);
        goal.AddComponent<LevelGoal>();
        goal.transform.SetParent(root.transform);
        Undo.RegisterCreatedObjectUndo(goal, "GoalTrigger");

        Debug.Log("[SEENLevelBuilder] Nivel: spawn → ruta salto (J1-J3) → ruta dash (D1-D4) → meta verde.");
    }

    // ── Ambiente ──────────────────────────────────────────────────
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

    static void EnsureManagers()
    {
        if (GameObject.Find("TestLevelHints") == null)
        {
            var go = new GameObject("TestLevelHints");
            go.AddComponent<TestLevelHints>();
            Undo.RegisterCreatedObjectUndo(go, "Hints");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────
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

    // ══════════════════════════════════════════════════════════════════
    //  NIVEL 1 COMPLETO — "PUNTO DE INFECCIÓN"
    //  Menú: SEEN → Construir Nivel Completo (Nivel 1)
    // ══════════════════════════════════════════════════════════════════

    static Material matConcrete, matCyan, matMagenta, matHazard;

    [MenuItem("SEEN/Construir Nivel Completo (Nivel 1)")]
    static void BuildFullLevel()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 1 — SEEN",
                "Se construirá el nivel completo con decoración:\n\n" +
                "• Zona 0: Punto de Infección (plaza de spawn)\n" +
                "• Zona 1: Pasillo de Notificaciones (barras rotatorias)\n" +
                "• Zona 2: El Abismo de Datos (plataformas móviles)\n" +
                "• Zona 3: Sala del Sistema (la Pantalla + Meta)\n\n" +
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
        Debug.Log("[SEENLevelBuilder] ¡NIVEL 1 construido! Play para jugar. " +
                  "WASD mover | Mouse mirar | Espacio doble salto | Shift dash.");
    }

    static void CreateLevel1Materials()
    {
        matConcrete = GetOrCreateMat("Mat_Concrete",   new Color(0.16f, 0.17f, 0.19f));
        matCyan    = GetOrCreateEmissiveMat("Mat_ScreenCyan",    new Color(0.05f, 0.12f, 0.18f), new Color(0.1f, 0.8f, 1f), 2.2f);
        matMagenta = GetOrCreateEmissiveMat("Mat_ScreenMagenta", new Color(0.14f, 0.04f, 0.12f), new Color(1f, 0.15f, 0.6f), 2.2f);
        matHazard  = GetOrCreateEmissiveMat("Mat_Hazard",        new Color(0.3f, 0.12f, 0.03f),  new Color(1f, 0.45f, 0.05f), 1.8f);
    }

    static Material GetOrCreateEmissiveMat(string name, Color baseColor, Color emissionColor, float intensity)
    {
        var mat = GetOrCreateMat(name, baseColor);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor * intensity);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ── Construcción por zonas ─────────────────────────────────────

    static void BuildLevel1()
    {
        var root = new GameObject("L1_Root");
        Undo.RegisterCreatedObjectUndo(root, "L1_Root");

        // Killzone global (caídas)
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

        Debug.Log("[SEENLevelBuilder] Zonas construidas: 0-Plaza | 1-Pasillo | 2-Abismo | 3-Sala del Sistema.");
    }

    /// <summary>ZONA 0 — Punto de Infección: plaza de spawn decorada.</summary>
    static void BuildZona0(GameObject root)
    {
        Platform(root, "L1_Plaza", new Vector3(0f, 0f, 0f), new Vector3(26f, 0.5f, 30f), matConcrete);

        Wall(root, "L1_Wall_Back", new Vector3(0f, 3.5f, -15.3f), new Vector3(26f, 7f, 0.6f));
        Wall(root, "L1_Wall_L", new Vector3(-13.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 30f));
        Wall(root, "L1_Wall_R", new Vector3(13.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 30f));
        Wall(root, "L1_Wall_FL", new Vector3(-8f, 3.5f, 15.3f), new Vector3(10.6f, 7f, 0.6f));
        Wall(root, "L1_Wall_FR", new Vector3(8f, 3.5f, 15.3f), new Vector3(10.6f, 7f, 0.6f));

        // Pilares
        Platform(root, "L1_Pilar_A", new Vector3(-6f, 3f, 4f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        Platform(root, "L1_Pilar_B", new Vector3(6f, 3f, 4f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        Platform(root, "L1_Pilar_C", new Vector3(0f, 3f, -8f), new Vector3(1.6f, 6f, 1.6f), matStructure);

        // Muro de publicaciones (rejilla de pantallas emisivas)
        for (int col = 0; col < 6; col++)
        {
            for (int row = 0; row < 3; row++)
            {
                var mat = (col + row) % 2 == 0 ? matCyan : matMagenta;
                Platform(root, $"L1_Post_{col}_{row}",
                    new Vector3(-5f + col * 2f, 1.8f + row * 1.4f, -14.85f),
                    new Vector3(1.3f, 1.3f, 0.15f), mat);
            }
        }

        // Escombros decorativos (distribución fija)
        var rng = new System.Random(7);
        L1Debris(root, rng, 6, new Bounds(new Vector3(0f, 0.6f, 4f), new Vector3(20f, 1.2f, 18f)));

        L1Light(root, "L1_Light_Plaza", new Vector3(0f, 5.5f, 0f),
            new Color(0.5f, 0.85f, 1f), 12f, 16f);
    }

    /// <summary>ZONA 1 — Pasillo de Notificaciones: pasillo con barras rotatorias.</summary>
    static void BuildZona1(GameObject root)
    {
        Platform(root, "L1_Corridor", new Vector3(0f, 0f, 34.5f), new Vector3(5.5f, 0.5f, 37f), matConcrete);
        Wall(root, "L1_CorWall_L", new Vector3(-3.6f, 3.5f, 34.5f), new Vector3(0.6f, 7f, 37f));
        Wall(root, "L1_CorWall_R", new Vector3(3.6f, 3.5f, 34.5f), new Vector3(0.6f, 7f, 37f));

        // Tiras emisivas en lo alto de las paredes
        Platform(root, "L1_Strip_L", new Vector3(-3.2f, 6.3f, 34.5f), new Vector3(0.12f, 0.06f, 36f), matCyan);
        Platform(root, "L1_Strip_R", new Vector3(3.2f, 6.3f, 34.5f), new Vector3(0.12f, 0.06f, 36f), matCyan);

        // 4 barras rotatorias ("notificaciones") a distintas velocidades
        L1RotatingBar(root, "L1_Bar_1", new Vector3(0f, 1.5f, 23f), 6.6f, 40f);
        L1RotatingBar(root, "L1_Bar_2", new Vector3(0f, 1.5f, 31f), 6.6f, 55f);
        L1RotatingBar(root, "L1_Bar_3", new Vector3(0f, 1.5f, 39f), 6.6f, 70f);
        L1RotatingBar(root, "L1_Bar_4", new Vector3(0f, 1.5f, 47f), 6.6f, 45f);

        // Cables decorativos en el techo
        L1Cable(root, "L1_Cable_1", new Vector3(-3.4f, 6.8f, 18f), new Vector3(-3.4f, 6.8f, 52f), 0.07f);
        L1Cable(root, "L1_Cable_2", new Vector3(3.4f, 6.8f, 18f), new Vector3(3.4f, 6.8f, 52f), 0.07f);

        L1Checkpoint(root, "L1_CP_Pasillo", new Vector3(0f, 0.5f, 19f));
        L1Light(root, "L1_Light_Pasillo", new Vector3(0f, 4.5f, 35f),
            new Color(1f, 0.55f, 0.2f), 8f, 16f);
    }

    /// <summary>ZONA 2 — El Abismo de Datos: plataformas fijas y móviles sobre el vacío.</summary>
    static void BuildZona2(GameObject root)
    {
        Platform(root, "L1_P1", new Vector3(0f, 2.5f, 56f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "L1_P2_MovH", new Vector3(0f, 3.8f, 61f), new Vector3(3f, 0.3f, 3f),
            new Vector3(7f, 0f, 0f), 1.2f);
        Platform(root, "L1_P3", new Vector3(0f, 5.5f, 68f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "L1_P4_MovV", new Vector3(-3.5f, 7.5f, 74f), new Vector3(3f, 0.3f, 3f),
            new Vector3(0f, 3.5f, 0f), 1f);
        Platform(root, "L1_P5", new Vector3(0f, 9.2f, 78f), new Vector3(4f, 0.4f, 4f), matConcrete);
        L1Checkpoint(root, "L1_CP_Abismo", new Vector3(0f, 9.5f, 78f));
        Platform(root, "L1_P6_Dash", new Vector3(0f, 9.2f, 86.5f), new Vector3(3.5f, 0.4f, 3.5f), matDash);
        Platform(root, "L1_P7", new Vector3(0f, 9.2f, 93.5f), new Vector3(5f, 0.4f, 5f), matConcrete);

        // Torres de servidores a los lados
        L1Tower(root, "L1_Tower_A", new Vector3(-10f, 4f, 56f), new Vector3(3f, 16f, 3f), false);
        L1Tower(root, "L1_Tower_B", new Vector3(10f, 4f, 56f), new Vector3(3f, 16f, 3f), true);
        L1Tower(root, "L1_Tower_C", new Vector3(-10f, 4f, 74f), new Vector3(3f, 16f, 3f), false);
        L1Tower(root, "L1_Tower_D", new Vector3(10f, 4f, 74f), new Vector3(3f, 16f, 3f), true);

        // Cables entre torres
        L1Cable(root, "L1_Cable_T1", new Vector3(-10f, 12f, 56f), new Vector3(-10f, 12f, 74f), 0.08f);
        L1Cable(root, "L1_Cable_T2", new Vector3(10f, 12f, 56f), new Vector3(10f, 12f, 74f), 0.08f);
        L1Cable(root, "L1_Cable_T3", new Vector3(-10f, 12f, 56f), new Vector3(10f, 12f, 56f), 0.08f);

        // Escombros flotantes sobre el vacío
        var rng = new System.Random(23);
        L1Debris(root, rng, 12, new Bounds(new Vector3(0f, 3.5f, 73f), new Vector3(18f, 5f, 38f)));

        L1Light(root, "L1_Light_Abismo", new Vector3(0f, 6f, 75f),
            new Color(0.3f, 0.7f, 1f), 10f, 22f);
    }

    /// <summary>ZONA 3 — Sala del Sistema: la Pantalla, el Comentario hostil y la Meta.</summary>
    static void BuildZona3(GameObject root)
    {
        Platform(root, "L1_Sala", new Vector3(0f, 8.95f, 111f), new Vector3(28f, 0.5f, 30f), matConcrete);
        Wall(root, "L1_SalaWall_Back", new Vector3(0f, 13f, 126.3f), new Vector3(28f, 9f, 0.6f));
        Wall(root, "L1_SalaWall_L", new Vector3(-14.3f, 13f, 111f), new Vector3(0.6f, 9f, 30f));
        Wall(root, "L1_SalaWall_R", new Vector3(14.3f, 13f, 111f), new Vector3(0.6f, 9f, 30f));

        // La Pantalla del Sistema (monitor gigante)
        Platform(root, "L1_PantallaSistema", new Vector3(0f, 13f, 125.9f), new Vector3(9f, 4.5f, 0.4f), matMagenta);

        // Pantallas laterales
        Platform(root, "L1_Screen_A", new Vector3(13.9f, 10.5f, 101f), new Vector3(0.3f, 2.5f, 3f), matCyan);
        Platform(root, "L1_Screen_B", new Vector3(-13.9f, 10.5f, 101f), new Vector3(0.3f, 2.5f, 3f), matCyan);
        Platform(root, "L1_Screen_C", new Vector3(13.9f, 10.5f, 115f), new Vector3(0.3f, 2.5f, 3f), matCyan);
        Platform(root, "L1_Screen_D", new Vector3(-13.9f, 10.5f, 115f), new Vector3(0.3f, 2.5f, 3f), matCyan);

        // Pilares
        Platform(root, "L1_SalaPilar_A", new Vector3(-5f, 12.5f, 100f), new Vector3(1.6f, 8f, 1.6f), matStructure);
        Platform(root, "L1_SalaPilar_B", new Vector3(5f, 12.5f, 100f), new Vector3(1.6f, 8f, 1.6f), matStructure);
        Platform(root, "L1_SalaPilar_C", new Vector3(-5f, 12.5f, 118f), new Vector3(1.6f, 8f, 1.6f), matStructure);
        Platform(root, "L1_SalaPilar_D", new Vector3(5f, 12.5f, 118f), new Vector3(1.6f, 8f, 1.6f), matStructure);

        // Entidad hostil: "Comentario" que patrulla
        L1Patrol(root, "L1_ENT_Comentario", new Vector3(-4.5f, 9.8f, 108f), new Vector3(4.5f, 9.8f, 108f), 2.5f);

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

        // Faro de meta
        L1Beacon(root, "L1_MetaBeacon", new Vector3(0f, 10.6f, 118f), Color.green);

        // Cables del techo
        L1Cable(root, "L1_Cable_S1", new Vector3(-14f, 15.5f, 97f), new Vector3(-14f, 15.5f, 124f), 0.07f);
        L1Cable(root, "L1_Cable_S2", new Vector3(14f, 15.5f, 97f), new Vector3(14f, 15.5f, 124f), 0.07f);

        L1Checkpoint(root, "L1_CP_Sala", new Vector3(0f, 9.6f, 99f));
        L1Light(root, "L1_Light_Meta", new Vector3(0f, 13f, 118f),
            new Color(1f, 0.2f, 0.65f), 12f, 18f);
        L1Light(root, "L1_Light_Sala", new Vector3(0f, 11f, 101f),
            new Color(0.2f, 0.75f, 1f), 9f, 16f);
    }

    // ── Helpers del Nivel 1 ───────────────────────────────────────

    static void L1Checkpoint(GameObject parent, string name, Vector3 pos)
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
        trig.AddComponent<Checkpoint>().beaconRenderer = beacon.GetComponent<MeshRenderer>();
        Undo.RegisterCreatedObjectUndo(trig, name);
    }

    static void L1RotatingBar(GameObject parent, string name, Vector3 pos, float length, float speed)
    {
        var go = Platform(parent, name, pos, new Vector3(length, 0.4f, 0.4f), matHazard);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var rot = go.AddComponent<RotatingHazard>();
        rot.rotationSpeed = new Vector3(0f, 0f, speed);
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

    // ── Post-procesado (Bloom + ACES + Viñeta) ────────────────────

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
}
#endif
