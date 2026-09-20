# Assignment1 AR iOS Build

This Unity 6000.3 project is configured for one iOS AR app that combines both workshop requirements:

- surface tracking with plane detection and tap-to-place spheres
- image tracking with a rotating cube on the `hiro.png` marker

## Important Paths

- Combined scene: `Assets/CombinedLab.unity`
- iOS setup and export automation: `Assets/Editor/IOSBuildSetup.cs`
- Xcode export packager: `Tools/Package-Xcode.ps1`
- GitHub Actions unsigned IPA builder: `.github/workflows/ios-unsigned.yml`

## Local Export

From the Unity editor, run `Assignment -> Configure Combined iOS AR Build`, then `Assignment -> Build iOS Xcode Export`.

From PowerShell, the same export can be run with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Build-iOS-Xcode.ps1
```

Package the exported Xcode folder with:

```powershell
powershell.exe -NoProfile -File .\Tools\Package-Xcode.ps1 `
  -ExportDirectory '.\Builds\iOS-Xcode' `
  -ZipPath '.\Exports\export-001\xcode-export.zip'
```

Upload `xcode-export.zip` as a GitHub release asset, then run the `iOS unsigned build` workflow with that release tag. The produced unsigned IPA can be signed and installed on Windows with Sideloadly.

## Versions

- Unity Editor: 6000.3.11f1
- AR Foundation: 6.3.5
- Apple ARKit XR Plug-in: 6.3.5
- Input System: 1.19.0
- Minimum iOS target: 15.0

## Notes

`hiro.png` is registered in `MyMarkers.asset` with a default physical width of 0.16 m. Adjust that size in Unity if your printed marker uses a different width.
