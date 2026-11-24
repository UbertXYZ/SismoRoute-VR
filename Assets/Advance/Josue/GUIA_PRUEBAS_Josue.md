# Guía paso a paso para probar todo (para quien empieza con Unity)

No necesitas escribir comandos. Todo se hace con clics dentro de Unity.

## 0. Vocabulario mínimo
- **Project** (abajo): las carpetas y archivos del proyecto.
- **Hierarchy** (izquierda): los objetos que hay en la escena abierta.
- **Scene** (centro): vista para mover objetos. **Game**: lo que ve el jugador al darle Play.
- **Console** (`Window → General → Console`): mensajes del programa. **Rojo = error**, amarillo = aviso.
- **Play** (triángulo ▶ arriba al centro): arranca la prueba. Al volver a pulsarlo, se detiene y los cambios hechos durante Play se pierden (es normal).

## 1. Abrir el proyecto
1. Abre **Unity Hub**. Si el proyecto no aparece: *Add → Add project from disk* y elige la carpeta `SismoRoute-VR`.
2. Usa la versión **6000.3.25f1** (la que pide el proyecto). Si el Hub dice que falta, instálala desde ahí.
3. La primera vez tarda varios minutos ("Importing…"). Espera a que termine.
4. Abre la **Console**. Si hay mensajes **rojos de compilación** (`error CS...`), no sigas y avísame con una captura.

> Si Unity pregunta *"The project contains compilation errors / Enter Safe Mode"*, elige **Ignore** o **Enter Safe Mode** y avísame del mensaje.

## 2. Prueba rápida (3 minutos): aprender los controles
1. En el menú de arriba: **SismoRoute → Josue → 2 - Crear escena de prueba (extintor + fuego + senales)**.
   (Si antes no hiciste los prefabs, lo hace solo.)
2. Dale **Play ▶**. Haz **clic dentro de la ventana Game**.
3. A los **3 segundos** aparece un fuego frente a ti.
4. Controles (simulan el visor VR con teclado y mouse):

| Qué quieres hacer | Qué haces |
|---|---|
| Caminar | `W A S D` |
| Mirar | mantén **botón derecho** del mouse y muévelo |
| Activar la mano derecha | mantén **`Espacio`** (o pulsa `Y` para dejarla fija; `Esc` la suelta) |
| Apuntar la mano | con la mano activa, mueve el mouse. La rueda del mouse acerca/aleja |
| Agarrar | apunta al objeto y pulsa **`G`** |
| Gatillo / usar | **clic izquierdo** (con la mano activa) |
| Rociar el extintor | agarrado: clic izquierdo, o mantén **`F`** |

5. Camina hacia el extintor suelto (en el piso, a tu derecha), agárralo, apunta la boquilla al fuego y rocía ~3 s seguidos. El fuego se apaga. Arriba a la izquierda ves la carga y el estado del fuego.
6. Mira las **5 señales** en fila a la derecha (Botiquín, Extintor, Riesgo eléctrico, Zona segura, Salida; en pared y en poste).

Si esto funciona, ya sabes manejar el simulador.

## 3. Prueba completa en el edificio de Uberto (escaleras + fuego + rocas + extintor)
1. Menú **SismoRoute → Bloqueos (Josue) → 1 - Crear escena de prueba (copia de Prueba01)**. Si pregunta, elige **Reemplazar**.
   - Crea `Assets/Advance/Josue/Bloqueos/Escenas/PruebaBloqueos.unity`, una **copia** de la escena de Uberto (la suya no se toca) con mi generador de fuego y rocas.
2. Dale **Play ▶** y haz clic en la ventana Game. Espera que desaparezca la pantalla de carga (unos 3 s).
3. En la **Console** debe aparecer una línea `[Bloqueos Josue] N bloqueos colocados… | por piso (colocados/sorteados): P0:1/2 P1:0/0 …`. Cada piso sortea entre 0 y 2 bloqueos, así que el total cambia en cada partida (puede salir bajo; con `F6` se vuelve a sortear).
4. **Inicia el sismo con la tecla `F8`.** Esto te teletransporta al último piso, arranca el cronómetro y empiezan a aparecer fuegos y derrumbes (entre 1 y 12 s después).
   - `F7` = activar todos los bloqueos ya. `F6` = volver a sortearlos (distintos cada vez).
