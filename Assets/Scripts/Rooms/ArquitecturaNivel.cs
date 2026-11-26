using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ajusta colisiones sin obstruir puertas ni elevadores, decora los niveles con
/// prefabs sci-fi reales del proyecto y construye el Nivel 4 (Gancho + Antivirus).
/// </summary>
public class ArquitecturaNivel : MonoBehaviour
{
    private const string NombreRaiz = "ArquitecturaNivel_Root";

    private readonly List<Material> materialesCreados = new List<Material>();
    private readonly Dictionary<string, GameObject> plantillas = new Dictionary<string, GameObject>();

    private Material matEstructura;
    private Material matPiso;
    private Material matPasarela;
    private Material matCian;
    private Material matMagenta;
    private Material matPeligro;
    private Material matMeta;
    private Material matPuerta;

    private static readonly string[] PrefijosDecorativosSinColision =
    {
        "Guia", "Strip", "Luz", "Vent", "Riel", "Beacon", "Faro",
        "Post_", "Prompt", "Light_SideLight", "Pulso", "MallaOrbe",
        "Wall_Simple_05", "DoorWay", "Door_", "Grid_01", "TurnShort",
        "Elevador", "Wall_Console", "Intercom", "Glass", "Stairway",
        "Sign_", "Airing", "Pipes"
    };

    private static readonly string[] NombresPlantillas =
    {
        "Column_01_Big", "Crate_01", "Roof_Glass_01_6x6", "Room_Floor_3x3",
        "Stairway_Plateform_01", "Grid_01", "Door_Right_01", "Door_Left_01",
        "Wall_Simple_04_Long", "Wall_Down_01_Long", "Wall_Cube_01_Long",
        "Wall_Console_01_Large", "Wall_Console_01_Half", "Floor_Squared_01_6x6",
        "Airing_01", "Pipes_01", "Sign_Caution_01", "Sign_Caution_02"
    };

