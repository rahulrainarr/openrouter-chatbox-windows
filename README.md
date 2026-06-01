# OpenRouter Chatbox for Windows

A lightweight Windows desktop chat client for [OpenRouter](https://openrouter.ai/). It installs as a normal Windows application and stores your API key locally on your laptop.

## Download

Download the Windows installer directly:

[Download OpenRouter Chatbox for Windows](https://github.com/rahulrainarr/openrouter-chatbox-windows/raw/main/release-assets/OpenRouter-Chatbox-Setup-Windows.exe)

## Features

- Native Windows desktop application
- OpenRouter API support
- Default `openrouter/auto` model selection
- Searchable OpenRouter model list
- Preset model choices with locally stored API keys
- Switch models during an active chat while preserving conversation context
- Attach local images, PDFs, text files, and common code files
- Configurable temperature and system prompt
- Locally stored API key
- Desktop and Start Menu shortcuts
- Windows uninstall support
- In-app feedback button

## Install

1. Download `OpenRouter-Chatbox-Setup-Windows.exe`.
2. Double-click the installer.
3. Click `OK` when the installer asks for confirmation.
4. Launch the app from the Desktop shortcut or Start Menu.
5. Paste your OpenRouter API key and click `Save settings`.

## Attachments

Use `Attach files` before sending a message.

- Images are sent as local base64 image inputs to vision-capable models.
- PDFs are sent as local base64 PDF file inputs.
- Text, Markdown, CSV, JSON, XML, HTML, CSS, JavaScript, TypeScript, Python, C#, Java, SQL, and log files are added to the text context.
- Files are limited to 10 MB each.

## Model Switching

Choose another model from the model field at any time during a conversation. The full conversation context, including prior attachments, is sent to the newly selected model.

API keys are stored locally per selected model. OpenRouter normally uses one API key across its catalog, but separate locally stored values are supported when you want to maintain different keys.

The installer is not digitally signed. Windows SmartScreen may show a warning.

## Feedback

Use the in-app `Send feedback` button or [open a GitHub issue](https://github.com/rahulrainarr/openrouter-chatbox-windows/issues/new?template=feedback.md).

## Local Data

The app stores settings at:

```text
%APPDATA%\OpenRouterChatbox\settings.json
```

## Build

The application uses Windows Forms and the .NET Framework C# compiler included with Windows.

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

## Privacy

Your API key remains on your laptop. Chat requests are sent directly to OpenRouter.

## License

MIT
