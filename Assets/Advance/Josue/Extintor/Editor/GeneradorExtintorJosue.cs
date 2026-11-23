using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static UtilEditorJosue;

// Menu: SismoRoute > Josue > Extintor. Crea los prefabs del extintor y de su estacion de pared.
public static class GeneradorExtintorJosue
{
    const string Base = Raiz + "/Extintor";
    const string Mats = Base + "/Materiales";
    public const string RutaExtintor = Base + "/Prefabs/Extintor.prefab";
    public const string RutaEstacion = Base + "/Prefabs/EstacionExtintor.prefab";

    [MenuItem("SismoRoute/Josue/Extintor/1 - Crear prefabs (Extintor y Estacion)")]
    public static void CrearTodo()
    {
        var ext = CrearExtintor();
        CrearEstacion(ext);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(ext);
        Debug.Log("[Josue] Prefabs creados en " + Base + "/Prefabs: Extintor y EstacionExtintor.");
    }

    public static GameObject CrearExtintor()
    {
        var rojo = CrearMat(Mats, "Extintor_Rojo", new Color(0.8f, 0.04f, 0.04f), 0.3f, 0.65f);
        var negro = CrearMat(Mats, "Extintor_Negro", new Color(0.05f, 0.05f, 0.05f), 0f, 0.3f);
        var metal = CrearMat(Mats, "Extintor_Metal", new Color(0.7f, 0.7f, 0.72f), 0.9f, 0.7f);
        var etiqueta = CrearMat(Mats, "Extintor_Etiqueta", new Color(0.95f, 0.93f, 0.85f), 0f, 0.1f);
        var medidorMat = CrearMat(Mats, "Extintor_Medidor", new Color(0.1f, 0.75f, 0.2f), 0f, 0.3f);
        var chorroMat = MaterialChorro();

        var raiz = new GameObject("Extintor");

        // --- Forma: cuerpo cilindrico rojo con domo, cuello, palanca y manguera ---
        Pieza(PrimitiveType.Cylinder, "Cuerpo", raiz.transform, new Vector3(0, 0.25f, 0), new Vector3(0.16f, 0.22f, 0.16f), rojo);
        Pieza(PrimitiveType.Sphere, "Domo", raiz.transform, new Vector3(0, 0.47f, 0), new Vector3(0.16f, 0.1f, 0.16f), rojo);
        Pieza(PrimitiveType.Cylinder, "Cuello", raiz.transform, new Vector3(0, 0.55f, 0), new Vector3(0.05f, 0.04f, 0.05f), metal);
        Pieza(PrimitiveType.Cube, "Palanca", raiz.transform, new Vector3(0, 0.605f, 0), new Vector3(0.15f, 0.02f, 0.03f), negro);
        Pieza(PrimitiveType.Cube, "Manija", raiz.transform, new Vector3(0, 0.575f, -0.05f), new Vector3(0.02f, 0.05f, 0.05f), negro);
        Pieza(PrimitiveType.Cylinder, "Base", raiz.transform, new Vector3(0, 0.025f, 0), new Vector3(0.17f, 0.012f, 0.17f), negro);
        Pieza(PrimitiveType.Cube, "Etiqueta", raiz.transform, new Vector3(0, 0.26f, -0.081f), new Vector3(0.09f, 0.14f, 0.005f), etiqueta);

        // Manguera (de la cabeza hacia la boquilla, en +Z) y boquilla
        Pieza(PrimitiveType.Capsule, "Manguera", raiz.transform, new Vector3(0, 0.46f, 0.13f), new Vector3(0.022f, 0.1f, 0.022f), negro, new Vector3(75f, 0, 0));
        var tobera = Pieza(PrimitiveType.Cylinder, "Tobera", raiz.transform, new Vector3(0, 0.38f, 0.245f), new Vector3(0.04f, 0.03f, 0.04f), negro, new Vector3(90f, 0, 0));
        var boquilla = new GameObject("Boquilla").transform;       // eje Z = hacia donde sale el chorro
        boquilla.SetParent(raiz.transform, false);
        boquilla.localPosition = new Vector3(0, 0.38f, 0.28f);

        // Medidor de carga (barra verde en el costado)
        var medidor = Pieza(PrimitiveType.Cube, "Medidor", raiz.transform, new Vector3(0.085f, 0.3f, 0), new Vector3(0.012f, 0.1f, 0.03f), medidorMat);

        // --- Agarre ---
        var agarre = new GameObject("PuntoAgarre").transform;
        agarre.SetParent(raiz.transform, false);
        agarre.localPosition = new Vector3(0, 0.42f, -0.03f);

        var col = raiz.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.31f, 0.05f);
        col.size = new Vector3(0.2f, 0.62f, 0.3f);

