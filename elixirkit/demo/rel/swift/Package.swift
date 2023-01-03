// swift-tools-version: 5.7

import PackageDescription

let package = Package(
    name: "Demo",
    dependencies: [
        .package(name: "ElixirKit", path: "../../../elixirkit_swift")
    ],
    targets: [
        .executableTarget(
            name: "Demo",
            dependencies: ["ElixirKit"]
        )
    ]
)
