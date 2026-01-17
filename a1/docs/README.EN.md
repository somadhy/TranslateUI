# TranslateUI

A minimalistic desktop translator built on **Ollama** + **TranslateGemma**.  
Target stack: **.NET 10**, **C# 14**, **Avalonia UI**, **MVVM**. No paid libraries.

## Goals
- Translate text, files, and (optionally) images.
- Provide strict aesthetic UI with adaptive layout and tray support.
- Stay lightweight in RAM/CPU/Disk usage.

## Features
- **Text translation**: source/target panels, translate on button click, option to save result.
- **File translation**: file picker or drag&drop, choose output path, link to open result.
- **Image translation**: only when a vision-capable model is available.
- **Language selection**: source/target languages based on `translategemma.MD`.
- **Model selection**: discrete slider (4B, 12B, 27B) with “Speed → Accuracy”.
- **Model download**: warning if missing, direct download or auto-download before translate.
- **Clipboard**: full copy/paste support.
- **Debug logging**: configurable log level in settings.

## Requirements Snapshot
- **Supported formats**: PDF, TXT, MD, DOCX, ODT, PNG, JPEG, TIFF.
- **Settings**: default languages, Ollama URL, default model, UI language.
- **Logging**: configurable log level (Debug/Info/Warn/Error/Off).
- **Security**: safe file access, no arbitrary execution, sanitize paths.

## Getting Started (Developer)
- Install **.NET 10 SDK** and **Ollama**.
- Ensure Ollama is running (`http://localhost:11434` by default).
- Download model if needed: `ollama pull translategemma:4b`.

## Documentation Map
- `01-Requirements.md` — functional and non-functional requirements
- `02-Architecture.md` — MVVM structure, services, patterns
- `03-UI-UX.md` — layouts, interactions, tray behavior
- `04-Ollama-Integration.md` — HTTP endpoints, model handling
- `05-File-Formats.md` — extraction/output strategies
- `06-Prompting-Languages.md` — prompt format + language list usage
- `07-Settings-Localization.md` — settings and resources
- `08-Testing.md` — unit/integration/UI test guidance
- `09-Security.md` — security and safety checklist
- `10-Implementation-Plan.md` — phased delivery plan

## Licensing
Use only free/open-source libraries.
