using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SISTEMA DE ESTRUCTURAS DE DATOS - SEEN (Versión Actualizada)
/// =========================================================================
/// Este sistema implementa 5 estructuras de datos clásicas y las integra
/// directamente con los sistemas existentes de SEEN (movimiento, hackeo,
/// feedback y niveles). No reemplaza ni elimina ninguna lógica original:
/// solo se conecta a puntos clave para registrar y gestionar datos.
///
/// TEMÁTICA: Mal uso de las redes sociales.
/// El protagonista es un acosador/victimario absorbido por su computadora.
/// Las estructuras de datos representan cómo el sistema digital almacena,
/// procesa y recupera la información extraída de redes sociales (perfiles,
/// publicaciones, metadatos, notificaciones).
///
/// Estructuras implementadas e integradas:
/// 1. Stack<T>  (Pila)      - LIFO: Historial de acciones del jugador
///                              (salto, dash, sigilo, hackeo).
/// 2. Queue<T>  (Cola)      - FIFO: Notificaciones del sistema (ansiedad por
///                              notificaciones constantes de redes sociales).
/// 3. List<T>   (Lista)     - Secuencia dinámica: módulos de hackeo resueltos
///                              y perfiles vigilados.
/// 4. Array     (Arreglo)   - Tamaño fijo: posiciones de perfiles objetivo
///                              o publicaciones en el mapa.
/// 5. Dictionary (Diccionario) - Clave→Valor: Nivel→Dificultad y Usuario→
///                              Información privada extraída (robo de datos).
/// =========================================================================
public class EstructurasDatosJuego : MonoBehaviour
{
    public static EstructurasDatosJuego Instancia { get; private set; }

    [Header("Configuración")]
    [SerializeField] private bool mostrarLogs = true;
    [SerializeField] private int maxHistorialAcciones = 30;

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  1. PILA (Stack<string>) — LIFO
    //    Integración: Historial de acciones del jugador en el mundo distópico.
    //    Cada vez que el jugador salta, hace dash, agacharse o completa un
    //    hackeo, se registra en la pila.
    // ═══════════════════════════════════════════════════════════════════════════════════
    private Stack<string> historialAccionesJugador = new Stack<string>();

    public void RegistrarAccionJugador(string accion)
    {
        if (historialAccionesJugador.Count >= maxHistorialAcciones)
            historialAccionesJugador.Pop();
        historialAccionesJugador.Push(accion);
        if (mostrarLogs) Debug.Log($"[ED-Pila] LIFO - Registrada acción: '{accion}'");
    }

    public string DeshacerUltimaAccion()
    {
        if (historialAccionesJugador.Count == 0)
        {
            if (mostrarLogs) Debug.LogWarning("[ED-Pila] Pila vacía: no hay acciones que deshacer.");
            return null;
        }
        string accion = historialAccionesJugador.Pop();
        if (mostrarLogs) Debug.Log($"[ED-Pila] LIFO - Deshecha acción: '{accion}'");
        return accion;
    }

    public string VerUltimaAccion() => historialAccionesJugador.Count > 0 ? historialAccionesJugador.Peek() : null;
    public int TotalAccionesRegistradas => historialAccionesJugador.Count;

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  2. COLA (Queue<string>) — FIFO
    //    Integración: Cola de notificaciones del sistema. Cada mensaje temporal
    //    (checkpoint, aviso, nivel completado) se encola y se procesa en orden
    //    de llegada. Simula la ansiedad por notificaciones de redes sociales.
    // ═══════════════════════════════════════════════════════════════════════════════════
    private Queue<string> colaNotificacionesSistema = new Queue<string>();

    public void EncolarNotificacion(string notificacion)
    {
        colaNotificacionesSistema.Enqueue(notificacion);
        if (mostrarLogs) Debug.Log($"[ED-Cola] FIFO - Notificación encolada: '{notificacion}'");
    }

    public string ProcesarSiguienteNotificacion()
    {
        if (colaNotificacionesSistema.Count == 0)
        {
            if (mostrarLogs) Debug.LogWarning("[ED-Cola] Cola vacía: no hay notificaciones pendientes.");
            return null;
        }
        string not = colaNotificacionesSistema.Dequeue();
        if (mostrarLogs) Debug.Log($"[ED-Cola] FIFO - Notificación procesada: '{not}'");
        return not;
    }

