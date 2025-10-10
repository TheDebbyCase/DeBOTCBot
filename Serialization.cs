using System.Text.Json;
namespace DeBOTCBot
{
    public static class Serialization
    {
        internal const string infoFilePath = "D:\\Stuff\\Discord\\Bot Server Info";
        public static async Task ResetDebugLog()
        {
            string filePath = $"{infoFilePath}\\Output.log";
            try
            {
                StreamWriter writer = new(filePath);
                await writer.WriteAsync(string.Empty);
            }
            catch (Exception exception)
            {
                await ServerInfo.BotLog($"Unable to write to file at: \"{filePath}\"");
                await ServerInfo.BotLog(exception);
            }
        }
        public static async Task WriteLog(string source, string message, LogType type = LogType.Info)
        {
            string timeServerString = $"[{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToShortDateString()}] [{type}  :   {source}]";
            StreamWriter writer = File.AppendText($"{infoFilePath}\\Output.log");
            string toWrite = $"{timeServerString} {message.Replace("\n", $"\n{timeServerString}")}\r\n";
            await writer.WriteAsync(toWrite);
            await writer.DisposeAsync();
        }
        public static async Task WriteToFile<T>(string filePath, T objectToWrite) where T : new()
        {
            FileStream file = null;
            try
            {
                file = new(filePath, FileMode.Create);
                await JsonSerializer.SerializeAsync(file, objectToWrite);
            }
            catch (Exception exception)
            {
                await ServerInfo.BotLog($"Unable to write to file at: \"{filePath}\"");
                await ServerInfo.BotLog(exception);
            }
            finally
            {
                if (file != null)
                {
                    await file.DisposeAsync();
                }
            }
        }
        public static async ValueTask<T> ReadFromFile<T>(string filePath) where T : new()
        {
            FileStream file = null;
            try
            {
                file = new(filePath, FileMode.Open);
                return await JsonSerializer.DeserializeAsync<T>(file);
            }
            catch (Exception exception)
            {
                await ServerInfo.BotLog($"Unable to read file at: \"{filePath}\"");
                await ServerInfo.BotLog(exception);
                return default;
            }
            finally
            {
                if (file != null)
                {
                    await file.DisposeAsync();
                }
            }
        }
    }
}