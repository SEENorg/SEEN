/// <summary>
/// Tipos de módulo de hackeo disponibles.
/// Cada terminal de hackeo puede encadenar varios módulos (1 en el Nivel 1,
/// hasta 5 en el Nivel 3).
/// </summary>
public enum HackableType
{
    /// <summary>Descifrar un código con desplazamiento César (teclado o ratón).</summary>
    DescifrarCodigo,

    /// <summary>Unir cables del mismo color (ratón).</summary>
    UnirCables,

    /// <summary>Repetir una secuencia de luces (ratón).</summary>
    Patron,

    /// <summary>Encontrar el glifo diferente en una cuadrícula (ratón).</summary>
    Escanear,

    /// <summary>Sincronizar la señal en la zona verde (timing, ratón o espacio).</summary>
    Sincronizar,

    /// <summary>Completar una secuencia lógica eligiendo la opción correcta.</summary>
    Secuencia
}
