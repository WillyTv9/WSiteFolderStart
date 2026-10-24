import SwiftUI
import AppKit

@main
struct WSiteFolderStartApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var delegate
    @StateObject private var store = Store()

    var body: some Scene {
        WindowGroup("WSiteFolderStart") {
            ContentView()
                .environmentObject(store)
                .containerBackground(.ultraThinMaterial, for: .window)
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)

        Window("Preset e IDE", id: "presets") {
            PresetsWindow()
                .environmentObject(store)
                .containerBackground(.ultraThinMaterial, for: .window)
        }
        .defaultLaunchBehavior(.suppressed)
        .restorationBehavior(.disabled)
    }
}

// X rossa sulla finestra principale = chiude l'app. "Esci" dal Dock e Cmd+Q funzionano di default.
class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }

    func applicationDidFinishLaunching(_ notification: Notification) {
        NotificationCenter.default.addObserver(forName: NSWindow.willCloseNotification,
                                               object: nil, queue: .main) { note in
            if let window = note.object as? NSWindow, window.title == "WSiteFolderStart" {
                NSApp.terminate(nil)
            }
        }
    }
}

// MARK: - Finestra principale

struct ContentView: View {
    @EnvironmentObject var store: Store
    @Environment(\.openWindow) private var openWindow
    @State private var folderName = ""
    @State private var errorText = ""
    @FocusState private var focused: Bool

    private var idePath: String { store.selected.idePath ?? "" }

    private var nameIsEmpty: Bool {
        folderName.trimmingCharacters(in: .whitespaces).isEmpty
    }

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
                        .onSubmit { scatena() }
                }
                .padding(.vertical, 10).padding(.horizontal, 16)
                .frame(width: 250)
                .modifier(FakeGlass(shape: Capsule()))

                presetRow

                if !errorText.isEmpty {
                    Text(errorText).font(.caption).foregroundStyle(.red)
                }

                Button("Scatena") { scatena() }
                    .buttonStyle(GlassButtonStyle())
                    .disabled(nameIsEmpty)
                    .padding(.top, 4)
            }
            .padding(32)
            .modifier(FakeGlass(shape: RoundedRectangle(cornerRadius: 36, style: .continuous)))
            .padding(28)
        }
        .frame(width: 400)
        .onAppear { focused = true }
    }

    private var presetRow: some View {
        HStack(spacing: 8) {
            Menu {
                ForEach(store.presets) { p in
                    Button { store.selectedID = p.id } label: {
                        if p.id == store.selectedID {
                            Label(p.name, systemImage: "checkmark")
                        } else {
                            Text(p.name)
                        }
                    }
                }
            } label: {
                HStack(spacing: 6) {
                    Image(systemName: "square.stack.3d.up")
                    Text(store.selected.name).lineLimit(1)
                    Image(systemName: "chevron.up.chevron.down").font(.system(size: 9))
                }
                .font(.system(size: 13))
                .padding(.vertical, 8).padding(.horizontal, 14)
                .modifier(FakeGlass(shape: Capsule()))
            }
            .menuStyle(.borderlessButton)
            .menuIndicator(.hidden)
            .fixedSize()

            Button { openWindow(id: "presets") } label: {
                Image(systemName: "slider.horizontal.3")
                    .font(.system(size: 13))
                    .padding(9)
                    .modifier(FakeGlass(shape: Circle()))
            }
            .buttonStyle(.plain)
            .help("Gestisci preset e IDE")
        }
    }

    private func scatena() {
        let name = Builder.safe(folderName)
        guard !name.isEmpty, name != ".", name != ".." else { return }
        do {
            let root = try Builder.create(named: name, preset: store.selected)
            if !idePath.isEmpty {
                let config = NSWorkspace.OpenConfiguration()
                NSWorkspace.shared.open([root],
                                        withApplicationAt: URL(fileURLWithPath: idePath),
                                        configuration: config) { _, error in
                    DispatchQueue.main.async {
                        if error == nil {
                            NSApp.terminate(nil)
                        } else {
                            errorText = "Cartella creata, ma non riesco ad aprire l'IDE. Controlla la scelta nel preset."
                        }
                    }
                }
            } else {
                NSApp.terminate(nil)
            }
        } catch {
            errorText = "Errore: \(error.localizedDescription)"
        }
    }
}

// MARK: - Effetto "liquid glass" simulato (funziona anche su Sequoia)

/// Macchie di colore sfocate che si muovono lentamente dietro al vetro.
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
    let tint: Color?
    @Environment(\.isEnabled) private var enabled

    init(tint: Color? = .blue) {
        self.tint = tint
    }

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .semibold))
            .foregroundStyle(tint == nil ? Color.primary : Color.white)
            .padding(.horizontal, 34).padding(.vertical, 11)
            .modifier(FakeGlass(shape: Capsule(), tint: tint))
            .scaleEffect(configuration.isPressed ? 0.94 : 1)
            .opacity(enabled ? 1 : 0.7)
            .animation(.spring(response: 0.3, dampingFraction: 0.6), value: configuration.isPressed)
    }
}
