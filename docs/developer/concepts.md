[Documentation](../README.md) · [Developer quick start](quick-start.md) · [Developer how-to](how-to.md) · **Developer concepts**

# Module concepts

A module extends Espluque with support for a specific technology, file format or external library.

A module can contain several contributions:

* detectors,
* grabbers,
* viewers,
* supporting contributions required by the module.

For example, a Markdown module can detect Markdown files, extract Markdown properties and display their rendered content.

## Contributions

Each contribution implements a dedicated contract from `Espluque.Contracts`.

The contract defines the method called by the engine and the expected return type.

### Detector

A detector identifies whether a file matches a format or concept.

```csharp
using Espluque.Contracts.Interfaces;

namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IDetector
    {
        Task<IFileFormat> Detect(string filePath);
    }
}
```

The engine calls `Detect` with the path of the analyzed file.

The returned `IFileFormat` describes the detected result.

### Grabber

A grabber extracts properties from a file.

```csharp
namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IGrabber
    {
        Task<List<KeyValuePair<string, string>>> Grab(string filePath);
    }
}
```

The engine calls `Grab` with the path of the analyzed file.

The returned key-value pairs are displayed as a property list in the interface.

### WPF viewer

A viewer creates a WPF representation of a file.

```csharp
namespace Espluque.Contracts.ModuleInterfaces.Contributions
{
    public interface IWpfViewer
    {
        Task<object?> GetViewer(string filePath);
    }
}
```

The engine calls `GetViewer` after the analysis.

The returned object contains the XAML user control displayed in the file tab.

## Module templates

Three project templates are available:

* detector module,
* grabber module,
* viewer module.

Each template provides the minimum structure required to implement one contribution type.

A module is not limited to one contribution. Several contracts can be implemented in the same module when they rely on the same technology or library.

## Composite modules

A composite module contains several contribution types in a single assembly.

The composite Markdown sample declares:

* one detector;
* one grabber;
* one WPF viewer;
* one managed-dependency contribution.

The same class can implement several contribution interfaces when the contributions share the same implementation context.

## Module manifest

Each module contains a `module.json` manifest.

Example:

```json
{
  "name": "Composit Module Sample",
  "version": "0.0.1",
  "assembly": "CompositeMdModule.dll",
  "contributions": [
    {
      "interfaceType": "IDetector",
      "label": "Markdown detector",
      "className": "CompositeMdModule.ModuleService",
      "tags": [ "Markdown" ],
      "active": true
    },
    {
      "interfaceType": "IGrabber",
      "label": "Markdown infos",
      "className": "CompositeMdModule.ModuleService",
      "tags": [ "Markdown" ],
      "active": true
    },
    {
      "interfaceType": "IWpfViewer",
      "label": "Markdown viewer",
      "className": "CompositeMdModule.ModuleService",
      "tags": [ "Markdown" ],
      "active": true
    }
  ]
}
```

### Manifest fields

#### `name`

The display name of the module.

#### `version`

The version of the module.

#### `assembly`

The main assembly loaded for the module.

#### `contributions`

The list of contributions exposed by the module.

Each contribution contains the following fields.

#### `interfaceType`

The contribution contract implemented by the class.

Examples:

* `IDetector`,
* `IGrabber`,
* `IWpfViewer`

#### `label`

The name displayed for the contribution.

#### `className`

The fully qualified name of the class implementing the contribution.

Several entries can reference the same class when it implements several interfaces.

#### `tags`

The thesaurus terms associated with the contribution.

A tag normally contains the main term of a thesaurus concept.

The engine uses these tags to attach the contribution to the corresponding concept and determine when it must be executed.

#### `active`

Defines whether the contribution is enabled.

An inactive contribution remains declared in the module but is not used by the engine.

## Tags and thesaurus concepts

Tags connect module contributions to the thesaurus.

For example:

```json
"tags": [ "Markdown" ]
```

associates the contribution with the thesaurus concept whose main term is `Markdown`.

The thesaurus then controls when the contribution is activated:

* a detector participates in format identification;
* a grabber extracts properties when its concept is reached;
* a viewer is queued when its concept is reached.

This keeps module implementation separate from analysis orchestration: modules declare their contributions and tags, while the thesaurus determines how they participate in the analysis.

---

[Documentation home](../README.md) · [Previous: Developer how-to](how-to.md)
