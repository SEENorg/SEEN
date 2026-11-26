/// <summary>
/// Lo que el HUD de munición necesita saber de un arma. Así el HUD no depende de la
/// clase Pistola (si mañana hay otra arma, solo tiene que cumplir este contrato).
/// </summary>
public interface IArmaMunicion
{
    string NombreArma { get; }
    int MunicionActual { get; }
    int Capacidad { get; }
    bool Recargando { get; }

    /// <summary>De 0 a 1 mientras recarga.</summary>
    float ProgresoRecarga { get; }

    /// <summary>De 0 a 1 mientras mantiene el botón para disparo sobrecargado.</summary>
    float ProgresoCarga { get; }

    /// <summary>Sube en 1 cada vez que se dispara (el HUD lo usa para animar).</summary>
    int DisparosRealizados { get; }

    /// <summary>Sube en 1 cada vez que se intenta disparar sin balas.</summary>
    int IntentosSinMunicion { get; }
}
