using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Game.Scanner.EditorTools
{
    /// <summary>
    /// Herramienta de editor: arma la Pantalla del Sistema completa en la escena abierta.
    /// Menú: SEEN > Pantalla del Sistema > Crear en la escena actual.
    ///
    /// Qué hace:
    /// 1. Crea la capa "ScannerUI" si no existe.
    /// 2. Crea (o reutiliza) los assets ScannerConfig, ScannerEvents y EncryptedTextEffect
    ///    en Assets/Settings/Scanner.
    /// 3. Crea el objeto PantallaSistema con el controlador, la entrada, el detector y el audio.
    /// 4. Crea el Canvas World Space con fondo, halo, borde, líneas, ruido y textos.
    /// 5. Crea una cámara Overlay (URP) que solo dibuja la capa ScannerUI, la apila
    ///    sobre la cámara del jugador y quita esa capa de la cámara del jugador.
    /// 6. Conecta todas las referencias del Inspector.
    ///
    /// Es código de editor (no va en el build). Los métodos son static porque así
    /// lo exige el atributo MenuItem de Unity.
    /// </summary>
    public class ScannerSetupTool
    {
        private const string NombreCapa = "ScannerUI";
        private const string CarpetaPadre = "Assets/Settings";
        private const string CarpetaDatos = "Assets/Settings/Scanner";
        private const float AnchoCanvas = 1000f;
        private const float AltoCanvas = 620f;
        private const float EscalaCanvas = 0.001f;   // 1000 unidades = 1 metro
        private const float GrosorBorde = 3f;
        private const float GrosorBrillo = 14f;

        [MenuItem("SEEN/Pantalla del Sistema/Crear en la escena actual")]
        public static void Crear()
        {
            if (Object.FindFirstObjectByType<ScannerController>() != null)
            {
                EditorUtility.DisplayDialog("Pantalla del Sistema",
                    "Esta escena ya tiene una Pantalla del Sistema (un ScannerController). Bórrala primero si quieres crearla de nuevo.", "OK");
                return;
            }

            Camera camara = ObtenerCamara();
            if (camara == null)
            {
                EditorUtility.DisplayDialog("Pantalla del Sistema",
                    "No encontré la cámara del jugador. Selecciona la Main Camera (o el jugador) en el Hierarchy y vuelve a intentarlo.", "OK");
                return;
            }

            int capa = AsegurarCapa(NombreCapa);
            if (capa < 0)
            {
                EditorUtility.DisplayDialog("Pantalla del Sistema",
                    "No hay capas libres para crear \"ScannerUI\". Libera una en Project Settings > Tags and Layers.", "OK");
                return;
            }

            // ── Assets compartidos ──────────────────────────────────────
            bool configEsNueva = AssetDatabase.LoadAssetAtPath<ScannerConfig>(CarpetaDatos + "/ScannerConfig.asset") == null;
            ScannerConfig config = CargarOCrear<ScannerConfig>("ScannerConfig");
            ScannerEvents eventos = CargarOCrear<ScannerEvents>("ScannerEvents");
            EncryptedTextEffect efecto = CargarOCrear<EncryptedTextEffect>("EncryptedTextEffect");

            if (configEsNueva)
            {
                // Ve todo lo que ve un raycast normal, menos la propia pantalla.
                config.capasDeteccion = Physics.DefaultRaycastLayers & ~(1 << capa);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            // ── Objeto raíz con la lógica ───────────────────────────────
            GameObject raiz = new GameObject("PantallaSistema");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear Pantalla del Sistema");

            NewInputSystemScannerInput entrada = raiz.AddComponent<NewInputSystemScannerInput>();
            RaycastEnemyDetector detector = raiz.AddComponent<RaycastEnemyDetector>();
            ScannerController controlador = raiz.AddComponent<ScannerController>();
            AudioSource fuente = raiz.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            ScannerAudioFeedback audio = raiz.AddComponent<ScannerAudioFeedback>();

            // ── Canvas World Space ──────────────────────────────────────
            GameObject goCanvas = new GameObject("ScannerCanvas", typeof(RectTransform));
            goCanvas.transform.SetParent(raiz.transform, false);

            Canvas canvas = goCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rtCanvas = (RectTransform)goCanvas.transform;
            rtCanvas.sizeDelta = new Vector2(AnchoCanvas, AltoCanvas);
            rtCanvas.localScale = Vector3.one * EscalaCanvas;
            rtCanvas.position = camara.transform.position + camara.transform.forward * config.distancia;
            rtCanvas.rotation = camara.transform.rotation;

            CanvasGroup grupo = goCanvas.AddComponent<CanvasGroup>();
            CameraFollow seguir = goCanvas.AddComponent<CameraFollow>();
            ScannerUIView vista = goCanvas.AddComponent<ScannerUIView>();

            // Orden de dibujo: lo primero queda atrás.
            Image fondo = CrearImagen(goCanvas.transform, "Fondo");
            Estirar((RectTransform)fondo.transform, 0f);

            RawImage scanlines = CrearRawImage(goCanvas.transform, "Scanlines");
            RawImage ruido = CrearRawImage(goCanvas.transform, "Ruido");

            GameObject goBrillo = Hijo(goCanvas.transform, "Halo");
            Estirar((RectTransform)goBrillo.transform, 0f);
            Image[] brillos = CrearMarco(goBrillo.transform, GrosorBrillo);

            GameObject goBorde = Hijo(goCanvas.transform, "Borde");
            Estirar((RectTransform)goBorde.transform, 0f);
            Image[] bordes = CrearMarco(goBorde.transform, GrosorBorde);

            GameObject goContenido = Hijo(goCanvas.transform, "Contenido");
            Estirar((RectTransform)goContenido.transform, 40f);
            VerticalLayoutGroup columna = goContenido.AddComponent<VerticalLayoutGroup>();
            columna.spacing = 12f;
            columna.childAlignment = TextAnchor.UpperLeft;
            columna.childControlWidth = true;
            columna.childControlHeight = true;
            columna.childForceExpandWidth = true;
            columna.childForceExpandHeight = false;

            TextMeshProUGUI textoEstado = CrearTexto(goContenido.transform, "Estado", 34f, true);
            TextMeshProUGUI textoNombre = CrearTexto(goContenido.transform, "Nombre", 56f, true);
            TextMeshProUGUI textoTipo = CrearTexto(goContenido.transform, "Tipo", 34f, false);
            TextMeshProUGUI textoAmenaza = CrearTexto(goContenido.transform, "Amenaza", 34f, true);
            TextMeshProUGUI textoVida = CrearTexto(goContenido.transform, "Vida", 34f, false);
            TextMeshProUGUI textoDescripcion = CrearTexto(goContenido.transform, "Descripcion", 28f, false);
            textoDescripcion.overflowMode = TextOverflowModes.Ellipsis;

            PonerCapa(goCanvas, capa);

            // ── Cámara Overlay para la UI ───────────────────────────────
            Camera camaraUI = CrearCamaraOverlay(camara, capa);
            canvas.worldCamera = camaraUI;

            // ── Referencias ─────────────────────────────────────────────
            GameObject jugador = GameObject.FindWithTag("Player");
            Transform raizJugador = null;
            if (jugador != null) raizJugador = jugador.transform;

            SerializedObject soEntrada = new SerializedObject(entrada);
            Asignar(soEntrada, "config", config);
            soEntrada.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soDetector = new SerializedObject(detector);
            Asignar(soDetector, "origen", camara.transform);
            Asignar(soDetector, "raizJugador", raizJugador);
            Asignar(soDetector, "config", config);
            soDetector.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soControlador = new SerializedObject(controlador);
            Asignar(soControlador, "componenteEntrada", entrada);
            Asignar(soControlador, "componenteDetector", detector);
            Asignar(soControlador, "componenteVista", vista);
            Asignar(soControlador, "eventos", eventos);
            Asignar(soControlador, "config", config);
            soControlador.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soAudio = new SerializedObject(audio);
            Asignar(soAudio, "eventos", eventos);
            soAudio.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soSeguir = new SerializedObject(seguir);
            Asignar(soSeguir, "camara", camara.transform);
            Asignar(soSeguir, "config", config);
            soSeguir.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soVista = new SerializedObject(vista);
            Asignar(soVista, "config", config);
            Asignar(soVista, "efectoTexto", efecto);
            Asignar(soVista, "canvas", canvas);
            Asignar(soVista, "grupo", grupo);
            Asignar(soVista, "fondo", fondo);
            AsignarLista(soVista, "bordes", bordes);
            AsignarLista(soVista, "brillos", brillos);
            Asignar(soVista, "scanlines", scanlines);
            Asignar(soVista, "ruido", ruido);
            Asignar(soVista, "textoEstado", textoEstado);
            Asignar(soVista, "textoNombre", textoNombre);
            Asignar(soVista, "textoTipo", textoTipo);
            Asignar(soVista, "textoAmenaza", textoAmenaza);
            Asignar(soVista, "textoVida", textoVida);
            Asignar(soVista, "textoDescripcion", textoDescripcion);
            soVista.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(raiz.scene);
            Selection.activeGameObject = raiz;

            string aviso = "Pantalla del Sistema creada.\n\nGuarda la escena (Ctrl + S) y pulsa Play. La tecla por defecto es Q (se cambia en Assets/Settings/Scanner/ScannerConfig).\n\nRecuerda agregar EnemyScannable a los enemigos que quieras escanear.";
            if (raizJugador == null)
                aviso += "\n\nOJO: no encontré un objeto con la etiqueta Player. Arrastra el jugador al campo \"Raiz Jugador\" del RaycastEnemyDetector.";
            EditorUtility.DisplayDialog("Pantalla del Sistema", aviso, "OK");
        }

        /// <summary>
        /// Agrega EnemyBaseScanAdapter a todos los enemigos (EnemyBase) de la escena
        /// abierta que todavía no se puedan escanear. Se puede ejecutar varias veces.
        /// </summary>
        [MenuItem("SEEN/Pantalla del Sistema/Hacer escaneables los enemigos de la escena")]
        public static void HacerEnemigosEscaneables()
        {
            EnemyBase[] enemigos = Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int agregados = 0;
            int yaListos = 0;

            for (int i = 0; i < enemigos.Length; i++)
            {
                GameObject go = enemigos[i].gameObject;
                if (go.GetComponent<IEnemyDataProvider>() != null)
                {
                    yaListos++;
                    continue;
                }
                EnemyBaseScanAdapter adaptador = Undo.AddComponent<EnemyBaseScanAdapter>(go);
                SerializedObject so = new SerializedObject(adaptador);
                Asignar(so, "enemigo", enemigos[i]);
                so.ApplyModifiedPropertiesWithoutUndo();
                agregados++;
            }

            if (agregados > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            string mensaje = "Enemigos encontrados: " + enemigos.Length
                           + "\nAhora escaneables: " + agregados
                           + "\nYa lo eran: " + yaListos;
            if (enemigos.Length == 0)
                mensaje += "\n\nEsta escena no tiene enemigos con EnemyBase.";
            else
                mensaje += "\n\nGuarda la escena (Ctrl + S).";
            EditorUtility.DisplayDialog("Pantalla del Sistema", mensaje, "OK");
        }

        // ── Cámara ─────────────────────────────────────────────────────

        private static Camera ObtenerCamara()
        {
            GameObject seleccionado = Selection.activeGameObject;
            if (seleccionado != null)
            {
                Camera enSeleccion = seleccionado.GetComponentInChildren<Camera>();
                if (enSeleccion != null) return enSeleccion;
            }
            return Camera.main;
        }

        private static Camera CrearCamaraOverlay(Camera camaraBase, int capa)
        {
            Transform existente = camaraBase.transform.Find("ScannerUICamera");
            GameObject go;
            if (existente != null)
            {
                go = existente.gameObject;
            }
            else
            {
                go = new GameObject("ScannerUICamera");
                Undo.RegisterCreatedObjectUndo(go, "Crear cámara de la Pantalla del Sistema");
                go.transform.SetParent(camaraBase.transform, false);
            }

            Camera camaraUI = go.GetComponent<Camera>();
            if (camaraUI == null) camaraUI = go.AddComponent<Camera>();
            camaraUI.cullingMask = 1 << capa;
            camaraUI.fieldOfView = camaraBase.fieldOfView;
            camaraUI.nearClipPlane = 0.05f;
            camaraUI.farClipPlane = 10f;

            UniversalAdditionalCameraData datosUI = camaraUI.GetUniversalAdditionalCameraData();
            datosUI.renderType = CameraRenderType.Overlay;

            Undo.RecordObject(camaraBase, "Quitar capa ScannerUI de la cámara del jugador");
            camaraBase.cullingMask = camaraBase.cullingMask & ~(1 << capa);

            UniversalAdditionalCameraData datosBase = camaraBase.GetUniversalAdditionalCameraData();
            if (datosBase.renderType != CameraRenderType.Base)
            {
                Debug.LogWarning("[ScannerSetupTool] La cámara " + camaraBase.name + " no es de tipo Base. La cámara Overlay solo se apila sobre una cámara Base.");
            }
            else if (!datosBase.cameraStack.Contains(camaraUI))
            {
                Undo.RecordObject(datosBase, "Apilar cámara de la Pantalla del Sistema");
                datosBase.cameraStack.Add(camaraUI);
                EditorUtility.SetDirty(datosBase);
            }

            EditorUtility.SetDirty(camaraBase);
            EditorUtility.SetDirty(camaraUI);
            return camaraUI;
        }

        // ── Capas y assets ─────────────────────────────────────────────

        private static int AsegurarCapa(string nombre)
        {
            int existente = LayerMask.NameToLayer(nombre);
            if (existente >= 0) return existente;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return -1;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty capas = tagManager.FindProperty("layers");
            if (capas == null) return -1;

            // Las capas 0 a 7 son de Unity; las de usuario empiezan en la 8.
            for (int i = 8; i < capas.arraySize; i++)
            {
                SerializedProperty capa = capas.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(capa.stringValue))
                {
                    capa.stringValue = nombre;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }
            return -1;
        }

        private static T CargarOCrear<T>(string nombre) where T : ScriptableObject
        {
            string ruta = CarpetaDatos + "/" + nombre + ".asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (asset != null) return asset;

            if (!AssetDatabase.IsValidFolder(CarpetaPadre)) AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder(CarpetaDatos)) AssetDatabase.CreateFolder(CarpetaPadre, "Scanner");

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, ruta);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void Asignar(SerializedObject so, string campo, Object valor)
        {
            SerializedProperty propiedad = so.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogError("[ScannerSetupTool] No existe el campo \"" + campo + "\" en " + so.targetObject.GetType().Name + ". ¿Le cambiaron el nombre?");
                return;
            }
            propiedad.objectReferenceValue = valor;
        }

        private static void AsignarLista(SerializedObject so, string campo, Object[] valores)
        {
            SerializedProperty propiedad = so.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogError("[ScannerSetupTool] No existe el campo \"" + campo + "\" en " + so.targetObject.GetType().Name + ". ¿Le cambiaron el nombre?");
                return;
            }
            propiedad.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
            {
                propiedad.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            }
        }

        // ── Construcción de la UI ──────────────────────────────────────

        private static GameObject Hijo(Transform padre, string nombre)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return go;
        }

        private static void Estirar(RectTransform rt, float margen)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margen, margen);
            rt.offsetMax = new Vector2(-margen, -margen);
        }

        private static Image CrearImagen(Transform padre, string nombre)
        {
            GameObject go = Hijo(padre, nombre);
            Image imagen = go.AddComponent<Image>();
            imagen.raycastTarget = false;
            return imagen;
        }

        private static RawImage CrearRawImage(Transform padre, string nombre)
        {
            GameObject go = Hijo(padre, nombre);
            RawImage imagen = go.AddComponent<RawImage>();
            imagen.raycastTarget = false;
            Estirar((RectTransform)go.transform, 0f);
            return imagen;
        }

        /// <summary>Crea cuatro tiras (arriba, abajo, izquierda, derecha) pegadas al borde.</summary>
        private static Image[] CrearMarco(Transform padre, float grosor)
        {
            Image[] tiras = new Image[4];
            tiras[0] = CrearTira(padre, "Arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, grosor));
            tiras[1] = CrearTira(padre, "Abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, grosor));
            tiras[2] = CrearTira(padre, "Izquierda", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(grosor, 0f));
            tiras[3] = CrearTira(padre, "Derecha", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(grosor, 0f));
            return tiras;
        }

        private static Image CrearTira(Transform padre, string nombre, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 tamano)
        {
            Image tira = CrearImagen(padre, nombre);
            RectTransform rt = (RectTransform)tira.transform;
            rt.anchorMin = anclaMin;
            rt.anchorMax = anclaMax;
            rt.pivot = pivote;
            rt.sizeDelta = tamano;
            rt.anchoredPosition = Vector2.zero;
            return tira;
        }

        private static TextMeshProUGUI CrearTexto(Transform padre, string nombre, float tamano, bool negrita)
        {
            GameObject go = Hijo(padre, nombre);
            TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
            texto.fontSize = tamano;
            texto.alignment = TextAlignmentOptions.TopLeft;
            texto.raycastTarget = false;
            texto.text = "";
            if (negrita) texto.fontStyle = FontStyles.Bold;
            return texto;
        }

        private static void PonerCapa(GameObject go, int capa)
        {
            go.layer = capa;
            foreach (Transform hijo in go.transform)
            {
                PonerCapa(hijo.gameObject, capa);
            }
        }
    }
}
