# BBGreenTimer

This is a .NET Framework 4.7.2 class library intended for use with the game **BETON BRUTAL**.  

## Requirements

To successfully build this project, the following libraries must be placed in the `Lib` folder:

- `0Harmony.dll`
- `Assembly-CSharp.dll`  – Extracted from the game (BETON BRUTAL)
- `BBModMenu.dll`  – Built from the BBModMenu project
- `MelonLoader.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `UnityEngine.UIElementsModule.dll`

> 💡 **All DLLs should be placed in the `Lib` folder at the root of the project to ensure successful compilation.**

## Build Output

The output of the project is a DLL file:
- Located in `bin\Debug\` or `bin\Release\` depending on the build configuration.
- After build, the DLL is **automatically copied** to the game's mod directory:

