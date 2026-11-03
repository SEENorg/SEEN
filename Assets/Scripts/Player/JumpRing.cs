using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Onda de choque del DOBLE SALTO: crea un anillo expansivo en el piso, justo
/// donde el jugador saltó. Se suscribe al evento OnDobleSalto del PlayerController.
/// Se coloca en el jugador.
/// </summary>
public class JumpRing : MonoBehaviour
{
    [Header("Apariencia")]
    [SerializeField] private Color color = new Color(0.25f, 0.90f, 1f, 0.95f);
    [SerializeField] private float duracion = 0.5f;
    [SerializeField] private float escalaInicial = 0.5f;
    [SerializeField] private float escalaFinal = 3.2f;
    [SerializeField] private float alturaSuelo = 0.06f;

    private PlayerController pc;
    private readonly List<Onda> ondas = new List<Onda>();

    private class Onda
    {
        public GameObject go;
        public SpriteRenderer render;
        public float t;
    }

    private void Start()
    {
        pc = GetComponent<PlayerController>();
        if (pc != null)
            pc.OnDobleSalto += CrearOnda;
    }

    private void OnDestroy()
    {
        if (pc != null)
            pc.OnDobleSalto -= CrearOnda;
    }

    private void CrearOnda(Vector3 posicionJugador)
    {
        var go = new GameObject("OndaSalto");
        go.transform.position = new Vector3(posicionJugador.x,
                                            posicionJugador.y - 1f + alturaSuelo,
                                            posicionJugador.z);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteAnillo;
        var mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                               ?? Shader.Find("Sprites/Default"));
        mat.color = color;
        sr.material = mat;
        sr.color = color;

        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // acostado en el piso
        go.transform.localScale = Vector3.one * escalaInicial;

        ondas.Add(new Onda { go = go, render = sr, t = 0f });
    }

    private void Update()
    {
        for (int i = ondas.Count - 1; i >= 0; i--)
        {
            var o = ondas[i];
            o.t += Time.deltaTime;
            float p = Mathf.Clamp01(o.t / duracion);

            o.go.transform.localScale = Vector3.one * Mathf.Lerp(escalaInicial, escalaFinal, p);

            if (o.render != null)
            {
                Color c = o.render.color;
                c.a = Mathf.Lerp(color.a, 0f, p * p);
                o.render.color = c;
            }

            if (p >= 1f)
            {
                Destroy(o.go);
                ondas.RemoveAt(i);
            }
        }
    }

    // Textura de anillo generada una sola vez
    private static Sprite _spriteAnillo;
    private static Sprite SpriteAnillo
    {
        get
        {
            if (_spriteAnillo == null)
                _spriteAnillo = GenerarSpriteAnillo(128, 9);
            return _spriteAnillo;
        }
    }

    private static Sprite GenerarSpriteAnillo(int size, int grosor)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float centro = (size - 1) / 2f;
        float radioExt = size / 2f - 1f;
        float radioInt = radioExt - grosor;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centro;
                float dy = y - centro;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                bool anillo = d <= radioExt && d >= radioInt;
                tex.SetPixel(x, y, anillo ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