    public string VerProximaNotificacion() => colaNotificacionesSistema.Count > 0 ? colaNotificacionesSistema.Peek() : null;
    public int NotificacionesPendientes => colaNotificacionesSistema.Count;

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  3. LISTA (List<string>) — Secuencia dinámica
    //    Integración A: Módulos de hackeo resueltos (llamado desde HackManager).
    //    Integración B: Perfiles de usuarios vigilados por el jugador.
    // ═══════════════════════════════════════════════════════════════════════════════════
    private List<string> modulosHackeoResueltos = new List<string>();
    private List<string> perfilesVigilados = new List<string>();

    public void RegistrarModuloHackeoResuelto(string nombreModulo)
    {
        if (!modulosHackeoResueltos.Contains(nombreModulo))
        {
            modulosHackeoResueltos.Add(nombreModulo);
            if (mostrarLogs) Debug.Log($"[ED-Lista] Módulo resuelto agregado: '{nombreModulo}' (total: {modulosHackeoResueltos.Count})");
        }
    }

    public bool ModuloEstaResuelto(string nombreModulo) => modulosHackeoResueltos.Contains(nombreModulo);
    public string ObtenerModuloEnPosicion(int indice)
    {
        if (indice < 0 || indice >= modulosHackeoResueltos.Count) return null;
        return modulosHackeoResueltos[indice];
    }
    public int TotalModulosResueltos => modulosHackeoResueltos.Count;

    public void RegistrarPerfilVigilado(string nombrePerfil)
    {
        if (!perfilesVigilados.Contains(nombrePerfil))
        {
            perfilesVigilados.Add(nombrePerfil);
            if (mostrarLogs) Debug.Log($"[ED-Lista] Perfil vigilado agregado: '{nombrePerfil}' (total: {perfilesVigilados.Count})");
        }
    }
    public bool PerfilEstaVigilado(string nombrePerfil) => perfilesVigilados.Contains(nombrePerfil);
    public int TotalPerfilesVigilados => perfilesVigilados.Count;

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  4. ARREGLO (Vector3[]) — Tamaño fijo
    //    Integración: Posiciones predefinidas de perfiles objetivo o publicaciones
    //    en el mapa del nivel. Configuración estática.
    // ═══════════════════════════════════════════════════════════════════════════════════
    private Vector3[] posicionesPerfilesObjetivo;

    public void InicializarPosicionesPerfiles(int cantidad)
    {
        posicionesPerfilesObjetivo = new Vector3[cantidad];
        if (mostrarLogs) Debug.Log($"[ED-Arreglo] Arreglo de posiciones de perfiles creado: {cantidad} entradas.");
    }

    public void SetearPosicionPerfil(int indice, Vector3 posicion)
    {
        if (posicionesPerfilesObjetivo == null)
        {
            Debug.LogWarning("[ED-Arreglo] Arreglo de perfiles no inicializado.");
            return;
        }
        if (indice < 0 || indice >= posicionesPerfilesObjetivo.Length)
        {
            Debug.LogWarning($"[ED-Arreglo] Índice {indice} fuera de rango (0-{posicionesPerfilesObjetivo.Length - 1}).");
            return;
        }
        posicionesPerfilesObjetivo[indice] = posicion;
        if (mostrarLogs) Debug.Log($"[ED-Arreglo] Perfil[{indice}] posicionado en {posicion}");
    }

    public Vector3 ObtenerPosicionPerfil(int indice)
    {
        if (posicionesPerfilesObjetivo == null || indice < 0 || indice >= posicionesPerfilesObjetivo.Length)
            return Vector3.zero;
        return posicionesPerfilesObjetivo[indice];
    }
    public int TotalPosicionesPerfiles => posicionesPerfilesObjetivo != null ? posicionesPerfilesObjetivo.Length : 0;

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  5. DICCIONARIO (Dictionary<string, int> y Dictionary<string, string>)
    //    Integración A: Nivel → Dificultad de puzles (llamado desde LevelInfo).
    //    Integración B: Usuario → Información privada extraída (simula robo de
    //    datos de redes sociales). Búsqueda O(1).
    // ═══════════════════════════════════════════════════════════════════════════════════
    private Dictionary<string, int> dificultadPorNivel = new Dictionary<string, int>();
    private Dictionary<string, string> informacionPrivadaExtraida = new Dictionary<string, string>();

    public void SetearDificultadNivel(string nombreNivel, int dificultad)
    {
        dificultadPorNivel[nombreNivel] = dificultad;
        if (mostrarLogs) Debug.Log($"[ED-Diccionario] Nivel '{nombreNivel}' → dificultad {dificultad}");
    }