        var rb = raiz.AddComponent<Rigidbody>();
        rb.mass = 2.5f;

        var grab = raiz.AddComponent<XRGrabInteractable>();
        grab.attachTransform = agarre;
        grab.throwOnDetach = false;

        // --- Chorro (particulas) ---
        var chorro = CrearChorro(boquilla, chorroMat);

        // --- Logica ---
        var logica = raiz.AddComponent<ExtintorJosue>();
        logica.boquilla = boquilla;
        logica.chorro = chorro;
        logica.medidor = medidor.transform;
        logica.sonidoClic = AssetDatabase.LoadAssetAtPath<AudioClip>(ClicJheeremy);   // audio de Jheeremy
        if (logica.sonidoClic == null)
            Debug.LogWarning("[Josue] No se encontro el click de Jheeremy en " + ClicJheeremy + "; el extintor quedara sin sonido de clic.");

        return GuardarPrefab(raiz, RutaExtintor);
    }

    static Material MaterialChorro()
    {
        AsegurarCarpeta(Mats);
        string ruta = Mats + "/ChorroExtintor.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        m = new Material(sh);
        if (m.HasProperty("_Surface")) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); }  // transparente
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.6f));
        m.color = new Color(1f, 1f, 1f, 0.6f);
        m.renderQueue = 3000;
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static ParticleSystem CrearChorro(Transform boquilla, Material mat)
    {
        var go = new GameObject("Chorro");
        go.transform.SetParent(boquilla, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new Color(0.95f, 0.97f, 1f, 0.7f);
        main.gravityModifier = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;

        var em = ps.emission;
        em.rateOverTime = 180f;
        em.enabled = false;                 // ExtintorJosue lo enciende al rociar

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 9f;
        forma.radius = 0.015f;

        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 2.4f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = grad;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    // Estacion: tablero + repisa + cartel "EXTINTOR" + un extintor puesto. Pivote en el suelo, espalda en z=0,
    // el frente (donde esta el extintor y se lee el texto) es el lado -Z. Sirve contra una pared o con su base.
    public static GameObject CrearEstacion(GameObject extintor)
    {
        var rojo = CrearMat(Mats, "Extintor_Rojo", new Color(0.8f, 0.04f, 0.04f), 0.3f, 0.65f);
        var tablero = CrearMat(Mats, "Estacion_Tablero", new Color(0.92f, 0.92f, 0.92f));
        var borde = CrearMat(Mats, "Estacion_Borde", new Color(0.75f, 0.05f, 0.05f));
        var repisa = CrearMat(Mats, "Estacion_Repisa", new Color(0.25f, 0.25f, 0.27f), 0.4f, 0.4f);

        var raiz = new GameObject("EstacionExtintor");
        Pieza(PrimitiveType.Cube, "Borde", raiz.transform, new Vector3(0, 0.95f, 0.012f), new Vector3(0.64f, 1.34f, 0.02f), borde);
        Pieza(PrimitiveType.Cube, "Tablero", raiz.transform, new Vector3(0, 0.95f, -0.003f), new Vector3(0.58f, 1.28f, 0.02f), tablero, null, true);
        Pieza(PrimitiveType.Cube, "Repisa", raiz.transform, new Vector3(0, 0.3f, -0.17f), new Vector3(0.4f, 0.03f, 0.34f), repisa, null, true);
        Pieza(PrimitiveType.Cube, "BasePie", raiz.transform, new Vector3(0, 0.02f, -0.2f), new Vector3(0.6f, 0.04f, 0.45f), repisa, null, true);
        Pieza(PrimitiveType.Cube, "Soporte", raiz.transform, new Vector3(0, 0.16f, -0.015f), new Vector3(0.08f, 0.28f, 0.04f), repisa);

        // Cartel rojo con texto, arriba del tablero
        Pieza(PrimitiveType.Cube, "Cartel", raiz.transform, new Vector3(0, 1.4f, -0.02f), new Vector3(0.5f, 0.2f, 0.02f), rojo);
        var t = new GameObject("Texto").AddComponent<TextMeshPro>();
        t.transform.SetParent(raiz.transform, false);
        t.transform.localPosition = new Vector3(0, 1.4f, -0.034f);
        t.text = "EXTINTOR";
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.fontSize = 2.4f;
        t.rectTransform.sizeDelta = new Vector2(0.48f, 0.18f);

        // Extintor colocado sobre la repisa (prefab anidado: se puede agarrar y sacar)
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(extintor);
        inst.transform.SetParent(raiz.transform, false);
        inst.transform.localPosition = new Vector3(0, 0.315f, -0.17f);

        return GuardarPrefab(raiz, RutaEstacion);
    }
}
