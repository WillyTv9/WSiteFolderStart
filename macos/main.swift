import SwiftUI
import AppKit

@main
struct WSiteFolderStartApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var delegate

    var body: some Scene {
        WindowGroup {
            ContentView()
                .containerBackground(.ultraThinMaterial, for: .window)
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)
    }
}

// X rossa = chiude l'app. "Esci" dal Dock e Cmd+Q funzionano di default.
class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }
}

// MARK: - Interfaccia

struct ContentView: View {
    @State private var folderName = ""
    @State private var errorText = ""
    @FocusState private var focused: Bool

    var body: some View {
        ZStack {
            LiquidBackdrop()

            VStack(spacing: 16) {
                Image(nsImage: NSApp.applicationIconImage)
                    .resizable().frame(width: 68, height: 68)
                    .shadow(color: .black.opacity(0.2), radius: 8, y: 4)

                Text("App in esecuzione")
                    .font(.system(size: 22, weight: .semibold, design: .rounded))

                Text("Inserisci nome cartella principale")
                    .font(.system(size: 13))
                    .foregroundStyle(Color.primary.opacity(0.75))

                ZStack {
                    if folderName.isEmpty {
                        Text("es. MioSito")
                            .foregroundStyle(Color.primary.opacity(0.7))
                            .allowsHitTesting(false)
                    }
                    TextField("", text: $folderName)
                        .textFieldStyle(.plain)
                        .multilineTextAlignment(.center)
                        .focused($focused)
                        .onSubmit(scatena)
                }
                .padding(.vertical, 10).padding(.horizontal, 16)
                .frame(width: 250)
                .modifier(FakeGlass(shape: Capsule()))

                if !errorText.isEmpty {
                    Text(errorText).font(.caption).foregroundStyle(.red)
                }

                Button("Scatena", action: scatena)
                    .buttonStyle(GlassButtonStyle())
                    .disabled(folderName.trimmingCharacters(in: .whitespaces).isEmpty)
                    .padding(.top, 4)
            }
            .padding(32)
            .modifier(FakeGlass(shape: RoundedRectangle(cornerRadius: 36, style: .continuous)))
            .padding(28)
        }
        .frame(width: 400)
        .onAppear { focused = true }
    }

    func scatena() {
        let name = folderName
            .replacingOccurrences(of: "/", with: "-")
            .replacingOccurrences(of: ":", with: "-")
            .trimmingCharacters(in: .whitespaces)
        guard !name.isEmpty else { return }
        do {
            try Builder.create(named: name)
            NSApp.terminate(nil)
        } catch {
            errorText = "Errore: \(error.localizedDescription)"
        }
    }
}

// MARK: - Effetto "liquid glass" simulato (funziona anche su Sequoia)

/// Macchie di colore sfocate che si muovono lentamente dietro al vetro:
/// danno l'effetto di rifrazione/liquido.
struct LiquidBackdrop: View {
    @State private var move = false
    var body: some View {
        ZStack {
            Circle().fill(Color(red: 0.25, green: 0.75, blue: 0.80)).frame(width: 220)
                .offset(x: move ? -110 : -50, y: move ? -130 : -80)
            Circle().fill(Color(red: 0.88, green: 0.45, blue: 0.72)).frame(width: 200)
                .offset(x: move ? 110 : 60, y: move ? 80 : 130)
            Circle().fill(Color(red: 0.55, green: 0.50, blue: 0.92)).frame(width: 180)
                .offset(x: move ? 70 : 120, y: move ? -110 : -60)
        }
        .blur(radius: 55)
        .opacity(0.55)
        .onAppear {
            withAnimation(.easeInOut(duration: 7).repeatForever(autoreverses: true)) { move = true }
        }
    }
}

/// Vetro: materiale sfocato + riflesso diagonale + bordo luminoso + luce interna + ombra morbida.
struct FakeGlass<S: InsettableShape>: ViewModifier {
    let shape: S
    let tint: Color?
    @Environment(\.colorScheme) private var scheme

    init(shape: S, tint: Color? = nil) {
        self.shape = shape
        self.tint = tint
    }

    func body(content: Content) -> some View {
        let dark = scheme == .dark
        content
            .background(
                ZStack {
                    shape.fill(.ultraThinMaterial)
                    if let tint { shape.fill(tint.opacity(0.65)) }
                    shape.fill(LinearGradient(
                        colors: [.white.opacity(dark ? 0.16 : 0.42), .white.opacity(0.02)],
                        startPoint: .topLeading, endPoint: .bottomTrailing))
                }
            )
            .overlay(
                shape.strokeBorder(LinearGradient(
                    colors: [.white.opacity(dark ? 0.55 : 0.95),
                             .white.opacity(0.06),
                             .white.opacity(dark ? 0.28 : 0.55)],
                    startPoint: .topLeading, endPoint: .bottomTrailing), lineWidth: 1.2)
            )
            .overlay(
                shape.inset(by: 1.5)
                    .stroke(LinearGradient(colors: [.white.opacity(0.4), .clear],
                                           startPoint: .top, endPoint: .center), lineWidth: 2)
                    .blur(radius: 1.5)
                    .clipShape(shape)
            )
            .shadow(color: .black.opacity(dark ? 0.35 : 0.14), radius: 16, y: 9)
    }
}

struct GlassButtonStyle: ButtonStyle {
    @Environment(\.isEnabled) private var enabled
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .semibold))
            .foregroundStyle(.white)
            .padding(.horizontal, 34).padding(.vertical, 11)
            .modifier(FakeGlass(shape: Capsule(), tint: .blue))
            .scaleEffect(configuration.isPressed ? 0.94 : 1)
            .opacity(enabled ? 1 : 0.7)
            .animation(.spring(response: 0.3, dampingFraction: 0.6), value: configuration.isPressed)
    }
}

// MARK: - Creazione cartelle (sul Desktop)

enum Builder {
    static func create(named name: String) throws {
        let fm = FileManager.default
        let desktop = fm.urls(for: .desktopDirectory, in: .userDomainMask)[0]
        let root = desktop.appendingPathComponent(name, isDirectory: true)

        let files: [(String, String, String)] = [
            ("Html", "index.html", """
            <!DOCTYPE html>
            <html lang="it">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>\(name)</title>
              <link rel="stylesheet" href="../Css/style.css">
            </head>
            <body>
              <h1>\(name)</h1>
              <script src="../Js/script.js"></script>
            </body>
            </html>
            """),
            ("Css", "style.css", "/* Stili di \(name) */\n"),
            ("Php", "login.php", "<?php\n// Login di \(name)\n"),
            ("Js", "script.js", "// Script di \(name)\n"),
        ]

        for (folder, file, content) in files {
            let dir = root.appendingPathComponent(folder, isDirectory: true)
            try fm.createDirectory(at: dir, withIntermediateDirectories: true)
            let path = dir.appendingPathComponent(file)
            if !fm.fileExists(atPath: path.path) {
                try content.write(to: path, atomically: true, encoding: .utf8)
            }
        }
        try fm.createDirectory(at: root.appendingPathComponent("Img", isDirectory: true),
                               withIntermediateDirectories: true)
    }
}
