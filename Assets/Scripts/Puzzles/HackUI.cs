using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Utilidades para construir por código la interfaz de hackeo
/// (paneles, textos, botones y bordes). No guarda estado del hackeo.
/// </summary>
public static class HackUI
{
    private static readonly Color ColBotonPorDefecto = new Color(0.10f, 0.13f, 0.17f);
    public static readonly Color ColAcento = new Color(0.15f, 0.85f, 1f);
    public static readonly Color ColVerde = new Color(0.15f, 1f, 0.4f);
    public static readonly Color ColRojo = new Color(1f, 0.25f, 0.25f);

    private static Font _fuente;

    public static Font Fuente
    {
        get
        {
            if (_fuente == null)
                _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _fuente;
        }
    }

    public static GameObject NuevoUI(Transform parent, string nombre, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    public static void Estirar(GameObject go)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void Contorno(Transform parent, Color color)
    {
        float w = 920f, h = 640f, t = 3f;
        var sup = NuevoUI(parent, "BordeSup", new Vector2(0f, h / 2f), new Vector2(w, t));
        sup.AddComponent<Image>().color = color;
        var inf = NuevoUI(parent, "BordeInf", new Vector2(0f, -h / 2f), new Vector2(w, t));
        inf.AddComponent<Image>().color = color;
        var izq = NuevoUI(parent, "BordeIzq", new Vector2(-w / 2f, 0f), new Vector2(t, h));
        izq.AddComponent<Image>().color = color;
        var der = NuevoUI(parent, "BordeDer", new Vector2(w / 2f, 0f), new Vector2(t, h));
        der.AddComponent<Image>().color = color;
    }

    public static Text CrearTexto(Transform parent, string nombre, string contenido, int size,
                                  TextAnchor align, Vector2 pos, Vector2 dimension, Color color)
    {
        var go = NuevoUI(parent, nombre, pos, dimension);
        var txt = go.AddComponent<Text>();
        txt.text = contenido;
        txt.font = Fuente;
        txt.fontSize = size;
        txt.alignment = align;
        txt.color = color;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        return txt;
    }

    public static Button CrearBoton(Transform parent, string nombre, string etiqueta,
                                    Vector2 pos, Vector2 size, Color? colorBase = null)
    {
        Color baseColor;
        if (colorBase.HasValue)
            baseColor = colorBase.Value;
        else
            baseColor = ColBotonPorDefecto;

        var go = NuevoUI(parent, nombre, pos, size);
        var img = go.AddComponent<Image>();
        img.color = baseColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;

        // El tint REEMPLAZA el color del gráfico: por eso los colores del tint
        // deben ser el color del propio botón (así se ven los cables/patrones de color)
        var c = btn.colors;
        c.normalColor = baseColor;
        c.highlightedColor = Color.Lerp(baseColor, Color.white, 0.22f);
        c.pressedColor = Color.Lerp(baseColor, Color.white, 0.45f);
        c.selectedColor = baseColor;
        c.disabledColor = Color.Lerp(baseColor, Color.black, 0.4f);
        c.fadeDuration = 0.05f;
        c.colorMultiplier = 1f;
        btn.colors = c;
        btn.image.color = baseColor;

        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        if (!string.IsNullOrEmpty(etiqueta))
        {
            var lbl = CrearTexto(btn.transform, "Texto", etiqueta, 22, TextAnchor.MiddleCenter,
                                 Vector2.zero, size, Color.white);
            lbl.fontStyle = FontStyle.Bold;
        }

        return btn;
    }

    public static void AsegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}