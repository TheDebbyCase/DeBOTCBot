using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using System.Runtime.CompilerServices;
namespace DeBOTCBot
{
    public static class Extensions
    {
        public const string Vowels = "aeiouAEIOU";
        public static async Task<bool> RespondRegistered(this CommandContext context, ServerInfo info, DiscordMessageBuilder builder)
        {
            return info.RegisterMessage(await context.EditResponseAsync(builder));
        }
        public static async Task<bool> RespondRegistered(this CommandContext context, ServerInfo info, string content)
        {
            return await context.RespondRegistered(info, new DiscordMessageBuilder().WithContent(content));
        }
        public static async Task<bool> FollowupRegistered(this SlashCommandContext context, ServerInfo info, DiscordFollowupMessageBuilder builder)
        {
            return info.RegisterMessage(await context.FollowupAsync(builder));
        }
        public static async Task<bool> FollowupRegistered(this SlashCommandContext context, ServerInfo info, string content, bool ephemeral = false)
        {
            return await context.FollowupRegistered(info, new DiscordFollowupMessageBuilder().WithContent(content).AsEphemeral(ephemeral));
        }
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, params Dictionary<TKey, TValue>[] toMerge)
        {
            IEnumerable<KeyValuePair<TKey, TValue>> dictBuilder = dictionary;
            for (int i = 0; i < toMerge.Length; i++)
            {
                dictBuilder = dictBuilder.Concat(toMerge[i]);
            }
            return dictBuilder.ToDictionary();
        }
        public static bool VowelStart(this string toCheck)
        {
            return Vowels.Contains(toCheck.First());
        }
        public static bool VowelStart(this string toCheck, out string aOrAn, bool capsStart = false, bool allCaps = false)
        {
            bool result = VowelStart(toCheck);
            aOrAn = "a";
            if (capsStart)
            {
                aOrAn = aOrAn.ToUpper();
            }
            if (result)
            {
                aOrAn += "n";
            }
            if (allCaps)
            {
                aOrAn = aOrAn.ToUpper();
            }
            return result;
        }
        public static int Length(this ITuple tuple)
        {
            return tuple.Length;
        }
        public static T Value<T>(this ITuple tuple, int index)
        {
            if (index < 0 || index >= tuple.Length)
            {
                throw new IndexOutOfRangeException("Tuple index must be a value above or equal to zero and below the size of the tuple!");
            }
            if (tuple[index].GetType() != typeof(T))
            {
                throw new InvalidCastException($"Tuple value at index: \"{index}\" is not of the generic type!");
            }   
            return (T)tuple[index];
        }
        public static object Value(this ITuple tuple, int index)
        {
            if (index < 0 || index >= tuple.Length)
            {
                throw new IndexOutOfRangeException("Tuple index must be a value above or equal to zero and below the size of the tuple!");
            }
            return tuple[index];
        }
        public static Dictionary<T1, T2> TupleListToDict<T1, T2>(this List<(T1, T2)> tuples)
        {
            Dictionary<T1, T2> dict = [];
            for (int i = 0; i < tuples.Count; i++)
            {
                (T1, T2) tuple = tuples[i];
                dict.TryAdd(tuple.Item1, tuple.Item2);
            }
            return dict;
        }
        public static bool AreSimilar(this string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                string currentString = strings[i];
                currentString = currentString.Replace("'", "").Replace("&", "and").Replace(" ", "").ToLower();
                strings[i] = currentString;
            }
            return strings.Distinct().Count() == 1;
        }
        public static bool AreSimilar(this string first, params string[] others)
        {
            string[] strings = new string[others.Length + 1];
            for (int i = 0; i < strings.Length; i++)
            {
                strings[i] = others[i];
            }
            strings[^1] = first;
            return strings.AreSimilar();
        }
    }
}