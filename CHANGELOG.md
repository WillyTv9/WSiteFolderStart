# Changelog

Il formato segue [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/)
e il versionamento è [SemVer](https://semver.org/lang/it/).

## [2.1.0] - 2026-10-22

### Aggiunto
- **Liquid Glass** su macOS 26 (Tahoe): la scheda del modulo usa `NSGlassEffectView`
  e il pulsante principale lo stile vetro di sistema.
- Ripiego automatico su `NSVisualEffectView` (vetro sfocato) per macOS 11–15.
- Nuovi sfondi chiaro/scuro più ricchi di colore, così il vetro ha qualcosa da rifrangere.
- Struttura della repo: script di build (`macos/build.sh`), CI, release automatiche.

### Modificato
- I controlli del modulo ora stanno dentro la scheda di vetro (coordinate relative).

## [2.0.0]

- Versione iniziale importata: interfaccia nativa in JavaScript for Automation,
  tema chiaro/scuro, impostazioni salvate, finestra di emergenza.
