using UnityEngine;

/// <summary>
/// Herramientas de dibujo para los HUD hechos con OnGUI (vida y munición):
/// rectángulos, cajas redondeadas, bordes redondeados, brillos y texto con sombra.
/// Todo se dibuja en coordenadas de una pantalla de 1920 x 1080 y se escala
/// solo al tamaño real con EmpezarEscala() / TerminarEscala().
/// </summary>
public class PincelHUD
{
    private const int TamanoTextura = 32;
    private const int Radio = 10;

    private readonly Texture2D texturaRelleno;
    private readonly Texture2D texturaBorde;
    private readonly Texture2D texturaBrillo;
    private readonly GUIStyle estiloRelleno;
    private readonly GUIStyle estiloBorde;
    private readonly GUIStyle estiloTexto;
    private Matrix4x4 matrizAnterior;

    public PincelHUD()
    {
        texturaRelleno = CrearRedondeada(false);
        texturaBorde = CrearRedondeada(true);
        texturaBrillo = CrearBrilloHorizontal();

        estiloRelleno = new GUIStyle();
        estiloRelleno.normal.background = texturaRelleno;
        estiloRelleno.border = new RectOffset(Radio + 2, Radio + 2, Radio + 2, Radio + 2);

        estiloBorde = new GUIStyle();
        estiloBorde.normal.background = texturaBorde;
        estiloBorde.border = new RectOffset(Radio + 2, Radio + 2, Radio + 2, Radio + 2);

        estiloTexto = new GUIStyle();
        estiloTexto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        estiloTexto.richText = true;
        estiloTexto.clipping = TextClipping.Overflow;
    }

    public void Liberar()
    {
        if (texturaRelleno != null) Object.Destroy(texturaRelleno);
        if (texturaBorde != null) Object.Destroy(texturaBorde);
        if (texturaBrillo != null) Object.Destroy(texturaBrillo);
    }

    /// <summary>Ancho de la pantalla de referencia (cambia si la pantalla no es 16:9).</summary>
    public static float AnchoReferencia
    {
        get { return Screen.width * (1080f / Mathf.Max(1f, Screen.height)); }
    }

    public void EmpezarEscala()
    {
        matrizAnterior = GUI.matrix;
        float escala = Screen.height / 1080f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
    }

    public void TerminarEscala()
    {
        GUI.matrix = matrizAnterior;
    }

    public void Rectangulo(Rect r, Color color)
    {
        Color previo = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = previo;
    }

    public void Caja(Rect r, Color color)
    {
        Dibujar(estiloRelleno, r, color);
    }

    public void Borde(Rect r, Color color)
    {
        Dibujar(estiloBorde, r, color);
    }

    /// <summary>Halo suave alrededor de una caja (varias capas de borde que se van apagando).</summary>
    public void Halo(Rect r, Color color, int capas)
    {
        for (int i = capas; i >= 1; i--)
        {
            float crecer = i * 3f;
            Rect grande = new Rect(r.x - crecer, r.y - crecer, r.width + crecer * 2f, r.height + crecer * 2f);
            Color capa = color;
            capa.a = color.a * (1f - (float)i / (capas + 1));
            Borde(grande, capa);
        }
    }

    /// <summary>Franja de luz que se desvanece a los lados (para el destello que recorre la barra).</summary>
    public void Brillo(Rect r, Color color)
    {
        Color previo = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, texturaBrillo);
        GUI.color = previo;
    }

    public void Texto(Rect r, string texto, int tamano, Color color, TextAnchor alineacion, bool negrita)
    {
        estiloTexto.fontSize = tamano;
        estiloTexto.alignment = alineacion;
        if (negrita) estiloTexto.fontStyle = FontStyle.Bold;
        else estiloTexto.fontStyle = FontStyle.Normal;

        // Sombra para que se lea sobre cualquier fondo
        estiloTexto.normal.textColor = new Color(0f, 0f, 0f, 0.75f * color.a);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), texto, estiloTexto);
        estiloTexto.normal.textColor = color;
        GUI.Label(r, texto, estiloTexto);
    }

    public float AnchoTexto(string texto, int tamano, bool negrita)
    {
        estiloTexto.fontSize = tamano;
        if (negrita) estiloTexto.fontStyle = FontStyle.Bold;
        else estiloTexto.fontStyle = FontStyle.Normal;
        return estiloTexto.CalcSize(new GUIContent(texto)).x;
    }

    private void Dibujar(GUIStyle estilo, Rect r, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        Color previo = GUI.color;
        GUI.color = color;
        estilo.Draw(r, GUIContent.none, false, false, false, false);
        GUI.color = previo;
    }

    // ── Texturas ────────────────────────────────────────────────────

    private static Texture2D CrearRedondeada(bool soloBorde)
    {
        Texture2D textura = new Texture2D(TamanoTextura, TamanoTextura, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;
        float grosor = 2f;

        for (int y = 0; y < TamanoTextura; y++)
        {
            for (int x = 0; x < TamanoTextura; x++)
            {
                float distancia = DistanciaAlBorde(x + 0.5f, y + 0.5f);
                float alfa = Mathf.Clamp01(distancia + 0.5f);
                if (soloBorde)
                {
                    float interior = Mathf.Clamp01(distancia - grosor + 0.5f);
                    alfa = alfa - interior;
                }
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, alfa));
            }
        }
        textura.Apply();
        return textura;
    }

    /// <summary>Distancia (positiva hacia adentro) desde el borde de un rectángulo con esquinas redondeadas.</summary>
    private static float DistanciaAlBorde(float x, float y)
    {
        float lado = TamanoTextura;
        float cx = Mathf.Clamp(x, Radio, lado - Radio);
        float cy = Mathf.Clamp(y, Radio, lado - Radio);
        float dx = x - cx;
        float dy = y - cy;
        float fuera = Mathf.Sqrt(dx * dx + dy * dy);
        if (fuera > 0f) return Radio - fuera;

        float alBorde = Mathf.Min(Mathf.Min(x, lado - x), Mathf.Min(y, lado - y));
        return alBorde;
    }

    private static Texture2D CrearBrilloHorizontal()
    {
        Texture2D textura = new Texture2D(32, 1, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < 32; x++)
        {
            float t = x / 31f;
            float alfa = Mathf.Sin(t * Mathf.PI);
            textura.SetPixel(x, 0, new Color(1f, 1f, 1f, alfa * alfa));
        }
        textura.Apply();
        return textura;
    }
}
