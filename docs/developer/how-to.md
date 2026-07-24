[Documentation](../README.md) · [Developer quick start](quick-start.md) · **Developer how-to** · [Developer concepts](concepts.md)

# Developer how-to guides

## Request the analysis of another file

A module may need Espluque to analyze another file outside the current analysis execution.

This is done by:

1. creating an `IMessage`;
2. sending it through `IMessageCenter`.

Two message types are available:

* `Analyze`: analyzes an existing file;
* `ExtractAndAnalyze`: extracts a file from a container with 7-Zip, then analyzes the extracted file.

### Required services

The module requires an `IEntityFactory` to create the message and an `IMessageCenter` to send it.

```csharp
private readonly IMessageCenter _messageCenter;
private readonly IEntityFactory _entityFactory;

public AnyModuleViewerUC(string filePath, IMessageCenter messageCenter, IEntityFactory entityFactory)
{
    _filePath = filePath;
    _messageCenter = messageCenter;
    _entityFactory = entityFactory;

    InitializeComponent();
}
```

### Message structure

```csharp
using Espluque.Contracts.Enums;

namespace Espluque.Contracts.MessageInterfaces
{
    public interface IMessage
    {
        string MessageLabel { get; set; }
        MessageTypeEnum MessageType { get; set; }
        List<KeyValuePair<string, string>> Payload { get; set; }
    }
}
```

Messages are created through the entity factory:

```csharp
IMessage message = _entityFactory.CreateMessage(
    messageType,
    messageLabel,
    payload);
```

### Analyze an existing file

An `Analyze` message requires a `FilePath` payload entry.

```csharp
IMessage message = _entityFactory.CreateMessage(
    MessageTypeEnum.Analyze,
    "Analyze",
    [
        new("FilePath", filePath)
    ]);

await _messageCenter.SendAsync(message);
```

Espluque verifies that the file exists, then starts a new analysis for it.

### Extract and analyze a contained file

An `ExtractAndAnalyze` message requires:

* `FilePath`: the path of the container;
* `InternalPath`: the path of the file inside the container.

```csharp
IMessage message = _entityFactory.CreateMessage(
    MessageTypeEnum.ExtractAndAnalyze,
    "ExtractAndAnalyze",
    [
        new("FilePath", containerFilePath),
        new("InternalPath", internalPath)
    ]);

await _messageCenter.SendAsync(message);
```

Espluque then:

1. creates a temporary folder;
2. extracts the requested entry with 7-Zip;
3. starts a new analysis for the extracted file.

### Example from a viewer

The viewer sends the message when the user double-clicks a file contained in an archive:

```csharp
private async void LeafDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    IMessage message = _entityFactory.CreateMessage(
        MessageTypeEnum.ExtractAndAnalyze,
        "ExtractAndAnalyze",
        [
            new("FilePath", _filePath),
            new("InternalPath", internalPath)
        ]);

    await _messageCenter.SendAsync(message);

    e.Handled = true;
}
```

---

[Documentation home](../README.md) · [Previous: Developer quick start](quick-start.md) · [Next: Developer concepts](concepts.md)
