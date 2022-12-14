// swift-tools-version:5.5
import PackageDescription

let package = Package(
    name: "ElixirKit",
    products: [
        .library(
            name: "ElixirKit",
            targets: ["ElixirKit"]
        )
    ],
    dependencies: [],
    targets: [
        .target(
            name: "ElixirKit",
            dependencies: []
        )
    ]
)
