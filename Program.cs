using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JsonBeautifier;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

Console.OutputEncoding = Encoding.UTF8;

Console.InputEncoding = Encoding.GetEncoding(866);

using RSA rsa = RSA.Create();

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5000");
var token = string.Empty;
string? currentChat = null;
Timer? pollingTimer = null;
string? lastMessagesResponse = null;
Console.WriteLine("Enter host or whitespace to http://localhost:5000");
var input = Console.ReadLine();

if (!string.IsNullOrWhiteSpace(input))
{
    httpClient.BaseAddress = new Uri(input);
}
if (!File.Exists("rsa.env"))
{
    var response = await httpClient.GetAsync("pubkey");
    var pubkey = JsonNode.Parse(await response.Content.ReadAsStringAsync())["message"]!.ToString();
    File.WriteAllText("rsa.env", pubkey);
}
var rsaKeyPem = File.ReadAllText("rsa.env");
var rsaKeyBytes = Convert.FromBase64String(rsaKeyPem);
rsa.ImportRSAPublicKey(rsaKeyBytes, out _);
async void PollChat(object? state)
{
    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(currentChat))
        return;

    try
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "chat")
        {
            Content = JsonContent.Create(new { chat = currentChat })
        };
        var response = await httpClient.SendAsync(request);
        var answer = await response.Content.ReadAsStringAsync();


        if (lastMessagesResponse == null)
        {
            lastMessagesResponse = answer;
            return;
        }

        if (HaveNewMessages(lastMessagesResponse, answer))
        {
            Console.WriteLine($"\n[Новые сообщения в чате '{currentChat}']:");
            PrintNewMessages(lastMessagesResponse, answer);
            lastMessagesResponse = answer;
            Console.Write("\nПожалуйста, введите команду + параметры: ");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nОшибка опроса: {ex.Message}");
    }
}

static bool HaveNewMessages(string oldResponse, string newResponse)
{
    try
    {
        var oldMessages = ExtractMessagesArray(oldResponse);
        var newMessages = ExtractMessagesArray(newResponse);

        if (oldMessages == null || newMessages == null)
            return oldResponse != newResponse;

        return newMessages.Count > oldMessages.Count;
    }
    catch
    {
        return oldResponse != newResponse;
    }
}

static JsonArray? ExtractMessagesArray(string jsonResponse)
{
    var node = JsonNode.Parse(jsonResponse);
    var message = node?["message"];
    return message?["messages"]?.AsArray();
}

void PrintNewMessages(string oldResponse, string newResponse)
{
    try
    {
        var oldMessages = ExtractMessagesArray(oldResponse);
        var newMessages = ExtractMessagesArray(newResponse);

        if (oldMessages == null || newMessages == null)
        {
            JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(newResponse)!);
            return;
        }
        var onlyNew = newMessages.Skip(oldMessages.Count);
        foreach (var msg in onlyNew)
        {
            Console.WriteLine($"  {msg}");
        }
    }
    catch
    {
        JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(newResponse)!);
    }
}

