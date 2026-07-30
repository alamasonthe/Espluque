# Espluque
[![Build](https://github.com/alamasonthe/Espluque/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/alamasonthe/Espluque/actions/workflows/build.yml) [![License: MIT](https://img.shields.io/github/license/alamasonthe/Espluque)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet) ![WPF](https://img.shields.io/badge/UI-WPF-5C2D91) ![Platform](https://img.shields.io/badge/platform-Windows-0078D4) [![Last commit](https://img.shields.io/github/last-commit/alamasonthe/Espluque)](https://github.com/alamasonthe/Espluque/commits/master)

> A modular, thesaurus-driven file analysis engine built with WPF and .NET.

Espluque combines independent modules through a graph of concepts. A detector can directly identify a file format, regardless of the reference system it uses or its level of specificity. 
The engine maps this identification to the corresponding concept, then follows the graph relationships to continue the analysis and activate all associated contributions.

![Espluque main window](docs/images/Espluque_Reg_Analysis.png)

## Overview

Espluque is a modular desktop application for identifying, analyzing and displaying files through independent contributions such as detectors, grabbers and viewers. 
A thesaurus-driven engine connects these contributions and orchestrates the analysis according to the concepts associated with each file.

This repository is an architectural showcase focused on runtime module discovery, contract-based extensibility and thesaurus-driven orchestration. 
Its core contribution is a multi-parent concept graph that allows file formats to belong to several classification paths, enabling the engine to activate relevant detectors, grabbers and viewers without relying on a rigid hierarchy.

![Espluque Thesaurus](docs/images/Espluque_Thesaurus.png)

## Key concepts

- Independent runtime modules
- Multi-parent thesaurus concept graph
- Concept-driven contribution orchestration
- Progressive and composite file analysis

|                                                                     |                                                        |
| ------------------------------------------------------------------- | ------------------------------------------------------ |
| ![Video properties](docs/images/Espluque_Video_properties.png)      | ![Video viewer](docs/images/Espluque_Video_Viewer.png) |
| ![Thesaurus module diagnotic](docs/images/Espluque_Module_Diag.png) | ![SQL Edit](docs/images/Espluque_Sql_Edit.png)         |

## Getting started

Espluque currently has no installer and must be built from source.

### Requirements

- Windows 10 or later

- .NET 10 SDK

- Visual Studio 2026 with the **.NET desktop development** workload

### Build and run

1. Clone the repository.

2. Open the solution in Visual Studio.

3. Set Espluquer as the startup project.

4. Build and run the solution.

5. Drag a file into the application to start an analysis.

The module projects are copied automatically to the application's Modules directory during the build.

## Documentation

[Open the documentation](docs/README.md)

## License

This project is licensed under the [MIT License](LICENSE).