import AppKit
import ElixirKit

class AppDelegate: NSObject, NSApplicationDelegate {
    private var release : ElixirKit.Release!
    private var window : NSWindow!
    private var button : NSButton!

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        /* let logPath = "\(NSHomeDirectory())/Library/Logs/Demo.log" */

        release = ElixirKit.Release(name: "app") { task in
            if task.terminationStatus != 0 {
                DispatchQueue.main.sync {
                    let alert = NSAlert()
                    alert.alertStyle = .critical
                    alert.messageText = "release exited with \(task.terminationStatus)"
                    /* alert.informativeText = "Logs available at: \(logPath)" */
                    alert.runModal()
                }
            }

            NSApp.terminate(nil)
        }

        let menuItemOne = NSMenuItem()
        menuItemOne.submenu = NSMenu(title: "Demo")
        menuItemOne.submenu?.items = [
            NSMenuItem(title: "Quit Demo", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        ]
        let menu = NSMenu()
        menu.items = [menuItemOne]
        NSApp.mainMenu = menu

        window = NSWindow(contentRect: NSMakeRect(0, 0, 200, 200),
                          styleMask: [.titled, .closable],
                          backing: .buffered,
                          defer: true)
        window.orderFrontRegardless()
        window.title = "Demo"
        window.center()
        NSApp.activate(ignoringOtherApps: true)

        button = NSButton(title: "Press me!", target: self, action: #selector(buttonPressed))
        window.contentView!.addSubview(button)
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ app: NSApplication) -> Bool {
        return true
    }

    @objc
    func buttonPressed() {
        release.publish("dbg", "button pressed!")
    }
}

let app = NSApplication.shared
app.setActivationPolicy(.regular)
let delegate = AppDelegate()
app.delegate = delegate
app.run()
