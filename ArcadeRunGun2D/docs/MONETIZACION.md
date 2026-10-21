# Monetización con anuncios recompensados

El juego **solo** usa anuncios recompensados (*rewarded video*): el jugador decide verlos a cambio de algo. Es el formato más sencillo de implementar, el que mejor tolera la audiencia hipercasual y no requiere pagos dentro de la app.

## Arquitectura del módulo

```
RewardedAdButton / RevivePopup / ResultPopup
              │  AdsManager.Instance.ShowRewarded(placement, onRewarded, onFailed)
              ▼
         AdsManager  ── límites diarios, cooldown, pausa del juego, eventos de analítica
              │  IAdsProvider
     ┌────────┼──────────────┬───────────────┐
 MockAdsProvider   AdMobProvider    UnityAdsProvider    (tu proveedor)
   (editor)        #if HA_ADMOB     #if HA_UNITY_ADS
```

- El juego compila **sin ningún SDK** instalado: los proveedores reales están detrás de símbolos de compilación.
- En el **editor** siempre se usa `MockAdsProvider` (simula 1,5 s y recompensa; `mockFailChance` permite probar errores).
- `AdsManager` pausa el juego (`Time.timeScale = 0`, `AudioListener.pause`) durante el anuncio y reanuda al cerrarlo.
- Precarga automática y reintentos con espera exponencial (5 s → 60 s).
- Analítica: suscríbete a `AdsManager.AdFinished (placement, resultado)` para enviar eventos a Firebase/GameAnalytics.

## Video entre niveles (intersticial)

Además de los anuncios recompensados, cada **2 niveles ganados** se proyecta un video a pantalla completa **sin recompensa**:

- Se cuenta cada victoria en Modo Niveles (el contador se guarda en `SaveData.ads.levelsSinceInterstitial`).
- El video aparece al **salir de la pantalla de victoria** (Siguiente, Reintentar o Menú), nunca en mitad de una partida.
- Si el anuncio aún no está cargado, el juego **no espera**: continúa y lo intenta en la siguiente transición.
- Frecuencia configurable en `EconomyConfig.interstitialEveryLevels` (2 por defecto; 0 = desactivado).
- IDs: `AdsConfig.admobInterstitialAndroid/IOS` (vienen los de prueba de Google) o `unityInterstitialAndroid/IOS` (`Interstitial_Android` / `Interstitial_iOS`).
- En el editor se simula con el mensaje "[Video publicitario de prueba]".

Los videos recompensados siguen funcionando igual y a elección del jugador: revivir, x2 monedas, +10 diamantes y +100 monedas.

## Botones de recompensa incluidos

| Botón | Componente | Configuración |
|---|---|---|
| "Ver anuncio para obtener +10 Diamantes" | `RewardedAdButton` (Reward = Diamonds) | Menú (`MenuDiamonds`) y Tienda (`ShopDiamonds`) |
| "Ver anuncio para +100 Monedas Gratis" | `RewardedAdButton` (Reward = Coins) | Menú (`MenuCoins`) y Tienda (`ShopCoins`) |
| "Ver anuncio para revivir" | `RevivePopup` | Al morir, 1 vez por partida, cuenta atrás de 6 s ("No, gracias" aparece a los 1,2 s) |
| "Ver anuncio: x2 monedas (+N)" | `RewardedAdButton` (Reward = Custom) en `ResultPopup` | Al ganar y al terminar una partida infinita |

Para añadir un botón nuevo: crea un `Button`, añade `RewardedAdButton`, elige *placement* y recompensa. No hace falta código.

## Opción A — AdMob (recomendada para reventa)

1. Descarga el plugin **Google Mobile Ads for Unity** (v9 o superior) desde GitHub *googleads/googleads-mobile-unity* e impórtalo (`.unitypackage`).
2. **Assets → External Dependency Manager → Android Resolver → Resolve**.
3. **Assets → Google Mobile Ads → Settings**: pega el **App ID** de Android e iOS del cliente.
4. **Tools → HyperArcade → Anuncios → Activar AdMob (HA_ADMOB)**.
5. En `Data/Config/AdsConfig.asset`: `provider = AdMob`, IDs de bloque *Rewarded* del cliente y `testMode = false` antes de publicar.
   Los IDs por defecto son los **IDs de prueba oficiales de Google** (seguros durante el desarrollo).
6. **Consentimiento (EEE/Reino Unido):** integra el *User Messaging Platform* (UMP) incluido en el plugin y llama a `MobileAds.Initialize` después de obtener el consentimiento (ver comentario en `AdMobProvider.Initialize`).
7. Si la app está dirigida a menores, marca `childDirected` (COPPA / Families Policy).

## Opción B — Unity Ads

1. **Window → Package Manager → Unity Registry → Advertisement Legacy** (`com.unity.ads`).
2. En el panel de Unity Ads (Unity Cloud → Monetization) crea el proyecto y copia el **Game ID** de Android/iOS.
3. **Tools → HyperArcade → Anuncios → Activar Unity Ads (HA_UNITY_ADS)**.
4. En `AdsConfig`: `provider = UnityAds`, `unityGameIdAndroid/IOS` y los IDs de bloque (por defecto `Rewarded_Android` / `Rewarded_iOS`).

> Unity está migrando Unity Ads hacia **LevelPlay** (mediación). Para usar LevelPlay basta con crear un `LevelPlayProvider : AdsProviderBase` y añadirlo en `AdsManager.CreateProvider`.

## Ajustes de economía publicitaria (`EconomyConfig`)

| Campo | Defecto | Para qué |
|---|---|---|
| `adRewardDiamonds` / `adRewardCoins` | 10 / 100 | Cantidad por anuncio |
| `maxDiamondAdsPerDay` / `maxCoinAdsPerDay` | 5 / 10 | Evita inflar la moneda premium y cumple políticas de redes |
| `menuAdCooldownSeconds` | 60 | Tiempo mínimo entre anuncios de moneda |
| `maxRevivesPerMatch` | 1 | Revivir con anuncio por partida |
| `reviveHealthPercent` / `reviveCountdownSeconds` | 1 / 6 | Vida al revivir y tiempo de decisión |

Los contadores diarios se guardan en `SaveData.ads` y se reinician con el cambio de día (hora local).

## Buenas prácticas incluidas

- Los botones de compra nunca se deshabilitan por falta de saldo: muestran el aviso y hacen palpitar el botón de anuncio de la tienda (empuje suave, no intrusivo).
- El botón de anuncio muestra *"Cargando anuncio..."*, *"Disponible en 45s"* o *"Límite diario alcanzado"* en lugar de fallar en silencio.
- La recompensa solo se entrega con el resultado `Rewarded` (si el usuario cierra antes, no hay premio).
- Al volver de segundo plano durante una partida, el juego queda en pausa (salvo si la causa fue un anuncio).
