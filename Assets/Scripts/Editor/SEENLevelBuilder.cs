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

        // Mirilla en primera persona
        if (player.GetComponent<Crosshair>() == null)
            player.AddComponent<Crosshair>();

        // Menú de pausa
        if (player.GetComponent<PauseMenu>() == null)
            player.AddComponent<PauseMenu>();

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

        // UI de feedback en pantalla (checkpoints, pantalla de nivel completado)
        if (GameObject.Find("L1_UI") == null)
        {
            var ui = new GameObject("L1_UI");
            ui.AddComponent<GameFeedback>();
            Undo.RegisterCreatedObjectUndo(ui, "Feedback");
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

    /// <summary>
    /// Plataforma definida por sus BORDES en Z (zInicio..zFin) y su altura.
    /// Usar bordes garantiza que las secciones continuas queden PEGADAS
    /// (sin huecos donde el jugador se puede trabar).
    /// </summary>
    static GameObject PlataformaZ(GameObject parent, string name, float zInicio, float zFin,
                                  float y, float alto, Material mat, float xCentro = 0f, float ancho = 3f)
    {
        float centro = (zInicio + zFin) / 2f;
        float profundidad = Mathf.Max(0.1f, zFin - zInicio);
        return Platform(parent, name, new Vector3(xCentro, y, centro),
                        new Vector3(ancho, alto, profundidad), mat);
    }

    static GameObject Wall(GameObject parent, string name, Vector3 pos, Vector3 scale)
        => Platform(parent, name, pos, scale, matStructure);

    // ══════════════════════════════════════════════════════════════════
    //  NIVEL 1 COMPLETO — "PUNTO DE INFECCIÓN"
    //  Menú: SEEN → Construir Nivel Completo (Nivel 1)
    // ══════════════════════════════════════════════════════════════════

    static Material matConcrete, matCyan, matMagenta, matHazard, matBoton;

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
        // Objetos dañinos: VERDE que emana luz (energía tóxica)
        matHazard  = GetOrCreateEmissiveMat("Mat_Hazard", new Color(0.02f, 0.10f, 0.04f), new Color(0.12f, 1f, 0.28f), 2.6f);
        // Botón de hackeo: cuadrado NARANJA bien visible
        matBoton   = GetOrCreateEmissiveMat("Mat_BotonNaranja", new Color(0.25f, 0.10f, 0.02f), new Color(1f, 0.45f, 0.05f), 3.2f);
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

        L1LevelInfo(root, "Nivel 1 — Tutorial de Movimiento", "Nivel2");

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

        // Vigas del techo
        Wall(root, "L1_Viga_A", new Vector3(-6f, 7.2f, 0f), new Vector3(0.5f, 0.5f, 30f));
        Wall(root, "L1_Viga_B", new Vector3(0f, 7.2f, 0f), new Vector3(0.5f, 0.5f, 30f));
        Wall(root, "L1_Viga_C", new Vector3(6f, 7.2f, 0f), new Vector3(0.5f, 0.5f, 30f));

        // Arco de entrada al pasillo
        Wall(root, "L1_Arco_L", new Vector3(-3.2f, 3f, 15.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "L1_Arco_R", new Vector3(3.2f, 3f, 15.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "L1_Arco_Top", new Vector3(0f, 6.4f, 15.3f), new Vector3(7.3f, 0.7f, 0.9f));
        Platform(root, "L1_Arco_Luz", new Vector3(0f, 5.9f, 15.3f), new Vector3(6f, 0.12f, 0.4f), matCyan);

        // Pilares
        Platform(root, "L1_Pilar_A", new Vector3(-6f, 3f, 4f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        Platform(root, "L1_Pilar_B", new Vector3(6f, 3f, 4f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        Platform(root, "L1_Pilar_C", new Vector3(0f, 3f, -8f), new Vector3(1.6f, 6f, 1.6f), matStructure);
        // Molduras emisivas en los pilares
        Platform(root, "L1_Pilar_A_Luz", new Vector3(-6f, 5.6f, 4f), new Vector3(1.7f, 0.12f, 1.7f), matCyan);
        Platform(root, "L1_Pilar_B_Luz", new Vector3(6f, 5.6f, 4f), new Vector3(1.7f, 0.12f, 1.7f), matCyan);
        Platform(root, "L1_Pilar_C_Luz", new Vector3(0f, 5.6f, -8f), new Vector3(1.7f, 0.12f, 1.7f), matMagenta);

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

        // Rack de servidores en una esquina
        Platform(root, "L1_Rack", new Vector3(-11.5f, 2.2f, -4f), new Vector3(1.6f, 4.4f, 1.6f), matStructure);
        for (int i = 0; i < 4; i++)
            Platform(root, $"L1_Rack_Luz_{i}", new Vector3(-11.1f, 0.8f + i * 1.1f, -4f),
                     new Vector3(0.08f, 0.12f, 1.2f), matCyan);

        // Cajas / escombros apilados
        Platform(root, "L1_Caja_A", new Vector3(-8.5f, 0.5f, 8f), new Vector3(1.2f, 1f, 1.2f), matJump);
        Platform(root, "L1_Caja_B", new Vector3(-8.5f, 1.5f, 8f), new Vector3(0.8f, 1f, 0.8f), matJump);
        Platform(root, "L1_Caja_C", new Vector3(9.5f, 0.55f, 6.5f), new Vector3(1.1f, 1.1f, 1.1f), matJump);

        // Guías emisivas en el piso (marcan la ruta)
        Platform(root, "L1_Guia_1", new Vector3(-2f, 0.28f, 6f), new Vector3(0.5f, 0.06f, 10f), matCyan);
        Platform(root, "L1_Guia_2", new Vector3(2f, 0.28f, 6f), new Vector3(0.5f, 0.06f, 10f), matCyan);

        // Escombros decorativos (distribución fija)
        var rng = new System.Random(7);
        L1Debris(root, rng, 6, new Bounds(new Vector3(0f, 0.6f, 4f), new Vector3(20f, 1.2f, 18f)));

        // Caños a lo largo de las paredes laterales
        L1Cable(root, "L1_Cano_L", new Vector3(-12.9f, 6.5f, -14f), new Vector3(-12.9f, 6.5f, 14f), 0.18f);
        L1Cable(root, "L1_Cano_R", new Vector3(12.9f, 6.5f, -14f), new Vector3(12.9f, 6.5f, 14f), 0.18f);

        L1Light(root, "L1_Light_Plaza", new Vector3(0f, 5.5f, 0f),
            new Color(0.5f, 0.85f, 1f), 14f, 18f);
    }

    /// <summary>ZONA 1 — Pasillo de Notificaciones: pasillo con barras rotatorias.</summary>
    static void BuildZona1(GameObject root)
    {
        // Piso CONTINUO: se extiende hasta tocar la plaza (sin huecos donde trabarse)
        PlataformaZ(root, "L1_Corridor", 15f, 53f, 0f, 0.5f, matConcrete, 0f, 5.5f);
        Wall(root, "L1_CorWall_L", new Vector3(-3.6f, 3.5f, 34f), new Vector3(0.6f, 7f, 38f));
        Wall(root, "L1_CorWall_R", new Vector3(3.6f, 3.5f, 34f), new Vector3(0.6f, 7f, 38f));

        // Tiras emisivas en lo alto de las paredes
        Platform(root, "L1_Strip_L", new Vector3(-3.2f, 6.3f, 34f), new Vector3(0.12f, 0.06f, 37f), matCyan);
        Platform(root, "L1_Strip_R", new Vector3(3.2f, 6.3f, 34f), new Vector3(0.12f, 0.06f, 37f), matCyan);

        // Paneles de techo con luces cada ~6 unidades
        for (int i = 0; i < 5; i++)
        {
            float z = 19f + i * 8f;
            Wall(root, $"L1_Techo_{i}", new Vector3(0f, 7.2f, z), new Vector3(5.5f, 0.3f, 2.6f));
            Platform(root, $"L1_LuzTecho_{i}", new Vector3(0f, 7.0f, z), new Vector3(1.2f, 0.08f, 0.7f), matCyan);
        }

        // Rejillas de energía verde en el piso (antes de cada barrera)
        L1Vent(root, "L1_Vent_1", new Vector3(0f, 0.28f, 27f), new Vector3(4.4f, 0.05f, 2.2f));
        L1Vent(root, "L1_Vent_2", new Vector3(0f, 0.28f, 35f), new Vector3(4.4f, 0.05f, 2.2f));
        L1Vent(root, "L1_Vent_3", new Vector3(0f, 0.28f, 43f), new Vector3(4.4f, 0.05f, 2.2f));

        // 4 barras rotatorias ("notificaciones") a distintas velocidades
        L1RotatingBar(root, "L1_Bar_1", new Vector3(0f, 1.5f, 23f), 6.6f, 40f);
        L1RotatingBar(root, "L1_Bar_2", new Vector3(0f, 1.5f, 31f), 6.6f, 55f);
        L1RotatingBar(root, "L1_Bar_3", new Vector3(0f, 1.5f, 39f), 6.6f, 70f);
        L1RotatingBar(root, "L1_Bar_4", new Vector3(0f, 1.5f, 47f), 6.6f, 45f);

        // Cables/caños decorativos en el techo
        L1Cable(root, "L1_Cable_1", new Vector3(-3.4f, 6.8f, 18f), new Vector3(-3.4f, 6.8f, 52f), 0.07f);
        L1Cable(root, "L1_Cable_2", new Vector3(3.4f, 6.8f, 18f), new Vector3(3.4f, 6.8f, 52f), 0.07f);

        // Marco de salida hacia el abismo
        Wall(root, "L1_Puerta_L", new Vector3(-3.9f, 2.6f, 53f), new Vector3(0.5f, 5.2f, 0.5f));
        Wall(root, "L1_Puerta_R", new Vector3(3.9f, 2.6f, 53f), new Vector3(0.5f, 5.2f, 0.5f));
        Wall(root, "L1_Puerta_Top", new Vector3(0f, 5.4f, 53f), new Vector3(8.3f, 0.4f, 0.5f));
        Platform(root, "L1_Puerta_Luz", new Vector3(0f, 5.1f, 53f), new Vector3(7f, 0.1f, 0.3f), matMagenta);

        // ── HACKEO 1 (1 módulo: DescifrarCodigo) ──
        var puerta1 = L1Puerta(root, "L1_Puerta1", new Vector3(0f, 2.4f, 50f), 40f, 4.8f);
        L1Tableta(root, "L1_Tableta1", new Vector3(-2.3f, 0.25f, 47.5f),
                  new[] { HackableType.DescifrarCodigo }, "PUERTA 1", true, puerta1);

        L1Checkpoint(root, "L1_CP_Pasillo", new Vector3(0f, 0.5f, 19f), 1);
        L1Light(root, "L1_Light_Pasillo", new Vector3(0f, 4.5f, 35f),
            new Color(0.4f, 0.8f, 1f), 10f, 17f);
    }

    /// <summary>ZONA 2 — El Abismo de Datos: plataformas fijas y móviles sobre el vacío.</summary>
    static void BuildZona2(GameObject root)
    {
        Platform(root, "L1_P1", new Vector3(0f, 2.5f, 56f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1Riel(root, "L1_P1_Riel", new Vector3(0f, 2.68f, 56f), new Vector3(2.5f, 0.12f, 0.3f));
        L1MovingPlatform(root, "L1_P2_MovH", new Vector3(0f, 3.8f, 61f), new Vector3(3f, 0.3f, 3f),
            new Vector3(7f, 0f, 0f), 1.2f);
        Platform(root, "L1_P3", new Vector3(0f, 5.5f, 68f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1Riel(root, "L1_P3_Riel", new Vector3(0f, 5.68f, 68f), new Vector3(2.5f, 0.12f, 0.3f));
        L1MovingPlatform(root, "L1_P4_MovV", new Vector3(-3.5f, 7.5f, 74f), new Vector3(3f, 0.3f, 3f),
            new Vector3(0f, 3.5f, 0f), 1f);
        Platform(root, "L1_P5", new Vector3(0f, 9.2f, 78f), new Vector3(4f, 0.4f, 4f), matConcrete);
        L1Riel(root, "L1_P5_Riel", new Vector3(0f, 9.43f, 78f), new Vector3(4f, 0.12f, 0.4f));
        L1Checkpoint(root, "L1_CP_Abismo", new Vector3(0f, 9.5f, 78f), 2);
        Platform(root, "L1_P6_Dash", new Vector3(0f, 9.2f, 86.5f), new Vector3(3.5f, 0.4f, 3.5f), matDash);
        L1Riel(root, "L1_P6_Riel", new Vector3(0f, 9.43f, 86.5f), new Vector3(3.5f, 0.12f, 0.4f));
        Platform(root, "L1_P7", new Vector3(0f, 9.2f, 93.5f), new Vector3(5f, 0.4f, 5f), matConcrete);
        L1Riel(root, "L1_P7_Riel", new Vector3(0f, 9.43f, 93.5f), new Vector3(5f, 0.12f, 0.5f));

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
        Platform(root, "L1_Sala", new Vector3(0f, 8.95f, 111f), new Vector3(28f, 0.5f, 30f), matConcrete);
        Wall(root, "L1_SalaWall_Back", new Vector3(0f, 13f, 126.3f), new Vector3(28f, 9f, 0.6f));
        Wall(root, "L1_SalaWall_L", new Vector3(-14.3f, 13f, 111f), new Vector3(0.6f, 9f, 30f));
        Wall(root, "L1_SalaWall_R", new Vector3(14.3f, 13f, 111f), new Vector3(0.6f, 9f, 30f));

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

        // ── HACKEO 2 (1 módulo: UnirCables) ──
        var puerta2 = L1Puerta(root, "L1_Puerta2", new Vector3(0f, 11.6f, 104f), 28f, 4.6f);
        L1Tableta(root, "L1_Tableta2", new Vector3(-4.5f, 9.2f, 101.5f),
                  new[] { HackableType.UnirCables }, "PUERTA 2", true, puerta2);

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

    // ── Helpers del Nivel 1 ───────────────────────────────────────

    /// <summary>Riel/relieve emisivo sobre una plataforma (detalle estético).</summary>
    static void L1Riel(GameObject parent, string name, Vector3 pos, Vector3 scale)
    {
        Platform(parent, name, pos, scale, matCyan);
    }

    // ── Hackeo: puertas y tabletas ───────────────────────────────

    /// <summary>
    /// Crea una puerta de dos hojas con marco emisivo y muros laterales que
    /// bloquean todo el ancho indicado (no se puede rodear la puerta).
    /// Devuelve el GameObject del grupo.
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
        float anchoMuro = mitad - huecoMitad;
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
    /// Crea la tableta de hackeo: un CUADRADO NARANJA emisivo (el "botón" del puzzle)
    /// sobre un pedestal, con su luz y prompt flotante. Se conecta a las puertas dadas.
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

        var lista = new System.Collections.Generic.List<DoorController>();
        foreach (var p in puertas)
        {
            if (p == null) continue;
            var dc = p.GetComponent<DoorController>();
            if (dc == null) dc = p.GetComponentInChildren<DoorController>();
            if (dc != null) lista.Add(dc);
        }
        if (lista.Count > 0) term.puertas = lista.ToArray();
    }

    /// <summary>Objeto con la info del nivel (nombre y escena siguiente).</summary>
    static void L1LevelInfo(GameObject root, string nombre, string siguiente)
    {
        var go = new GameObject("L1_LevelInfo");
        go.transform.SetParent(root.transform, true);
        go.AddComponent<LevelInfo>().nombreNivel = nombre;
        go.GetComponent<LevelInfo>().escenaSiguiente = siguiente;
        Undo.RegisterCreatedObjectUndo(go, "LevelInfo");
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
        cp.beaconRenderer = beacon.GetComponent<MeshRenderer>();
        cp.numero = numero;
        Undo.RegisterCreatedObjectUndo(trig, name);
    }

    static void L1RotatingBar(GameObject parent, string name, Vector3 pos, float length, float speed)
    {
        var go = Platform(parent, name, pos, new Vector3(length, 0.4f, 0.4f), matHazard);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var rot = go.AddComponent<RotatingHazard>();
        rot.rotationSpeed = new Vector3(0f, 0f, speed);
        var glow = go.AddComponent<HazardGlow>();
        glow.crearLuz = false;
    }

    /// <summary>Rejilla/ventilación de energía verde peligrosa (no se mueve).</summary>
    static void L1Vent(GameObject parent, string name, Vector3 pos, Vector3 scale)
    {
        var go = Platform(parent, name, pos, scale, matHazard);
        var glow = go.AddComponent<HazardGlow>();
        glow.crearLuz = false;
        glow.minIntensidad = 1.2f;
        glow.maxIntensidad = 2.6f;
        glow.velocidad = 1.4f;
    }

    static void L1Patrol(GameObject parent, string name, Vector3 a, Vector3 b, float speed)
    {
        var go = Platform(parent, name, a, new Vector3(1.2f, 1.2f, 1.2f), matHazard);
        go.GetComponent<BoxCollider>().isTrigger = true;
        var p = go.AddComponent<PatrolHazard>();
        p.pointA = a;
        p.pointB = b;
        p.speed = speed;
        go.AddComponent<HazardGlow>();
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

    // ══════════════════════════════════════════════════════════════
    //  NIVEL 2 — "RED DE DATOS"  (hackeos: 1, 2 y 3 módulos)
    // ══════════════════════════════════════════════════════════════

    [MenuItem("SEEN/Construir Nivel 2 (Red de Datos)")]
    static void BuildNivel2()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 2 — SEEN",
                "Se creará una escena nueva <b>Nivel2</b>:\n\n" +
                "• Plaza de entrada\n• Plataformeo + plataforma móvil horizontal\n" +
                "• Pasillo con barras rotatorias → HACKEO 1 (1 módulo)\n" +
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

        // Killzone
        var kz = new GameObject("N2_KillZone");
        kz.transform.SetParent(root.transform, true);
        kz.transform.position = new Vector3(0f, -14f, 55f);
        var kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(400f, 4f, 500f);
        Undo.RegisterCreatedObjectUndo(kz, "KillZone");

        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(0f, 1.3f, 7f);

        L1LevelInfo(root, "Nivel 2 — Red de Datos", "Nivel3");

        SetupAtmosphere();
        SetupPostProcessing();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Nivel2.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] NIVEL 2 construido: 3 hackeos (1, 2 y 3 módulos).");
    }

    static void BuildNivel2_ZonaPlaza(GameObject root)
    {
        Platform(root, "N2_Plaza", new Vector3(0f, 0f, 0f), new Vector3(24f, 0.5f, 24f), matConcrete);
        Wall(root, "N2_Wall_Back", new Vector3(0f, 3.5f, -12.3f), new Vector3(24f, 7f, 0.6f));
        Wall(root, "N2_Wall_L", new Vector3(-12.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 24f));
        Wall(root, "N2_Wall_R", new Vector3(12.3f, 3.5f, 0f), new Vector3(0.6f, 7f, 24f));
        Wall(root, "N2_Wall_FL", new Vector3(-8.2f, 3.5f, 12.3f), new Vector3(8f, 7f, 0.6f));
        Wall(root, "N2_Wall_FR", new Vector3(8.2f, 3.5f, 12.3f), new Vector3(8f, 7f, 0.6f));

        // Muro de publicaciones
        for (int col = 0; col < 5; col++)
            for (int row = 0; row < 2; row++)
                Platform(root, $"N2_Post_{col}_{row}",
                    new Vector3(-4f + col * 2f, 2f + row * 1.5f, -12.05f),
                    new Vector3(1.4f, 1.2f, 0.15f), (col + row) % 2 == 0 ? matCyan : matMagenta);

        // Arco de salida
        Wall(root, "N2_Arco_L", new Vector3(-3.2f, 3f, 12.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "N2_Arco_R", new Vector3(3.2f, 3f, 12.3f), new Vector3(0.9f, 6f, 0.9f));
        Wall(root, "N2_Arco_Top", new Vector3(0f, 6.4f, 12.3f), new Vector3(7.3f, 0.7f, 0.9f));
        Platform(root, "N2_Arco_Luz", new Vector3(0f, 5.9f, 12.3f), new Vector3(6f, 0.12f, 0.4f), matCyan);

        // Guías en el piso
        Platform(root, "N2_Guia", new Vector3(0f, 0.28f, 4f), new Vector3(0.5f, 0.06f, 16f), matCyan);
        L1Vent(root, "N2_VentPlaza", new Vector3(-6f, 0.28f, -4f), new Vector3(3f, 0.05f, 3f));

        L1Light(root, "N2_Light_Plaza", new Vector3(0f, 5.5f, 0f), new Color(0.4f, 0.8f, 1f), 12f, 18f);
    }

    static void BuildNivel2_ZonaTorre(GameObject root)
    {
        // Plataformeo inicial (gaps amplios, todos saltables)
        Platform(root, "N2_P1", new Vector3(0f, 1.5f, 16f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N2_P2", new Vector3(0f, 3.5f, 22f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N2_P3", new Vector3(2f, 5.5f, 28f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1Riel(root, "N2_P1_R", new Vector3(0f, 1.68f, 16f), new Vector3(2.5f, 0.12f, 0.3f));
        L1MovingPlatform(root, "N2_P4_MovH", new Vector3(0f, 6.5f, 34f), new Vector3(3f, 0.3f, 3f),
            new Vector3(7f, 0f, 0f), 1.2f);

        // Torres laterales
        L1Tower(root, "N2_Tower_A", new Vector3(-9f, 4f, 20f), new Vector3(3f, 16f, 3f), false);
        L1Tower(root, "N2_Tower_B", new Vector3(9f, 4f, 20f), new Vector3(3f, 16f, 3f), true);
        L1Cable(root, "N2_Cable_T", new Vector3(-9f, 12f, 20f), new Vector3(9f, 12f, 20f), 0.08f);
        var rng = new System.Random(41);
        L1Debris(root, rng, 8, new Bounds(new Vector3(0f, 3f, 24f), new Vector3(14f, 4f, 20f)));

        // Pasillo con barras (piso continuo, misma altura que la plataforma móvil)
        PlataformaZ(root, "N2_Corredor", 38f, 56f, 6.75f, 0.5f, matConcrete, 0f, 6f);
        Wall(root, "N2_CorWall_L", new Vector3(-3.6f, 10f, 47f), new Vector3(0.6f, 7f, 18f));
        Wall(root, "N2_CorWall_R", new Vector3(3.6f, 10f, 47f), new Vector3(0.6f, 7f, 18f));
        Platform(root, "N2_Strip_L", new Vector3(-3.2f, 13.2f, 47f), new Vector3(0.12f, 0.06f, 17f), matCyan);
        Platform(root, "N2_Strip_R", new Vector3(3.2f, 13.2f, 47f), new Vector3(0.12f, 0.06f, 17f), matCyan);
        for (int i = 0; i < 3; i++)
        {
            float z = 41f + i * 6f;
            Wall(root, $"N2_Techo_{i}", new Vector3(0f, 13.7f, z), new Vector3(6f, 0.3f, 2.2f));
            Platform(root, $"N2_LuzT_{i}", new Vector3(0f, 13.5f, z), new Vector3(1.2f, 0.08f, 0.7f), matCyan);
        }
        L1Vent(root, "N2_Vent_Cor", new Vector3(0f, 7f, 45f), new Vector3(5f, 0.05f, 2.2f));
        L1RotatingBar(root, "N2_Bar_1", new Vector3(0f, 8.2f, 43f), 6.8f, 45f);
        L1RotatingBar(root, "N2_Bar_2", new Vector3(0f, 8.2f, 50f), 6.8f, 60f);

        // ── HACKEO 1 (1 módulo) ──
        var puertaA = L1Puerta(root, "N2_PuertaA", new Vector3(0f, 9f, 54f), 40f, 4.4f);
        L1Tableta(root, "N2_TabletaA", new Vector3(-2.3f, 7f, 52.5f),
                  new[] { HackableType.DescifrarCodigo }, "ARCHIVO 1", puertaA);
        L1Checkpoint(root, "N2_CP_A", new Vector3(0f, 7.5f, 57f), 1);

        // Torre de señal
        Platform(root, "N2_P5", new Vector3(0f, 8f, 61f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N2_P6", new Vector3(0f, 9.5f, 67f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "N2_P7_MovV", new Vector3(-3.5f, 9.5f, 72f), new Vector3(3f, 0.3f, 3f),
            new Vector3(0f, 3.5f, 0f), 1f);
        PlataformaZ(root, "N2_P8", 76f, 80f, 13.2f, 0.4f, matConcrete, 0f, 4f);
        L1Checkpoint(root, "N2_CP_B", new Vector3(0f, 13.5f, 78f), 2);

        // ── HACKEO 2 (2 módulos) ── plataforma continua 80..90 (tope 13.4 = P8)
        PlataformaZ(root, "N2_PuertaPlat", 80f, 90f, 13.15f, 0.5f, matConcrete, 0f, 10f);
        var puertaB = L1Puerta(root, "N2_PuertaB", new Vector3(0f, 15.6f, 84f), 30f, 4.4f);
        L1Tableta(root, "N2_TabletaB", new Vector3(-3.5f, 13.4f, 87.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables }, "ARCHIVO 2", puertaB);

        // Hueco con dash: D1 toca la plataforma en 90; D2/D3 separados ≥4 unidades
        PlataformaZ(root, "N2_D1", 90f, 93f, 13.2f, 0.4f, matDash, 0f, 3f);
        PlataformaZ(root, "N2_D2", 97f, 100f, 13.2f, 0.4f, matDash, 0f, 3f);
        PlataformaZ(root, "N2_D3", 104f, 107f, 14f, 0.4f, matDash, 0f, 3.5f);

        L1Light(root, "N2_Light_Torre", new Vector3(0f, 11f, 70f), new Color(0.3f, 0.7f, 1f), 12f, 22f);
    }

    static void BuildNivel2_ZonaFinal(GameObject root)
    {
        // Sala: piso continuo desde 107 (tope 14.2 = D3) hasta 129
        PlataformaZ(root, "N2_Sala", 107f, 129f, 13.95f, 0.5f, matConcrete, 0f, 26f);
        Wall(root, "N2_SalaWall_Back", new Vector3(0f, 18f, 129.3f), new Vector3(26f, 9f, 0.6f));
        Wall(root, "N2_SalaWall_L", new Vector3(-13.3f, 18f, 118f), new Vector3(0.6f, 9f, 22f));
        Wall(root, "N2_SalaWall_R", new Vector3(13.3f, 18f, 118f), new Vector3(0.6f, 9f, 22f));

        // La Pantalla
        Platform(root, "N2_Pantalla", new Vector3(0f, 18f, 128.9f), new Vector3(9f, 4.5f, 0.4f), matMagenta);
        for (int i = 0; i < 3; i++)
        {
            Platform(root, $"N2_Rack_{i}", new Vector3(-12.2f, 15.4f, 110f + i * 8f),
                     new Vector3(1.4f, 2.9f, 4f), matStructure);
            Platform(root, $"N2_RackLuz_{i}", new Vector3(-11.4f, 15.4f, 110f + i * 8f),
                     new Vector3(0.08f, 2.4f, 0.5f), matCyan);
        }
        for (int i = 0; i < 4; i++)
            Wall(root, $"N2_SalaPilar_{i}",
                 new Vector3(i % 2 == 0 ? -5f : 5f, 17.5f, 109f + (i / 2) * 9f),
                 new Vector3(1.6f, 8f, 1.6f));

        // Guías a la meta
        Platform(root, "N2_SalaGuia_1", new Vector3(-2.5f, 14.22f, 118f), new Vector3(0.16f, 0.05f, 12f), matCyan);
        Platform(root, "N2_SalaGuia_2", new Vector3(2.5f, 14.22f, 118f), new Vector3(0.16f, 0.05f, 12f), matCyan);

        // ── HACKEO 3 (3 módulos) ──
        var puertaC = L1Puerta(root, "N2_PuertaC", new Vector3(0f, 16.4f, 113f), 26f, 4.4f);
        L1Tableta(root, "N2_TabletaC", new Vector3(-4.5f, 14.2f, 110.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Patron },
                  "ARCHIVO 3", puertaC);
        L1Checkpoint(root, "N2_CP_C", new Vector3(0f, 14.4f, 109f), 3);

        // Entidad hostil (sobre el piso, tope 14.2)
        L1Patrol(root, "N2_ENT", new Vector3(-4.5f, 14.8f, 120f), new Vector3(4.5f, 14.8f, 120f), 2.8f);

        // Meta
        Platform(root, "N2_Meta", new Vector3(0f, 14.6f, 125f), new Vector3(4.5f, 0.4f, 4.5f), matGoal);
        var goal = new GameObject("N2_GoalTrigger");
        goal.transform.SetParent(root.transform, true);
        goal.transform.position = new Vector3(0f, 16f, 125f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(4f, 3f, 4f);
        goal.AddComponent<LevelGoal>();
        L1Beacon(root, "N2_MetaBeacon", new Vector3(0f, 15.9f, 125f), Color.green);

        L1Light(root, "N2_Light_Meta", new Vector3(0f, 18f, 124f), new Color(0.3f, 1f, 0.5f), 14f, 20f);
        L1Light(root, "N2_Light_Sala", new Vector3(0f, 16f, 111f), new Color(0.2f, 0.75f, 1f), 10f, 18f);
    }

    // ══════════════════════════════════════════════════════════════
    //  NIVEL 3 — "EL NÚCLEO"  (hackeos: 1, 2, 3, 4 y 5 módulos)
    // ══════════════════════════════════════════════════════════════

    [MenuItem("SEEN/Construir Nivel 3 (El Núcleo)")]
    static void BuildNivel3()
    {
        if (!EditorUtility.DisplayDialog("Construir Nivel 3 — SEEN",
                "Se creará una escena nueva <b>Nivel3</b>:\n\n" +
                "• 5 hackeos progresivos: 1, 2, 3, 4 y 5 módulos\n" +
                "• Más hazards, plataformas móviles y patrullas\n" +
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
        kz.transform.position = new Vector3(0f, -14f, 60f);
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

        L1LevelInfo(root, "Nivel 3 — El Núcleo", "");

        SetupAtmosphere();
        SetupPostProcessing();
        EnsureManagers();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Nivel3.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[SEENLevelBuilder] NIVEL 3 construido: 5 hackeos (hasta 5 módulos).");
    }

    static void BuildNivel3_Plaza(GameObject root)
    {
        Platform(root, "N3_Plaza", new Vector3(0f, 0f, 0f), new Vector3(28f, 0.5f, 28f), matConcrete);
        Wall(root, "N3_Wall_Back", new Vector3(0f, 4f, -14.3f), new Vector3(28f, 8f, 0.6f));
        Wall(root, "N3_Wall_L", new Vector3(-14.3f, 4f, 0f), new Vector3(0.6f, 8f, 28f));
        Wall(root, "N3_Wall_R", new Vector3(14.3f, 4f, 0f), new Vector3(0.6f, 8f, 28f));
        Wall(root, "N3_Wall_FL", new Vector3(-9f, 4f, 14.3f), new Vector3(10f, 8f, 0.6f));
        Wall(root, "N3_Wall_FR", new Vector3(9f, 4f, 14.3f), new Vector3(10f, 8f, 0.6f));

        for (int col = 0; col < 6; col++)
            for (int row = 0; row < 3; row++)
                Platform(root, $"N3_Post_{col}_{row}",
                    new Vector3(-5f + col * 2f, 1.6f + row * 1.4f, -14.05f),
                    new Vector3(1.4f, 1.2f, 0.15f), (col + row) % 2 == 0 ? matCyan : matMagenta);

        L1Vent(root, "N3_Vent_Plaza", new Vector3(-7f, 0.28f, -4f), new Vector3(4f, 0.05f, 4f));
        Platform(root, "N3_Guia", new Vector3(0f, 0.28f, 6f), new Vector3(0.5f, 0.06f, 14f), matMagenta);
        L1Light(root, "N3_Light_Plaza", new Vector3(0f, 6f, 0f), new Color(0.4f, 0.8f, 1f), 14f, 20f);
    }

    static void BuildNivel3_Sector1(GameObject root)
    {
        // Plataformeo (gaps amplios)
        Platform(root, "N3_P1", new Vector3(0f, 1.5f, 18f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N3_P2", new Vector3(0f, 3.2f, 24f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "N3_P3_MovH", new Vector3(0f, 4.5f, 30f), new Vector3(3f, 0.3f, 3f),
            new Vector3(8f, 0f, 0f), 1.3f);
        PlataformaZ(root, "N3_P4", 35.5f, 38.5f, 6f, 0.3f, matConcrete, 0f, 3f);
        L1Tower(root, "N3_Tower_A", new Vector3(-10f, 3f, 22f), new Vector3(3f, 14f, 3f), false);
        L1Tower(root, "N3_Tower_B", new Vector3(10f, 3f, 22f), new Vector3(3f, 14f, 3f), true);

        // Pasillo peligroso: PEGADO a P4 (empieza en 38.5), tope 6.25
        PlataformaZ(root, "N3_Pasillo", 38.5f, 54.5f, 6f, 0.5f, matConcrete, 0f, 6f);
        Wall(root, "N3_PasoWall_L", new Vector3(-3.6f, 9f, 46.5f), new Vector3(0.6f, 6f, 16f));
        Wall(root, "N3_PasoWall_R", new Vector3(3.6f, 9f, 46.5f), new Vector3(0.6f, 6f, 16f));
        Platform(root, "N3_PasoStrip", new Vector3(0f, 12.2f, 46.5f), new Vector3(5.5f, 0.1f, 15f), matCyan);
        L1RotatingBar(root, "N3_Bar_1", new Vector3(0f, 7.6f, 41f), 6.8f, 50f);
        L1RotatingBar(root, "N3_Bar_2", new Vector3(0f, 7.6f, 48f), 6.8f, 65f);
        L1Vent(root, "N3_Vent_Paso", new Vector3(0f, 6.28f, 44f), new Vector3(5f, 0.05f, 2f));
        L1Patrol(root, "N3_Patrulla_1", new Vector3(-2f, 6.9f, 51f), new Vector3(2f, 6.9f, 51f), 2.2f);

        // ── HACKEO 1 (1 módulo) ──
        var p1 = L1Puerta(root, "N3_Puerta1", new Vector3(0f, 8.4f, 54f), 40f, 4.6f);
        L1Tableta(root, "N3_Tableta1", new Vector3(-2.3f, 6.25f, 52.5f),
                  new[] { HackableType.DescifrarCodigo }, "NODO 1", p1);
        L1Checkpoint(root, "N3_CP_1", new Vector3(0f, 6.5f, 57f), 1);
    }

    static void BuildNivel3_Sector2(GameObject root)
    {
        // Torre con plataforma vertical
        Platform(root, "N3_P5", new Vector3(0f, 7.5f, 61f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        Platform(root, "N3_P6", new Vector3(0f, 9f, 67f), new Vector3(2.5f, 0.3f, 2.5f), matConcrete);
        L1MovingPlatform(root, "N3_P7_MovV", new Vector3(-3.5f, 9f, 72f), new Vector3(3f, 0.3f, 3f),
            new Vector3(0f, 4f, 0f), 1.1f);
        PlataformaZ(root, "N3_P8", 76f, 80f, 13f, 0.4f, matConcrete, 0f, 4f);
        L1Checkpoint(root, "N3_CP_2", new Vector3(0f, 13.3f, 78f), 2);

        // ── HACKEO 2 (2 módulos) ── plataforma 80..90, mismo tope que P8 (13.2)
        PlataformaZ(root, "N3_Plat2", 80f, 90f, 12.95f, 0.5f, matConcrete, 0f, 10f);
        var p2 = L1Puerta(root, "N3_Puerta2", new Vector3(0f, 15.4f, 84f), 30f, 4.4f);
        L1Tableta(root, "N3_Tableta2", new Vector3(-3.5f, 13.2f, 87.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables }, "NODO 2", p2);

        // Plataforma de dash, pegada a Plat2 en 90
        PlataformaZ(root, "N3_D1", 90f, 93f, 13f, 0.4f, matDash, 0f, 3f);
        L1Vent(root, "N3_Vent_Gap", new Vector3(0f, 12f, 94.5f), new Vector3(0.2f, 3f, 3f));

        L1Light(root, "N3_Light_S2", new Vector3(0f, 10f, 75f), new Color(0.3f, 0.7f, 1f), 12f, 22f);
    }

    static void BuildNivel3_Sector3(GameObject root)
    {
        // Pasillo de hazards: separado de D1 por un hueco de 3 (salto)
        PlataformaZ(root, "N3_Pasillo2", 96f, 111f, 12.95f, 0.5f, matConcrete, 0f, 6f);
        Wall(root, "N3_Paso2Wall_L", new Vector3(-3.6f, 15.5f, 103.5f), new Vector3(0.6f, 6f, 15f));
        Wall(root, "N3_Paso2Wall_R", new Vector3(3.6f, 15.5f, 103.5f), new Vector3(0.6f, 6f, 15f));
        Platform(root, "N3_Paso2Strip", new Vector3(0f, 19f, 103.5f), new Vector3(5.5f, 0.1f, 14f), matMagenta);
        L1RotatingBar(root, "N3_Bar_3", new Vector3(0f, 14.8f, 98f), 6.8f, 55f);
        L1RotatingBar(root, "N3_Bar_4", new Vector3(0f, 14.8f, 105f), 6.8f, 70f);
        L1Vent(root, "N3_Vent_Paso2", new Vector3(0f, 13.2f, 108f), new Vector3(5f, 0.05f, 2f));
        L1Patrol(root, "N3_Patrulla_2", new Vector3(-2f, 13.8f, 109.5f), new Vector3(2f, 13.8f, 109.5f), 2.6f);

        // ── HACKEO 3 (3 módulos) ──
        var p3 = L1Puerta(root, "N3_Puerta3", new Vector3(0f, 15.6f, 109.5f), 40f, 4.4f);
        L1Tableta(root, "N3_Tableta3", new Vector3(-2.3f, 13.2f, 107.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Patron },
                  "NODO 3", p3);
        L1Checkpoint(root, "N3_CP_3", new Vector3(0f, 13.5f, 100f), 3);

        // Plataformas de aproximación al núcleo (gaps de 4 = salto/dash)
        PlataformaZ(root, "N3_P9", 114f, 117f, 13f, 0.4f, matConcrete, 0f, 3f);
        PlataformaZ(root, "N3_P10", 121f, 124.5f, 13f, 0.4f, matConcrete, 0f, 3.5f);

        // ── HACKEO 4 (4 módulos) ── plataforma 124.5..134, pegada a P10
        PlataformaZ(root, "N3_Plat4", 124.5f, 134f, 12.95f, 0.5f, matConcrete, 0f, 9f);
        var p4 = L1Puerta(root, "N3_Puerta4", new Vector3(0f, 15.4f, 129f), 30f, 4.4f);
        L1Tableta(root, "N3_Tableta4", new Vector3(-3.5f, 13.2f, 131.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables,
                          HackableType.Patron, HackableType.UnirCables }, "NODO 4", p4);
        L1Checkpoint(root, "N3_CP_4", new Vector3(0f, 13.4f, 127f), 4);
    }

    static void BuildNivel3_Nucleo(GameObject root)
    {
        // Núcleo: piso continuo 134..156, mismo tope que Plat4 (13.2)
        PlataformaZ(root, "N3_Nucleo", 134f, 156f, 12.95f, 0.5f, matConcrete, 0f, 26f);
        Wall(root, "N3_NucWall_Back", new Vector3(0f, 19f, 156.3f), new Vector3(26f, 9f, 0.6f));
        Wall(root, "N3_NucWall_L", new Vector3(-13.3f, 19f, 145f), new Vector3(0.6f, 9f, 22f));
        Wall(root, "N3_NucWall_R", new Vector3(13.3f, 19f, 145f), new Vector3(0.6f, 9f, 22f));

        // Pantalla del Núcleo
        Platform(root, "N3_Pantalla", new Vector3(0f, 19f, 155.9f), new Vector3(10f, 5f, 0.4f), matMagenta);
        for (int i = 0; i < 4; i++)
            Platform(root, $"N3_NucRack_{i}",
                     new Vector3(i % 2 == 0 ? -12.2f : 12.2f, 16.4f, 137f + (i / 2) * 10f),
                     new Vector3(1.4f, 2.9f, 4f), matStructure);
        for (int i = 0; i < 4; i++)
            Wall(root, $"N3_NucPilar_{i}",
                 new Vector3(i % 2 == 0 ? -5f : 5f, 18.5f, 137f + (i / 2) * 10f),
                 new Vector3(1.6f, 8f, 1.6f));

        Platform(root, "N3_NucGuia_1", new Vector3(-2.5f, 13.22f, 145f), new Vector3(0.16f, 0.05f, 16f), matMagenta);
        Platform(root, "N3_NucGuia_2", new Vector3(2.5f, 13.22f, 145f), new Vector3(0.16f, 0.05f, 16f), matMagenta);

        // ── HACKEO 5 (5 módulos) ──
        var p5 = L1Puerta(root, "N3_Puerta5", new Vector3(0f, 15.5f, 140f), 26f, 4.6f);
        L1Tableta(root, "N3_Tableta5", new Vector3(-4.5f, 13.2f, 142.5f),
                  new[] { HackableType.DescifrarCodigo, HackableType.UnirCables, HackableType.Patron,
                          HackableType.UnirCables, HackableType.DescifrarCodigo }, "NÚCLEO", p5);
        L1Checkpoint(root, "N3_CP_5", new Vector3(0f, 13.4f, 137f), 5);

        L1Patrol(root, "N3_Patrulla_3", new Vector3(-4.5f, 13.8f, 147f), new Vector3(4.5f, 13.8f, 147f), 3f);

        // Meta final
        Platform(root, "N3_Meta", new Vector3(0f, 13.6f, 152f), new Vector3(5f, 0.4f, 5f), matGoal);
        var goal = new GameObject("N3_GoalTrigger");
        goal.transform.SetParent(root.transform, true);
        goal.transform.position = new Vector3(0f, 15.4f, 152f);
        var gCol = goal.AddComponent<BoxCollider>();
        gCol.isTrigger = true;
        gCol.size = new Vector3(4f, 3f, 4f);
        goal.AddComponent<LevelGoal>();
        L1Beacon(root, "N3_MetaBeacon", new Vector3(0f, 15f, 152f), Color.green);

        L1Light(root, "N3_Light_Nucleo", new Vector3(0f, 18f, 138f), new Color(0.3f, 1f, 0.5f), 16f, 22f);
        L1Light(root, "N3_Light_Meta", new Vector3(0f, 17f, 152f), new Color(0.3f, 1f, 0.5f), 14f, 20f);
    }

    /// <summary>Registra una escena en Build Settings (junto al menú y las demás).</summary>
    static void RegistrarEscena(string nombre)
    {
        string ruta = $"Assets/Scenes/{nombre}.unity";
        var escenas = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Juego.unity", true)
        };

        foreach (var existente in EditorBuildSettings.scenes)
        {
            if (existente.path.Contains("/Nivel2") && nombre != "Nivel2")
                escenas.Add(existente);
            if (existente.path.Contains("/Nivel3") && nombre != "Nivel3")
                escenas.Add(existente);
        }

        if (!escenas.Exists(e => e.path == ruta))
            escenas.Add(new EditorBuildSettingsScene(ruta, true));

        EditorBuildSettings.scenes = escenas.ToArray();
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