    public int ObtenerDificultadNivel(string nombreNivel)
    {
        if (dificultadPorNivel.TryGetValue(nombreNivel, out int diff)) return diff;
        Debug.LogWarning($"[ED-Diccionario] Nivel '{nombreNivel}' no registrado.");
        return -1;
    }

    public void RegistrarInformacionPrivada(string usuario, string dato)
    {
        informacionPrivadaExtraida[usuario] = dato;
        if (mostrarLogs) Debug.Log($"[ED-Diccionario] Dato privado extraído de '{usuario}': '{dato}'");
    }

    public string ObtenerInformacionPrivada(string usuario)
    {
        if (informacionPrivadaExtraida.TryGetValue(usuario, out string dato)) return dato;
        return null;
    }
    public bool ExisteInformacionDeUsuario(string usuario) => informacionPrivadaExtraida.ContainsKey(usuario);
    public int TotalUsuariosConDatoRobado => informacionPrivadaExtraida.Count;
    public int TotalNivelesRegistrados => dificultadPorNivel.Count;

    // ═══════════════════════════════════════════════════════════════════════════════════
    //  Ciclo de vida e inicialización
    // ═══════════════════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(gameObject); // persiste entre niveles

        InicializarPosicionesPerfiles(10);

        // Registrar dificultades y tipos de amenaza por nivel (temática redes sociales)
        SetearDificultadNivel("Juego", 0);      // Nivel 1: Vigilancia básica (perfil público)
        SetearDificultadNivel("Nivel2", 1);     // Nivel 2: Comparación social (likes/métricas)
        SetearDificultadNivel("Nivel3", 2);     // Nivel 3: Vigilancia normalizada (metadatos)
        SetearDificultadNivel("Nivel4", 2);     // Nivel 4: Ansiedad por notificaciones
        SetearDificultadNivel("Nivel5", 2);     // Nivel 5: Algoritmos opacos / viralización

        if (mostrarLogs)
            Debug.Log("[ED] Sistema de Estructuras de Datos integrado a SEEN (actualizado). Pila, Cola, Lista, Arreglo y Diccionario activos.");
    }

    private void Update()
    {
        // Teclas de demostración (solo en editor para pruebas):
        // J = registrar acción de vigilancia en Pila
        // N = encolar notificación del sistema en Cola
        // U = deshacer última acción de la Pila
        // R = mostrar resumen de todas las estructuras
        if (Input.GetKeyDown(KeyCode.J))
            RegistrarAccionJugador("Vigiló perfil en redes sociales");
        if (Input.GetKeyDown(KeyCode.N))
            EncolarNotificacion("Nueva notificación: @" + (NotificacionesPendientes + 1));
        if (Input.GetKeyDown(KeyCode.U))
            DeshacerUltimaAccion();
        if (Input.GetKeyDown(KeyCode.R))
            MostrarResumenEstructuras();
    }

    [ContextMenu("Mostrar resumen de estructuras de datos (SEEN)")]
    public void MostrarResumenEstructuras()
    {
        Debug.Log("╔═════════════════════════════════════════════════════════════════════╗");
        Debug.Log("║         RESUMEN DE ESTRUCTURAS DE DATOS - SEEN (Redes Sociales)    ║");
        Debug.Log("╠═════════════════════════════════════════════════════════════════════╣");
        Debug.Log($"║ 1. PILA (Stack)       - Acciones jugador: {TotalAccionesRegistradas,-12} | Última: {VerUltimaAccion() ?? "ninguna",-22}║");
        Debug.Log($"║ 2. COLA (Queue)       - Notificaciones: {NotificacionesPendientes,-12} | Próxima: {VerProximaNotificacion() ?? "ninguna",-22}║");
        Debug.Log($"║ 3. LISTA (List<T>)    - Módulos resueltos: {TotalModulosResueltos,-13} | Perfiles vigilados: {TotalPerfilesVigilados,-10}║");
        Debug.Log($"║ 4. ARREGLO (Array)    - Posiciones perfiles: {TotalPosicionesPerfiles,-13}║");
        Debug.Log($"║ 5. DICCIONARIO        - Niveles: {TotalNivelesRegistrados,-14} | Usuarios con dato robado: {TotalUsuariosConDatoRobado,-8}║");
        Debug.Log("╚═════════════════════════════════════════════════════════════════════╝");
    }
}
