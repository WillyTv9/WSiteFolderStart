// WSiteFolderStart - app nativa macOS (JavaScript for Automation + Cocoa)
// Nessuna dipendenza: usa solo ciò che c'è già in macOS.

ObjC.import('Cocoa');
ObjC.import('stdlib');

var APP_NAME = 'WSiteFolderStart';
var W = 440, H = 520;

var RES, app, delegate, defaults;
var mainWin, settingsWin, bgView, bgLight, bgDark, darkShown = null;
var field, button, errorLabel, captionButton;
var popup, destPathLabel, cbClose, cbReveal, cbTemplate;

// segnali per il launcher: "ok" = finestra mostrata, "quit" = chiusura normale
function mark(n) {
  var dir = String(ObjC.unwrap($.NSHomeDirectory())) + '/Library/Logs';
  $.system('touch ' + q(dir + '/WSiteFolderStart.' + n));
}

/* ------------------------------------------------------------------ */
/* Utilità                                                             */
/* ------------------------------------------------------------------ */

function call0(o, n) {            // chiama un metodo senza argomenti
  var r = o[n];
  if (typeof r === 'function') { return r.call(o); }
  return r;
}
function rect(x, y, w, h) { return $.NSMakeRect(x, y, w, h); }
function q(s) { return "'" + String(s).replace(/'/g, "'\\''") + "'"; }   // quote per shell
function safe(fn) {
  try { return fn(); }
  catch (e) { try { errorLabel.stringValue = 'Errore: ' + e.message; } catch (_) {} }
}

function mkLabel(text, x, y, w, h, size, weight, color, align) {
  var l = $.NSTextField.labelWithString(text);
  l.frame = rect(x, y, w, h);
  l.font = $.NSFont.systemFontOfSizeWeight(size, weight);
  l.textColor = color;
  l.alignment = align;
  return l;
}

/* ------------------------------------------------------------------ */
/* Impostazioni (salvate automaticamente)                              */
/* ------------------------------------------------------------------ */

function pref(key) { return String(ObjC.unwrap(defaults.stringForKey(key))); }
function prefBool(key) { return defaults.boolForKey(key) ? true : false; }

function destPath() {
  var home = String(ObjC.unwrap($.NSHomeDirectory()));
  var d = pref('dest');
  if (d === 'documents') { return home + '/Documents'; }
  if (d === 'downloads') { return home + '/Downloads'; }
  if (d === 'custom' && pref('customPath')) { return pref('customPath'); }
  return home + '/Desktop';
}
function destName() {
  var d = pref('dest');
  if (d === 'documents') { return 'Documenti'; }
  if (d === 'downloads') { return 'Download'; }
  if (d === 'custom' && pref('customPath')) {
    var parts = pref('customPath').split('/');
    return parts[parts.length - 1] || pref('customPath');
  }
  return 'Scrivania';
}
function updateCaption() {
  captionButton.title = 'Verrà creata in: ' + destName() + '   ›';
}

/* ------------------------------------------------------------------ */
/* Tema chiaro / scuro (segue il sistema)                              */
/* ------------------------------------------------------------------ */

function isDark() {
  var n = String(ObjC.unwrap(app.effectiveAppearance.name));
  if (n.indexOf('Dark') >= 0) { return true; }
  // secondo controllo: impostazione globale di macOS (vale anche per il tema automatico)
  var st = ObjC.unwrap($.NSUserDefaults.standardUserDefaults.stringForKey('AppleInterfaceStyle'));
  return String(st) === 'Dark';
}
function applyTheme() {
  var d = isDark();
  if (d === darkShown) { return; }
  darkShown = d;
  bgView.image = d ? bgDark : bgLight;
}

/* ------------------------------------------------------------------ */
/* Creazione della struttura                                           */
/* ------------------------------------------------------------------ */

function doCreate() {
  var raw = String(ObjC.unwrap(field.stringValue));
  var name = raw.replace(/[\/\\:]/g, '').replace(/^[\s.]+/, '').replace(/\s+$/, '');
  if (!name) {
    errorLabel.stringValue = 'Scrivi un nome per la cartella';
    mainWin.makeFirstResponder(field);
    return;
  }
  errorLabel.stringValue = '';

  var root = destPath() + '/' + name;
  var tpl = prefBool('template');
  var dirs = ['Html', 'Css', 'Php', 'Js', 'Img'];
  var files = [['Html', 'index.html'], ['Css', 'style.css'], ['Php', 'login.php'], ['Js', 'script.js']];

  var cmds = ['mkdir -p ' + dirs.map(function (d) { return q(root + '/' + d); }).join(' ')];
  files.forEach(function (f) {
    var dst = q(root + '/' + f[0] + '/' + f[1]);
    if (tpl) { cmds.push('(cp -n ' + q(RES + '/templates/' + f[1]) + ' ' + dst + ' 2>/dev/null || true)'); }
    else { cmds.push('touch ' + dst); }
  });

  var status = $.system(cmds.join(' && '));
  if (status !== 0) {
    errorLabel.stringValue = 'Impossibile creare la cartella (controlla i permessi)';
    return;
  }

  if (prefBool('reveal')) { $.system('open ' + q(root)); }

  button.title = 'Creata  ✓';
  button.enabled = false;
  $.NSTimer.scheduledTimerWithTimeIntervalTargetSelectorUserInfoRepeats(0.7, delegate, 'afterCreate:', null, false);
}

function afterCreate() {
  if (prefBool('closeAfter')) {
    app.terminate(null);
  } else {
    button.title = 'Scatena';
    button.enabled = true;
    field.stringValue = '';
    mainWin.makeFirstResponder(field);
  }
}

/* ------------------------------------------------------------------ */
/* Impostazioni: sincronizzazione UI <-> preferenze                    */
/* ------------------------------------------------------------------ */

function syncSettingsUI() {
  var idx = { desktop: 0, documents: 1, downloads: 2, custom: 3 }[pref('dest')];
  popup.selectItemAtIndex(idx === undefined ? 0 : idx);
  destPathLabel.stringValue = destPath();
  cbClose.state = prefBool('closeAfter') ? 1 : 0;
  cbReveal.state = prefBool('reveal') ? 1 : 0;
  cbTemplate.state = prefBool('template') ? 1 : 0;
}

function onDestChanged() {
  var idx = Number(popup.indexOfSelectedItem);
  if (idx === 3) {
    var p = $.NSOpenPanel.openPanel;
    p.canChooseFiles = false;
    p.canChooseDirectories = true;
    p.canCreateDirectories = true;
    p.prompt = 'Scegli';
    p.message = 'Scegli dove creare la cartella del sito';
    if (Number(call0(p, 'runModal')) === 1) {
      defaults.setObjectForKey(String(ObjC.unwrap(p.URL.path)), 'customPath');
      defaults.setObjectForKey('custom', 'dest');
    }
  } else {
    defaults.setObjectForKey(['desktop', 'documents', 'downloads'][idx], 'dest');
  }
  syncSettingsUI();
  updateCaption();
}

function onPrefChanged() {
  defaults.setBoolForKey(cbClose.state == 1, 'closeAfter');
  defaults.setBoolForKey(cbReveal.state == 1, 'reveal');
  defaults.setBoolForKey(cbTemplate.state == 1, 'template');
}

function openSettings() {
  if (!settingsWin) { buildSettingsWindow(); }
  syncSettingsUI();
  call0(settingsWin, 'center');
  settingsWin.makeKeyAndOrderFront(null);
  app.activateIgnoringOtherApps(true);
}

/* ------------------------------------------------------------------ */
/* Delegate (azioni dei bottoni, chiusura finestra, timer)             */
/* ------------------------------------------------------------------ */

ObjC.registerSubclass({
  name: 'WSFSDelegate',
  superclass: 'NSObject',
  methods: {
    'applicationShouldTerminateAfterLastWindowClosed:': {
      types: ['bool', ['id']],
      implementation: function (a) { return true; }
    },
    'applicationWillTerminate:': {
      types: ['void', ['id']],
      implementation: function (n) { mark('quit'); }
    },
    'windowWillClose:': {
      types: ['void', ['id']],
      implementation: function (n) {
        // chiudere la finestra principale con la X = chiudere l'app
        if (String(ObjC.unwrap(n.object.title)) === APP_NAME) { app.terminate(null); }
      }
    },
    'scatena:':      { types: ['void', ['id']], implementation: function (s) { safe(doCreate); } },
    'afterCreate:':  { types: ['void', ['id']], implementation: function (t) { safe(afterCreate); } },
    'openSettings:': { types: ['void', ['id']], implementation: function (s) { safe(openSettings); } },
    'destChanged:':  { types: ['void', ['id']], implementation: function (s) { safe(onDestChanged); } },
    'prefChanged:':  { types: ['void', ['id']], implementation: function (s) { safe(onPrefChanged); } },
    'late:':         { types: ['void', ['id']], implementation: function (t) { safe(buildLate); } },
    'tick:':         { types: ['void', ['id']], implementation: function (t) { safe(applyTheme); } }
  }
});

/* ------------------------------------------------------------------ */
/* Menu                                                                */
/* ------------------------------------------------------------------ */

function addItem(menu, title, sel, key, target, mask) {
  var it = $.NSMenuItem.alloc.initWithTitleActionKeyEquivalent(title, sel, key || '');
  if (target) { it.target = target; }
  if (mask) { it.keyEquivalentModifierMask = mask; }
  menu.addItem(it);
  return it;
}
function addSub(mainMenu, title) {
  var holder = $.NSMenuItem.alloc.init;
  var sub = $.NSMenu.alloc.initWithTitle(title);
  holder.submenu = sub;
  mainMenu.addItem(holder);
  return sub;
}

function buildMenu() {
  var CMD = 1048576, SHIFT = 131072;
  var mainMenu = $.NSMenu.alloc.initWithTitle('MainMenu');

  var appMenu = addSub(mainMenu, APP_NAME);
  addItem(appMenu, 'Informazioni su ' + APP_NAME, 'orderFrontStandardAboutPanel:', '', null);
  appMenu.addItem($.NSMenuItem.separatorItem);
  addItem(appMenu, 'Impostazioni…', 'openSettings:', ',', delegate);
  appMenu.addItem($.NSMenuItem.separatorItem);
  addItem(appMenu, 'Nascondi ' + APP_NAME, 'hide:', 'h', null);
  appMenu.addItem($.NSMenuItem.separatorItem);
  addItem(appMenu, 'Esci da ' + APP_NAME, 'terminate:', 'q', null);

  var fileMenu = addSub(mainMenu, 'File');
  addItem(fileMenu, 'Chiudi', 'performClose:', 'w', null);

  var editMenu = addSub(mainMenu, 'Modifica');
  addItem(editMenu, 'Annulla', 'undo:', 'z', null);
  addItem(editMenu, 'Ripeti', 'redo:', 'z', null, CMD | SHIFT);
  editMenu.addItem($.NSMenuItem.separatorItem);
  addItem(editMenu, 'Taglia', 'cut:', 'x', null);
  addItem(editMenu, 'Copia', 'copy:', 'c', null);
  addItem(editMenu, 'Incolla', 'paste:', 'v', null);
  addItem(editMenu, 'Seleziona tutto', 'selectAll:', 'a', null);

  var winMenu = addSub(mainMenu, 'Finestra');
  addItem(winMenu, 'Riduci a icona', 'performMiniaturize:', 'm', null);

  app.mainMenu = mainMenu;
}

/* ------------------------------------------------------------------ */
/* Finestra principale                                                 */
/* ------------------------------------------------------------------ */

function mkBox(x, y, w, h, radius, fill, border) {
  var b = $.NSBox.alloc.initWithFrame(rect(x, y, w, h));
  b.boxType = 4;               // personalizzato
  b.titlePosition = 0;         // senza titolo
  b.borderWidth = border ? 1 : 0;
  if (border) { b.borderColor = border; }
  b.cornerRadius = radius;
  b.fillColor = fill;
  b.contentViewMargins = $.NSMakeSize(0, 0);
  return b;
}

function buildMainWindow() {
  H = 520;
  mainWin = $.NSWindow.alloc.initWithContentRectStyleMaskBackingDefer(rect(0, 0, W, H), 1 | 2 | 4 | 32768, 2, false);
  mainWin.title = APP_NAME;
  mainWin.titlebarAppearsTransparent = true;
  mainWin.titleVisibility = 1;
  mainWin.movableByWindowBackground = true;
  mainWin.releasedWhenClosed = false;
  mainWin.delegate = delegate;
  var cv = mainWin.contentView;

  // sfondo (cambia da solo tra chiaro e scuro)
  bgLight = $.NSImage.alloc.initWithContentsOfFile(RES + '/bg_light.jpg');
  bgDark = $.NSImage.alloc.initWithContentsOfFile(RES + '/bg_dark.jpg');
  bgView = $.NSImageView.alloc.initWithFrame(rect(0, 0, W, H));
  bgView.imageScaling = 1;
  cv.addSubview(bgView);
  applyTheme();

  // logo, titolo, sottotitolo
  var iconView = $.NSImageView.alloc.initWithFrame(rect((W - 150) / 2, 362, 150, 150));
  iconView.image = $.NSImage.alloc.initWithContentsOfFile(RES + '/icon.png');
  iconView.imageScaling = 3;
  cv.addSubview(iconView);
  cv.addSubview(mkLabel('App in esecuzione', 0, 324, W, 34, 28, 0.3, $.NSColor.labelColor, 1));
  cv.addSubview(mkLabel('Prepara in un attimo la struttura del tuo sito', 0, 300, W, 18, 13, 0, $.NSColor.secondaryLabelColor, 1));

  // scheda "vetro" con il modulo (si adatta da sola a tema chiaro/scuro)
  cv.addSubview(mkBox(30, 84, 380, 200, 18,
    $.NSColor.windowBackgroundColor.colorWithAlphaComponent(0.58), $.NSColor.separatorColor));

  cv.addSubview(mkLabel('Inserisci nome cartella principale', 52, 250, 336, 16, 12, 0.23, $.NSColor.secondaryLabelColor, 0));
  field = $.NSTextField.alloc.initWithFrame(rect(52, 216, 336, 30));
  field.placeholderString = 'es. MioSito';
  field.font = $.NSFont.systemFontOfSizeWeight(15, 0);
  field.bezelStyle = 1;
  cv.addSubview(field);

  errorLabel = mkLabel('', 30, 196, 380, 14, 11, 0, $.NSColor.systemRedColor, 1);
  cv.addSubview(errorLabel);

  button = $.NSButton.alloc.initWithFrame(rect(52, 146, 336, 38));
  button.title = 'Scatena';
  button.bezelStyle = 1;
  button.controlSize = 3;
  button.font = $.NSFont.systemFontOfSizeWeight(16, 0.3);
  button.bezelColor = $.NSColor.colorWithSRGBRedGreenBlueAlpha(0.13, 0.55, 0.60, 1);
  button.keyEquivalent = '\r';
  button.target = delegate;
  button.action = 'scatena:';
  cv.addSubview(button);

  // anteprima di cosa verrà creato: cinque "etichette" colorate
  var chips = [['Html', $.NSColor.systemOrangeColor], ['Css', $.NSColor.systemBlueColor],
               ['Php', $.NSColor.systemPurpleColor], ['Js', $.NSColor.systemYellowColor],
               ['Img', $.NSColor.systemGreenColor]];
  var cw = 58, gap = 11, x0 = 52 + (336 - (5 * cw + 4 * gap)) / 2;
  chips.forEach(function (c, i) {
    var x = x0 + i * (cw + gap);
    cv.addSubview(mkBox(x, 104, cw, 24, 12, c[1].colorWithAlphaComponent(0.22), null));
    cv.addSubview(mkLabel(c[0], x, 108, cw, 16, 12, 0.23, $.NSColor.labelColor, 1));
  });

  // destinazione (cliccabile: apre le impostazioni)
  captionButton = $.NSButton.alloc.initWithFrame(rect(60, 56, 320, 20));
  captionButton.bordered = false;
  captionButton.font = $.NSFont.systemFontOfSizeWeight(12, 0);
  captionButton.contentTintColor = $.NSColor.secondaryLabelColor;
  captionButton.toolTip = 'Cambia le impostazioni';
  captionButton.target = delegate;
  captionButton.action = 'openSettings:';
  cv.addSubview(captionButton);
  updateCaption();

  // ingranaggio
  var gear = $.NSButton.alloc.initWithFrame(rect(W - 50, H - 72, 30, 30));
  var sym = $.NSImage.imageWithSystemSymbolNameAccessibilityDescription('gearshape.fill', 'Impostazioni');
  if (sym) { gear.image = sym; } else { gear.title = '⚙︎'; }
  gear.bordered = false;
  gear.contentTintColor = $.NSColor.secondaryLabelColor;
  gear.toolTip = 'Impostazioni  (⌘,)';
  gear.target = delegate;
  gear.action = 'openSettings:';
  cv.addSubview(gear);

  call0(mainWin, 'center');
  mainWin.makeKeyAndOrderFront(null);
  mainWin.makeFirstResponder(field);
}

// cose non necessarie alla prima schermata: costruite dopo che la finestra è già visibile
function buildLate() {
  buildMenu();
  buildSettingsWindow();
  syncSettingsUI();
}

/* ------------------------------------------------------------------ */
/* Finestra Impostazioni                                               */
/* ------------------------------------------------------------------ */

function buildSettingsWindow() {
  var SW = 420, SH = 320;
  function T(top, h) { return SH - top - h; }

  settingsWin = $.NSWindow.alloc.initWithContentRectStyleMaskBackingDefer(rect(0, 0, SW, SH), 1 | 2 | 4, 2, false);
  settingsWin.title = 'Impostazioni';
  settingsWin.releasedWhenClosed = false;
  var cv = settingsWin.contentView;

  function section(text, top) {
    cv.addSubview(mkLabel(text, 24, T(top, 16), 372, 16, 12, 0.4, $.NSColor.labelColor, 0));
  }
  function separator(top) {
    var s = $.NSBox.alloc.initWithFrame(rect(24, T(top, 1), 372, 1));
    s.boxType = 2;
    cv.addSubview(s);
  }
  function checkbox(title, top) {
    var c = $.NSButton.checkboxWithTitleTargetAction(title, delegate, 'prefChanged:');
    c.frame = rect(24, T(top, 18), 372, 18);
    cv.addSubview(c);
    return c;
  }

  section('Crea la cartella in', 22);
  popup = $.NSPopUpButton.alloc.initWithFramePullsDown(rect(24, T(44, 26), 250, 26), false);
  ['Scrivania', 'Documenti', 'Download', 'Altra cartella…'].forEach(function (t) { popup.addItemWithTitle(t); });
  popup.target = delegate;
  popup.action = 'destChanged:';
  cv.addSubview(popup);
  destPathLabel = mkLabel('', 24, T(76, 14), 372, 14, 11, 0, $.NSColor.tertiaryLabelColor, 0);
  destPathLabel.lineBreakMode = 5;           // tronca nel mezzo
  cv.addSubview(destPathLabel);

  separator(102);

  section('Dopo la creazione', 116);
  cbClose = checkbox('Chiudi l’app', 140);
  cbReveal = checkbox('Mostra la cartella nel Finder', 164);

  separator(194);

  section('File di partenza', 208);
  cbTemplate = checkbox('Inserisci un contenuto base nei file', 232);
  cv.addSubview(mkLabel('Se disattivato, i file vengono creati vuoti.', 42, T(254, 14), 354, 14, 11, 0, $.NSColor.tertiaryLabelColor, 0));

  cv.addSubview(mkLabel('Le modifiche si salvano automaticamente.', 0, T(292, 14), SW, 14, 11, 0, $.NSColor.tertiaryLabelColor, 1));
}

/* ------------------------------------------------------------------ */
/* Avvio                                                               */
/* ------------------------------------------------------------------ */

function startApp() {
  console.log('1 avvio');
  $.NSProcessInfo.processInfo.setProcessName(APP_NAME);

  defaults = $.NSUserDefaults.alloc.initWithSuiteName('local.wsitefolderstart');
  defaults.registerDefaults($({ dest: 'desktop', customPath: '', closeAfter: true, reveal: false, template: false }));

  app = $.NSApplication.sharedApplication;
  app.setActivationPolicy(0);
  delegate = $.WSFSDelegate.alloc.init;
  app.delegate = delegate;
  console.log('2 app ok');

  buildMainWindow();                      // prima la finestra: si vede subito
  console.log('3 finestra ok');

  var appIcon = $.NSImage.alloc.initWithContentsOfFile(RES + '/icon_dock.png');
  if (appIcon) { app.applicationIconImage = appIcon; }

  app.activateIgnoringOtherApps(true);
  mark('ok');

  // il resto (menu, impostazioni, controllo tema) subito dopo
  $.NSTimer.scheduledTimerWithTimeIntervalTargetSelectorUserInfoRepeats(0.05, delegate, 'late:', null, false);
  $.NSTimer.scheduledTimerWithTimeIntervalTargetSelectorUserInfoRepeats(1.0, delegate, 'tick:', null, true);
  console.log('4 in esecuzione');
  call0(app, 'run');
}

function run(argv) {
  RES = argv[0];
  try { startApp(); }
  catch (e) {
    console.log('ERRORE: ' + e + ' (riga ' + e.line + ')');
    $.exit(1);
  }
}
