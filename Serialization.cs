using Newtonsoft.Json;
namespace DeBOTCBot
{
    public static class Serialization
    {
        internal const string infoFilePath = "D:\\Stuff\\Discord\\Bot Server Info";
        public static void ResetDebugLog()
        {
            string filePath = $"{infoFilePath}\\Output.log";
            try
            {
                StreamWriter writer = new(filePath);
                writer.Write(string.Empty);
            }
            catch
            {
                Console.WriteLine($"Unable to write to file at: \"{filePath}\"");
            }
        }
        public static void WriteLog(string source, string message, LogType type = LogType.Info)
        {
            string timeServerString = $"[{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToShortDateString()}] [{type}  :   {source}]";
            StreamWriter writer = File.AppendText($"{infoFilePath}\\Output.log");
            string toWrite = $"{timeServerString} {message.Replace("\n", $"\n{timeServerString}")}\r\n";
            writer.Write(toWrite);
            writer.Close();
        }
        public static void WriteToFile<T>(string filePath, T objectToWrite) where T : new()
        {
            StreamWriter writer = null;
            try
            {
                writer = new(filePath);
                string toWrite = JsonConvert.SerializeObject(objectToWrite);
                writer.Write(toWrite);
            }
            catch
            {
                Console.WriteLine($"Unable to write to file at: \"{filePath}\"");
            }
            finally
            {
                writer?.Close();
            }
        }
        public static T ReadFromFile<T>(string filePath) where T : new()
        {
            StreamReader reader = null;
            try
            {
                reader = new(filePath);
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
            catch
            {
                Console.WriteLine($"Unable to read file at: \"{filePath}\"");
                return default;
            }
            finally
            {
                reader?.Close();
            }
        }
    }
}