void StartPolling()
{
    if (pollingTimer == null && !string.IsNullOrEmpty(token))
    {
        pollingTimer = new Timer(PollChat, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        Console.WriteLine("Фоновый опрос чата запущен (каждые 5 секунд).");
    }
}
while (true)
{
    Console.WriteLine("Please enter command + params");
    var commandInput = Console.ReadLine();
    if (commandInput == null) break;

    if (commandInput.StartsWith("register"))
    {
        var username = commandInput.Split(' ')[1];
        var password = commandInput.Split(' ')[2];
        var response = await httpClient.PostAsJsonAsync("register", new {
            user = username,
            password = password
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
        token = JsonNode.Parse(answer)["token"]!.GetValue<string>();
        StartPolling();
    }
    else if (commandInput.StartsWith("login"))
    {
        var username = commandInput.Split(' ')[1];
        var password = commandInput.Split(' ')[2];
        var response = await httpClient.PostAsJsonAsync("login", new {
            user = username,
            password = password
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
        token = JsonNode.Parse(answer)["token"]!.GetValue<string>();
        StartPolling();
    }
    else if (commandInput.StartsWith("token"))
    {
        var response = await httpClient.PostAsJsonAsync("token", new {
            token = token
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
    }
    else if (commandInput.StartsWith("exit"))
    {
        break;
    }
    else if (commandInput.StartsWith("users"))
    {
        var response = await httpClient.GetStringAsync("users");
        JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(response)!);
    }
    else if (commandInput.StartsWith("chats"))
    {
        var response = await httpClient.GetStringAsync("chats");
        JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(response));
    }   
    else if (commandInput.StartsWith("newchat"))
    {
        var response = await httpClient.PostAsJsonAsync("chat", new {
            token = token,
            chat = commandInput.Split(' ')[1]
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);

        currentChat = commandInput.Split(' ')[1];
        lastMessagesResponse = null;
        Console.WriteLine($"Теперь вы в чате '{currentChat}'");
    }
    else if (commandInput.StartsWith("chat"))
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "chat")
        {
            Content = JsonContent.Create(new {
                chat = currentChat ?? commandInput.Split(' ')[1] 
            })
        };
        var response = await httpClient.SendAsync(request);
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);

        lastMessagesResponse = answer;
    }
    else if (commandInput.StartsWith("leave"))
    {
        var response = await httpClient.PostAsJsonAsync("leave", new {
            token = token,
            chat = commandInput.Split(' ')[1]
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
         currentChat = null;
        Console.WriteLine("Активный чат сброшен");
    }
    else if (commandInput.StartsWith("deletechat"))
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "chat")
        {
            Content = JsonContent.Create(new {
                token = token,
                chat = commandInput.Split(' ')[1]
            })
        };
        var response = await httpClient.SendAsync(request);
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
    }
    else if (commandInput.StartsWith("join"))
    {
        var response = await httpClient.PostAsJsonAsync("join", new {
            token = token,
            chat = commandInput.Split(' ')[1]
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
        currentChat = commandInput.Split(' ')[1];
        lastMessagesResponse = null;
        Console.WriteLine($"Теперь вы в чате '{currentChat}'");
    }
    else if (commandInput.StartsWith("message"))
    {
        if (commandInput.Split(' ').Length < 2 )
        {
            Console.WriteLine("Недостаточно параметров для команды 'message'. Формат: message <chat> <text>");
            continue;
        }
        HttpResponseMessage response;
        if (commandInput.Split(' ').Length == 2 && currentChat != null)
        {
            var encryptedBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(commandInput.Split(' ')[1]), RSAEncryptionPadding.OaepSHA256);
            var encryptedBase64 = Convert.ToBase64String(encryptedBytes);
            response = await httpClient.PostAsJsonAsync("message", new
            {
                token = token,
                chat = currentChat,
                message = encryptedBase64
            });
        }
        else
        {
            var encryptedBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(commandInput.Split(' ')[1]), RSAEncryptionPadding.OaepSHA256);
            var encryptedBase64 = Convert.ToBase64String(encryptedBytes);
            response = await httpClient.PostAsJsonAsync("message", new
            {
                token = token,
                chat = commandInput.Split(' ')[1],
                message = encryptedBase64
            });
            currentChat = commandInput.Split(' ')[2];
            Console.WriteLine($"Теперь вы в чате '{currentChat}'");
        }
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
    }
    else if (commandInput.StartsWith("activechat"))
    {
        Console.WriteLine(currentChat ?? "Активный чат не выбран");
    }
    else if (commandInput.StartsWith("setchat"))
    {
        var chatName = commandInput.Split(' ')[1];
        currentChat = chatName;
        lastMessagesResponse = null;
        Console.WriteLine($"Активный чат установлен на '{chatName}'");
    }
}