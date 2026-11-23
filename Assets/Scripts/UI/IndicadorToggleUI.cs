using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escribe "ACTIVADO" o "DESACTIVADO" junto a un Toggle, para que su estado
/// no dependa solo del color (accesibilidad para daltonismo).
/// </summary>
public class IndicadorToggleUI : MonoBehaviour
{
    private Toggle toggle;
    private Text texto;

    public void Configurar(Toggle toggle, Text texto)
    {
        this.toggle = toggle;
        this.texto = texto;
        Actualizar();
    }

    private void Update()
    {
        Actualizar();
    }

    private void Actualizar()
    {
        if (toggle == null || texto == null) return;
        if (toggle.isOn)
        {
            texto.text = "ACTIVADO";
            texto.color = HackUI.ColAcento;
        }
        else
        {
            texto.text = "DESACTIVADO";
            texto.color = new Color(0.70f, 0.78f, 0.84f);
        }
    }
}
