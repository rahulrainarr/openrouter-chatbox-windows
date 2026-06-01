using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OpenRouterChatbox
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ChatForm());
        }
    }

    internal sealed class ChatSettings
    {
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public double Temperature { get; set; }
        public string SystemPrompt { get; set; }
        public Dictionary<string, string> ModelApiKeys { get; set; }

        public static ChatSettings Default()
        {
            return new ChatSettings
            {
                ApiKey = "",
                Model = "openrouter/auto",
                Temperature = 0.7,
                SystemPrompt = "You are a helpful AI assistant.",
                ModelApiKeys = new Dictionary<string, string>()
            };
        }
    }

    internal sealed class ChatMessage
    {
        public string role { get; set; }
        public object content { get; set; }
        public string display { get; set; }
    }

    internal sealed class AttachmentItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Kind { get; set; }

        public override string ToString()
        {
            return Name + " [" + Kind + "]";
        }
    }

    internal sealed class ChatForm : Form
    {
        private const string ChatUrl = "https://openrouter.ai/api/v1/chat/completions";
        private const string ModelsUrl = "https://openrouter.ai/api/v1/models";

        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private readonly List<ChatMessage> history = new List<ChatMessage>();
        private readonly RichTextBox transcript = new RichTextBox();
        private readonly TextBox messageInput = new TextBox();
        private readonly TextBox apiKeyInput = new TextBox();
        private readonly ComboBox modelInput = new ComboBox();
        private readonly NumericUpDown temperatureInput = new NumericUpDown();
        private readonly TextBox systemPromptInput = new TextBox();
        private readonly Label statusLabel = new Label();
        private readonly Button sendButton = new Button();
        private readonly Button saveButton = new Button();
        private readonly ListBox attachmentList = new ListBox();
        private readonly List<AttachmentItem> pendingAttachments = new List<AttachmentItem>();
        private ChatSettings settings;
        private string activeModelForKey = "";

        public ChatForm()
        {
            Text = "OpenRouter Chatbox";
            MinimumSize = new Size(920, 620);
            Size = new Size(1120, 740);
            Font = new Font("Segoe UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;

            settings = LoadSettings();
            BuildLayout();
            ApplySettingsToFields();
            SetStatus(string.IsNullOrWhiteSpace(settings.ApiKey) ? "Add your OpenRouter API key in Settings." : "Ready.");
        }

        private void BuildLayout()
        {
            var root = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                SplitterDistance = 315
            };
            Controls.Add(root);

            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 14,
                Padding = new Padding(14),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Panel1.Controls.Add(left);

            AddTitle(left, "OpenRouter Chatbox", "Windows desktop AI client");
            AddButton(left, "New chat", NewChat);
            AddLabel(left, "API key");
            apiKeyInput.UseSystemPasswordChar = true;
            AddControl(left, apiKeyInput, 32);
            AddLabel(left, "Model");
            modelInput.DropDownStyle = ComboBoxStyle.DropDown;
            modelInput.Items.AddRange(new object[]
            {
                "openrouter/auto",
                "openai/gpt-4.1",
                "openai/gpt-4.1-mini",
                "anthropic/claude-sonnet-4",
                "google/gemini-2.5-pro",
                "google/gemini-2.5-flash",
                "deepseek/deepseek-chat-v3-0324",
                "meta-llama/llama-4-maverick"
            });
            modelInput.SelectedIndexChanged += delegate { LoadApiKeyForSelectedModel(); };
            AddControl(left, modelInput, 32);
            AddLabel(left, "Temperature");
            temperatureInput.DecimalPlaces = 1;
            temperatureInput.Increment = 0.1M;
            temperatureInput.Minimum = 0;
            temperatureInput.Maximum = 2;
            AddControl(left, temperatureInput, 32);
            AddLabel(left, "System prompt");
            systemPromptInput.Multiline = true;
            systemPromptInput.ScrollBars = ScrollBars.Vertical;
            AddControl(left, systemPromptInput, 98);
            AddButton(left, "Save settings", SaveSettingsFromFields);
            AddButton(left, "Open OpenRouter keys", delegate { OpenUrl("https://openrouter.ai/keys"); });
            AddButton(left, "Open Chatbox AI website", delegate { OpenUrl("https://chatboxai.app/en"); });
            AddButton(left, "Send feedback", delegate { OpenUrl("https://github.com/rahulrainarr/openrouter-chatbox-windows/issues/new?template=feedback.md"); });
            AddButton(left, "Load model list", LoadModels);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(14)
            };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            root.Panel2.Controls.Add(right);

            transcript.Dock = DockStyle.Fill;
            transcript.ReadOnly = true;
            transcript.BackColor = Color.White;
            transcript.BorderStyle = BorderStyle.FixedSingle;
            transcript.Font = new Font("Segoe UI", 10F);
            right.Controls.Add(transcript, 0, 0);

            var attachments = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            attachments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            attachments.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            attachmentList.Dock = DockStyle.Fill;
            attachments.Controls.Add(attachmentList, 0, 0);
            var attachmentActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
            var attachButton = new Button { Text = "Attach files", Width = 104, Height = 30 };
            attachButton.Click += delegate { AddAttachments(); };
            var clearButton = new Button { Text = "Clear files", Width = 104, Height = 30 };
            clearButton.Click += delegate { ClearAttachments(); };
            attachmentActions.Controls.Add(attachButton);
            attachmentActions.Controls.Add(clearButton);
            attachments.Controls.Add(attachmentActions, 1, 0);
            right.Controls.Add(attachments, 0, 1);

            var composer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            messageInput.Multiline = true;
            messageInput.ScrollBars = ScrollBars.Vertical;
            messageInput.Dock = DockStyle.Fill;
            messageInput.Font = new Font("Segoe UI", 10F);
            messageInput.KeyDown += MessageInputKeyDown;
            composer.Controls.Add(messageInput, 0, 0);
            sendButton.Text = "Send";
            sendButton.Dock = DockStyle.Fill;
            sendButton.Click += delegate { SendMessage(); };
            composer.Controls.Add(sendButton, 1, 0);
            right.Controls.Add(composer, 0, 2);

            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.ForeColor = Color.FromArgb(71, 85, 105);
            right.Controls.Add(statusLabel, 0, 3);
        }

        private void AddTitle(TableLayoutPanel panel, string title, string subtitle)
        {
            var box = new Panel { Height = 58, Dock = DockStyle.Top };
            var titleLabel = new Label { Text = title, Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 0) };
            var subtitleLabel = new Label { Text = subtitle, ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(1, 30) };
            box.Controls.Add(titleLabel);
            box.Controls.Add(subtitleLabel);
            panel.Controls.Add(box);
        }

        private void AddLabel(TableLayoutPanel panel, string text)
        {
            panel.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, Height = 24, TextAlign = ContentAlignment.BottomLeft });
        }

        private void AddControl(TableLayoutPanel panel, Control control, int height)
        {
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control);
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        }

        private void AddButton(TableLayoutPanel panel, string text, Action action)
        {
            var button = new Button { Text = text, Height = 36, Dock = DockStyle.Top };
            button.Click += delegate { action(); };
            panel.Controls.Add(button);
        }

        private void ApplySettingsToFields()
        {
            apiKeyInput.Text = settings.ApiKey;
            modelInput.Text = settings.Model;
            activeModelForKey = settings.Model;
            temperatureInput.Value = Convert.ToDecimal(settings.Temperature);
            systemPromptInput.Text = settings.SystemPrompt;
        }

        private void SaveSettingsFromFields()
        {
            SaveApiKeyForSelectedModel();
            settings = new ChatSettings
            {
                ApiKey = apiKeyInput.Text.Trim(),
                Model = string.IsNullOrWhiteSpace(modelInput.Text) ? "openrouter/auto" : modelInput.Text.Trim(),
                Temperature = Convert.ToDouble(temperatureInput.Value),
                SystemPrompt = systemPromptInput.Text.Trim(),
                ModelApiKeys = settings.ModelApiKeys ?? new Dictionary<string, string>()
            };
            SaveSettings(settings);
            SetStatus("Settings saved.");
        }

        private void NewChat()
        {
            history.Clear();
            transcript.Clear();
            ClearAttachments();
            messageInput.Focus();
            SetStatus("Ready.");
        }

        private void MessageInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        }

        private void SendMessage()
        {
            var content = messageInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(content) || !sendButton.Enabled)
            {
                return;
            }

            SaveSettingsFromFields();
            messageInput.Clear();
            var userMessage = BuildUserMessage(content);
            history.Add(userMessage);
            AppendMessage("You", userMessage.display, Color.FromArgb(15, 118, 110));
            ClearAttachments();
            sendButton.Enabled = false;
            SetStatus("Sending to OpenRouter...");

            Task.Factory.StartNew(delegate
            {
                return SendChatRequest();
            }).ContinueWith(task =>
            {
                sendButton.Enabled = true;
                if (task.IsFaulted)
                {
                    var error = task.Exception.GetBaseException().Message;
                    AppendMessage("Error", error, Color.FromArgb(180, 35, 24));
                    SetStatus(error);
                    return;
                }
                history.Add(new ChatMessage { role = "assistant", content = task.Result.Item1, display = task.Result.Item1 });
                AppendMessage("Assistant", task.Result.Item1, Color.FromArgb(51, 65, 85));
                SetStatus("Answered by " + task.Result.Item2);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private Tuple<string, string> SendChatRequest()
        {
            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new InvalidOperationException("Add your OpenRouter API key before sending a message.");
            }

            var messages = new ArrayList();
            if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
            {
                messages.Add(new Dictionary<string, object> { { "role", "system" }, { "content", settings.SystemPrompt } });
            }
            foreach (var item in history)
            {
                messages.Add(new Dictionary<string, object> { { "role", item.role }, { "content", item.content } });
            }

            var payload = new Dictionary<string, object>
            {
                { "model", string.IsNullOrWhiteSpace(settings.Model) ? "openrouter/auto" : settings.Model },
                { "messages", messages },
                { "temperature", settings.Temperature }
            };

            var result = PostJson(ChatUrl, payload, settings.ApiKey);
            var choices = result.ContainsKey("choices") ? result["choices"] as ArrayList : null;
            if (choices == null || choices.Count == 0)
            {
                throw new InvalidOperationException("OpenRouter returned no choices.");
            }
            var choice = choices[0] as Dictionary<string, object>;
            var message = choice != null && choice.ContainsKey("message") ? choice["message"] as Dictionary<string, object> : null;
            var content = message != null && message.ContainsKey("content") ? Convert.ToString(message["content"]) : "";
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("OpenRouter returned an empty response.");
            }
            var model = result.ContainsKey("model") ? Convert.ToString(result["model"]) : settings.Model;
            return Tuple.Create(content, model);
        }

        private Dictionary<string, object> PostJson(string url, Dictionary<string, object> payload, string apiKey)
        {
            var json = serializer.Serialize(payload);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers["Authorization"] = "Bearer " + apiKey;
            request.Headers["HTTP-Referer"] = "https://chatboxai.app/en";
            request.Headers["X-Title"] = "OpenRouter Chatbox";
            request.Headers["X-OpenRouter-Title"] = "OpenRouter Chatbox";

            var bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            return ReadJsonResponse(request);
        }

        private Dictionary<string, object> ReadJsonResponse(HttpWebRequest request)
        {
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var message = ex.Message;
                if (ex.Response != null)
                {
                    using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        var body = reader.ReadToEnd();
                        message = ExtractError(body, message);
                    }
                }
                throw new InvalidOperationException(message);
            }
        }

        private string ExtractError(string body, string fallback)
        {
            try
            {
                var parsed = serializer.Deserialize<Dictionary<string, object>>(body);
                if (parsed.ContainsKey("error"))
                {
                    var error = parsed["error"] as Dictionary<string, object>;
                    if (error != null && error.ContainsKey("message"))
                    {
                        return Convert.ToString(error["message"]);
                    }
                }
                if (parsed.ContainsKey("message"))
                {
                    return Convert.ToString(parsed["message"]);
                }
            }
            catch
            {
                return string.IsNullOrWhiteSpace(body) ? fallback : body;
            }
            return fallback;
        }

        private void LoadModels()
        {
            SetStatus("Loading OpenRouter models...");
            Task.Factory.StartNew(delegate
            {
                var request = (HttpWebRequest)WebRequest.Create(ModelsUrl);
                request.Method = "GET";
                request.Accept = "application/json";
                var parsed = ReadJsonResponse(request);
                var models = new List<string>();
                var data = parsed.ContainsKey("data") ? parsed["data"] as ArrayList : null;
                if (data != null)
                {
                    foreach (var row in data)
                    {
                        var model = row as Dictionary<string, object>;
                        if (model != null && model.ContainsKey("id"))
                        {
                            models.Add(Convert.ToString(model["id"]));
                        }
                    }
                }
                models.Sort(StringComparer.OrdinalIgnoreCase);
                return models;
            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    SetStatus(task.Exception.GetBaseException().Message);
                    return;
                }
                using (var window = new ModelsForm(task.Result))
                {
                    if (window.ShowDialog(this) == DialogResult.OK)
                    {
                        modelInput.Text = window.SelectedModel;
                        SaveSettingsFromFields();
                    }
                }
                SetStatus("Loaded " + task.Result.Count + " models.");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void AppendMessage(string label, string content, Color labelColor)
        {
            transcript.SelectionStart = transcript.TextLength;
            transcript.SelectionColor = labelColor;
            transcript.SelectionFont = new Font(transcript.Font, FontStyle.Bold);
            transcript.AppendText(label + Environment.NewLine);
            transcript.SelectionFont = transcript.Font;
            transcript.SelectionColor = Color.FromArgb(15, 23, 42);
            transcript.AppendText(content.Trim() + Environment.NewLine + Environment.NewLine);
            transcript.ScrollToCaret();
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }

        private static string SettingsPath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenRouterChatbox");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "settings.json");
        }

        private ChatSettings LoadSettings()
        {
            try
            {
                var path = SettingsPath();
                if (!File.Exists(path))
                {
                    return ChatSettings.Default();
                }
                var loaded = serializer.Deserialize<ChatSettings>(File.ReadAllText(path, Encoding.UTF8));
                var defaults = ChatSettings.Default();
                loaded.ApiKey = loaded.ApiKey ?? "";
                loaded.Model = string.IsNullOrWhiteSpace(loaded.Model) ? defaults.Model : loaded.Model;
                loaded.SystemPrompt = loaded.SystemPrompt ?? defaults.SystemPrompt;
                loaded.ModelApiKeys = loaded.ModelApiKeys ?? new Dictionary<string, string>();
                return loaded;
            }
            catch
            {
                return ChatSettings.Default();
            }
        }

        private void SaveSettings(ChatSettings value)
        {
            File.WriteAllText(SettingsPath(), serializer.Serialize(value), Encoding.UTF8);
        }

        private void SaveApiKeyForSelectedModel()
        {
            if (settings.ModelApiKeys == null)
            {
                settings.ModelApiKeys = new Dictionary<string, string>();
            }
            var model = string.IsNullOrWhiteSpace(activeModelForKey) ? modelInput.Text.Trim() : activeModelForKey;
            var key = apiKeyInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(key))
            {
                settings.ModelApiKeys[model] = key;
            }
        }

        private void LoadApiKeyForSelectedModel()
        {
            SaveApiKeyForSelectedModel();
            var model = modelInput.Text.Trim();
            string key;
            if (settings.ModelApiKeys != null && settings.ModelApiKeys.TryGetValue(model, out key))
            {
                apiKeyInput.Text = key;
            }
            activeModelForKey = model;
            SetStatus("Active model: " + model + ". Existing conversation context will be preserved.");
        }

        private void AddAttachments()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.Filter = "Supported files|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.pdf;*.txt;*.md;*.csv;*.json;*.xml;*.html;*.css;*.js;*.ts;*.py;*.cs;*.java;*.sql;*.log|Images|*.png;*.jpg;*.jpeg;*.gif;*.webp|PDF files|*.pdf|Text and code files|*.txt;*.md;*.csv;*.json;*.xml;*.html;*.css;*.js;*.ts;*.py;*.cs;*.java;*.sql;*.log";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                foreach (var path in dialog.FileNames)
                {
                    var info = new FileInfo(path);
                    if (info.Length > 10 * 1024 * 1024)
                    {
                        MessageBox.Show(info.Name + " is larger than 10 MB and was not added.", "OpenRouter Chatbox");
                        continue;
                    }
                    var item = new AttachmentItem { Name = info.Name, Path = path, Kind = AttachmentKind(path) };
                    pendingAttachments.Add(item);
                    attachmentList.Items.Add(item);
                }
                SetStatus(pendingAttachments.Count + " attachment(s) ready. They will stay in the conversation context.");
            }
        }

        private void ClearAttachments()
        {
            pendingAttachments.Clear();
            attachmentList.Items.Clear();
        }

        private ChatMessage BuildUserMessage(string text)
        {
            if (pendingAttachments.Count == 0)
            {
                return new ChatMessage { role = "user", content = text, display = text };
            }

            var parts = new ArrayList();
            var textBuilder = new StringBuilder(text);
            var names = new List<string>();
            foreach (var attachment in pendingAttachments)
            {
                names.Add(attachment.Name);
                if (attachment.Kind == "text")
                {
                    textBuilder.Append("\n\n--- Attached file: " + attachment.Name + " ---\n");
                    textBuilder.Append(File.ReadAllText(attachment.Path));
                }
            }
            parts.Add(new Dictionary<string, object> { { "type", "text" }, { "text", textBuilder.ToString() } });
            foreach (var attachment in pendingAttachments)
            {
                if (attachment.Kind == "image")
                {
                    parts.Add(new Dictionary<string, object>
                    {
                        { "type", "image_url" },
                        { "image_url", new Dictionary<string, object> { { "url", DataUrl(attachment.Path) } } }
                    });
                }
                else if (attachment.Kind == "pdf")
                {
                    parts.Add(new Dictionary<string, object>
                    {
                        { "type", "file" },
                        { "file", new Dictionary<string, object> { { "filename", attachment.Name }, { "file_data", DataUrl(attachment.Path) } } }
                    });
                }
            }
            return new ChatMessage
            {
                role = "user",
                content = parts,
                display = text + "\n\nAttachments: " + string.Join(", ", names.ToArray())
            };
        }

        private static string AttachmentKind(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif" || extension == ".webp") return "image";
            if (extension == ".pdf") return "pdf";
            return "text";
        }

        private static string DataUrl(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var mime = "application/octet-stream";
            if (extension == ".png") mime = "image/png";
            else if (extension == ".jpg" || extension == ".jpeg") mime = "image/jpeg";
            else if (extension == ".gif") mime = "image/gif";
            else if (extension == ".webp") mime = "image/webp";
            else if (extension == ".pdf") mime = "application/pdf";
            return "data:" + mime + ";base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }

        private static void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch
            {
                MessageBox.Show("Could not open " + url, "OpenRouter Chatbox");
            }
        }
    }

    internal sealed class ModelsForm : Form
    {
        private readonly ListBox list = new ListBox();
        public string SelectedModel { get; private set; }

        public ModelsForm(List<string> models)
        {
            Text = "OpenRouter models";
            Size = new Size(560, 560);
            StartPosition = FormStartPosition.CenterParent;

            list.Dock = DockStyle.Fill;
            foreach (var model in models)
            {
                list.Items.Add(model);
            }
            list.DoubleClick += delegate { AcceptSelection(); };
            Controls.Add(list);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 46 };
            var use = new Button { Text = "Use selected model", Dock = DockStyle.Right, Width = 150 };
            use.Click += delegate { AcceptSelection(); };
            bottom.Controls.Add(use);
            Controls.Add(bottom);
        }

        private void AcceptSelection()
        {
            if (list.SelectedItem == null)
            {
                return;
            }
            SelectedModel = Convert.ToString(list.SelectedItem);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