5. **Escaleras de Uberto:** camina hacia una escalera para bajar. Algunas se derrumban cuando entras en su zona (el sistema de Uberto, independiente de mi generador).
6. **Extintores:** hay gabinetes (cajas) en las paredes. Apunta al gabinete y pulsa **`G`** para abrir la puerta. Dentro está el extintor: agárralo con `G` y rocía con `F` o clic izquierdo.
7. Apaga un fuego (~3 s). El paso queda libre y el texto de la esquina cambia a "APAGADO".
8. Llega a la salida (zona de `FinNivel`): aparece el menú de resultado.

### Qué mirar para saber si funciona
- Panel abajo a la izquierda: *"Bloqueos Josue: X colocados, Y activos…"*.
- En la vista **Scene** (con el botón *Gizmos* activado): cajas de color en cada bloqueo y la ruta libre en verde.
- Si pone **0 colocados**, copia la línea de la Console que diga `[Bloqueos Josue]…` y mándamela.

### Probar solo los bloqueos (fuego y rocas), paso a paso
1. **SismoRoute → Bloqueos (Josue) → 1 - Crear escena de prueba** → "Reemplazar". **Play ▶** y clic en Game.
2. En la **Console** busca las líneas `[Bloqueos Josue] + Rocas/Fuego en piso N Pasillo_X_... (x, y, z)`: ahí dice **dónde** quedó cada bloqueo y la línea resumen dice cuántos hay por piso.
3. Pulsa **F8** (inicia el sismo; te lleva al último piso, el 4) y luego **F7** (activa todos al instante). Sin F7, aparecen solos entre 1 y 12 s después.
4. Para **verlos**, cambia a la pestaña **Scene** con **Gizmos** activado: cajas **naranjas = fuego**, **cafés = rocas**, esferas **magenta** = los dos lados que deben seguir conectados y línea **verde** = ruta libre.
5. Para **ir hasta uno** sin caminar mucho: durante Play, en la **Hierarchy** elige `XR Origin (XR Rig)`, y en el **Inspector** (Transform → Position) escribe las coordenadas de la Console (deja 3 m de distancia). Recuerda que cada piso tiene su altura (`y` ≈ 0, 4.5, 9, 13.5, 18).
6. **Qué debe pasar**: **rocas** = polvo, caen y un muro invisible te frena. **Fuego** = explosión, llamas, humo y también te frena; con el extintor (~3 s) se apaga y el paso se libera.
7. **Comprobar que nunca se cierra todo**: pulsa **F6** varias veces (cada vez sortea otros) y mira que en la Console aparezcan líneas `Descartado … cerraba…`: es el sistema rechazando bloqueos que dejarían sin salida. El panel de abajo a la izquierda dice si hay ruta.
8. Si en un sorteo salen pocos bloqueos es normal (cada piso sortea entre 0 y 2): **F6** otra vez.

## 4. Para las señales (opcional)
1. En **Project**, entra a `Assets/Advance/Josue/Senales/Prefabs` y haz clic en un prefab (por ejemplo `Senal_Salida_Pared`). Hay 5 señales, cada una en `_Pared` y `_Poste`.
2. En la vista **Scene**, apunta con la cámara a una pared.
3. Menú **SismoRoute → Josue → Colocar prefab seleccionado donde mira la vista Scene**.
   - Guarda la escena con `Ctrl+S` si quieres conservarla.

## 5. Problemas típicos
| Síntoma | Qué hacer |
|---|---|
| El mouse no mueve nada | Haz clic dentro de la ventana **Game**; mantén `Espacio` para la mano |
| No agarro el extintor | Apunta bien a él con la mano activa y pulsa `G`; si no, acércate |
| No hay fuego | Espera 3 s (escena rápida) o pulsa `F8`/`F7` (escena grande) |
| Error rojo `error CS...` | Captura de la Console y me la mandas |
| Error de OpenXR "only standalone targets" | **Es normal en Linux**, ignóralo |
| Nada reacciona tras Play | Mira que no esté en pausa (botón ⏸ azul) |

## 6. Para actualizar tu copia cuando otros suban cambios (terminal, opcional)
Abre una terminal en la carpeta del proyecto y escribe:

```bash
git pull origin main
```

Hazlo con Unity **cerrado**. Si te dice que tienes cambios locales, avísame antes de continuar.
