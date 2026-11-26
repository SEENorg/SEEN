using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HASHTAG: antena de red que proyecta enlaces visibles de escudo a los enemigos
/// cercanos (bloquean disparos normales; se rompen con disparo sobrecargado) y
/// emite ondas en el suelo que el jugador debe saltar.
/// Al morir, desactiva todos los escudos y aturde a los enemigos enlazados.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class EnemyHashtag : EnemyBase
{
    [Header("Buff y Escudos")]
    [Tooltip("Radio del escudo de red (mínimo efectivo 15m)")]
    [SerializeField] private float radioBuff = 15f;

    [Header("Onda de suelo")]
    [SerializeField] private float intervaloOnda = 3.2f;
    [SerializeField] private float radioMaximoOnda = 7.5f;
    [SerializeField] private float duracionOnda = 1.35f;
    [SerializeField] private float alcanceActivacionOnda = 16f;

    private readonly List<EnemyBase> enemigosBuffeados = new List<EnemyBase>();
    private readonly List<LineRenderer> rayosEnlace = new List<LineRenderer>();

    private float radioEfectivo;
    private float proximoEscaneoEnemigos;
    private float temporizadorOnda = 1.5f;
    private bool cargandoOnda;
    private Material materialRayo;

    protected override void Start()
    {
        base.Start();
        mataAlTocar = false;
        radioEfectivo = Mathf.Max(radioBuff, 15f);

        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.isTrigger = true;
            sc.radius = radioEfectivo;
        }

        // Asegurar hitbox sólida tras pasar SphereCollider a trigger
        AsegurarHitboxSolida();
        BuscarEnemigosEnRango();
    }

    private void OnDestroy()
    {
        LimpiarRayos();
        if (materialRayo != null) Destroy(materialRayo);
    }

    public override void ReiniciarPosicion()
    {
        LimpiarRayos();
        enemigosBuffeados.Clear();
        temporizadorOnda = 1.5f;
        cargandoOnda = false;
        base.ReiniciarPosicion();
    }

    protected override void Update()
    {
        base.Update();
        if (muerto)
        {
            LimpiarRayos();
            return;
        }

        if (Time.time >= proximoEscaneoEnemigos)
        {
            proximoEscaneoEnemigos = Time.time + 0.3f;
            BuscarEnemigosEnRango();
        }

        ActualizarRayos();

        if (!PuedeMoverse || jugador == null)
        {
            estadoMecanico = "";
            return;
        }

        float dist = Vector3.Distance(transform.position, jugador.position);
        if (dist > alcanceActivacionOnda)
        {
            cargandoOnda = false;
            estadoMecanico = "";
            return;
        }

        temporizadorOnda -= Time.deltaTime;
        if (!cargandoOnda && temporizadorOnda <= 0.45f)
        {
            cargandoOnda = true;
            estadoMecanico = "[¡ONDA!]";
            AplicarTinteLuz(new Color(1f, 0.55f, 0.1f), 4f);
        }

        if (temporizadorOnda <= 0f)
        {
            cargandoOnda = false;
            temporizadorOnda = intervaloOnda;
            estadoMecanico = "";
            ActualizarColorLuz();
            EmitirOnda();
        }
    }

    private void BuscarEnemigosEnRango()
    {
        EnemyBase[] todos = Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // Limpiar enemigos muertos o fuera de rango
        for (int i = enemigosBuffeados.Count - 1; i >= 0; i--)
        {
            EnemyBase e = enemigosBuffeados[i];
            if (e == null || e.Muerto || !e.gameObject.activeInHierarchy ||
                Vector3.Distance(transform.position, e.transform.position) > radioEfectivo)
            {
                if (e != null) e.AplicarBuff(false);
                enemigosBuffeados.RemoveAt(i);
                if (i < rayosEnlace.Count)
                {
                    if (rayosEnlace[i] != null) Destroy(rayosEnlace[i].gameObject);
                    rayosEnlace.RemoveAt(i);
                }
            }
        }

        // Enlazar enemigos nuevos dentro del radio
        for (int i = 0; i < todos.Length; i++)
        {
            EnemyBase e = todos[i];
            if (e == null || e == this || e.Muerto || e is EnemyHashtag) continue;
            if (enemigosBuffeados.Contains(e)) continue;
            if (Vector3.Distance(transform.position, e.transform.position) <= radioEfectivo)
            {
                enemigosBuffeados.Add(e);
                e.AplicarBuff(true);
                rayosEnlace.Add(CrearRayo());
            }
        }
    }

    private LineRenderer CrearRayo()
    {
        if (materialRayo == null)
        {
            Shader s = Shader.Find("Sprites/Default");
            materialRayo = new Material(s);
        }

        GameObject go = new GameObject("EnlaceHashtag");
        go.transform.SetParent(transform, true);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = materialRayo;
        lr.positionCount = 2;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.05f;
        Color c = new Color(1f, 0.55f, 0.1f, 0.85f);
        lr.startColor = c;
        lr.endColor = c;
        lr.useWorldSpace = true;
        return lr;
    }

    private void ActualizarRayos()
    {
        Vector3 origen = ObtenerLimitesVisuales().center;
        for (int i = 0; i < enemigosBuffeados.Count && i < rayosEnlace.Count; i++)
        {
            LineRenderer lr = rayosEnlace[i];
            EnemyBase e = enemigosBuffeados[i];
            if (lr == null) continue;
            if (e == null || e.Muerto || !e.gameObject.activeInHierarchy)
            {
                lr.enabled = false;
                continue;
            }
            lr.enabled = true;
            lr.SetPosition(0, origen);
            lr.SetPosition(1, e.transform.position);
        }
    }

    private void LimpiarRayos()
    {
        for (int i = 0; i < rayosEnlace.Count; i++)
        {
            if (rayosEnlace[i] != null) Destroy(rayosEnlace[i].gameObject);
        }
        rayosEnlace.Clear();
    }

    private void EmitirOnda()
    {
        Vector3 centro = ObtenerLimitesVisuales().center;
        float ySuelo = transform.position.y - 0.8f;
        RaycastHit hit;
        if (Physics.Raycast(centro, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            ySuelo = hit.point.y + 0.1f;

        GameObject go = new GameObject("OndaHashtag");
        go.transform.position = new Vector3(centro.x, ySuelo, centro.z);
        OndaHashtag onda = go.AddComponent<OndaHashtag>();
        onda.Inicializar(radioMaximoOnda, duracionOnda);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // El buff se gestiona por distancia en BuscarEnemigosEnRango
    }

    protected override void OnMuerte()
    {
        LimpiarRayos();
        foreach (EnemyBase e in enemigosBuffeados)
        {
            if (e != null && !e.Muerto)
            {
                e.AplicarBuff(false);
                e.Aturdir(2.0f);
            }
        }
        enemigosBuffeados.Clear();
        GameFeedback.Mensaje("#HASHTAG destruido — Escudos desactivados");
    }
}

/// <summary>Anillo expansivo en el suelo emitido por EnemyHashtag. Se esquiva saltando.</summary>
public class OndaHashtag : MonoBehaviour
{
    private const int Puntos = 40;

    private float radioMaximo = 7.5f;
    private float duracion = 1.35f;
    private float transcurrido;
    private bool golpeoJugador;
    private LineRenderer linea;
    private Material mat;
    private PlayerController pc;

    public void Inicializar(float maxRadio, float duracionSegundos)
    {
        radioMaximo = maxRadio;
        duracion = duracionSegundos;
        pc = Object.FindFirstObjectByType<PlayerController>();

        Shader s = Shader.Find("Sprites/Default");
        mat = new Material(s);

        linea = gameObject.AddComponent<LineRenderer>();
        linea.material = mat;
        linea.loop = true;
        linea.useWorldSpace = true;
        linea.positionCount = Puntos;
        linea.startWidth = 0.22f;
        linea.endWidth = 0.22f;
        Color c = new Color(1f, 0.35f, 0.1f, 0.95f);
        linea.startColor = c;
        linea.endColor = c;
    }

    private void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }

    private void Update()
    {
        transcurrido += Time.deltaTime;
        float t = Mathf.Clamp01(transcurrido / duracion);
        float radio = Mathf.Lerp(0.6f, radioMaximo, t);

        Vector3 centro = transform.position;
        for (int i = 0; i < Puntos; i++)
        {
            float ang = i * (Mathf.PI * 2f / Puntos);
            linea.SetPosition(i, centro + new Vector3(Mathf.Cos(ang) * radio, 0f, Mathf.Sin(ang) * radio));
        }

        Color c = Color.Lerp(new Color(1f, 0.35f, 0.1f, 0.95f), new Color(1f, 0.2f, 0.05f, 0.25f), t);
        linea.startColor = c;
        linea.endColor = c;

        if (!golpeoJugador && pc != null)
        {
            Vector3 p = pc.transform.position;
            float distXZ = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(centro.x, centro.z));
            float diffAltura = p.y - centro.y;
            if (Mathf.Abs(distXZ - radio) <= 0.7f && diffAltura < 1.15f && diffAltura > -1.0f && pc.IsGrounded)
            {
                golpeoJugador = true;
                PlayerKill.KillPlayer();
            }
        }

        if (transcurrido >= duracion) Destroy(gameObject);
    }
}