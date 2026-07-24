# Espluque Module Templates

`Espluque.Templates` provides .NET project templates for creating modules inside the Espluque source repository.

The templates generate the standard module structure, project references, deployment target, and a minimal working implementation showing how an Espluque contribution is built.

## Available templates

### Detector module

Creates a minimal detector module implementing `IDetector`.

```powershell
dotnet new espluque-detector -n MyDetector
```

The generated project includes:

* references to `Espluque.Contracts` and `Espluque.ModuleCommons`;
* a minimal `Detector` implementation;
* a `module.json` manifest;
* automatic deployment to the Espluquer module output directory.

### Grabber module

Creates a minimal grabber module implementing the Espluque grabber contract.

```powershell
dotnet new espluque-grabber -n MyGrabber
```

### Viewer module

Creates a minimal WPF viewer module implementing the Espluque viewer contract.

```powershell
dotnet new espluque-viewer -n MyViewer
```

## Installation

Build the template package:

```powershell
dotnet pack
```

Install the generated package:

```powershell
dotnet new install .\bin\Release\Espluque.Templates.1.0.0.nupkg
```

To reinstall a modified local version:

```powershell
dotnet new uninstall Espluque.Templates
dotnet new install .\bin\Release\Espluque.Templates.1.0.0.nupkg
```

## Usage

Run the template command from the repository `Modules` directory:

```powershell
cd Modules
dotnet new espluque-detector -n MyDetector
```

This generates the standard Espluque module structure:

```text
Modules
└── MyDetector
    └── MyDetector
        ├── MyDetector.csproj
        ├── Detector.cs
        └── module.json
```

Projects generated under `Modules` automatically use `Modules\Directory.Build.props` to resolve shared Espluque project paths and the Espluquer module output directory.

## Template philosophy

The generated code is intentionally small but functional.

Each template is designed to:

* compile immediately;
* expose the services available to the module;
* show the expected contribution contract;
* provide a minimal implementation that can be modified directly;
* reduce the need to read the full Espluque documentation before starting.

The example values and detection logic are placeholders and must be adapted to the module being implemented.

## Repository

Espluque is available at:

`https://github.com/alamasonthe/Espluque`
