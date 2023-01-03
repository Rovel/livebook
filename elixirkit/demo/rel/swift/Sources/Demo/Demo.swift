import Foundation
import ElixirKit

@main
struct Demo {
    public static func main() {
        signal(SIGINT) { signal in
            ElixirKit.API.stop()
            exit(signal)
        }

        ElixirKit.API.start(name: "demo")
        ElixirKit.API.publish("log", "Hello from Swift!")
        ElixirKit.API.waitUntilExit()
    }
}
