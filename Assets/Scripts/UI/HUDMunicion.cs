using UnityEngine;

/// <summary>
/// Contador de munición grande e interactivo (abajo a la derecha):
///   • Número grande que "salta" con cada disparo y cambia de color según quedan balas.
///   • Una cápsula por bala: se apagan al disparar y se van llenando una a una al recargar.
///   • Barra de progreso y texto "RECARGANDO…" durante la recarga.
///   • Aviso "MUNICIÓN BAJA" / "SIN MUNICIÓN" con la tecla para recargar (R o RB),
///     y una sacudida si se intenta disparar sin balas.
/// Lee el arma a través de IArmaMunicion (no conoce la clase Pistola).
/// </summary>
public class HUDMunicion : MonoBehaviour
{
    private const float Ancho = 360f;
    private const float Alto = 150f;
    private const float Margen = 24f;

    private readonly Color colorFondo = new Color(0.02f, 0.04f, 0.06f, 0.88f);
    private readonly Color colorAcento = new Color(0.15f, 0.85f, 1f);
    private readonly Color colorBajo = new Color(1f, 0.62f, 0.15f);
    private readonly Color colorVacio = new Color(1f, 0.22f, 0.30f);
    private readonly Color colorTexto = new Color(0.88f, 0.95f, 1f);
    private readonly Color colorApagado = new Color(0.10f, 0.14f, 0.18f, 0.95f);

    private IArmaMunicion arma;
    private PincelHUD pincel;

    private int ultimosDisparos;
    private int ultimosIntentosVacios;
    private float golpeNumero;
    private float sacudida;
    private float destelloBala;
    private int indiceBalaDisparada = -1;
    private float aparicion;

    public void Configurar(IArmaMunicion arma)
    {
        this.arma = arma;
        if (arma != null)
        {
            ultimosDisparos = arma.DisparosRealizados;
            ultimosIntentosVacios = arma.IntentosSinMunicion;
        }
        aparicion = 0f;
    }

    private void OnDestroy()
    {
        if (pincel != null) pincel.Liberar();
    }

    private void Update()
    {
        if (arma == null) return;
        float dt = Time.unscaledDeltaTime;

        if (arma.DisparosRealizados != ultimosDisparos)
        {
            ultimosDisparos = arma.DisparosRealizados;
            golpeNumero = 1f;
            destelloBala = 1f;
            indiceBalaDisparada = arma.MunicionActual;
        }
        if (arma.IntentosSinMunicion != ultimosIntentosVacios)
        {
            ultimosIntentosVacios = arma.IntentosSinMunicion;
            sacudida = 1f;
        }

        golpeNumero = Mathf.MoveTowards(golpeNumero, 0f, dt * 6f);
        destelloBala = Mathf.MoveTowards(destelloBala, 0f, dt * 3f);
        sacudida = Mathf.MoveTowards(sacudida, 0f, dt * 3.5f);
        aparicion = Mathf.MoveTowards(aparicion, 1f, dt * 3f);
    }

