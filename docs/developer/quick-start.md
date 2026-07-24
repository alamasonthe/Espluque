[Documentation](../README.md) · **Developer quick start** · [Developer how-to](how-to.md) · [Developer concepts](concepts.md)

# Developer quick start

## Install the module templates

Install the template package:

```powershell
dotnet new install <path-to>/Espluque.Templates.nupkg
```

The package provides three project templates:

- `espluque-detector`
- `espluque-grabber`
- `espluque-viewer`

## Create a module

Create a detector module:

```powershell
dotnet new espluque-detector -n MyDetectorModule
```

Create a grabber module:

```powershell
dotnet new espluque-grabber -n MyGrabberModule
```

Create a viewer module:

```powershell
dotnet new espluque-viewer -n MyViewerModule
```

Each template contains the minimum structure required for one contribution type.

## Implement the contribution

Open the generated project and implement the contribution method provided by its contract:

- `Detect` for a detector;
- `Grab` for a grabber;
- `GetViewer` for a WPF viewer.

## Configure the module

Update `module.json` with:

- the module name and version;
- the generated assembly name;
- the contribution interface;
- the implementing class;
- the thesaurus tags associated with the contribution.

Example:

```json
{
  "name": "My module",
  "version": "0.0.1",
  "assembly": "MyModule.dll",
  "contributions": [
    {
      "interfaceType": "IDetector",
      "label": "My detector",
      "className": "MyModule.ModuleService",
      "tags": [ "Target concept" ],
      "active": true
    }
  ]
}
```

## Build and test the module

Build the project:

```powershell
dotnet build
```

Launch Espluque and open the module diagnostics screen.

Verify that:

- the module is discovered;
- its assembly is loaded;
- its contribution is active;
- its tags match existing thesaurus concepts.

Analyze a compatible file to confirm that the contribution is executed.

---

[Documentation home](../README.md) · [Next: Developer how-to](how-to.md)
