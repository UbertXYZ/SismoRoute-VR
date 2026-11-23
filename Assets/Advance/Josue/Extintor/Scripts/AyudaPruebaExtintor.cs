using UnityEngine;

// Texto en pantalla para probar el extintor sin adivinar (solo se dibuja en el Editor / Development build).
// Muestra la carga del extintor y cuanto le falta a cada fuego para apagarse.
public class AyudaPruebaExtintor : MonoBehaviour
{
    ExtintorJosue[] _extintores;
    float _busqueda;

    void Update()
    {
        if (Time.time < _busqueda) return;
        _busqueda = Time.time + 2f;
        _extintores = FindObjectsByType<ExtintorJosue>(FindObjectsSortMode.None);
    }

    void OnGUI()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("EXTINTOR: agarralo y rocia con el gatillo (o mantén F). Apunta la tobera al fuego.");
        if (_extintores != null)
            foreach (var e in _extintores)
                if (e != null) sb.AppendLine("  " + e.name + ": carga " + Mathf.RoundToInt(e.Carga01 * 100f) + "%" + (e.Rociando ? "  (rociando)" : ""));
        foreach (var f in FuegoApagable.Todos)
            if (f != null)
                sb.AppendLine("  " + f.name + ": " + (f.EstaApagado ? "APAGADO (paso libre)" : f.Encendido ? "encendido " + Mathf.RoundToInt(f.Vida * 100f) + "%" : "aun sin encender"));
        GUI.Box(new Rect(10, 10, 620, 24 + 20 * (2 + FuegoApagable.Todos.Count + (_extintores != null ? _extintores.Length : 0))), sb.ToString());
    }
}
