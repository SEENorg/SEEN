using UnityEngine;

/// <summary>
/// Indicadores de habilidades en la esquina inferior izquierda:
///  • DASH: círculo que se vuelve opaco al usarse y recupera su opacidad
///    mientras baja el cooldown.
///  • DOBLE SALTO: círculo que se opaca cuando se gasta el salto en el aire
///    y vuelve a la normalidad al tocar el piso.
/// Se coloca en el jugador (dibuja en OnGUI).
/// </summary>
public class SkillIndicators : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private Color colorDash = new Color(0.15f, 0.75f, 1f, 0.95f);
    [SerializeField] private Color colorDashOcupado = new Color(0.15f, 0.75f, 1f, 0.25f);
    [Tooltip("Etiqueta bajo el círculo")]
    [SerializeField] private string etiquetaDash = "DASH";

    [Header("Doble salto")]
    [SerializeField] private Color colorSalto = new Color(1f, 0.55f, 0.15f, 0.95f);
    [SerializeField] private Color colorSaltoOcupado = new Color(1f, 0.55f, 0.15f, 0.25f);
    [SerializeField] private string etiquetaSalto = "SALTO";

    [Header("Posición")]
    [SerializeField] private float margen = 34f;
    [SerializeField] private float diametro = 62f;
    [SerializeField] private float separacion = 18f;

    private PlayerController pc;
    private Texture2D texturaCirculo;
    private GUIStyle estiloEtiqueta;

    private void Awake()
    {
        texturaCirculo = TexturaCirculo(128, 8);
    }

    private void Start()
    {
        pc = GetComponent<PlayerController>();
    }

    private void OnGUI()
    {
        if (pc == null) return;
        if (HackManager.Abierto || PauseMenu.Pausado || CipherWheel.Abierta) return;

        if (estiloEtiqueta == null)
        {
            estiloEtiqueta = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            estiloEtiqueta.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        }

        float y = Screen.height - margen - diametro;

        float cooldownTotal = Mathf.Max(0.01f, pc.DashCooldown);
        bool listo = !pc.IsDashing && pc.DashCooldownLeft <= 0f;

        float disponible;
        if (listo)
            disponible = 1f;
        else
            disponible = Mathf.Clamp01(pc.DashCooldownLeft / cooldownTotal);

        Color cDash = colorDash;
        cDash.a = Mathf.Lerp(colorDashOcupado.a, colorDash.a, disponible);
        DibujarCirculo(margen, y, diametro, cDash);
        GUI.Label(new Rect(margen, y + diametro + 2f, diametro, 20f), etiquetaDash, estiloEtiqueta);

        bool saldoDisponible = pc.IsGrounded || pc.JumpsLeft >= 1;

        Color cSalto;
        if (saldoDisponible)
            cSalto = colorSalto;
        else
            cSalto = colorSaltoOcupado;

        DibujarCirculo(margen + diametro + separacion, y, diametro, cSalto);
        GUI.Label(new Rect(margen + diametro + separacion, y + diametro + 2f, diametro, 20f),
                  etiquetaSalto, estiloEtiqueta);
    }

    private void DibujarCirculo(float x, float y, float d, Color c)
    {
        Color previo = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(x, y, d, d), texturaCirculo);
        GUI.color = previo;
    }

    private static Texture2D TexturaCirculo(int size, int borde)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float centro = (size - 1) / 2f;
        float radioExt = size / 2f - 1f;
        float radioInt = radioExt - borde;

        for (int yy = 0; yy < size; yy++)
        {
            for (int xx = 0; xx < size; xx++)
            {
                float dx = xx - centro;
                float dy = yy - centro;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color c;
                if (dist > radioExt) c = new Color(1f, 1f, 1f, 0f);
                else if (dist > radioInt) c = new Color(1f, 1f, 1f, 0.85f);
                else c = new Color(1f, 1f, 1f, 0.16f);
                tex.SetPixel(xx, yy, c);
            }
        }
        tex.Apply();
        return tex;
    }
}