using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JsonBeautifier;

// Регистрируем провайдера кодировок для поддержки CP866
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Настройка UTF-8 для вывода (чтобы видеть кириллицу в консоли)
Console.OutputEncoding = Encoding.UTF8;

// Устанавливаем кодировку ввода CP866 (русская консоль Windows)
Console.InputEncoding = Encoding.GetEncoding(866);

using RSA rsa = RSA.Create();

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5000");
var token = string.Empty;
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
    }
    else if (commandInput.StartsWith("chat"))
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "chat")
        {
            Content = JsonContent.Create(new {
                chat = commandInput.Split(' ')[1]
            })
        };
        var response = await httpClient.SendAsync(request);
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
    }
    else if (commandInput.StartsWith("leave"))
    {
        var response = await httpClient.PostAsJsonAsync("leave", new {
            token = token,
            chat = commandInput.Split(' ')[1]
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
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
    }
    else if (commandInput.StartsWith("message"))
    {
        var encryptedBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(commandInput.Split(' ')[2]), RSAEncryptionPadding.OaepSHA256);
        var encryptedBase64 = Convert.ToBase64String(encryptedBytes);
        var response = await httpClient.PostAsJsonAsync("message", new
        {
            token = token,
            chat = commandInput.Split(' ')[1],
            message = encryptedBase64
        });
        var answer = await response.Content.ReadAsStringAsync();
         JsonBeautifier.JsonBeautifier.FromBeauty(JsonBeautifier.JsonBeautifier.PrettyJson(answer)!);
    }
}