    private void OnGUI()
    {
        if (arma == null) return;
        if (HackManager.Abierto || PauseMenu.Pausado || CipherWheel.Abierta ||
            GameFeedback.NivelCompletadoVisible || GameFeedback.GameOverVisible || PantallaControles.Visible)
            return;

        if (pincel == null) pincel = new PincelHUD();
        pincel.EmpezarEscala();

        float ahora = Time.unscaledTime;
        int actual = arma.MunicionActual;
        int capacidad = Mathf.Max(1, arma.Capacidad);
        float proporcion = (float)actual / capacidad;

        // Color según el estado
        Color colorEstado = colorAcento;
        string textoEstado = "LISTA";
        if (arma.Recargando)
        {
            textoEstado = "RECARGANDO";
        }
        else if (actual <= 0)
        {
            colorEstado = colorVacio;
            textoEstado = "SIN MUNICIÓN";
        }
        else if (proporcion <= 0.3f)
        {
            colorEstado = colorBajo;
            textoEstado = "MUNICIÓN BAJA";
        }

        // Posición (entra deslizándose desde la derecha al equipar el arma)
        float entrada = 1f - aparicion;
        float desplazamiento = entrada * entrada * 420f;
        float temblor = Mathf.Sin(ahora * 80f) * sacudida * 10f;
        Rect panel = new Rect(PincelHUD.AnchoReferencia - Margen - Ancho + desplazamiento + temblor,
                              1080f - Margen - Alto, Ancho, Alto);

        bool alerta = !arma.Recargando && proporcion <= 0.3f;
        float latido = 0.5f + 0.5f * Mathf.Sin(ahora * 6f);
        Color colorHalo = colorEstado;
        colorHalo.a = 0.25f;
        if (alerta) colorHalo.a = 0.25f + 0.45f * latido;
        pincel.Halo(panel, colorHalo, 4);
        pincel.Caja(panel, colorFondo);
        pincel.Borde(panel, Color.Lerp(colorAcento, colorEstado, 0.6f));
        pincel.Rectangulo(new Rect(panel.xMax - 88f, panel.y, 70f, 3f), ConstructorMenuJuego.ColorMagenta);

        // Encabezado
        pincel.Texto(new Rect(panel.x + 18f, panel.y + 10f, 220f, 22f), arma.NombreArma, 15, colorAcento,
                     TextAnchor.MiddleLeft, true);
        Color colorTextoEstado = colorEstado;
        if (alerta && latido < 0.4f) colorTextoEstado.a = 0.4f;
        pincel.Texto(new Rect(panel.xMax - 178f, panel.y + 10f, 160f, 22f), textoEstado, 14, colorTextoEstado,
                     TextAnchor.MiddleRight, true);

        // Número grande (salta un poco con cada disparo)
        int tamanoNumero = 66 + Mathf.RoundToInt(golpeNumero * 14f);
        Color colorNumero = colorEstado;
        if (golpeNumero > 0.5f) colorNumero = Color.Lerp(colorEstado, Color.white, golpeNumero);
        string numero = actual.ToString();
        if (arma.Recargando) numero = Mathf.RoundToInt(arma.ProgresoRecarga * capacidad).ToString();
        float anchoNumero = pincel.AnchoTexto(numero, tamanoNumero, true);
        pincel.Texto(new Rect(panel.x + 18f, panel.y + 30f, anchoNumero + 10f, 76f), numero, tamanoNumero, colorNumero,
                     TextAnchor.MiddleLeft, true);
        pincel.Texto(new Rect(panel.x + 26f + anchoNumero, panel.y + 52f, 90f, 40f), "/ " + capacidad, 26,
                     new Color(colorTexto.r, colorTexto.g, colorTexto.b, 0.6f), TextAnchor.MiddleLeft, true);

        // Cápsulas de balas (a la derecha del número)
        DibujarBalas(new Rect(panel.x + 196f, panel.y + 44f, panel.width - 214f, 52f), actual, capacidad, colorEstado);

        // Pie: barra de recarga, barra de sobrecarga o tecla para recargar
        Rect pie = new Rect(panel.x + 18f, panel.yMax - 34f, panel.width - 36f, 22f);
        if (arma.Recargando)
        {
            Rect barra = new Rect(pie.x, pie.y + 7f, pie.width, 8f);
            pincel.Caja(barra, colorApagado);
            if (arma.ProgresoRecarga > 0.05f)
                pincel.Caja(new Rect(barra.x, barra.y, barra.width * arma.ProgresoRecarga, barra.height), colorAcento);
        }
        else if (arma.ProgresoCarga > 0.05f)
        {
            Rect barra = new Rect(pie.x, pie.y + 9f, pie.width, 6f);
            pincel.Caja(barra, colorApagado);
            Color colorCarga = arma.ProgresoCarga >= 1f ? new Color(1f, 0.72f, 0.15f) : colorAcento;
            pincel.Caja(new Rect(barra.x, barra.y, barra.width * arma.ProgresoCarga, barra.height), colorCarga);
            string aviso = arma.ProgresoCarga >= 1f ? "¡SOBRECARGA LISTA! (suelta)" : "CARGANDO SOBRECARGA...";
            pincel.Texto(new Rect(pie.x, pie.y - 8f, pie.width, 16f), aviso, 12, colorCarga, TextAnchor.MiddleCenter, true);
        }
        else
        {
            Color colorAyuda = new Color(colorTexto.r, colorTexto.g, colorTexto.b, 0.55f);
            if (alerta) colorAyuda = new Color(colorEstado.r, colorEstado.g, colorEstado.b, 0.5f + 0.5f * latido);
            pincel.Texto(pie, "[R]/[RB] RECARGAR  ·  MANTÉN DISPARO: SOBRECARGA", 12, colorAyuda, TextAnchor.MiddleCenter, true);
        }

        pincel.TerminarEscala();
    }

    /// <summary>Dos filas de cápsulas; la bala recién disparada destella y se apaga.</summary>
    private void DibujarBalas(Rect zona, int actual, int capacidad, Color color)
    {
        int porFila = capacidad;
        int filas = 1;
        if (capacidad > 10)
        {
            filas = 2;
            porFila = Mathf.CeilToInt(capacidad / 2f);
        }

        float separacion = 5f;
        float anchoBala = Mathf.Min(16f, (zona.width - separacion * (porFila - 1)) / porFila);
        float altoBala = (zona.height - separacion * (filas - 1)) / filas;
        if (filas == 1) altoBala = Mathf.Min(altoBala, 44f);

        float llenas = actual;
        if (arma.Recargando) llenas = arma.ProgresoRecarga * capacidad;

        for (int i = 0; i < capacidad; i++)
        {
            int fila = i / porFila;
            int columna = i % porFila;
            Rect bala = new Rect(zona.x + columna * (anchoBala + separacion),
                                 zona.y + fila * (altoBala + separacion) + (zona.height - altoBala * filas - separacion * (filas - 1)) / 2f,
                                 anchoBala, altoBala);

            pincel.Rectangulo(bala, colorApagado);

            float lleno = Mathf.Clamp01(llenas - i);
            if (lleno > 0f)
            {
                // Se llena de abajo hacia arriba
                float alto = bala.height * lleno;
                Rect relleno = new Rect(bala.x, bala.yMax - alto, bala.width, alto);
                pincel.Rectangulo(relleno, color);
                pincel.Rectangulo(new Rect(bala.x + 2f, relleno.y + 2f, 3f, Mathf.Max(0f, alto - 4f)), new Color(1f, 1f, 1f, 0.3f));
            }

            // La que se acaba de disparar: destello blanco
            if (i == indiceBalaDisparada && destelloBala > 0f)
                pincel.Rectangulo(bala, new Color(1f, 1f, 1f, destelloBala * 0.85f));
        }
    }
}