    public static void Inicializar(int dificultad)
    {
        if (Object.FindFirstObjectByType<PlayerController>() == null) return;
        if (GameObject.Find(NombreRaiz) != null) return;

        GameObject go = new GameObject(NombreRaiz);
        ArquitecturaNivel arq = go.AddComponent<ArquitecturaNivel>();
        arq.Construir(dificultad);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < materialesCreados.Count; i++)
        {
            if (materialesCreados[i] != null)
                Destroy(materialesCreados[i]);
        }
        materialesCreados.Clear();
    }

    private void Construir(int dificultad)
    {
        CapturarRecursosEscena();
        CrearMaterialesFaltantes();

        bool esNivel4 = dificultad >= 3 || SceneManager.GetActiveScene().name == "Nivel4";
        if (esNivel4)
        {
            ConstruirNivel4();
            AmpliarKillZones();
            return;
        }

        SolidificarEscena();
        AmpliarKillZones();

        if (dificultad <= 0)
            ConstruirNivel1();
        else if (dificultad == 1)
            ConstruirNivel2();
        else
            ConstruirNivel3();
    }

    // ══════════════════════════════════════════════════════════════
    //  1. MATERIALES Y PLANTILLAS DE ESCENA (CREEPY_CAT)
    // ══════════════════════════════════════════════════════════════

    private void CapturarRecursosEscena()
    {
        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int p = 0; p < NombresPlantillas.Length; p++)
        {
            string prefijo = NombresPlantillas[p];
            if (plantillas.ContainsKey(prefijo)) continue;

            GameObject mejor = null;
            for (int i = 0; i < todos.Length; i++)
            {
                Transform t = todos[i];
                if (t == null || !t.name.StartsWith(prefijo)) continue;
                if (t.parent != null && t.parent.name.StartsWith(prefijo)) continue;
                if (t.GetComponentInChildren<MeshRenderer>(true) == null) continue;

                mejor = t.gameObject;
                if (!t.name.Contains("(")) break;
            }
            if (mejor != null)
                plantillas[prefijo] = mejor;
        }

        for (int i = 0; i < todos.Length; i++)
        {
            Transform t = todos[i];
            if (t == null) continue;
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterial == null) continue;

            string n = t.name;
            Material m = mr.sharedMaterial;
            if (matPiso == null && (n == "L1_Plaza" || n == "N2_PreSalon_Piso" || n == "N3_Plaza" || n.StartsWith("N2_Plaza")))
                matPiso = m;
            if (matEstructura == null && (n == "L1_Wall_Back" || n == "N2_PreSalon_Wall_L" || n == "N3_Wall_Back" || n.StartsWith("N2_Wall_")))
                matEstructura = m;
            if (matCian == null && (n.Contains("Strip") || n.Contains("Guia")))
                matCian = m;
            if (matMeta == null && (n == "L1_Meta" || n == "N2_Meta" || n == "N3_Meta" || n == "Meta"))
                matMeta = m;
        }

        RotatingHazard rh = Object.FindFirstObjectByType<RotatingHazard>();
        if (rh != null)
        {
            MeshRenderer mr = rh.GetComponentInChildren<MeshRenderer>();
            if (mr != null) matPeligro = mr.sharedMaterial;
        }
    }

    private void CrearMaterialesFaltantes()
    {
        if (matEstructura == null) matEstructura = NuevoMaterial("Arq_Estructura", new Color(0.11f, 0.13f, 0.17f), Color.black, 0f);
        if (matPiso == null) matPiso = NuevoMaterial("Arq_Piso", new Color(0.16f, 0.18f, 0.22f), Color.black, 0f);
        if (matPasarela == null) matPasarela = NuevoMaterial("Arq_Pasarela", new Color(0.14f, 0.21f, 0.27f), new Color(0.05f, 0.28f, 0.38f), 0.7f);
        if (matCian == null) matCian = NuevoMaterial("Arq_Cian", new Color(0.05f, 0.26f, 0.36f), new Color(0.15f, 0.85f, 1f), 2.4f);
        if (matMagenta == null) matMagenta = NuevoMaterial("Arq_Magenta", new Color(0.24f, 0.06f, 0.20f), new Color(1f, 0.18f, 0.65f), 2.4f);
        if (matPeligro == null) matPeligro = NuevoMaterial("Arq_Peligro", new Color(0.25f, 0.04f, 0.04f), new Color(1f, 0.16f, 0.12f), 2.6f);
        if (matMeta == null) matMeta = NuevoMaterial("Arq_Meta", new Color(0.04f, 0.22f, 0.09f), new Color(0.18f, 1f, 0.42f), 2.4f);
        matPuerta = NuevoMaterial("Arq_Puerta", new Color(0.14f, 0.17f, 0.23f), new Color(0.08f, 0.32f, 0.45f), 0.8f);
    }

    private Material NuevoMaterial(string nombre, Color baseCol, Color emision, float intensidad)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.name = nombre;
        mat.color = baseCol;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
        if (intensidad > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emision * intensidad);
        }
        materialesCreados.Add(mat);
        return mat;
    }

    // ══════════════════════════════════════════════════════════════
    //  2. SOLIDEZ SEGURA ANTI NO-CLIP (SIN BLOQUEAR PUERTAS/ASCENSOR)
    // ══════════════════════════════════════════════════════════════

    private void SolidificarEscena()
    {
        RepararMeshCollidersVacios();
        EngrosarPisosDelgados();
        AñadirCollidersAMallasSinColision();
    }

    private static void RepararMeshCollidersVacios()
    {
        MeshCollider[] meshCols = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < meshCols.Length; i++)
        {
            MeshCollider mc = meshCols[i];
            if (mc == null || mc.sharedMesh != null) continue;
            mc.enabled = false;
        }
    }

    private static void EngrosarPisosDelgados()
    {
        BoxCollider[] boxes = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < boxes.Length; i++)
        {
            BoxCollider bc = boxes[i];
            if (bc == null || bc.isTrigger) continue;

            string nombre = bc.name;
            bool esPisoOTecho = nombre.Contains("Floor_") || nombre.Contains("Room_Floor_") || nombre.Contains("Roof_");
            if (!esPisoOTecho) continue;

            Vector3 s = bc.size;
            Vector3 c = bc.center;
            bool cambiado = false;

            if (s.z < 0.05f)
            {
                c.z -= 0.12f;
                s.z = 0.24f;
                cambiado = true;
            }
            if (s.y < 0.05f) { s.y = 0.24f; cambiado = true; }
            if (s.x < 0.05f) { s.x = 0.24f; cambiado = true; }

            if (cambiado)
            {
                bc.size = s;
                bc.center = c;
            }
        }
    }

    private static void AñadirCollidersAMallasSinColision()
    {
        MeshFilter[] filtros = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < filtros.Length; i++)
        {
            MeshFilter mf = filtros[i];
            if (mf == null || mf.sharedMesh == null || !mf.gameObject.activeInHierarchy) continue;

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || !mr.enabled) continue;
            if (mf.GetComponent<Collider>() != null) continue;
            if (DebeOmitirColision(mf)) continue;

            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }
    }

    private static bool DebeOmitirColision(MeshFilter mf)
    {
        if (mf.GetComponent<TextMesh>() != null) return true;
        if (mf.GetComponentInParent<PlayerController>() != null) return true;
        if (mf.GetComponentInParent<EnemyBase>() != null) return true;
        if (mf.GetComponentInParent<Pistola>() != null) return true;
        if (mf.GetComponentInParent<Gancho>() != null) return true;
        if (mf.GetComponentInParent<AnclajeGancho>() != null) return true;
        if (mf.GetComponentInParent<Checkpoint>() != null) return true;
        if (mf.GetComponentInParent<LevelGoal>() != null) return true;
        if (mf.GetComponentInParent<RotatingHazard>() != null) return true;
        if (mf.GetComponentInParent<PatrolHazard>() != null) return true;
        if (mf.GetComponentInParent<BarraPeligro>() != null) return true;
        if (mf.GetComponentInParent<Antivirus>() != null) return true;
        if (mf.GetComponentInParent<HackTerminal>() != null) return true;
        if (mf.GetComponentInParent<DoorController>() != null) return true;
        if (mf.GetComponentInParent<Ascensor>() != null) return true;
        if (mf.GetComponentInParent<MovingPlatform>() != null) return true;
        if (mf.GetComponentInParent<Rigidbody>() != null) return true;
        if (mf.GetComponentInParent<Game.Scanner.ScannerRevealable>() != null) return true;

        Transform cursor = mf.transform;
        while (cursor != null)
        {
            string nombre = cursor.name;
            for (int i = 0; i < PrefijosDecorativosSinColision.Length; i++)
            {
                if (nombre.Contains(PrefijosDecorativosSinColision[i])) return true;
            }
            cursor = cursor.parent;
        }
        return false;
    }

    private static void AmpliarKillZones()
    {
        BoxCollider[] boxes = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < boxes.Length; i++)
        {
            BoxCollider bc = boxes[i];
            if (bc != null && bc.name.Contains("KillZone"))
                bc.size = new Vector3(1200f, 8f, 1200f);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  3. NIVEL 1 — SALÓN PREVIO + DECORACIÓN SCI-FI LATERAL
    // ══════════════════════════════════════════════════════════════

    private void ConstruirNivel1()
    {
        GameObject muroBack = GameObject.Find("L1_Wall_Back");
        if (muroBack != null)
        {
            AbrirVanoEnMuro(muroBack, 7.1f);
            DespejarVano(new Vector3(0f, 2.2f, -16.8f), new Vector3(3.35f, 2.2f, 2.4f), "L1_Plaza");
        }

        ConstruirSalónPrevioGenerico("N1", -28.0f, -17.3f, 0.25f, 6.8f, 12.6f, "Door_Right_01", new Vector3(0f, 1.3f, -23.0f));

        // Decoración lateral sci-fi en los flancos exteriores (sin invadir pasillos, ascensor ni puertas)
        DecorarFlancosSciFi("N1_Plaza1", new float[] { -12.0f, 12.0f }, 0.25f, new float[] { -12f, -2f, 8f }, true);
        DecorarFlancosSciFi("N1_Plaza2", new float[] { -12.0f, 12.0f }, 0.25f, new float[] { 60f, 70f, 80f }, true);
        DecorarFlancosSciFi("N1_SalaSup", new float[] { -11.0f, 11.0f }, 9.25f, new float[] { 107f, 116f }, false);

        // Claraboyas altas sobre las plazas
        ColocarClaraboyas(new Vector3(0f, 6.95f, -4f), 1, 2, 10f);
        ColocarClaraboyas(new Vector3(0f, 6.95f, 70f), 1, 2, 10f);
        ColocarClaraboyas(new Vector3(0f, 16.5f, 113f), 1, 2, 9f);

        CrearTorresExteriores(new Vector3(0f, -6f, 45f), 25f, 16f, 24f, 4);
    }

    // ══════════════════════════════════════════════════════════════
    //  4. NIVEL 2 — DECORACIÓN SCI-FI RESPETANDO TODAS LAS RUTAS
    // ══════════════════════════════════════════════════════════════

    private void ConstruirNivel2()
    {
        // Reforzar decoración del salón previo y bordes de plaza/sala final sin cerrar ningún paso
        ColocarProp("Column_01_Big", new Vector3(-5.0f, 0.25f, -25.2f), Quaternion.identity, new Vector3(1.1f, 6.2f, 1.1f), true);
        ColocarProp("Column_01_Big", new Vector3(5.0f, 0.25f, -25.2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, 6.2f, 1.1f), true);
        ColocarProp("Crate_01", new Vector3(-4.5f, 0.25f, -18.5f), Quaternion.identity, new Vector3(1.2f, 1.2f, 1.2f), true);
        ColocarProp("Wall_Console_01_Half", new Vector3(5.0f, 0.25f, -20.0f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, 2.2f, 1.8f), true);
        ColocarClaraboyas(new Vector3(0f, 6.55f, -20.0f), 1, 2, 6f);

        DecorarFlancosSciFi("N2_Plaza", new float[] { -12.6f, 12.6f }, 0.25f, new float[] { -8f, 0f, 8f }, false);
        DecorarFlancosSciFi("N2_SalaFin", new float[] { -10.5f, 18.8f }, 10.35f, new float[] { 126f, 134f, 141f }, false);
        ColocarClaraboyas(new Vector3(0f, 6.55f, 0f), 2, 3, 7f);
        ColocarClaraboyas(new Vector3(4.4f, 16.5f, 133f), 2, 3, 7f);

        CrearTorresExteriores(new Vector3(0f, -3f, 45f), 24f, 16f, 22f, 5);
    }

    // ══════════════════════════════════════════════════════════════
    //  5. NIVEL 3 — SALÓN PREVIO + DECORACIÓN + CHECKPOINT EXTRA
    // ══════════════════════════════════════════════════════════════

    private void ConstruirNivel3()
    {
        GameObject muroBack = GameObject.Find("N3_Wall_Back");
        if (muroBack != null)
        {
            AbrirVanoEnMuro(muroBack, 7.1f);
            DespejarVano(new Vector3(0f, 2.2f, -14.8f), new Vector3(3.35f, 2.2f, 2.5f), "N3_Plaza");
        }

        ConstruirSalónPrevioGenerico("N3", -27.5f, -15.30f, 0.25f, 7.2f, 12.8f, "Door_Right_01", new Vector3(0f, 1.3f, -22.0f));

        DecorarFlancosSciFi("N3_Plaza1", new float[] { -13.8f, 13.8f }, 0.25f, new float[] { -9f, 0f, 9f }, false);
        DecorarFlancosSciFi("N3_Nucleo", new float[] { -22.0f, 22.0f }, 13.5f, new float[] { 172f, 188f, 204f }, false);
        ColocarClaraboyas(new Vector3(0f, 7.2f, 0f), 2, 3, 8f);
        ColocarClaraboyas(new Vector3(0f, 21.8f, 188f), 2, 4, 10f);

        CrearCheckpointExtra("N3_CP_NucleoCircuito", new Vector3(0f, 13.5f, 196.0f), 6);
        CrearTorresExteriores(new Vector3(0f, -2f, 65f), 26f, 18f, 24f, 6);
    }

    // ══════════════════════════════════════════════════════════════
    //  6. NIVEL 4 — GANCHO SPIDER-MAN + ESCAPE DEL ANTIVIRUS
    // ══════════════════════════════════════════════════════════════

    private void ConstruirNivel4()
    {
        LimpiarEscenaParaNivel4();

        // KillZone global de caída ampliada para todo el recorrido
        GameObject kz = new GameObject("N4_KillZone");
        kz.transform.SetParent(transform, true);
        kz.transform.position = new Vector3(0f, -15f, 220f);
        BoxCollider kzCol = kz.AddComponent<BoxCollider>();
        kzCol.isTrigger = true;
        kzCol.size = new Vector3(1200f, 8f, 1200f);

        // Iluminación direccional suave para realzar metales y colores del kit
        CrearLuzDireccionalNivel4();

        // ── ZONA 0: SALÓN DE EQUIPAMIENTO (Z = -28..-12.2, Y = 0..7) ──
        ConstruirSalaCerrada("N4_PreSalon", new Vector2(-6.5f, 6.5f), -28.0f, -12.2f, 0.25f, 7.0f, false, true);
        CrearMuroConPuertaHack("N4_PreSalon_Salida", new Vector3(0f, 0.25f, -12.2f), 13.0f, 6.75f, 7.0f, 4.7f, true);
        ColocarProp("Column_01_Big", new Vector3(-5.2f, 0.25f, -25.8f), Quaternion.identity, new Vector3(1.1f, 6.3f, 1.1f), true);
        ColocarProp("Column_01_Big", new Vector3(5.2f, 0.25f, -25.8f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, 6.3f, 1.1f), true);
        ColocarProp("Wall_Console_01_Half", new Vector3(-5.1f, 0.25f, -19.5f), Quaternion.identity, new Vector3(1.1f, 2.3f, 2.0f), true);
        ColocarProp("Crate_01", new Vector3(4.9f, 0.25f, -19.5f), Quaternion.Euler(0f, 15f, 0f), new Vector3(1.3f, 1.3f, 1.3f), true);
        ColocarProp("Sign_Caution_01", new Vector3(-4.2f, 3.3f, -12.8f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, 0f, 0f), false);
        ColocarClaraboyas(new Vector3(0f, 6.75f, -20.0f), 1, 2, 6.5f);
        CrearLuzPuntual(new Vector3(0f, 5.4f, -20.0f), new Color(0.3f, 0.85f, 1f), 8f, 22f);

        // Pedestal central + Gancho Spider-Man
        CrearBloque("N4_Pedestal_Gancho", new Vector3(0f, 0.55f, -17.5f), new Vector3(1.15f, 0.65f, 1.15f), matEstructura, matCian, true);
        GameObject ganchoGO = new GameObject("N4_Gancho");
        ganchoGO.transform.SetParent(transform, true);
        ganchoGO.transform.position = new Vector3(0f, 1.35f, -17.5f);
        BoxCollider gCol = ganchoGO.AddComponent<BoxCollider>();
        gCol.size = new Vector3(0.75f, 0.75f, 0.9f);
        ganchoGO.AddComponent<Gancho>();

        // Bot Antivirus esperando a que recojas el Gancho
        GameObject avGO = new GameObject("N4_Antivirus");
        avGO.transform.SetParent(transform, true);
        avGO.transform.position = new Vector3(0f, 2.2f, -26.2f);
        Antivirus av = avGO.AddComponent<Antivirus>();
        av.velocidad = 6.5f;
        av.radioAtrapar = 2.2f;
        av.retrasoInicial = 1.8f;
        av.esperarGancho = true;

        // Balcón de despegue saliendo por la puerta del salón (Z = -12.2 .. -5.0, Y = 0)
        CrearPiso("N4_Despegue", new Vector3(0f, 0f, -8.6f), new Vector3(13f, 0.5f, 7.2f));
        ColocarProp("Column_01_Big", new Vector3(-5.2f, 0.25f, -6.2f), Quaternion.identity, new Vector3(1.1f, 6.0f, 1.1f), true);
        ColocarProp("Column_01_Big", new Vector3(5.2f, 0.25f, -6.2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, 6.0f, 1.1f), true);

        // ── ZONA 1: GRAN ABISMO DE COLUMPIO 1 (Z = -5 .. 110, Y = 0 .. 4.0) ──
        ConstruirCañonSciFi("N4_C1", -5.0f, 110.0f, 18.0f, 18.0f, 5);
        CrearNodoGancho("N4_Nodo1", new Vector3(0f, 13.5f, 12.0f), 18.0f);
        CrearNodoGancho("N4_Nodo2", new Vector3(-2.5f, 14.5f, 36.0f), 18.0f);
        CrearNodoGancho("N4_Nodo3", new Vector3(2.5f, 15.2f, 62.0f), 18.0f);
        CrearNodoGancho("N4_Nodo4", new Vector3(0f, 16.0f, 88.0f), 18.0f);
        CrearIslaRescate("N4_Isla_1A", new Vector3(0f, 2.0f, 50.0f));
        CrearLuzPuntual(new Vector3(0f, 13.5f, 28.0f), new Color(0.2f, 0.85f, 1f), 7f, 34f);
        CrearLuzPuntual(new Vector3(0f, 14.5f, 76.0f), new Color(0.2f, 0.85f, 1f), 7f, 34f);

        // Sala de Aterrizaje 1 (Z = 110 .. 128, Y = 4.0f)
        ConstruirSalaIntermedia("N4_Sala1", 110.0f, 128.0f, 4.0f, 121.0f, "COMPUERTA 1", 2, 1, 124.8f);

        // ── ZONA 2: CAÑÓN LARGO ASCENDENTE (Z = 128 .. 280, Y = 4.0 .. 9.0) ──
        CrearPiso("N4_Despegue2", new Vector3(0f, 4.0f, 130.0f), new Vector3(13f, 0.5f, 4.0f));
        ConstruirCañonSciFi("N4_C2", 132.0f, 280.0f, 19.0f, 23.0f, 6);
        CrearNodoGancho("N4_Nodo5", new Vector3(0f, 17.5f, 148.0f), 23.0f);
        CrearNodoGancho("N4_Nodo6", new Vector3(3.0f, 18.5f, 174.0f), 23.0f);
        CrearNodoGancho("N4_Nodo7", new Vector3(-3.0f, 19.5f, 204.0f), 23.0f);
        CrearNodoGancho("N4_Nodo8", new Vector3(2.5f, 20.5f, 232.0f), 23.0f);
        CrearNodoGancho("N4_Nodo9", new Vector3(0f, 21.2f, 260.0f), 23.0f);
        CrearIslaRescate("N4_Isla_2A", new Vector3(0f, 5.8f, 188.0f));
        CrearIslaRescate("N4_Isla_2B", new Vector3(0f, 7.5f, 246.0f));
        CrearBarraGiratoria("N4_Laser_2", new Vector3(0f, 13.5f, 218.0f), 8.0f, 55f);
        CrearLuzPuntual(new Vector3(0f, 17.0f, 165.0f), new Color(1f, 0.25f, 0.65f), 7f, 36f);
        CrearLuzPuntual(new Vector3(0f, 18.5f, 218.0f), new Color(1f, 0.25f, 0.65f), 7f, 36f);
        CrearLuzPuntual(new Vector3(0f, 19.5f, 260.0f), new Color(0.2f, 0.85f, 1f), 7f, 36f);

        // Sala de Aterrizaje 2 (Z = 280 .. 298, Y = 9.0f)
        ConstruirSalaIntermedia("N4_Sala2", 280.0f, 298.0f, 9.0f, 291.5f, "COMPUERTA 2", 3, 2, 295.0f);

        // ── ZONA 3: RECTA FINAL EXTREMA Y SANTUARIO DE EXTRACCIÓN (Z = 298 .. 454, Y = 9.0 .. 13.0) ──
        CrearPiso("N4_Despegue3", new Vector3(0f, 9.0f, 300.0f), new Vector3(13f, 0.5f, 4.0f));
        ConstruirCañonSciFi("N4_C3", 302.0f, 430.0f, 19.5f, 26.5f, 5);
        CrearNodoGancho("N4_Nodo10", new Vector3(0f, 21.8f, 318.0f), 26.5f);
        CrearNodoGancho("N4_Nodo11", new Vector3(-3.0f, 22.8f, 346.0f), 26.5f);
        CrearNodoGancho("N4_Nodo12", new Vector3(3.0f, 23.6f, 376.0f), 26.5f);
        CrearNodoGancho("N4_Nodo13", new Vector3(0f, 24.4f, 406.0f), 26.5f);
        CrearIslaRescate("N4_Isla_3A", new Vector3(0f, 11.0f, 362.0f));
        CrearLuzPuntual(new Vector3(0f, 21.0f, 336.0f), new Color(0.2f, 0.9f, 1f), 8f, 36f);
        CrearLuzPuntual(new Vector3(0f, 22.0f, 392.0f), new Color(0.2f, 0.9f, 1f), 8f, 36f);

        // Santuario de Extracción (Z = 430 .. 454, Y = 13.0 base -> 13.25 sup)
        ConstruirSalaCerrada("N4_Santuario", new Vector2(-10.0f, 10.0f), 430.0f, 454.0f, 13.25f, 8.0f, true, false);
        ColocarProp("Column_01_Big", new Vector3(-8.2f, 13.25f, 434.0f), Quaternion.identity, new Vector3(1.2f, 7.2f, 1.2f), true);
        ColocarProp("Column_01_Big", new Vector3(8.2f, 13.25f, 434.0f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.2f, 7.2f, 1.2f), true);
        ColocarProp("Crate_01", new Vector3(7.4f, 13.25f, 448.0f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f), true);
        ColocarClaraboyas(new Vector3(0f, 20.75f, 442.0f), 2, 3, 7f);

        DoorController p3 = CrearMuroConPuertaHack("N4_Compuerta3", new Vector3(0f, 13.25f, 440.0f), 20.0f, 7.6f, 6.2f, 4.6f, false);
        CrearTerminalHack("N4_Tableta_3", new Vector3(-3.8f, 13.25f, 438.0f), 2, "EXTRACCIÓN", p3);

        // Meta final esmeralda
        CrearBloque("N4_Meta", new Vector3(0f, 13.35f, 447.0f), new Vector3(6.5f, 0.35f, 6.5f), matMeta, matMeta, true);
        GameObject faroMeta = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        faroMeta.name = "N4_MetaBeacon";
        faroMeta.transform.SetParent(transform, true);
        faroMeta.transform.position = new Vector3(0f, 16.2f, 447.0f);
        faroMeta.transform.localScale = new Vector3(0.45f, 2.8f, 0.45f);
        faroMeta.GetComponent<MeshRenderer>().sharedMaterial = matMeta;
        Destroy(faroMeta.GetComponent<Collider>());

        GameObject goalGO = new GameObject("N4_GoalTrigger");
        goalGO.transform.SetParent(transform, true);
        goalGO.transform.position = new Vector3(0f, 14.8f, 447.0f);
        BoxCollider goalCol = goalGO.AddComponent<BoxCollider>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector3(6.0f, 3.8f, 6.0f);
        goalGO.AddComponent<LevelGoal>();
        CrearLuzPuntual(new Vector3(0f, 17.5f, 447.0f), new Color(0.25f, 1f, 0.45f), 10f, 22f);

        PosicionarJugador(new Vector3(0f, 1.3f, -22.5f), 0f);
    }

    private void LimpiarEscenaParaNivel4()
    {
        // Preservar Player, UI, iluminación global, volumen y LevelInfo en raíz antes de apagar Nivel 2
        LevelInfo li = Object.FindFirstObjectByType<LevelInfo>();
        if (li != null && li.transform.parent != null) li.transform.SetParent(null, true);

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null && pc.transform.parent != null) pc.transform.SetParent(null, true);

        GameObject[] raices = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < raices.Length; i++)
        {
            GameObject r = raices[i];
            if (r == null || r == gameObject) continue;
            if (DebeConservarseEnNivel4(r)) continue;
            r.SetActive(false);
        }
    }

    private static bool DebeConservarseEnNivel4(GameObject r)
    {
        string n = r.name;
        if (n == NombreRaiz || n == "Player" || n == "MainCamera" || n == "Main Camera" ||
            n == "L1_UI" || n == "L1_GlobalVolume" || n == "TestLevelHints" || n == "HUDSistema" ||
            n == "GameFeedback" || n == "BootRedirect" || n == "EventSystem" ||
            n == "EstructurasDatosJuego" || n == "PantallaSistema" || n == "L1_LevelInfo")
            return true;

        if (r.GetComponent<PlayerController>() != null) return true;
        if (r.GetComponent<LevelInfo>() != null) return true;
        if (r.GetComponent<GameFeedback>() != null) return true;
        if (r.GetComponent<HUDSistema>() != null) return true;
        if (r.GetComponent<TestLevelHints>() != null) return true;
        if (r.GetComponent<Camera>() != null) return true;
        if (r.GetComponent("Volume") != null) return true;

        Light l = r.GetComponent<Light>();
        if (l != null && l.type == LightType.Directional) return true;

        return false;
    }

    // ══════════════════════════════════════════════════════════════
    //  7. HELPERS MODULARES SCI-FI Y COLUMPIO
    // ══════════════════════════════════════════════════════════════

    private void ConstruirSalónPrevioGenerico(string prefijo, float zMin, float zMax, float yPisoSup, float alto, float ancho, string prefabPuerta, Vector3 posJugador)
    {
        float medioX = ancho * 0.5f;
        float centroZ = (zMin + zMax) * 0.5f;
        float largo = zMax - zMin;
        float yBasePiso = yPisoSup - 0.25f;

        CrearPiso(prefijo + "_PreSalon_Piso", new Vector3(0f, yBasePiso, centroZ), new Vector3(ancho, 0.5f, largo));
        CrearBloque(prefijo + "_PreSalon_Techo", new Vector3(0f, yBasePiso + alto, centroZ), new Vector3(ancho, 0.5f, largo), matEstructura, matCian, true);
        CrearBloque(prefijo + "_PreSalon_WallL", new Vector3(-medioX, yBasePiso + alto * 0.5f, centroZ), new Vector3(0.6f, alto, largo), matEstructura, matCian, true);
        CrearBloque(prefijo + "_PreSalon_WallR", new Vector3(medioX, yBasePiso + alto * 0.5f, centroZ), new Vector3(0.6f, alto, largo), matEstructura, matMagenta, true);
        CrearBloque(prefijo + "_PreSalon_WallBack", new Vector3(0f, yBasePiso + alto * 0.5f, zMin - 0.3f), new Vector3(ancho + 0.6f, alto, 0.6f), matEstructura, matCian, true);

        // Marco y puerta interactiva en zMax (abriendo al salón principal)
        CrearPuertaEntradaInteractiva(prefijo + "_PuertaEntrada", new Vector3(0f, yPisoSup, zMax), prefabPuerta);

        // Decoración interna con prefabs del juego en bordes laterales
        ColocarProp("Floor_Squared_01_6x6", new Vector3(0f, yPisoSup + 0.01f, centroZ), Quaternion.identity, new Vector3(5.8f, 0f, 5.8f), true, false);
        ColocarProp("Column_01_Big", new Vector3(-medioX + 1.1f, yPisoSup, zMin + 2.0f), Quaternion.identity, new Vector3(1.1f, alto - 0.6f, 1.1f), true);
        ColocarProp("Column_01_Big", new Vector3(medioX - 1.1f, yPisoSup, zMin + 2.0f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, alto - 0.6f, 1.1f), true);
        ColocarProp("Wall_Console_01_Large", new Vector3(-medioX + 1.0f, yPisoSup, centroZ), Quaternion.identity, new Vector3(1.2f, 2.8f, 3.8f), true);
        ColocarProp("Crate_01", new Vector3(medioX - 1.4f, yPisoSup, centroZ + 1.0f), Quaternion.Euler(0f, 18f, 0f), new Vector3(1.3f, 1.3f, 1.3f), true);
        ColocarClaraboyas(new Vector3(0f, yBasePiso + alto - 0.28f, centroZ), 1, 2, 5.5f);
        CrearLuzPuntual(new Vector3(0f, yPisoSup + 4.8f, centroZ), new Color(0.35f, 0.85f, 1f), 9f, 20f);

        PosicionarJugador(posJugador, 0f);
    }

    private void AbrirVanoEnMuro(GameObject muro, float anchoVano)
    {
        Vector3 pos = muro.transform.position;
        Vector3 esc = muro.transform.localScale;
        if (esc.x <= anchoVano + 1f)
        {
            muro.SetActive(false);
            return;
        }

        float ladoAncho = (esc.x - anchoVano) * 0.5f;
        float centroOffset = anchoVano * 0.5f + ladoAncho * 0.5f;

        Material matMuro = matEstructura;
        MeshRenderer mrOrig = muro.GetComponent<MeshRenderer>();
        if (mrOrig != null && mrOrig.sharedMaterial != null) matMuro = mrOrig.sharedMaterial;

        CrearBloque(muro.name + "_Izq", new Vector3(pos.x - centroOffset, pos.y, pos.z),
                    new Vector3(ladoAncho, esc.y, esc.z), matMuro, matCian, true);
        CrearBloque(muro.name + "_Der", new Vector3(pos.x + centroOffset, pos.y, pos.z),
                    new Vector3(ladoAncho, esc.y, esc.z), matMuro, matMagenta, true);

        // Dintel superior sobre el vano (deja paso de 4.8m de alto)
        float pisoY = pos.y - esc.y * 0.5f;
        float techoY = pos.y + esc.y * 0.5f;
        float altoVano = 4.9f;
        float altoDintel = techoY - (pisoY + altoVano);
        if (altoDintel > 0.25f)
        {
            CrearBloque(muro.name + "_Dintel", new Vector3(pos.x, techoY - altoDintel * 0.5f, pos.z),
                        new Vector3(anchoVano + 0.4f, altoDintel, esc.z), matMuro, matCian, true);
        }

        muro.SetActive(false);
    }

    private static void DespejarVano(Vector3 centro, Vector3 mitadExtents, string conservarPiso)
    {
        Collider[] solapados = Physics.OverlapBox(centro, mitadExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < solapados.Length; i++)
        {
            Collider c = solapados[i];
            if (c == null) continue;
            if (c.GetComponentInParent<PlayerController>() != null) continue;
            if (c.GetComponentInParent<DoorController>() != null) continue;
            if (c.GetComponentInParent<ArquitecturaNivel>() != null) continue;
            if (c.name == conservarPiso || c.name.Contains("Floor") || c.name.Contains("Plaza")) continue;

            c.enabled = false;
        }
    }

    private void CrearPuertaEntradaInteractiva(string nombre, Vector3 centroBase, string prefijoPuerta)
    {
        // Pilares marco laterales
        CrearBloque(nombre + "_JambaL", centroBase + new Vector3(-3.8f, 2.5f, 0f), new Vector3(0.6f, 5.0f, 0.8f), matEstructura, matCian, true);
        CrearBloque(nombre + "_JambaR", centroBase + new Vector3(3.8f, 2.5f, 0f), new Vector3(0.6f, 5.0f, 0.8f), matEstructura, matCian, true);

        GameObject clon = ClonarPrefab(prefijoPuerta, centroBase + new Vector3(0f, 0.05f, 0f), Quaternion.identity, new Vector3(7.0f, 4.7f, 0.45f), true, false);
        if (clon == null)
        {
            clon = CrearBloque(nombre, centroBase + new Vector3(0f, 2.4f, 0f), new Vector3(7.0f, 4.7f, 0.4f), matPuerta, matCian, true);
        }
        else
        {
            clon.name = nombre;
            AjustarBoxColliderEnWorld(clon, centroBase + new Vector3(0f, 2.4f, 0f), new Vector3(7.0f, 4.7f, 0.7f));
        }

        DoorController dc = clon.AddComponent<DoorController>();
        dc.direccionApertura = Vector3.up;
        dc.distancia = 5.1f;
        dc.velocidad = 5.5f;
        dc.abrirConE = true;
        dc.textoPrompt = "Presione E para abrir la puerta";
    }

    private void ConstruirSalaCerrada(string nombre, Vector2 rangoX, float zMin, float zMax, float yPisoSup, float alto, bool cerrarFrente, bool cerrarAtras)
    {
        float ancho = rangoX.y - rangoX.x;
        float centroX = (rangoX.x + rangoX.y) * 0.5f;
        float largo = zMax - zMin;
        float centroZ = (zMin + zMax) * 0.5f;
        float yBasePiso = yPisoSup - 0.25f;

        CrearPiso(nombre + "_Piso", new Vector3(centroX, yBasePiso, centroZ), new Vector3(ancho, 0.5f, largo));
        CrearBloque(nombre + "_Techo", new Vector3(centroX, yBasePiso + alto, centroZ), new Vector3(ancho + 0.4f, 0.5f, largo), matEstructura, matCian, true);
        CrearBloque(nombre + "_WallL", new Vector3(rangoX.x - 0.3f, yBasePiso + alto * 0.5f, centroZ), new Vector3(0.6f, alto, largo), matEstructura, matCian, true);
        CrearBloque(nombre + "_WallR", new Vector3(rangoX.y + 0.3f, yBasePiso + alto * 0.5f, centroZ), new Vector3(0.6f, alto, largo), matEstructura, matMagenta, true);

        if (cerrarAtras)
            CrearBloque(nombre + "_WallBack", new Vector3(centroX, yBasePiso + alto * 0.5f, zMin - 0.3f), new Vector3(ancho + 1.2f, alto, 0.6f), matEstructura, matCian, true);

        if (cerrarFrente)
            CrearBloque(nombre + "_WallFront", new Vector3(centroX, yBasePiso + alto * 0.5f, zMax + 0.3f), new Vector3(ancho + 1.2f, alto, 0.6f), matEstructura, matMagenta, true);
    }

    private void ConstruirSalaIntermedia(string prefijo, float zMin, float zMax, float yBasePiso, float zPuerta,
                                         string nombreAcceso, int cantidadModulos, int numeroCP, float zCheckpoint)
    {
        float ySup = yBasePiso + 0.25f;
        ConstruirSalaCerrada(prefijo, new Vector2(-9.0f, 9.0f), zMin, zMax, ySup, 7.6f, false, false);

        ColocarProp("Column_01_Big", new Vector3(-7.6f, ySup, zMin + 3f), Quaternion.identity, new Vector3(1.1f, 6.8f, 1.1f), true);
        ColocarProp("Column_01_Big", new Vector3(7.6f, ySup, zMin + 3f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, 6.8f, 1.1f), true);
        ColocarProp("Wall_Console_01_Half", new Vector3(7.2f, ySup, zPuerta - 2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.1f, 2.2f, 2.0f), true);
        ColocarProp("Crate_01", new Vector3(-7.2f, ySup, zMax - 2.5f), Quaternion.identity, new Vector3(1.3f, 1.3f, 1.3f), true);
        ColocarClaraboyas(new Vector3(0f, yBasePiso + 7.35f, (zMin + zMax) * 0.5f), 2, 2, 7.5f);
        CrearLuzPuntual(new Vector3(0f, ySup + 5.2f, (zMin + zMax) * 0.5f), new Color(0.25f, 0.85f, 1f), 9f, 24f);

        DoorController dc = CrearMuroConPuertaHack(prefijo + "_Puerta", new Vector3(0f, ySup, zPuerta), 18.0f, 7.2f, 6.0f, 4.6f, false);
        CrearTerminalHack(prefijo + "_Tableta", new Vector3(-3.8f, ySup, zPuerta - 2.0f), cantidadModulos, nombreAcceso, dc);
        CrearCheckpointExtra(prefijo + "_CP_" + numeroCP, new Vector3(0f, ySup + 0.1f, zCheckpoint), numeroCP);
    }

    private DoorController CrearMuroConPuertaHack(string nombre, Vector3 baseCentro, float anchoTotal, float altoTotal, float anchoVano, float altoVano, bool esEntradaInteractiva)
    {
        float ladoAncho = (anchoTotal - anchoVano) * 0.5f;
        float offsetX = anchoVano * 0.5f + ladoAncho * 0.5f;
        float centroY = baseCentro.y + altoTotal * 0.5f;

        CrearBloque(nombre + "_AlaL", new Vector3(baseCentro.x - offsetX, centroY, baseCentro.z), new Vector3(ladoAncho, altoTotal, 0.65f), matEstructura, matCian, true);
        CrearBloque(nombre + "_AlaR", new Vector3(baseCentro.x + offsetX, centroY, baseCentro.z), new Vector3(ladoAncho, altoTotal, 0.65f), matEstructura, matMagenta, true);

        float altoDintel = altoTotal - altoVano;
        if (altoDintel > 0.25f)
            CrearBloque(nombre + "_Dintel", new Vector3(baseCentro.x, baseCentro.y + altoVano + altoDintel * 0.5f, baseCentro.z), new Vector3(anchoVano + 0.4f, altoDintel, 0.7f), matEstructura, matCian, true);

        GameObject hoja = ClonarPrefab("Grid_01", new Vector3(baseCentro.x, baseCentro.y + 0.05f, baseCentro.z), Quaternion.identity, new Vector3(anchoVano, altoVano, 0.45f), true, false);
        if (hoja == null)
        {
            hoja = CrearBloque(nombre + "_Hoja", new Vector3(baseCentro.x, baseCentro.y + altoVano * 0.5f, baseCentro.z), new Vector3(anchoVano, altoVano, 0.45f), matPuerta, matMagenta, true);
        }
        else
        {
            hoja.name = nombre + "_Hoja";
            AjustarBoxColliderEnWorld(hoja, new Vector3(baseCentro.x, baseCentro.y + altoVano * 0.5f, baseCentro.z), new Vector3(anchoVano, altoVano, 0.65f));
        }

        DoorController dc = hoja.AddComponent<DoorController>();
        dc.direccionApertura = Vector3.up;
        dc.distancia = altoVano + 0.5f;
        dc.velocidad = 7.0f;
        dc.abrirConE = esEntradaInteractiva;
        if (esEntradaInteractiva) dc.textoPrompt = "Presione E para abrir la puerta";
        return dc;
    }

    private void CrearTerminalHack(string nombre, Vector3 basePos, int cantidadModulos, string acceso, DoorController puerta)
    {
        CrearBloque(nombre + "_Base", basePos + new Vector3(0f, 0.55f, 0f), new Vector3(0.85f, 1.1f, 0.65f), matEstructura, matCian, true);
        GameObject panel = CrearCubo(nombre, basePos + new Vector3(0f, 1.25f, -0.08f), new Vector3(0.65f, 0.42f, 0.2f), matCian);

        HackTerminal ht = panel.AddComponent<HackTerminal>();
        HackableType[] mods = new HackableType[Mathf.Max(1, cantidadModulos)];
        for (int i = 0; i < mods.Length; i++) mods[i] = (HackableType)(i % 6);
        ht.modulos = mods;
        ht.nombreAcceso = acceso;
        ht.esTutorial = false;
        ht.ocultarTextoAlResolver = true;
        ht.textoPrompt = "Presione E para hackear";
        ht.puertas = new[] { puerta };
    }

    private void ConstruirCañonSciFi(string prefijo, float zMin, float zMax, float semiAncho, float techoY, int segmentos)
    {
        float largo = zMax - zMin;
        float centroZ = (zMin + zMax) * 0.5f;

        // Muros altos laterales delimitando el cañón espacial + torres de soporte
        CrearBloque(prefijo + "_MuroL", new Vector3(-semiAncho, techoY * 0.45f, centroZ), new Vector3(1.2f, techoY + 10f, largo), matEstructura, matCian, true);
        CrearBloque(prefijo + "_MuroR", new Vector3(semiAncho, techoY * 0.45f, centroZ), new Vector3(1.2f, techoY + 10f, largo), matEstructura, matMagenta, true);

        for (int i = 0; i < segmentos; i++)
        {
            float z = Mathf.Lerp(zMin + 6f, zMax - 6f, segmentos == 1 ? 0.5f : (float)i / (segmentos - 1));
            CrearBloque(prefijo + "_Viga_" + i, new Vector3(0f, techoY + 0.6f, z), new Vector3(semiAncho * 2f + 1.2f, 0.85f, 1.4f), matEstructura, i % 2 == 0 ? matCian : matMagenta, true);
            ColocarProp("Column_01_Big", new Vector3(-semiAncho + 1.1f, -2f, z), Quaternion.identity, new Vector3(1.3f, techoY + 2f, 1.3f), true);
            ColocarProp("Column_01_Big", new Vector3(semiAncho - 1.1f, -2f, z), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.3f, techoY + 2f, 1.3f), true);
            ColocarProp("Roof_Glass_01_6x6", new Vector3(0f, techoY + 1.2f, z), Quaternion.identity, new Vector3(14f, 0.8f, 6.5f), true, false);
        }

        CrearTorresExteriores(new Vector3(0f, -6f, centroZ), semiAncho + 8f, largo / Mathf.Max(1, segmentos), 26f, segmentos);
    }

    private void CrearNodoGancho(string nombre, Vector3 pos, float techoY)
    {
        float altoViga = Mathf.Max(0.8f, techoY - pos.y);
        CrearBloque(nombre + "_Soporte", new Vector3(pos.x, pos.y + altoViga * 0.5f + 0.45f, pos.z),
                    new Vector3(0.45f, altoViga, 0.45f), matEstructura, matCian, false);

        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, true);
        go.transform.position = pos;

        GameObject nucleo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nucleo.name = nombre + "_Nucleo";
        nucleo.transform.SetParent(go.transform, false);
        nucleo.transform.localScale = Vector3.one * 0.85f;
        Destroy(nucleo.GetComponent<Collider>());
        nucleo.GetComponent<MeshRenderer>().sharedMaterial = matCian;

        GameObject aro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        aro.name = nombre + "_Aro";
        aro.transform.SetParent(go.transform, false);
        aro.transform.localScale = new Vector3(1.5f, 0.25f, 1.5f);
        Destroy(aro.GetComponent<Collider>());
        aro.GetComponent<MeshRenderer>().sharedMaterial = matMagenta;

        AnclajeGancho anclaje = go.AddComponent<AnclajeGancho>();
        anclaje.ConfigurarVisual(aro.transform, null, new Color(0.15f, 0.9f, 1f));
    }

    private void CrearIslaRescate(string nombre, Vector3 centroSup)
    {
        CrearPiso(nombre, centroSup - new Vector3(0f, 0.25f, 0f), new Vector3(5.6f, 0.5f, 5.6f));
        CrearCubo(nombre + "_Borde", centroSup - new Vector3(0f, 0.04f, 0f), new Vector3(5.7f, 0.12f, 5.7f), matCian);
        ColocarProp("Stairway_Plateform_01", centroSup - new Vector3(0f, 0.5f, 0f), Quaternion.identity, new Vector3(4.8f, 0f, 4.8f), true, false);
    }

    private void DecorarFlancosSciFi(string prefijo, float[] xs, float ySup, float[] zs, bool conConsola)
    {
        for (int xi = 0; xi < xs.Length; xi++)
        {
            float x = xs[xi];
            Quaternion rotHaciaCentro = x < 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            for (int zi = 0; zi < zs.Length; zi++)
            {
                Vector3 pos = new Vector3(x, ySup, zs[zi]);
                if (zi % 2 == 0)
                {
                    if (ColocarProp("Column_01_Big", pos, rotHaciaCentro, new Vector3(1.1f, 6.2f, 1.1f), true) == null)
                        ColocarProp("Pipes_01", pos, rotHaciaCentro, new Vector3(1.1f, 5.5f, 1.1f), true);
                }
                else if (conConsola)
                {
                    ColocarProp("Wall_Console_01_Large", pos, rotHaciaCentro, new Vector3(1.0f, 2.4f, 3.0f), true);
                }
                else
                {
                    ColocarProp("Crate_01", pos, Quaternion.Euler(0f, zi * 25f, 0f), new Vector3(1.25f, 1.25f, 1.25f), true);
                }
            }
        }
    }

    private void ColocarClaraboyas(Vector3 centro, int filasX, int colsZ, float paso)
    {
        for (int ix = 0; ix < filasX; ix++)
        {
            float ox = (ix - (filasX - 1) * 0.5f) * paso;
            for (int iz = 0; iz < colsZ; iz++)
            {
                float oz = (iz - (colsZ - 1) * 0.5f) * paso;
                ColocarProp("Roof_Glass_01_6x6", centro + new Vector3(ox, 0f, oz), Quaternion.identity, new Vector3(5.8f, 0f, 5.8f), true, false);
            }
        }
    }

    private GameObject ColocarProp(string prefijo, Vector3 destinoBaseCentro, Quaternion rotacion, Vector3 tamanoObjetivo, bool quitarLuces, bool conColision = true)
    {
        return ClonarPrefab(prefijo, destinoBaseCentro, rotacion, tamanoObjetivo, quitarLuces, conColision);
    }

    private GameObject ClonarPrefab(string prefijo, Vector3 destinoBaseCentro, Quaternion rotacion, Vector3 tamanoObjetivo, bool quitarLuces, bool conColision)
    {
        if (!plantillas.TryGetValue(prefijo, out GameObject origen) || origen == null) return null;

        GameObject clon = Instantiate(origen, transform);
        clon.name = "Arq_" + prefijo;
        clon.SetActive(true);
        clon.transform.rotation = rotacion;
        clon.transform.localScale = Vector3.one;

        SanitizarClon(clon, quitarLuces);

        Bounds b = CalcularLimites(clon);
        if (b.size.sqrMagnitude < 0.0001f)
        {
            Destroy(clon);
            return null;
        }

        if (tamanoObjetivo != Vector3.zero)
        {
            Vector3 escala = clon.transform.localScale;
            if (tamanoObjetivo.x > 0f && tamanoObjetivo.y <= 0f && tamanoObjetivo.z <= 0f)
            {
                float factor = tamanoObjetivo.x / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
                escala *= factor;
            }
            else
            {
                if (tamanoObjetivo.x > 0f) escala.x *= tamanoObjetivo.x / Mathf.Max(0.01f, b.size.x);
                if (tamanoObjetivo.y > 0f) escala.y *= tamanoObjetivo.y / Mathf.Max(0.01f, b.size.y);
                if (tamanoObjetivo.z > 0f) escala.z *= tamanoObjetivo.z / Mathf.Max(0.01f, b.size.z);
            }
            clon.transform.localScale = escala;
            b = CalcularLimites(clon);
        }

        clon.transform.position += destinoBaseCentro - new Vector3(b.center.x, b.min.y, b.center.z);

        if (!conColision)
        {
            Collider[] cols = clon.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) Destroy(cols[i]);
        }
        else if (clon.GetComponentInChildren<Collider>(true) == null)
        {
            MeshFilter[] mfs = clon.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < mfs.Length; i++)
            {
                if (mfs[i].sharedMesh == null) continue;
                MeshCollider mc = mfs[i].gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mfs[i].sharedMesh;
            }
        }

        return clon;
    }

    private static void SanitizarClon(GameObject clon, bool quitarLuces)
    {
        MonoBehaviour[] scripts = clon.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null) Destroy(scripts[i]);
        }

        AudioSource[] audios = clon.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audios.Length; i++) Destroy(audios[i]);

        if (quitarLuces)
        {
            Light[] luces = clon.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < luces.Length; i++) Destroy(luces[i]);
        }
    }

    private static Bounds CalcularLimites(GameObject go)
    {
        MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static void AjustarBoxColliderEnWorld(GameObject go, Vector3 centroWorld, Vector3 tamanoWorld)
    {
        Collider[] prevs = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < prevs.Length; i++) Destroy(prevs[i]);

        GameObject colGO = new GameObject("DoorHitbox");
        colGO.transform.SetParent(go.transform, true);
        colGO.transform.position = centroWorld;
        colGO.transform.rotation = Quaternion.identity;
        Vector3 pLossy = go.transform.lossyScale;
        colGO.transform.localScale = new Vector3(
            1f / Mathf.Max(0.01f, pLossy.x),
            1f / Mathf.Max(0.01f, pLossy.y),
            1f / Mathf.Max(0.01f, pLossy.z));
        BoxCollider bc = colGO.AddComponent<BoxCollider>();
        bc.size = tamanoWorld;
    }

    private static void PosicionarJugador(Vector3 pos, float yaw)
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) return;
        pc.FijarOrientacion(pos, yaw);
    }

    private void CrearLuzDireccionalNivel4()
    {
        Light[] luces = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < luces.Length; i++)
            if (luces[i].type == LightType.Directional) return;

        GameObject dirGO = new GameObject("N4_DirectionalLight");
        dirGO.transform.SetParent(transform, true);
        dirGO.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light l = dirGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.82f, 0.90f, 1f);
        l.intensity = 1.1f;
    }

    private void CrearLuzPuntual(Vector3 pos, Color color, float intensidad, float rango)
    {
        GameObject go = new GameObject("Arq_Luz");
        go.transform.SetParent(transform, true);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = rango;
    }

    private GameObject CrearPiso(string nombre, Vector3 pos, Vector3 escala)
    {
        return CrearCubo(nombre, pos, escala, matPiso);
    }

    private GameObject CrearCubo(string nombre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(transform, true);
        go.transform.position = pos;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private GameObject CrearBloque(string nombre, Vector3 pos, Vector3 escala, Material matBase, Material matBorde, bool conColision)
    {
        GameObject go = CrearCubo(nombre, pos, escala, matBase);
        if (!conColision) Destroy(go.GetComponent<Collider>());

        GameObject ribete = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ribete.name = nombre + "_Borde";
        ribete.transform.SetParent(go.transform, false);
        ribete.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        ribete.transform.localScale = new Vector3(1.01f, 0.08f, 1.02f);
        ribete.GetComponent<MeshRenderer>().sharedMaterial = matBorde;
        Destroy(ribete.GetComponent<Collider>());
        return go;
    }

    private void CrearTorresExteriores(Vector3 origenCentro, float separacionX, float pasoZ, float altura, int pares)
    {
        for (int i = 0; i < pares; i++)
        {
            float z = origenCentro.z + i * pasoZ;
            Material neon = i % 2 == 1 ? matMagenta : matCian;
            CrearBloque("Arq_TorreL_" + i, new Vector3(origenCentro.x - separacionX, origenCentro.y, z),
                        new Vector3(3.4f, altura, 3.4f), matEstructura, neon, true);
            CrearBloque("Arq_TorreR_" + i, new Vector3(origenCentro.x + separacionX, origenCentro.y, z),
                        new Vector3(3.4f, altura, 3.4f), matEstructura, neon, true);
        }
    }

    private void CrearBarraGiratoria(string nombre, Vector3 pos, float largo, float velocidadZ)
    {
        GameObject go = CrearCubo(nombre, pos, new Vector3(largo, 0.36f, 0.36f), matPeligro);
        BoxCollider bc = go.GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = true;
        RotatingHazard rh = go.AddComponent<RotatingHazard>();
        rh.rotationSpeed = new Vector3(0f, 0f, velocidadZ);
    }

    private void CrearCheckpointExtra(string nombre, Vector3 pos, int numero)
    {
        GameObject faro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        faro.name = nombre + "_Beacon";
        faro.transform.SetParent(transform, true);
        faro.transform.position = pos + Vector3.up * 1.2f;
        faro.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
        faro.GetComponent<MeshRenderer>().sharedMaterial = matCian;
        Destroy(faro.GetComponent<Collider>());

        GameObject trig = new GameObject(nombre);
        trig.transform.SetParent(transform, true);
        trig.transform.position = pos + Vector3.up * 1.1f;
        BoxCollider bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(5.0f, 3.0f, 2.5f);

        Checkpoint cp = trig.AddComponent<Checkpoint>();
        cp.numero = numero;
        cp.beaconRenderer = faro.GetComponent<MeshRenderer>();
    }
}
