# revit2gltf

A tool focused on **converting Autodesk Revit models to GLTF format (and vice versa)**. It is designed to solve problems like large Revit model size and difficult cross-platform display. It supports lightweight processing of models in fields like architecture and structure, and enables sharing on multiple devices (such as web pages and mobile phones).

## 1. Project Introduction

### 1.1 Core Purpose



* Break the format barrier between Revit (a professional BIM modeling software) and GLTF (a common 3D real-time rendering format), to achieve efficient conversion of model data.

* Provide model geometric compression to reduce file size, and improve the efficiency of cross-platform transfer and loading.

* Support real-time status feedback during conversion, making it easy to monitor progress during development and use.

### 1.2 Application Scenarios



* Web display of BIM models in the construction industry (e.g., project result sharing, remote collaborative reviews).

* 3D model loading on mobile phones (e.g., model comparison in on-site construction).

* Lightweight backup and filing of Revit models.

## 2. Technologies Used in Development



| Technology Category           | Specific Technology / Tool                               | Function Description                                                                                                                         |
| ----------------------------- | -------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Programming Languages         | C# (96.6% of the code)                                   | The core programming language. It works with Autodesk Revit API to read model data and write conversion logic.                               |
|                               | C++ (3.4% of the code)                                   | Handles low-level geometric calculations to improve the execution efficiency of model compression algorithms.                                |
| Core Dependencies             | Autodesk Revit API                                       | Reads core data of Revit models (like components, materials, geometric shapes). It is the basis for model export.                            |
|                               | DracoNet (wraps Google Draco Library)                    | Compresses the geometry of converted GLTF models to reduce file size (e.g., compressing vertex data).                                        |
| Communication Module          | Basic TCP/UDP Socket (including `SocketMessageEvent.cs`) | Enables real-time data exchange during conversion (e.g., feedback on conversion progress, parameter transfer). It is not WebSocket protocol. |
| Development Framework & Tools | .NET Framework                                           | A unified code running environment that ensures compatibility and stability of C# modules.                                                   |
|                               | Visual Studio (Solution: Revit2GLTF.sln)                 | A project management tool that supports code editing, compilation and debugging.                                                             |
| Version Control Aid           | .gitignore                                               | Excludes temporary development files (e.g., `.exe`, `obj/`), ensuring Git only tracks core code.                                             |

## 3. Feature List

### 3.1 Core Model Conversion Features



* Export Revit models to GLTF format: Fully retain the model's component hierarchy, material properties and geometric shapes.

* Support batch conversion: Process multiple Revit model files at the same time to improve efficiency.

* Conversion parameter configuration: Allow customizing GLTF export precision (e.g., geometric detail level) to balance model quality and size.

### 3.2 Model Optimization Features



* Draco geometric compression: Use Draco algorithm to compress vertex and index data of GLTF models. File size can be reduced by 30%-70% (depending on model complexity).

* Redundant data cleaning: Automatically remove irrelevant temporary components and hidden elements in Revit models to optimize the GLTF model structure.

### 3.3 Communication & Interaction Features



* Real-time status feedback: Use Socket tell Revit to start the conversion task and communication to send conversion progress and success/failure status.

* Real-time parameter transfer: Support external programs to send configuration parameters (e.g., target export path, compression level) to the conversion module via Socket.

### 3.4 Project Support Features



* Cross-Revit version compatibility: Adapt to data reading of mainstream Revit versions (e.g., 2020-2024).

* Error log recording: Automatically generate log files (including error reasons and exception locations) when conversion fails, making it easy to troubleshoot problems.
