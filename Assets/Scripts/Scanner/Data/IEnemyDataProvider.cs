using System;
using UnityEngine;

namespace Game.Scanner
{
    /// <summary>
    /// Cualquier cosa que la Pantalla del Sistema puede escanear.
    ///
    /// CONTRATO (lo que toda implementación debe cumplir, principio de Liskov):
    /// 1. ObtenerDatos() se puede llamar en cualquier momento mientras el componente
    ///    exista, no tiene efectos secundarios y siempre devuelve datos válidos
    ///    (EnemyScanData ya garantiza textos no nulos y vida dentro de rango).
    /// 2. Objetivo nunca es null mientras el componente exista.
    /// 3. DatosCambiados se lanza solo cuando los datos cambian de verdad (por
    ///    ejemplo, al recibir daño), nunca cada frame, y siempre en el hilo principal.
    /// 4. El proveedor no sabe nada de la pantalla: no la busca ni la llama.
    /// Si una clase nueva cumple estas cuatro reglas, puede reemplazar a
    /// EnemyScannable sin que el escáner note la diferencia.
    /// </summary>
    public interface IEnemyDataProvider
    {
        /// <summary>Transform del enemigo (para marcadores, VFX, distancia...).</summary>
        Transform Objetivo { get; }

        /// <summary>Datos actuales del enemigo.</summary>
        EnemyScanData ObtenerDatos();

        /// <summary>Se lanza cuando cambian los datos (vida, amenaza...).</summary>
        event Action<IEnemyDataProvider> DatosCambiados;
    }
}
