// swift-tools-version:5.5
import PackageDescription

let package = Package(
    name: "Demo",
    platforms: [
        .macOS(.v11)
    ],
    dependencies: [],
    targets: [
        .executableTarget(
            name: "Demo",
            dependencies: []
        )
    ]
)
