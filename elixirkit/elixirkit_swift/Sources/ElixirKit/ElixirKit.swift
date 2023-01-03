import Foundation

public class API {
    static var relScript: String?
    static var process: Process?
    static var handle: FileHandle?
    private static var release: Release?

    public static func start(name: String, terminationHandler: ((Process) -> Void)? = nil) {
        release = Release(name: name, terminationHandler: terminationHandler)
    }

    public static func publish(_ name: String, _ data: String) {
        release!.publish(name, data)
    }

    public static func waitUntilExit() {
        release!.waitUntilExit();
    }

    public static func stop() {
        release!.stop();
    }
}

private class Release {
    var relScript: String
    var startProcess: Process
    var handle: FileHandle

    init(name: String, terminationHandler: ((Process) -> Void)? = nil) {
        let bundle = Bundle.main

        if bundle.bundlePath.hasSuffix(".app") {
            relScript = "\(bundle.bundlePath)/Contents/Resources/rel/bin/\(name)"
        }
        else {
            relScript = "\(bundle.bundlePath)/rel/bin/\(name)"
        }

        startProcess = Process()

        handle = FileHandle()
        var env = ProcessInfo.processInfo.environment
        let pipePath = pipePath()
        makePipe(path: pipePath)
        env["ELIXIRKIT_PIPE_PATH"] = pipePath
        startProcess.environment = env

        startProcess.launchPath = relScript
        startProcess.arguments = ["start"]
        startProcess.terminationHandler = terminationHandler
        try! startProcess.run()

        handle = FileHandle(forWritingAtPath: pipePath)!
    }

    public func publish(_ name: String, _ data: String) {
        let encoded = data.data(using: .utf8)!.base64EncodedString()
        let message = "event:\(name):\(encoded)\n"
        handle.write(message.data(using: .utf8)!)
    }

    public func waitUntilExit() {
        startProcess.waitUntilExit()
    }

    public func stop() {
        if !startProcess.isRunning {
            return;
        }

        let process = Process()
        process.launchPath = relScript
        process.arguments = ["stop"]
        try! process.run()
        process.waitUntilExit()
    }

    private func pipePath() -> String {
        let bundle = Bundle.main
        let id = bundle.infoDictionary!["CFBundleIdentifier"] ?? (bundle.executablePath! as NSString).lastPathComponent
        return "/tmp/elixirkit.\(id).fifo"
    }

    private func makePipe(path: String) {
        let fm = FileManager.default
        if fm.fileExists(atPath: path) {
            try! fm.removeItem(atPath: path)
        }
        let result = mkfifo(path, 0o600)
        if result < 0 {
            print("mkfifo \(path) failed with \(result)")
            exit(1)
        }
    }
}
