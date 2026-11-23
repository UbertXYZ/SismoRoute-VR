using UnityEngine;

// Sonidos sencillos generados por codigo (no hace falta ningun archivo de audio).
// Se usan SOLO para lo que Jheeremy no tiene grabado: chorro del extintor, crepitar del fuego
// y la explosion al encenderse. Los audios que si existen en Jheeremy/Video se asignan
// desde el generador (clic al agarrar/soltar el extintor, etc.).
public static class AudioProceduralJosue
{
    const int Frecuencia = 22050;
    static AudioClip _chorro, _crepitar, _explosion;

    // Siseo de gas a presion (ruido blanco filtrado), pensado para reproducirse en bucle.
    public static AudioClip Chorro()
    {
        if (_chorro != null) return _chorro;
        var datos = new float[Frecuencia * 2];
        float previo = 0f;
        var azar = new System.Random(11);
        for (int i = 0; i < datos.Length; i++)
        {
            float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
            previo = previo * 0.55f + ruido * 0.45f;           // pasa-bajos suave
            datos[i] = previo * 0.55f;
        }
        return _chorro = Crear("Chorro_Procedural", datos);
    }

    // Crepitar de llamas: ruido grave con chasquidos al azar. Tambien en bucle.
    public static AudioClip Crepitar()
    {
        if (_crepitar != null) return _crepitar;
        var datos = new float[Frecuencia * 3];
        var azar = new System.Random(5);
        float grave = 0f, chasquido = 0f;
        for (int i = 0; i < datos.Length; i++)
        {
            float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
            grave = grave * 0.97f + ruido * 0.03f;
            if (azar.NextDouble() < 0.0009) chasquido = (float)(0.4 + azar.NextDouble() * 0.5);
            chasquido *= 0.985f;
            datos[i] = grave * 3.2f + ruido * chasquido * 0.6f;
        }
        return _crepitar = Crear("Crepitar_Procedural", datos);
    }

    // Golpe grave con caida rapida (explosion del fuego al aparecer).
    public static AudioClip Explosion()
    {
        if (_explosion != null) return _explosion;
        var datos = new float[Frecuencia];
        var azar = new System.Random(3);
        float grave = 0f;
        for (int i = 0; i < datos.Length; i++)
        {
            float t = i / (float)Frecuencia;
            float envolvente = Mathf.Exp(-t * 5f);
            float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
            grave = grave * 0.9f + ruido * 0.1f;
            datos[i] = (grave * 2.5f + Mathf.Sin(t * 2f * Mathf.PI * 55f) * 0.6f) * envolvente;
        }
        return _explosion = Crear("Explosion_Procedural", datos);
    }

    static AudioClip Crear(string nombre, float[] datos)
    {
        for (int i = 0; i < datos.Length; i++) datos[i] = Mathf.Clamp(datos[i], -1f, 1f);
        var clip = AudioClip.Create(nombre, datos.Length, 1, Frecuencia, false);
        clip.SetData(datos, 0);
        return clip;
    }
}
