# Josue: extintor, fuego apagable, señales y bloqueos

Todo vive en `Assets/Advance/Josue/` (no toca carpetas de otros).

| Carpeta | Qué hay |
|---|---|
| `Extintor/Scripts` | `ExtintorJosue` (agarrable y rocía), `FuegoApagable` (se agrega al fuego de Jheeremy), `AudioProceduralJosue`, `AyudaPruebaExtintor` (texto en pantalla) |
| `Extintor/Prefabs` | `Extintor`, `EstacionExtintor` (tablero + cartel + extintor puesto) |
| `Senales/Prefabs` | 10 prefabs: 5 señales × versión `_Pared` y `_Poste` (imágenes en `Senales/Texturas`) |
| `Bloqueos` | Generador aleatorio de fuego/rocas que siempre deja salida |
| `Editor` | Menús y generadores (solo Editor) |
| `Escenas` | `PruebaExtintorSenales.unity` (se crea con el menú) |

## 1. Crear los prefabs (una sola vez)
Menú **SismoRoute → Josue**:
1. `Extintor → 1 - Crear prefabs` 
2. `Senales → 1 - Crear prefabs de senales`
3. `2 - Crear escena de prueba` (crea ambos si faltan y arma la escena)

Después haz **commit de los `.prefab`, `.mat` y `.meta` generados** para que tu equipo los tenga sin regenerar.

## 2. Probar el extintor y el fuego
1. Abre `Josue/Escenas/PruebaExtintorSenales.unity` y dale **Play**.
2. A los **3 s** se enciende el fuego de Jheeremy (explosión, llamas, humo, collider que bloquea).
3. Con el **XR Device Simulator** (teclado + mouse) agarra el extintor (suelto en el piso o en las estaciones de la pared izquierda). **Rocía con el gatillo del simulador o manteniendo la tecla `F`** mientras lo sostienes. Apunta la tobera (eje azul) al fuego.
4. Rocía ~3 s seguidos: llamas y luz se encogen, el fuego se apaga, **se libera el paso** (collider y NavMeshObstacle se desactivan). Si sueltas antes, se reaviva.
5. El texto de arriba a la izquierda muestra carga del extintor y estado del fuego.
6. Con un visor VR: agarre con el botón de agarre y rocío con el gatillo.

Ajustes en el prefab `Extintor` (componente `ExtintorJosue`): alcance, ángulo, duración de carga. En `FuegoApagable`: segundos para apagar y reavivado.

## 3. Audios
- **Reutilizados de Jheeremy:** `Jheeremy/Video/click-sound-effect…mp3` → clic al agarrar/soltar el extintor y al apagarse el fuego.
- Jheeremy dejó **vacíos** los slots de audio de `fuego.prefab` y `bloqueo.prefab` (explosión, fuego en bucle, crujido, derrumbe) y no tiene grabaciones de fuego ni extintor. Por eso `FuegoApagable` rellena los slots de `FuegoAlto` con sonidos generados por código (explosión, crepitar) y el extintor usa un siseo generado. Si Jheeremy agrega audios reales, basta arrastrarlos a esos campos y se usan en su lugar.

## 4. Señales
Solo estas 5 (salen de las imágenes de `Senales/Texturas`): **Botiquín**, **Extintor (PQS)**, **Atención: riesgo eléctrico**, **Zona segura en casos de sismos** y **Salida**.
Prefabs en `Senales/Prefabs`: `Senal_<Nombre>_Pared` y `Senal_<Nombre>_Poste`.
- **`_Pared`**: solo el cartel, pivote en su centro. **`_Poste`**: con poste, pivote en el suelo.
- **La imagen se ve por el lado −Z.** La flecha azul del gizmo debe apuntar **hacia la pared**.
- Para cambiar una imagen, reemplaza el `.png` (mismo nombre) y vuelve a crear los prefabs con el menú.

Cómo ponerlas:
- **Fácil:** selecciona el prefab en la ventana Project, apunta con la cámara de la vista **Scene** a la pared (o al suelo) y usa el menú **SismoRoute → Josue → Colocar prefab seleccionado donde mira la vista Scene**. Lo pega y lo gira solo.
- **A mano:** arrástralo a la escena; para una pared a la que apunta la normal `n`, rotación `LookRotation(-n)`.
- Funciona igual con `EstacionExtintor` y `Extintor`. Las paredes del edificio de Uberto ya tienen Collider.

## 5. Bloqueos aleatorios (fuego + rocas) por piso
`Bloqueos/GeneradorBloqueosAleatorios`: se agrega con el menú **SismoRoute → Bloqueos (Josue) → 2** o a mano a un objeto vacío (los prefabs de Jheeremy se llenan solos).
- **Usa el `PisoConfig` de Uberto** (uno por piso): con `anchoPabellon`, `largoPabellon`, `anchoPasillos`, `largoEscalera`, `anchoEscalera`, `hayPasilloZ`, `cuatroEscaleras` e `indicePiso` calcula dónde están los pasillos de cada piso, igual que Uberto los construye.
- **Sortea cuántos bloqueos lleva cada piso** (`Bloqueos Por Piso`, por defecto 0 a 2), el tipo (fuego o rocas), la posición y el momento. No pone bloqueos en la boca de las escaleras (esas las derrumba el sistema de Uberto, `BloqueoEscalera`).
- **Siempre deja salida:** descarta cualquier bloqueo que deje un pasillo sin paso o cierre la ruta del inicio a la salida.
- Los fuegos que crea se pueden apagar con el extintor (`FuegoApagable`).
- Teclas en el Editor: `F6` re-sortea, `F7` activa todos, `F8` inicia el sismo.

## 6. Qué subir a git
Solo `Assets/Advance/Josue/` (incluye los `.meta`). **No subas** lo que Unity cambia solo: `ProjectSettings/GraphicsSettings.asset`, `Assets/UniversalRenderPipelineGlobalSettings.asset*`, `Assets/_Recovery/0 (1).unity*` ni `.vscode/`.
