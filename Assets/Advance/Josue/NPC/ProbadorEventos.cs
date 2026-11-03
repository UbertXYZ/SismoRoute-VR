using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;   // el proyecto usa el Input System nuevo

// Solo para PROBAR sin VR: teclas que disparan eventos.
public class ProbadorEventos : MonoBehaviour
{
    public float duracionSismo = 8f;
    public GameObject bloqueo, fuego, aglomeracion;   // zonas que se activan/desactivan

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;
        if (k.digit1Key.wasPressedThisFrame) StartCoroutine(SismoTemporal());
        if (k.digit2Key.wasPressedThisFrame) Alternar(bloqueo);
        if (k.digit3Key.wasPressedThisFrame) Alternar(fuego);
        if (k.digit4Key.wasPressedThisFrame) Alternar(aglomeracion);
    }

    IEnumerator SismoTemporal()
    {
        EventosSismo.IniciarSismo();
        yield return new WaitForSeconds(duracionSismo);
        EventosSismo.TerminarSismo();
    }

    void Alternar(GameObject go) { if (go != null) go.SetActive(!go.activeSelf); }

    void OnGUI()   // texto en pantalla (vista Game)
    {
        GUI.skin.label.fontSize = 18;
        GUI.Label(new Rect(10, 10, 700, 120),
            "1 = Sismo | 2 = Bloqueo | 3 = Fuego | 4 = Aglomeracion\n" +
            "Sismo activo: " + EventosSismo.SismoActivo + "\n" +
            "A salvo: " + EventosSismo.NpcsSeguros + " / " + EventosSismo.NpcsTotales);
    }
}