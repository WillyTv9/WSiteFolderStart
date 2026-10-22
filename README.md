<div align="center">

<img src="assets/icon.png" alt="WSiteFolderStart" width="140">

# WSiteFolderStart

**Prepara in un attimo la struttura del tuo sito.**
Scrivi un nome, premi *Scatena*, e la cartella di progetto è pronta.

![macOS 11+](https://img.shields.io/badge/macOS-11%2B-111?logo=apple&logoColor=white)
![Liquid Glass](https://img.shields.io/badge/Liquid%20Glass-macOS%2026-218c99)
![Zero dipendenze](https://img.shields.io/badge/dipendenze-zero-success)
![Licenza MIT](https://img.shields.io/badge/licenza-MIT-blue)

</div>

<!-- Quando hai uno screenshot, salvalo in docs/screenshot.png e togli i commenti qui sotto:
<p align="center"><img src="docs/screenshot.png" width="460" alt="Schermata principale"></p>
-->

## Cosa fa

Inserisci il nome della cartella principale e WSiteFolderStart crea questa struttura:

```
MioSito/
├── Html/
│   └── index.html
├── Css/
│   └── style.css
├── Php/
│   └── login.php
├── Js/
│   └── script.js
└── Img/
```

I file possono essere **vuoti** oppure partire da un **contenuto base** (pagina HTML collegata a CSS e JS,
form di login PHP con sessione, ecc.). Non sovrascrive mai file già esistenti.

## Caratteristiche

- **Liquid Glass** — su macOS 26 (Tahoe) la scheda e il pulsante usano il vetro vero di Apple
  (`NSGlassEffectView`). Sui sistemi precedenti l'app ripiega da sola su un vetro sfocato classico.
- **Tema chiaro/scuro** automatico, con sfondi dedicati.
- **Impostazioni** (`⌘,`): scegli dove creare la cartella (Scrivania, Documenti, Download o una cartella a piacere),
  se chiudere l'app dopo la creazione, se mostrarla nel Finder, se inserire il contenuto base nei file.
- **Zero dipendenze**: è fatta solo con ciò che c'è già in macOS (JavaScript for Automation + Cocoa).
  Niente Xcode, niente Node, niente Electron.
- **Rete di sicurezza**: se l'interfaccia completa non dovesse partire, compare una finestra semplice ma
  funzionante e il motivo viene salvato in `~/Library/Logs/WSiteFolderStart.log`.

## Installazione

1. Scarica `WSiteFolderStart-macOS.zip` dalla pagina [Releases](../../releases) e decomprimilo.
2. Trascina `WSiteFolderStart.app` in **Applicazioni**.
3. **Alla prima apertura** fai *tasto destro → Apri* (l'app non è notarizzata da Apple).
   In alternativa, da Terminale:
   ```bash
   xattr -dr com.apple.quarantine /Applications/WSiteFolderStart.app
   ```

> Alla prima creazione macOS può chiedere il permesso di accedere a Scrivania / Documenti / Download:
> è normale, serve proprio a creare la cartella lì.

## Compilare dai sorgenti

Serve solo un Mac:

```bash
git clone <url-della-repo>
cd WSiteFolderStart
./macos/build.sh          # crea build/WSiteFolderStart.app
./macos/build.sh --zip    # in più crea build/WSiteFolderStart-macOS.zip
open build/WSiteFolderStart.app
```

## Struttura della repo

```
.
├── assets/          icone e sfondi (condivisi da tutte le piattaforme)
├── templates/       contenuto base dei file creati (index.html, style.css, ...)
├── macos/
│   ├── main.js      l'app: interfaccia nativa in JavaScript for Automation + Cocoa
│   ├── launcher     eseguibile del bundle: avvia main.js e fa da "guardiano"
│   ├── fallback.applescript   finestra di emergenza
│   ├── Info.plist
│   └── build.sh     assembla il .app
├── docs/
├── .github/         CI, release automatiche, template per issue e PR
├── VERSION
└── CHANGELOG.md
```

## Branch

| Branch    | Contenuto                                                        |
|-----------|------------------------------------------------------------------|
| `main`    | App per **macOS** (versione stabile)                             |
| `windows` | App per **Windows 10/11**, identica nell'aspetto e nelle funzioni |

## Contribuire

Vedi [CONTRIBUTING.md](CONTRIBUTING.md). Le novità di ogni versione sono nel [CHANGELOG](CHANGELOG.md).

## Licenza

[MIT](LICENSE)
