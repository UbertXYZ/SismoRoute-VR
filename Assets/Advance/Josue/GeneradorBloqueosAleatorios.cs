using UnityEngine;
using UnityEngine.AI;

public class GeneradorBloqueosAleatorios : MonoBehaviour
{
    [Header("Prefabs de Jheeremy (Arrastrar aquí)")]
    public GameObject[] prefabsBloqueos;
    public int cantidadDeBloqueos = 5;
    public float radioDelEdificio = 40f;

    void Start()
    {
        GenerarBloqueos();
    }

    void GenerarBloqueos()
    {
        if (prefabsBloqueos == null || prefabsBloqueos.Length == 0) return;

        for (int i = 0; i < cantidadDeBloqueos; i++)
        {
            Vector3 puntoAleatorio = transform.position + Random.insideUnitSphere * radioDelEdificio;
            NavMeshHit hit;
            
            // Busca un punto caminable válido en el NavMesh del edificio
            if (NavMesh.SamplePosition(puntoAleatorio, out hit, 10f, NavMesh.AllAreas))
            {
                GameObject prefabAleatorio = prefabsBloqueos[Random.Range(0, prefabsBloqueos.Length)];
                Instantiate(prefabAleatorio, hit.position, Quaternion.identity);
            }
        }
    }
}
