using UnityEngine;
using Unity.AI.Navigation;

// Prepara TODAS las puertas de la escena (las que tienen PuertaConfig de Uberto):
// - Les quita "Static" (si no, Unity congela su malla y no se ve girar)
// - Les agrega PuertaAutomatica (para que se abran solas)
// - Les agrega NavMeshModifier en "ignorar" (para que el NavMesh pase por la puerta)
public class PrepararPuertas : MonoBehaviour
{
    [Tooltip("Al dar Play, prepara también las puertas que falten.")]
    public bool alIniciar = true;

    void Awake()
    {
        if (alIniciar) Preparar();
    }

    // Clic derecho en el componente > "Preparar puertas" (hazlo SIN Play y ANTES del Bake)
    [ContextMenu("Preparar puertas")]
    public void Preparar()
    {
        int nuevas = 0, sinStatic = 0;
        var puertas = FindObjectsByType<PuertaConfig>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PuertaConfig p in puertas)
        {
#if UNITY_EDITOR
            // Quitar "Static" a la puerta y a su modelo (solo funciona fuera de Play)
            if (!Application.isPlaying)
            {
                foreach (Transform t in p.GetComponentsInChildren<Transform>(true))
                {
                    if (UnityEditor.GameObjectUtility.GetStaticEditorFlags(t.gameObject) != 0)
                    {
                        UnityEditor.GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                        sinStatic++;
                    }
                }
            }
#endif
            // Que el NavMesh ignore la hoja de la puerta
            if (!p.TryGetComponent(out NavMeshModifier mod))
                mod = p.gameObject.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true;
            mod.applyToChildren = true;

            // Que se abra sola
            if (!p.TryGetComponent(out PuertaAutomatica auto))
            {
                auto = p.gameObject.AddComponent<PuertaAutomatica>();
                auto.modelo = p.modelo;
                nuevas++;
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log($"PrepararPuertas: {puertas.Length} puertas revisadas, {nuevas} con apertura nueva, {sinStatic} objetos sin Static.");
    }
}