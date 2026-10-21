# Guía de marca blanca: reskin en menos de 1 hora

Objetivo: entregar una versión personalizada del juego (negocio, marca o evento) cambiando **protagonista, enemigos, escenario, logo, colores y sonidos** sin tocar código.

## Por qué es rápido

- Toda la lógica usa **claves**, nunca sprites. Ejemplo: `CharacterData.visualKey = "player_hero"`.
- Todo el arte y el audio de una versión vive en **un único asset**: `SkinProfile`.
- `GameConfig.activeSkin` decide qué skin usa la build; `GameConfig.fallbackSkin` cubre lo que falte (**reskin parcial**: si el cliente solo quiere cambiar el protagonista y el logo, solo rellenas eso).
- Las animaciones son listas de fotogramas (`SpriteClip`): no hay que crear Animators ni Animation Clips.
- Un solo prefab genérico para todos los enemigos y otro para todos los proyectiles: el aspecto lo pone el skin en tiempo de ejecución.

## Qué se puede cambiar en el `SkinProfile`

| Sección | Contenido | Usado por |
|---|---|---|
| Marca | Título del juego, empresa, eslogan, fuente TMP | `SkinnedText`, `SkinnedFont`, banners |
| Paleta | Primary, Secondary, Accent, Text, Background, Danger | `SkinnedUIColor` |
| Sprites de UI | Logo, fondo del menú, paneles, botones, iconos de moneda/diamante/estrellas/candado, potenciadores, joystick, botones de acción, pausa | `SkinnedUIImage` |
| Personajes | `player_hero`, `player_ninja`, `player_tank` (idle, run, jump, fall, attack, hurt, death) | `PlayerController` vía `SkinnedVisual` |
| Enemigos | `enemy_grunt`, `enemy_runner`, `enemy_bat`, `enemy_gunner`, `enemy_boss` | `EnemyController` |
| Objetos/FX | `proj_basic`, `proj_special`, `proj_enemy`, `pickup_coin`, `fx_shield` | `Projectile`, `Coin`, `PlayerHealth` |
| Iconos de armas | `weapon_blaster`, `weapon_shotgun`, `weapon_laser` | Tienda |
| Escenarios | `env_city`: capas de fondo con parallax, sprite de suelo en mosaico, color de cámara | `EnvironmentBuilder` |
| Audio | Música (menú, juego) y 17 efectos (`SfxId`) con variación de tono | `AudioManager` |

Cada `VisualSet` admite además `scale`, `offset`, `tint` y `facesRight` (si el arte original mira a la izquierda), así que no hace falta reexportar arte con otro tamaño u orientación.

## Procedimiento cronometrado

| Paso | Tiempo |
|---|---|
| 1. **Tools → HyperArcade → 3. Crear skin de cliente (plantilla)** → `Assets/_Game/Skins/<Cliente>/Skin_<Cliente>.asset` (acepta usarlo como activo) | 1 min |
| 2. Importa los PNG en `Skins/<Cliente>/Sprites`. Selecciónalos todos: *Texture Type = Sprite*, *Pixels Per Unit = 96* (personajes), *Pivot = Bottom* para personajes/enemigos | 5 min |
| 3. Rellena en el skin: protagonista (idle + run + jump), 2–5 enemigos, moneda, logo | 15–20 min |
| 4. Escenario: añade un `EnvironmentSet` con clave `env_city` (o la del nivel), 2–3 capas de fondo (*Mesh Type = Full Rect*) y el suelo | 5–10 min |
| 5. Título, empresa, eslogan y paleta de colores | 3 min |
| 6. Sonidos y música del cliente (opcional: lo vacío usa los de prueba) | 5 min |
| 7. **Tools → HyperArcade → 2. Validar skin activo** | 1 min |
| 8. Player Settings: *Company Name*, *Product Name*, *Package Name* (`com.cliente.juego`), icono · `AdsConfig` con los IDs del cliente | 5 min |
| 9. Build Android (`.aab`) | 5–10 min |
| **Total** | **≈ 45–60 min** |

## Especificación de arte para el cliente

`LEEME_RECURSOS.txt` (generado junto al skin) lista todas las claves. Resumen:

- **Personajes y enemigos:** PNG transparente, 96×128 px por fotograma (cualquier tamaño sirve: ajusta *Pixels Per Unit* o `VisualSet.scale`). Mínimo 1 fotograma idle; recomendado 2 idle + 2–6 run + 1 jump.
- **Jefe:** igual que un enemigo; el skin por defecto lo escala ×2.
- **Proyectiles:** 32–64 px, apuntando a la derecha.
- **Moneda:** 48 px, 1–4 fotogramas (giro).
- **Fondos:** capas de 512×256 px que se repitan en horizontal; cielo como degradado vertical.
- **Suelo:** baldosa de 64×64 px que se repita (parte superior = césped/borde).
- **UI:** logo 512×256; iconos 96×96; paneles y botones como 9-slice (definir bordes en el *Sprite Editor*).

## Ideas de personalización por tipo de cliente

- **Restaurante / marca de consumo:** el protagonista es la mascota; los enemigos, "plagas" o la competencia en clave cómica; las monedas, el producto.
- **Evento corporativo / feria:** logo y colores del evento; misión "Elimina a 30" con ranking del Modo Infinito para un concurso.
- **Educación:** enemigos con forma de errores ortográficos o números; textos de `Texts.cs` adaptados.

## Checklist de entrega

- [ ] Skin del cliente activo y validado sin recursos faltantes no deseados
- [ ] Título/icono/package name propios
- [ ] IDs de anuncios reales y `testMode = false`
- [ ] (Opcional) Sal propia en `SaveSystem.Salt` para que los guardados de distintas versiones no sean intercambiables
- [ ] Prueba completa en dispositivo: menú → tienda → nivel 1 → revivir con anuncio → victoria → x2 monedas
