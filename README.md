<div align="center">
  <img src="https://github.com/Greedeks/PixelForge/blob/main/Assets/PixelForge.png" width="256" height="256" alt="PixelForge">

  # PixelForge

  Lossless image compression and metadata cleanup for Windows.

</div>

---

## What it does

PixelForge shrinks image files without touching pixel quality. Instead of re-encoding or lowering resolution, it strips out everything that doesn't change how the image looks — EXIF data, color profiles, thumbnails, and other embedded metadata that images quietly carry around. The result: smaller files, same visual quality.

It also includes a built-in **SVG → XAML** converter, inspired by [BerndK/SvgToXaml](https://github.com/BerndK/SvgToXaml). That project hasn't been updated in years, and PixelForge's converter takes a different approach to producing the XAML output, aiming for a leaner result.

## Status

🚧 Early development. UI and core pipeline are being built out - not yet ready for general use.
