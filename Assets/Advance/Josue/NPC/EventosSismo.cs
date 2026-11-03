using System;
using UnityEngine;

// Clase ESTATICA: no se pone en ningun objeto, existe una sola vez para todo el juego.
// Funciona como un "altavoz": quien detecta el sismo llama IniciarSismo(),
// y todos los NPC que estan "escuchando" reaccionan.
public static class EventosSismo
{
    // "event Action" = lista de funciones que se llaman cuando ocurre algo.
    public static event Action SismoIniciado;
    public static event Action SismoTerminado;

    public static bool SismoActivo { get; private set; }
    public static int NpcsSeguros { get; private set; }
    public static int NpcsTotales;

    public static void IniciarSismo()
    {
        if (SismoActivo) return;          // evita iniciarlo dos veces
        SismoActivo = true;
        NpcsSeguros = 0;
        Debug.Log("[Sismo] INICIO");
        SismoIniciado?.Invoke();          // "?." = solo si alguien escucha
    }

    public static void TerminarSismo()
    {
        if (!SismoActivo) return;
        SismoActivo = false;
        Debug.Log("[Sismo] FIN");
        SismoTerminado?.Invoke();
    }

    public static void NpcLlegoASalvo() { NpcsSeguros++; }

    // Limpia el estado cada vez que presionas Play (las variables estaticas
    // podrian conservar valores de la ejecucion anterior).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetear()
    {
        SismoActivo = false; NpcsSeguros = 0; NpcsTotales = 0;
        SismoIniciado = null; SismoTerminado = null;
    }
}