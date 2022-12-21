import Foundation

public struct Release {
    let scriptPath: String
    let stdout: FileHandle
    let stderr: FileHandle
    var task: Process

    public init(name: String, logPath: String? = nil, terminationHandler: ((Process) -> Void)? = nil) {
        scriptPath = Bundle.main.path(forResource: "rel/bin/\(name)", ofType: "")!

        if logPath != nil {
            let fm = FileManager.default

            if !fm.fileExists(atPath: logPath!) {
                fm.createFile(atPath: logPath!, contents: Data())
            }

            let handle = FileHandle(forUpdatingAtPath: logPath!)!
            handle.seekToEndOfFile()
            stdout = handle
            stderr = handle
        } else {
            stdout = FileHandle.standardOutput
            stderr = FileHandle.standardError
        }

        let task = Process()
        self.task = task
        task.launchPath = scriptPath
        task.arguments = ["start"]
        task.standardOutput = stdout
        task.standardError = stderr
        task.terminationHandler = terminationHandler
        try! task.run()
        DispatchQueue.global(qos: .userInteractive).async {
            task.waitUntilExit()
        }
    }

    public func terminate() {
        if task.isRunning {
            task.terminate()
        }
    }

    public func publish(
            _ name: String,
            _ data: String,
            terminationHandler: ((Process) -> Void)? = nil) {
        if match(name, pattern: "^[a-zA-Z0-9-_]+$") {
            let input = Pipe()
            input.fileHandleForWriting.write("\(data)\n".data(using: .utf8)!)
            let task = Process()
            task.launchPath = scriptPath
            task.arguments = ["rpc", "ElixirKit.__publish__(:\(name))"]
            task.standardInput = input
            task.terminationHandler = terminationHandler
            try! task.run()
            task.waitUntilExit()
        }
    }

    func match(_ string: String, pattern: String) -> Bool {
        let range = NSRange(location: 0, length: string.utf16.count)
        let regex = try! NSRegularExpression(pattern: pattern)
        return regex.firstMatch(in: string, options: [], range: range) != nil
    }
}
