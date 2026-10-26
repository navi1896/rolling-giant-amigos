## 2.1.2

- Cooldown de transición en puertas de entrada y salida:
  - Se añadió un tiempo de espera de 2.5 segundos (configurable en BepInEx/LethalConfig) para que el Rolling Giant tarde lo mismo que un jugador en cruzar entre el interior y exterior de las instalaciones.
  - Reproduce el sonido de apertura/rechinido de la puerta al iniciar la transición.
  - Evita muertes instantáneas injustas al salir o entrar de la fábrica otorgando tiempo al jugador para reaccionar y alejarse del umbral.
- Ajuste del tiempo de reacción en la emboscada de la nave:
  - Se redujo el tiempo de pausa congelado al ser detectado en la puerta de 6 segundos a 4 segundos (configurable vía `SeenPauseSeconds`), haciendo la emboscada más rápida y tensa.
- Entrance transition cooldown & ambush tuning:
  - Added a 2.5s cooldown delay (configurable) so the Giant takes the same time as a player to transition through facility entrance/exit doors in both directions.
  - Plays the door creak/open audio cue when the transition begins.
  - Reduced ship ambush detection pause from 6.0s to 4.0s for a faster and tenser jump.

## 2.1.1

- Ajustes de tensión y volumen para la emboscada en la nave:
  - El monstruo se queda quieto en la puerta mirándote al detectarlo antes de avanzar.
  - Violín tétrico mucho más audible: inicia al 60% de volumen y va aumentando hasta el punto máximo conforme se acerca.
  - El sonido se corta de golpe en cuanto el monstruo desaparece.
- Ship ambush timing & audio improvements:
  - Monster stays frozen upon detection before charging.
  - Louder scary violin starting at 60% volume and crescendoing to maximum as it gets closer.
  - Sound cuts off immediately when the monster despawns.

## 2.1.0

- Audio y atmósfera mejorada para la interacción sorpresa en la nave:
  - Sonido de foco reventado cuando se apaga la luz.
  - Violín tétrico exclusivo en audífonos para la víctima al ver al monstruo.
  - Velocidad de aproximación reducida al 60% para mayor tensión cinematográfica.

## 2.0.0

- Corrección de bugs al salir y entrar de las instalaciones y mejoras de estabilidad.
- Hay una sorpresa más esperando para los jugadores que están en la nave.

## 1.1.0

- Compatibility and gameplay updates for Lethal Company V81.
