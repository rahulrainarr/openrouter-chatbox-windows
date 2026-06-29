# OpenRouter Chatbox For Windows

## Executive Summary

OpenRouter Chatbox for Windows is a lightweight desktop AI chat client for OpenRouter. It provides a practical Windows-first interface for model selection, local API-key storage, attachments, and configurable chat behavior.

## Business Value

Many users need a simple local desktop client for testing OpenRouter models without building a full web application or exposing credentials through a shared browser environment. This project demonstrates practical AI product packaging, local settings management, installer support, and human-friendly model experimentation.

## Key Features

- Native Windows desktop application
- OpenRouter API support with default `openrouter/auto` model selection
- Searchable model list and preset model choices
- Local API-key storage
- Model switching during an active conversation
- Attachment support for images, PDFs, text files, code files, CSV, JSON, XML, HTML, CSS, JavaScript, TypeScript, Python, C#, Java, SQL, and logs
- Configurable temperature and system prompt
- Desktop and Start Menu shortcuts
- Windows uninstall support
- In-app feedback path

## Target Users

- AI builders and technical users
- Consultants testing model behavior
- Windows users who prefer a desktop client
- Developers comparing OpenRouter model options

## Architecture Overview

```text
Windows Desktop App
|
Chat UI / Settings / Attachment Handling
|
OpenRouter API Client
|
Selected Model Provider
|
Local Settings Storage
```

## Technology Stack

- C#
- Windows Forms
- .NET Framework compiler included with Windows
- OpenRouter API
- Local `%APPDATA%` settings storage

## Installation

Download the latest installer:

[Download OpenRouter Chatbox for Windows](https://github.com/rahulrainarr/openrouter-chatbox-windows/raw/main/release-assets/OpenRouter-Chatbox-Setup-Windows.exe)

1. Download `OpenRouter-Chatbox-Setup-Windows.exe`.
2. Double-click the installer.
3. Confirm installation when prompted.
4. Launch the app from the Desktop shortcut or Start Menu.
5. Paste your OpenRouter API key and save settings.

The installer is not digitally signed. Windows SmartScreen may show a warning.

## Usage

1. Select or search for a model.
2. Configure temperature or system prompt if needed.
3. Attach files when useful.
4. Send a message and review the response.
5. Switch models during a conversation to compare behavior.

## Example Outputs

- Chat conversation with selected OpenRouter model
- Model comparison workflow
- Attachment-assisted prompt response
- Configured local settings profile

## Security And Privacy

Your API key remains on your laptop in local app settings. Chat requests and attachments are sent to OpenRouter and the selected downstream model provider according to their terms and privacy practices. Do not attach confidential files unless you have reviewed the provider path and data-handling assumptions.

Local settings path:

```text
%APPDATA%\OpenRouterChatbox\settings.json
```

## Build

```powershell
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' `
  /nologo /target:winexe /optimize+ `
  /out:OpenRouter-Chatbox-Windows.exe `
  /reference:System.dll `
  /reference:System.Core.dll `
  /reference:System.Drawing.dll `
  /reference:System.Windows.Forms.dll `
  /reference:System.Web.Extensions.dll `
  src\Program.cs
```

## Current Status

Portfolio app / usable Windows utility.

## Roadmap

- Add screenshots of the main chat and settings screens
- Add signed release option if distribution expands
- Add clearer provider/data-routing notes
- Add regression smoke checks for installer and settings behavior
- Add release checklist for future versions

## Disclaimer

This is personal portfolio software unless otherwise stated. Review API provider terms, data-handling policies, and security requirements before using it with sensitive or enterprise data.

## License

MIT
