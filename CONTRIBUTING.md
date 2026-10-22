# Contribuire

Grazie per l'interesse! Il progetto è volutamente piccolo e **senza dipendenze**: per favore, mantienilo così.

## Branch

- `main` — app macOS, sempre funzionante.
- `windows` — app Windows. Le risorse condivise (`assets/`, `templates/`) restano allineate a `main`:
  quando cambiano su `main`, fai `git merge main` dentro `windows`.
- Per le modifiche crea un branch dedicato: `feature/nome-breve` oppure `fix/nome-breve`,
  poi apri una Pull Request verso il branch di riferimento.

## Provare in locale

**macOS**
```bash
./macos/build.sh && open build/WSiteFolderStart.app
```
Se qualcosa non parte, guarda `~/Library/Logs/WSiteFolderStart.log`.

**Windows** (branch `windows`)
```powershell
powershell -STA -ExecutionPolicy Bypass -File windows\WSiteFolderStart.ps1
```
Se qualcosa non parte, guarda `%LOCALAPPDATA%\WSiteFolderStart\WSiteFolderStart.log`.

## Commit

Messaggi brevi, all'imperativo, in italiano o inglese (ma coerenti nella stessa PR):
`Aggiungi opzione X`, `Correggi crash all'avvio`, `Aggiorna sfondi`.

## Prima di aprire una PR

- Ho provato l'app davvero (non solo letto il codice).
- Ho aggiornato `CHANGELOG.md` se la modifica si vede dall'utente.
- Nessuna nuova dipendenza.
