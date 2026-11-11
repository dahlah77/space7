// Space7: original app and menu icon assets for the GPL fork.
import AppKit

func image(width: Int, height: Int, draw: (NSRect) -> Void) -> Data {
    guard let bitmap = NSBitmapImageRep(
        bitmapDataPlanes: nil, pixelsWide: width, pixelsHigh: height,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true,
        isPlanar: false, colorSpaceName: .deviceRGB,
        bytesPerRow: 0, bitsPerPixel: 0
    ), let context = NSGraphicsContext(bitmapImageRep: bitmap) else {
        fatalError("Cannot create Space7 image")
    }
    let bounds = NSRect(x: 0, y: 0, width: width, height: height)
    bitmap.size = bounds.size
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = context
    NSColor.clear.setFill()
    bounds.fill()
    draw(bounds)
    NSGraphicsContext.restoreGraphicsState()
    guard let output = bitmap.representation(using: .png, properties: [:]) else {
        fatalError("Cannot encode Space7 image")
    }
    return output
}

func wordMark(in rect: NSRect, size: CGFloat, color: NSColor) {
    let text = "S7" as NSString
    let font = NSFont.systemFont(ofSize: size, weight: .black)
    let attr: [NSAttributedString.Key: Any] = [
        .font: font,
        .foregroundColor: color,
        .kern: -10
    ]
    let measured = text.size(withAttributes: attr)
    let center = NSRect(
        x: rect.midX - measured.width / 2,
        y: rect.midY - measured.height / 2 - 20,
        width: measured.width, height: measured.height
    )
    text.draw(in: center, withAttributes: attr)
}

let root = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
let brand = root.appendingPathComponent("Resources/Brand")
let icon = image(width: 1024, height: 1024) { rect in
    let card = NSBezierPath(roundedRect: rect.insetBy(dx: 38, dy: 38), xRadius: 215, yRadius: 215)
    let gradient = NSGradient(colorsAndLocations:
        (NSColor(calibratedRed: 0.035, green: 0.075, blue: 0.15, alpha: 1), 0),
        (NSColor(calibratedRed: 0.12, green: 0.20, blue: 0.44, alpha: 1), 1)
    )!
    gradient.draw(in: card, angle: 125)
    let ring = NSBezierPath(ovalIn: rect.insetBy(dx: 145, dy: 145))
    ring.lineWidth = 12
    NSColor(calibratedRed: 0.29, green: 0.91, blue: 0.99, alpha: 0.23).setStroke()
    ring.stroke()
    wordMark(in: rect, size: 425, color: NSColor(calibratedRed: 0.32, green: 0.94, blue: 0.99, alpha: 1))
}
try icon.write(to: brand.appendingPathComponent("AppIcon-Default.png"))
let logo = image(width: 1024, height: 512) { rect in
    wordMark(in: rect, size: 365, color: NSColor.black)
}
try logo.write(to: brand.appendingPathComponent("logo.png"))
print("Space7 original icon assets generated")
