using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crea cajas de UI con esquinas redondeadas (relleno + borde) para los puzzles de hackeo.
/// Los sprites se generan por código, así no hay que importar imágenes.
///
/// Uso: crear una instancia en Construir() del puzzle y llamar a Liberar() en Detener()
/// para borrar las texturas generadas.
/// </summary>
public class CajasRedondeadasUI
{
    private const int TamanoSprite = 64;
    private const float RadioSprite = 16f;
    private const float GrosorBorde = 3f;

    private Texture2D texturaRelleno;
    private Texture2D texturaBorde;
    private Sprite spriteRelleno;
    private Sprite spriteBorde;

    public CajasRedondeadasUI()
    {
        texturaRelleno = CrearTextura(false);
        texturaBorde = CrearTextura(true);

        float margen = RadioSprite + 2f;
        Vector4 bordes9 = new Vector4(margen, margen, margen, margen);
        Rect area = new Rect(0f, 0f, TamanoSprite, TamanoSprite);
        Vector2 pivote = new Vector2(0.5f, 0.5f);

        spriteRelleno = Sprite.Create(texturaRelleno, area, pivote, 100f, 0, SpriteMeshType.FullRect, bordes9);
        spriteBorde = Sprite.Create(texturaBorde, area, pivote, 100f, 0, SpriteMeshType.FullRect, bordes9);
    }

    /// <summary>
    /// Crea una caja redondeada y devuelve el Image del borde. El relleno es el padre del borde.
    /// escalaEsquinas: 1 = esquinas de 16 unidades; más alto = esquinas más pequeñas.
    /// </summary>
    public Image CrearCaja(Transform padre, string nombre, Vector2 posicion, Vector2 tamano,
                           Color colorRelleno, Color colorBorde, float escalaEsquinas)
    {
        GameObject go = HackUI.NuevoUI(padre, nombre, posicion, tamano);
        Image relleno = go.AddComponent<Image>();
        relleno.sprite = spriteRelleno;
        relleno.type = Image.Type.Sliced;
        relleno.pixelsPerUnitMultiplier = escalaEsquinas;
        relleno.color = colorRelleno;

        GameObject goBorde = HackUI.NuevoUI(go.transform, "Borde", Vector2.zero, tamano);
        HackUI.Estirar(goBorde);
        Image borde = goBorde.AddComponent<Image>();
        borde.sprite = spriteBorde;
        borde.type = Image.Type.Sliced;
        borde.pixelsPerUnitMultiplier = escalaEsquinas;
        borde.color = colorBorde;
        borde.raycastTarget = false;

        return borde;
    }

    /// <summary>Crea solo un relleno redondeado (sin borde). Útil para puntos y barras.</summary>
    public Image CrearRelleno(Transform padre, string nombre, Vector2 posicion, Vector2 tamano,
                              Color color, float escalaEsquinas)
    {
        GameObject go = HackUI.NuevoUI(padre, nombre, posicion, tamano);
        Image relleno = go.AddComponent<Image>();
        relleno.sprite = spriteRelleno;
        relleno.type = Image.Type.Sliced;
        relleno.pixelsPerUnitMultiplier = escalaEsquinas;
        relleno.color = color;
        relleno.raycastTarget = false;
        return relleno;
    }

    /// <summary>Borra los sprites y texturas generados.</summary>
    public void Liberar()
    {
        if (spriteRelleno != null) Object.Destroy(spriteRelleno);
        if (spriteBorde != null) Object.Destroy(spriteBorde);
        if (texturaRelleno != null) Object.Destroy(texturaRelleno);
        if (texturaBorde != null) Object.Destroy(texturaBorde);
        spriteRelleno = null;
        spriteBorde = null;
        texturaRelleno = null;
        texturaBorde = null;
    }

    /// <summary>Textura blanca con esquinas redondeadas y bordes suavizados.</summary>
    private Texture2D CrearTextura(bool soloBorde)
    {
        Texture2D textura = new Texture2D(TamanoSprite, TamanoSprite, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < TamanoSprite; y++)
        {
            for (int x = 0; x < TamanoSprite; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float alpha = Cobertura(px, py, 0f, RadioSprite);
                if (soloBorde)
                {
                    float interior = Cobertura(px, py, GrosorBorde, RadioSprite - GrosorBorde);
                    alpha = Mathf.Clamp01(alpha - interior);
                }
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        textura.Apply();
        return textura;
    }

    /// <summary>Cuánto cubre el píxel un rectángulo redondeado separado "margen" del borde.</summary>
    private float Cobertura(float px, float py, float margen, float radio)
    {
        float minimo = margen + radio;
        float maximo = TamanoSprite - margen - radio;
        float cx = Mathf.Clamp(px, minimo, maximo);
        float cy = Mathf.Clamp(py, minimo, maximo);
        float dx = px - cx;
        float dy = py - cy;
        float distancia = Mathf.Sqrt(dx * dx + dy * dy);
        return Mathf.Clamp01(radio - distancia + 0.5f);
    }
}
