namespace JsonBeautifier;
using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

public class JsonBeautifier
{
    public static string PrettyJson(string unPrettyJson)
    {
        int indentation = 0;
        int quoteCount = 0;
        var result = 
            from ch in unPrettyJson
            let quotes = ch == '"' ? quoteCount++ : quoteCount
            let lineBreak = ch == ',' && quotes % 2 == 0 ? ch + Environment.NewLine +  String.Concat(Enumerable.Repeat(" ", indentation)) : null
            let openChar = ch == '{' || ch == '[' ? ch + Environment.NewLine + String.Concat(Enumerable.Repeat(" ", ++indentation)) : ch.ToString()
            let closeChar = ch == '}' || ch == ']' ? Environment.NewLine + String.Concat(Enumerable.Repeat(" ", --indentation)) + ch : ch.ToString()
            select lineBreak ?? (openChar.Length > 1
                            ? openChar
                            : closeChar);

        return String.Concat(result);
    }
public class Root
{
    public List<MessageData> Message { get; set; }   // теперь это список
}
public class MessageData
{
    public int Id { get; set; }
    public string Owner { get; set; }
    public string Name { get; set; }
    public List<string> Members { get; set; }
    public List<string> Messages { get; set; }
}

public static void FromBeauty(string jsonString)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
    };

    try
    {
        // Парсим весь JSON в DOM (без строгих классов)
        using JsonDocument doc = JsonDocument.Parse(jsonString);
        JsonElement root = doc.RootElement;

        // Ищем поле "message" (регистр важен? можно сделать case-insensitive, но JSON стандарт - точное совпадение)
        if (!root.TryGetProperty("message", out JsonElement messageElement))
        {
            Console.WriteLine("Поле 'message' не найдено");
            return;
        }

        // Сериализуем только узел message обратно в JSON-строку с настройками (сохраняет кириллицу)
        string prettyMessage = JsonSerializer.Serialize(messageElement, options);
        Console.WriteLine(prettyMessage);
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Ошибка парсинга JSON: {ex.Message}");
    }
}
}
