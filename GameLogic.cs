namespace DeBOTCBot
{
    public enum CharacterType
    {
        Demon,
        Minion,
        Outsider,
        Townsfolk
    }
    public class Range2D
    {
        public Range2D(int minAndMax)
        {
            min = minAndMax;
            max = minAndMax;
            onlyMinMax = true;
        }
        public Range2D(int minimumValue = 0, int maximumValue = 0, bool minMax = false)
        {
            min = minimumValue;
            max = maximumValue;
            onlyMinMax = minMax;
        }
        public int min;
        public int max;
        public bool onlyMinMax;
    }
    public class TypeRanges
    {
        public Range2D[] tokensToAdd;
        public TypeRanges(Range2D demons = default, Range2D minions = default, Range2D outsiders = default, Range2D townsfolks = default)
        {
            demons ??= new();
            minions ??= new();
            outsiders ??= new();
            townsfolks ??= new();
            tokensToAdd = [demons, minions, outsiders, townsfolks];
        }
    }
    public class Token
    {
        public string characterName;
        public CharacterType characterType;
        public TypeRanges typesToAdd;
        public string[] characterToAdd;
        public string[] forbiddenPair;
        public bool specialRule;
        public string description;
        public (int, int) nightOrder;
        public string orderFirstDescription;
        public string orderOtherDescription;
        public Token(string name, CharacterType type = CharacterType.Townsfolk, TypeRanges addingTypes = null, string[] addingCharacters = null, string[] removingCharacters = null, bool rule = false, int firstOrder = -1, int otherOrder = -1, string firstDesc = "", string otherDesc = "", string desc = "")
        {
            characterName = name;
            characterType = type;
            addingTypes ??= new();
            typesToAdd = addingTypes;
            characterToAdd = addingCharacters;
            forbiddenPair = removingCharacters;
            specialRule = rule;
            description = desc;
            nightOrder = (firstOrder, otherOrder);
            orderFirstDescription = firstDesc;
            orderOtherDescription = otherDesc;
        }
    }
    public class Player
    {
        public Player(int length, int index, Token character)
        {
            totalPlayers = length;
            seat = index;
            token = character;
            int left = seat - 1;
            int right = seat + 1;
            if (left < 0)
            {
                left = totalPlayers - 1;
            }
            if (right >= totalPlayers)
            {
                right = 0;
            }
            neighbours = (left, right);
        }
        public int totalPlayers;
        public int seat;
        public Token token;
        public (int, int) neighbours;
    }
    public class BOTCCharacters
    {
        public static readonly Dictionary<string, Token> allTokens = GenerateTokens();
        public ServerInfo info;
        public Dictionary<string, string[]> scripts;
        public void Initialize(ServerInfo newInfo = null)
        {
            if (newInfo != null)
            {
                info = newInfo;
            }
            info?.Log("Making default BOTC scripts");
            if (scripts == null || (scripts != null && scripts.Count == 0))
            {
                scripts = [];
                scripts.Add("Trouble Brewing", ["Imp", "Baron", "Scarlet Woman", "Spy", "Poisoner", "Saint", "Recluse", "Drunk", "Butler", "Mayor", "Soldier", "Slayer", "Virgin", "Ravenkeeper", "Monk", "Undertaker", "Fortune Teller", "Empath", "Chef", "Investigator", "Librarian", "Washerwoman"]);
                scripts.Add("Sects & Violets", ["Vortox", "No Dashii", "Vigormortis", "Fang Gu", "Pit-Hag", "Cerenovus", "Witch", "Evil Twin", "Klutz", "Barber", "Sweetheart", "Mutant", "Sage", "Juggler", "Artist", "Philosopher", "Seamstress", "Savant", "Oracle", "Town Crier", "Flowergirl", "Mathematician", "Snake Charmer", "Dreamer", "Clockmaker"]);
                scripts.Add("Bad Moon Rising", ["Po", "Shabaloth", "Pukka", "Zombuul", "Mastermind", "Assassin", "Devil's Advocate", "Godfather", "Moonchild", "Tinker", "Lunatic", "Goon", "Fool", "Pacifist", "Tea Lady", "Minstrel", "Professor", "Courtier", "Gossip", "Gambler", "Innkeeper", "Exorcist", "Chambermaid", "Sailor", "Grandmother"]);
            }
        }
        public string[] RollTokens(string[] script, int playerCount)
        {
            List<Token> scriptTokens = [];
            for (int i = 0; i < script.Length; i++)
            {
                string name = script[i];
                if (allTokens.TryGetValue(name, out Token token))
                {
                    scriptTokens.Add(token);
                }
                else
                {
                    info.Log($"\"{name}\" was not a valid token, did you misspell it?");
                }
            }
            List<Token> chosenCharacters = ChooseCharacters(scriptTokens, playerCount);
            if (chosenCharacters == null)
            {
                return null;
            }
            List<string> characterNames = [];
            for (int i = 0; i < chosenCharacters.Count; i++)
            {
                ConsoleColor consoleColor;
                switch (chosenCharacters[i].characterType)
                {
                    case CharacterType.Demon:
                        {
                            consoleColor = ConsoleColor.DarkRed;
                            break;
                        }
                    case CharacterType.Minion:
                        {
                            consoleColor = ConsoleColor.Red;
                            break;
                        }
                    case CharacterType.Outsider:
                        {
                            consoleColor = ConsoleColor.DarkCyan;
                            break;
                        }
                    default:
                        {
                            consoleColor = ConsoleColor.Cyan;
                            break;
                        }
                }
                info.Log($"Token {i + 1}: \"{chosenCharacters[i].characterName}\"", consoleColor);
                characterNames.Add(chosenCharacters[i].characterName);
            }
            return [..characterNames.OrderBy((x) => allTokens[x].characterType)];
        }
        public List<Token> ChooseCharacters(List<Token> scriptTokens, int playerCount)
        {
            int[] counts = BaseCharacterNumbers(playerCount);
            info.Log($"Starting counts: \"{counts[0]}, {counts[1]}, {counts[2]}, {counts[3]}\"");
            List<Token> initialPicks = [];
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] <= 0)
                {
                    continue;
                }
                List<Token> tokensOfType = scriptTokens.Where((x) => x.characterType == (CharacterType)Enum.ToObject(typeof(CharacterType), (byte)i)).ToList();
                for (int j = 0; j < counts[i]; j++)
                {
                    int newIndex = Random.Shared.Next(0, tokensOfType.Count);
                    initialPicks.Add(tokensOfType[newIndex]);
                    tokensOfType.RemoveAt(newIndex);
                }
            }
            for (int i = 0; i < initialPicks.Count - 1; ++i)
            {
                int newIndex = Random.Shared.Next(i, initialPicks.Count);
                (initialPicks[newIndex], initialPicks[i]) = (initialPicks[i], initialPicks[newIndex]);
            }
            List<string> completedSpecialRules = [];
            Dictionary<Token, int[]> countChangers = [];
            bool countsCorrect = false;
            int iterations = 0;
            while (!countsCorrect)
            {
                iterations++;
                completedSpecialRules.RemoveAll((x) => !initialPicks.Contains(allTokens[x]));
                initialPicks = SpecialRulesAdjustments(scriptTokens, initialPicks, completedSpecialRules, out completedSpecialRules, counts, out counts);
                for (int i = 0; i < initialPicks.Count; i++)
                {
                    Token token = initialPicks[i];
                    if (countChangers.ContainsKey(token))
                    {
                        continue;
                    }
                    int[] additions = CalculateAdditions(token, counts, out bool changed);
                    if (changed)
                    {
                        for (int j = 0; j < counts.Length; j++)
                        {
                            counts[j] += additions[j];
                        }
                        countChangers.Add(token, additions);
                        info.Log($"{token.characterName} changed counts by \"{additions[0]}, {additions[1]}, {additions[2]}, {additions[3]}\"");
                    }
                }
                info.Log($"Current counts: \"{counts[0]}, {counts[1]}, {counts[2]}, {counts[3]}\"");
                for (int i = 0; i < counts.Length; i++)
                {
                    CharacterType type = (CharacterType)Enum.ToObject(typeof(CharacterType), (byte)i);
                    List<Token> initialTokensOfType = initialPicks.Where((x) => x.characterType == type).ToList();
                    List<Token> tokensOfType = scriptTokens.Where((x) => x.characterType == type && !initialTokensOfType.Contains(x)).ToList();
                    int pickNumber = initialTokensOfType.Count;
                    initialTokensOfType.RemoveAll(countChangers.ContainsKey);
                    while (pickNumber != counts[i])
                    {
                        if (pickNumber < counts[i])
                        {
                            pickNumber++;
                            if (tokensOfType.Count == 0)
                            {
                                break;
                            }
                            int addIndex = Random.Shared.Next(0, tokensOfType.Count);
                            initialPicks.Add(tokensOfType[addIndex]);
                            tokensOfType.RemoveAt(addIndex);
                        }
                        else
                        {
                            int removeIndex = -1;
                            if (initialTokensOfType.Count == 0)
                            {
                                if (countChangers.Count == 0)
                                {
                                    break;
                                }
                                List<Token> countChanger = [..countChangers.Keys];
                                removeIndex = Random.Shared.Next(0, countChanger.Count);
                                Token token = countChanger[removeIndex];
                                initialPicks.Remove(token);
                                if (token.characterType == type)
                                {
                                    pickNumber--;
                                }
                                int[] additions = countChangers[token];
                                for (int j = 0; j < counts.Length; j++)
                                {
                                    counts[j] -= additions[j];
                                }
                                info.Log($"Removing {token.characterName} changed counts by \"{-additions[0]}, {-additions[1]}, {-additions[2]}, {-additions[3]}\"");
                                countChangers.Remove(token);
                            }
                            else
                            {
                                removeIndex = Random.Shared.Next(0, initialTokensOfType.Count);
                                Token token = initialTokensOfType[removeIndex];
                                initialPicks.Remove(token);
                                if (token.characterType == type)
                                {
                                    pickNumber--;
                                }
                                initialTokensOfType.RemoveAt(removeIndex);
                                if (countChangers.Count == 0)
                                {
                                    continue;
                                }
                                if (countChangers.TryGetValue(token, out int[] additions))
                                {
                                    for (int j = 0; j < counts.Length; j++)
                                    {
                                        counts[j] -= additions[j];
                                    }
                                    info.Log($"Removing {token.characterName} changed counts by \"{-additions[0]}, {-additions[1]}, {-additions[2]}, {-additions[3]}\"");
                                    countChangers.Remove(token);
                                }
                            }
                        }
                    }
                }
                int countsCorrected = 0;
                for (int i = 0; i < counts.Length; i++)
                {
                    CharacterType type = (CharacterType)Enum.ToObject(typeof(CharacterType), (byte)i);
                    List<Token> initialTokensOfType = initialPicks.Where((x) => x.characterType == type).ToList();
                    if (counts[i] == initialTokensOfType.Count)
                    {
                        countsCorrected++;
                    }
                }
                countsCorrect = countsCorrected >= 4;
                if (iterations >= 10)
                {
                    return null;
                }
            }
            info.Log($"Final counts: \"{counts[0]}, {counts[1]}, {counts[2]}, {counts[3]}\"");
            return initialPicks;
        }
        public static int[] CalculateAdditions(Token token, int[] originalCounts, out bool changedCount)
        {
            changedCount = false;
            int[] additions = new int[4];
            Range2D[] ranges = token.typesToAdd.tokensToAdd;
            for (int i = 0; i < ranges.Length; i++)
            {
                Range2D range = ranges[i];
                int countChange = 0;
                if (range.onlyMinMax)
                {
                    int minOrMax = Random.Shared.Next(0, 2);
                    if (minOrMax == 0)
                    {
                        countChange = range.min;
                    }
                    else
                    {
                        countChange = range.max;
                    }
                }
                else
                {
                    countChange += Random.Shared.Next(range.min, range.max + 1);
                }
                int endValue = originalCounts[i] + additions[i] + countChange;
                if (endValue < 0)
                {
                    countChange -= endValue;
                }
                additions[i] += countChange;
                if (i != 3)
                {
                    additions[3] -= countChange;
                }
                if (countChange != 0)
                {
                    changedCount = true;
                }
            }
            return additions;
        }
        public List<Token> SpecialRulesAdjustments(List<Token> scriptTokens, List<Token> adjustedList, List<string> completedSpecialRules, out List<string> newCompleted, int[] originalCounts, out int[] specialCounts)
        {
            specialCounts = originalCounts;
            List<KeyValuePair<Token, Token>> finalAdditions = [];
            Dictionary<Token, Token> finalRemovals = [];
            bool blockEvil = false;
            for (int i = 0; i < adjustedList.Count; i++)
            {
                bool doingSpecialRule = false;
                Token token = adjustedList[i];
                if (completedSpecialRules.Contains(token.characterName))
                {
                    continue;
                }
                if (token.characterToAdd != null && token.characterToAdd.Length > 0)
                {
                    for (int j = 0; j < token.characterToAdd.Length; j++)
                    {
                        finalAdditions.Add(new KeyValuePair<Token, Token>(token, allTokens[token.characterToAdd[j]]));
                    }
                    completedSpecialRules.Add(token.characterName);
                }
                if (token.forbiddenPair != null && token.forbiddenPair.Length > 0)
                {
                    for (int j = 0; j < token.forbiddenPair.Length; j++)
                    {
                        Token pairToken = allTokens[token.forbiddenPair[j]];
                        if (!(finalRemovals.ContainsKey(pairToken) || finalRemovals.ContainsValue(pairToken)) && !(finalRemovals.ContainsKey(token) || finalRemovals.ContainsValue(token)))
                        {
                            if (Random.Shared.Next(0, 2) == 0)
                            {
                                finalRemovals.Add(token, pairToken);
                            }
                            else
                            {
                                finalRemovals.Add(pairToken, token);
                            }
                        }
                    }
                    completedSpecialRules.Add(token.characterName);
                }
                if (!token.specialRule || (blockEvil && (token.characterType == CharacterType.Demon || token.characterType == CharacterType.Minion)))
                {
                    continue;
                }
                List<string> tokenForbidden = [];
                if (token.forbiddenPair != null)
                {
                    tokenForbidden = [..token.forbiddenPair];
                }
                doingSpecialRule = true;
                switch (token.characterName)
                {
                    case "Atheist":
                        {
                            List<Token> script = scriptTokens;
                            int removed = adjustedList.RemoveAll((x) => x != token);
                            specialCounts = [0, 0, 0, 1];
                            info.Log($"(Atheist) Removing Demons and Minions");
                            script.RemoveAll((x) => tokenForbidden.Contains(x.characterName) || x.characterType == CharacterType.Demon || x.characterType == CharacterType.Minion);
                            while (removed > 0)
                            {
                                if (script.Count == 0)
                                {
                                    break;
                                }
                                int index = Random.Shared.Next(0, script.Count);
                                specialCounts[(int)script[index].characterType] += 1;
                                adjustedList.Add(script[index]);
                                info.Log($"(Atheist) Adding a {script[index].characterName}");
                                removed--;
                            }
                            blockEvil = true;
                            break;
                        }
                    case "Heretic":
                        {
                            if (adjustedList.Contains(allTokens["Baron"]))
                            {
                                List<Token> outsiders = adjustedList.Where((x) => x.characterType == CharacterType.Outsider).ToList();
                                if (outsiders.Count > 0 && Random.Shared.Next(0, 2) == 0)
                                {
                                    int removeIndex = Random.Shared.Next(0, outsiders.Count);
                                    info.Log($"(Heretic) Removing a {outsiders[removeIndex].characterName}!");
                                    specialCounts[(int)outsiders[removeIndex].characterType] -= 1;
                                    adjustedList.Remove(outsiders[removeIndex]);
                                }
                            }
                            break;
                        }
                    case "Kazali":
                        {
                            List<Token> script = scriptTokens;
                            int removed = adjustedList.RemoveAll((x) => x.characterType == CharacterType.Minion);
                            specialCounts[(int)CharacterType.Minion] -= removed;
                            info.Log($"(Kazali) Removing Minions");
                            script.Remove(token);
                            script.RemoveAll((x) => tokenForbidden.Contains(x.characterName) || x.characterType != CharacterType.Townsfolk || adjustedList.Contains(x));
                            while (removed > 0)
                            {
                                if (script.Count == 0)
                                {
                                    break;
                                }
                                int index = Random.Shared.Next(0, script.Count);
                                specialCounts[(int)script[index].characterType] += 1;
                                adjustedList.Add(script[index]);
                                info.Log($"(Kazali) Adding a {script[index].characterName}");
                                script.RemoveAt(index);
                                removed--;
                            }
                            List<Token> townsfolks = adjustedList;
                            townsfolks.Remove(token);
                            List<Token> availableOutsiders = script.Where((x) => x.characterType == CharacterType.Outsider && !adjustedList.Contains(x)).ToList();
                            int max = (int)MathF.Min(availableOutsiders.Count, townsfolks.Count);
                            int targetOutsiders = Random.Shared.Next(0, max);
                            targetOutsiders -= Random.Shared.Next(0, (int)MathF.Floor(max / 2));
                            for (int j = 0; j < targetOutsiders; j++)
                            {
                                int townsIndex = Random.Shared.Next(0, townsfolks.Count);
                                int outsIndex = Random.Shared.Next(0, availableOutsiders.Count);
                                adjustedList[adjustedList.IndexOf(townsfolks[townsIndex])] = availableOutsiders[outsIndex];
                                specialCounts[(int)CharacterType.Townsfolk] -= 1;
                                specialCounts[(int)CharacterType.Outsider] += 1;
                                info.Log($"(Kazali) Replacing a {townsfolks[townsIndex].characterName} with a {availableOutsiders[outsIndex].characterName}");
                                townsfolks.RemoveAt(townsIndex);
                                availableOutsiders.RemoveAt(outsIndex);
                            }
                            break;
                        }
                    case "Legion":
                        {
                            List<Token> script = scriptTokens;
                            int removed = adjustedList.RemoveAll((x) => x.characterType == CharacterType.Minion || tokenForbidden.Contains(x.characterName));
                            specialCounts[(int)CharacterType.Minion] -= removed;
                            info.Log($"(Legion) Removing Minions");
                            script.Remove(token);
                            script.RemoveAll((x) => tokenForbidden.Contains(x.characterName) || x.characterType == CharacterType.Demon || x.characterType == CharacterType.Minion || adjustedList.Contains(x));
                            while (removed > 0)
                            {
                                if (script.Count == 0)
                                {
                                    break;
                                }
                                int index = Random.Shared.Next(0, script.Count);
                                specialCounts[(int)script[index].characterType] += 1;
                                adjustedList.Add(script[index]);
                                info.Log($"(Legion) Adding a {script[index].characterName}");
                                script.RemoveAt(index);
                                removed--;
                            }
                            int legionCount = 1;
                            float halfPlayers = adjustedList.Count / 2f;
                            while (legionCount <= halfPlayers)
                            {
                                List<Token> toReplace = adjustedList.Where((x) => x.characterType != CharacterType.Demon).ToList();
                                int replacingIndex = Random.Shared.Next(0, toReplace.Count);
                                specialCounts[(int)toReplace[replacingIndex].characterType] -= 1;
                                specialCounts[(int)token.characterType] += 1;
                                adjustedList[adjustedList.IndexOf(toReplace[replacingIndex])] = token;
                                info.Log($"(Legion) Replacing a {toReplace[replacingIndex].characterName} with a {token.characterName}");
                                legionCount++;
                            }
                            break;
                        }
                    case "Lil' Monsta":
                        {
                            List<Token> script = scriptTokens;
                            List<Token> availableMinions = script.Where((x) => !tokenForbidden.Contains(x.characterName) && x.characterType == CharacterType.Minion && !adjustedList.Contains(x)).ToList();
                            int minionIndex = Random.Shared.Next(0, availableMinions.Count);
                            specialCounts[(int)CharacterType.Demon] -= 1;
                            specialCounts[(int)CharacterType.Minion] += 1;
                            adjustedList[adjustedList.IndexOf(token)] = availableMinions[minionIndex];
                            info.Log($"(Lil' Monsta) Replacing a {token.characterName} with a {availableMinions[minionIndex].characterName}");
                            break;
                        }
                    case "Lord of Typhon":
                        {
                            List<Token> script = scriptTokens;
                            List<Token> townsfolks = adjustedList.Where((x) => x.characterType == CharacterType.Townsfolk).ToList();
                            List<Token> outsiders = adjustedList.Where((x) => x.characterType == CharacterType.Outsider).ToList();
                            List<Token> availableOutsiders = script.Where((x) => !tokenForbidden.Contains(x.characterName) && x.characterType == CharacterType.Outsider && !adjustedList.Contains(x)).ToList();
                            List<Token> availableMinions = script.Where((x) => !tokenForbidden.Contains(x.characterName) && x.characterType == CharacterType.Minion && !adjustedList.Contains(x)).ToList();
                            List<Token> toReplace = [..townsfolks, ..outsiders];
                            int replacingIndex = Random.Shared.Next(0, toReplace.Count);
                            int minionIndex = Random.Shared.Next(0, availableMinions.Count);
                            specialCounts[(int)toReplace[replacingIndex].characterType] -= 1;
                            specialCounts[(int)CharacterType.Minion] += 1;
                            adjustedList[adjustedList.IndexOf(toReplace[replacingIndex])] = availableMinions[minionIndex];
                            townsfolks.Remove(toReplace[replacingIndex]);
                            int max = (int)MathF.Min(availableOutsiders.Count, townsfolks.Count);
                            int targetOutsiders = Random.Shared.Next(0, max);
                            targetOutsiders -= Random.Shared.Next(0, (int)MathF.Floor(max / 2));
                            for (int j = 0; j < targetOutsiders; j++)
                            {
                                int townsIndex = Random.Shared.Next(0, townsfolks.Count);
                                int outsIndex = Random.Shared.Next(0, availableOutsiders.Count);
                                specialCounts[(int)CharacterType.Townsfolk] -= 1;
                                specialCounts[(int)CharacterType.Outsider] += 1;
                                adjustedList[adjustedList.IndexOf(townsfolks[townsIndex])] = availableOutsiders[outsIndex];
                                info.Log($"(Lord of Typhon) Replacing a {townsfolks[townsIndex].characterName} with a {availableOutsiders[outsIndex].characterName}");
                                townsfolks.RemoveAt(townsIndex);
                                availableOutsiders.RemoveAt(outsIndex);
                            }
                            break;
                        }
                    case "Village Idiot":
                        {
                            int numberToAdd = Random.Shared.Next(0, 3);
                            if (numberToAdd == 0)
                            {
                                break;
                            }
                            List<Token> townsfolks = adjustedList.Where((x) => x.characterType == CharacterType.Townsfolk && x != token).ToList();
                            for (int j = 0; j < numberToAdd; j++)
                            {
                                if (townsfolks.Count == 0)
                                {
                                    break;
                                }
                                int index = Random.Shared.Next(0, townsfolks.Count);
                                adjustedList[adjustedList.IndexOf(townsfolks[index])] = token;
                                info.Log($"(Village Idiot) Replacing a {townsfolks[index].characterName} with a {token.characterName}");
                                townsfolks.RemoveAt(index);
                            }
                            break;
                        }
                    case "Xaan":
                        {
                            List<Token> script = scriptTokens;
                            List<Token> townsfolks = adjustedList.Where((x) => x.characterType == CharacterType.Townsfolk).ToList();
                            List<Token> availableOutsiders = script.Where((x) => !tokenForbidden.Contains(x.characterName) && x.characterType == CharacterType.Outsider && !adjustedList.Contains(x)).ToList();
                            int max = (int)MathF.Min(availableOutsiders.Count, townsfolks.Count);
                            int targetOutsiders = Random.Shared.Next(0, max);
                            targetOutsiders -= Random.Shared.Next(0, (int)MathF.Floor(max / 2));
                            for (int j = 0; j < targetOutsiders; j++)
                            {
                                int townsIndex = Random.Shared.Next(0, townsfolks.Count);
                                int outsIndex = Random.Shared.Next(0, availableOutsiders.Count);
                                specialCounts[(int)CharacterType.Townsfolk] -= 1;
                                specialCounts[(int)CharacterType.Outsider] += 1;
                                adjustedList[adjustedList.IndexOf(townsfolks[townsIndex])] = availableOutsiders[outsIndex];
                                info.Log($"(Xaan) Replacing a {townsfolks[townsIndex].characterName} with a {availableOutsiders[outsIndex].characterName}");
                                townsfolks.RemoveAt(townsIndex);
                                availableOutsiders.RemoveAt(outsIndex);
                            }
                            break;
                        }
                }
                if (doingSpecialRule)
                {
                    i = 0;
                    completedSpecialRules.Add(token.characterName);
                }
            }
            for (int i = 0; i < finalAdditions.Count; i++)
            {
                if (!adjustedList.Contains(finalAdditions[i].Key) || adjustedList.Contains(finalAdditions[i].Value))
                {
                    continue;
                }
                List<Token> toReplace = adjustedList.Where(finalRemovals.ContainsValue).ToList();
                toReplace.Remove(finalAdditions[i].Key);
                if (toReplace.Count == 0)
                {
                    toReplace = adjustedList.Where((x) => x.characterType == finalAdditions[i].Value.characterType).ToList();
                    toReplace.Remove(finalAdditions[i].Key);
                    if (toReplace.Count == 0)
                    {
                        toReplace = adjustedList.Where((x) => x.characterType == CharacterType.Outsider).ToList();
                        toReplace.Remove(finalAdditions[i].Key);
                        if (toReplace.Count == 0)
                        {
                            toReplace = adjustedList.Where((x) => x.characterType == CharacterType.Townsfolk).ToList();
                            toReplace.Remove(finalAdditions[i].Key);
                            if (toReplace.Count == 0)
                            {
                                info.Log($"No valid tokens were found to replace with {finalAdditions[i].Value.characterName}!");
                                continue;
                            }
                        }
                    }
                }
                Token replacingToken = toReplace[Random.Shared.Next(0, toReplace.Count)];
                info.Log($"(Final Additions) Replacing a {replacingToken.characterName} with a {finalAdditions[i].Value.characterName}");
                specialCounts[(int)replacingToken.characterType] -= 1;
                specialCounts[(int)finalAdditions[i].Value.characterType] += 1;
                adjustedList[adjustedList.IndexOf(replacingToken)] = finalAdditions[i].Value;
            }
            foreach (KeyValuePair<Token, Token> pair in finalRemovals)
            {
                if (!adjustedList.Contains(pair.Key) || !adjustedList.Contains(pair.Value))
                {
                    continue;
                }
                Token removingToken = pair.Value;
                adjustedList.Remove(removingToken);
                List<Token> script = scriptTokens;
                List<string> keyForbiddenPairs = [..pair.Key.forbiddenPair];
                List<Token> availableTokens = script.Where((x) => x.characterType == removingToken.characterType && !keyForbiddenPairs.Contains(x.characterName)).ToList();
                if (availableTokens.Count == 0)
                {
                    info.Log($"No available tokens to replace the {pair.Key.characterName} removed {removingToken.characterName} were found!");
                    continue;
                }
                int newIndex = Random.Shared.Next(0, availableTokens.Count);
                Token newToken = availableTokens[newIndex];
                info.Log($"(Final Removals) Adding a {newToken.characterName} to replace a {removingToken.characterName}");
                adjustedList.Add(newToken);
            }
            newCompleted = completedSpecialRules;
            return adjustedList;
        }
        public static Dictionary<string, Token> GenerateTokens()
        {
            Dictionary<string, Token> tokens = [];
            CharacterType[] characterTypes = Enum.GetValues<CharacterType>();
            for (int i = 0; i < characterTypes.Length; i++)
            {
                tokens = tokens.Concat(TokensFromType(characterTypes[i])).ToDictionary();
            }
            return tokens;
        }
        public static Dictionary<string, Token> TokensFromType(CharacterType type)
        {
            Dictionary<string, Token> newTokens = [];
            switch (type)
            {
                case CharacterType.Demon:
                    {
                        newTokens = GenerateDemons();
                        break;
                    }
                case CharacterType.Minion:
                    {
                        newTokens = GenerateMinions();
                        break;
                    }
                case CharacterType.Outsider:
                    {
                        newTokens = GenerateOutsiders();
                        break;
                    }
                case CharacterType.Townsfolk:
                    {
                        newTokens = GenerateTownsfolks();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            return newTokens;
        }
        public static Dictionary<string, Token> GenerateDemons()
        {
            List<Token> demons = [];
            demons.Add(new("Al-Hadikhia", otherOrder: 38, otherDesc: "Al-Hadikhia chooses 3 players, each chosen player chooses whether they live or die. If all 3 choose to live, they all die", desc: "Each night*, you may choose 3 players (all players learn who): each silently chooses to live or die, but if all live, all die"));
            demons.Add(new("Fang Gu", addingTypes: new(outsiders: new(1)), otherOrder: 32, otherDesc: "Fang Gu chooses a player to kill", desc: "Each night*, choose a player: they die. The 1st Outsider this kills becomes an evil Fang Gu & you die instead\r[+1 Outsider]"));
            demons.Add(new("Imp", otherOrder: 27, otherDesc: "Imp chooses a player to kill", desc: "Each night*, choose a player: they die. If you kill yourself this way, a Minion becomes the Imp"));
            demons.Add(new("Kazali", rule: true, firstOrder: 1, firstDesc: "Kazali chooses as many players as the grimoire allows to become a minion of their choice",  otherOrder: 42, otherDesc: "Kazali chooses a player to kill", desc: "Each night*, choose a player: they die. You choose which players are which Minions\r[-? to +? Outsiders]"));
            demons.Add(new("Legion", removingCharacters: ["Engineer"], rule: true, otherOrder: 26, otherDesc: "The storyteller chooses a player to kill", desc: "Each night*, a player might die. Executions fail if only evil voted. You register as a Minion too\r[Most players are Legion]"));
            demons.Add(new("Leviathan", firstOrder: 66, firstDesc: "At dawn, announce publicly that the Leviathan is in play", otherOrder: 82, otherDesc: "At dawn, if 2 good players are executed, evil wins. At the dawn of the 5th day, evil wins", desc: "If more than 1 good player is executed, evil wins. All players know you are in play. After day 5, evil wins"));
            demons.Add(new("Lil' Monsta", rule: true, firstOrder: 18, firstDesc: "Minions choose which minion holds Lil' Monsta", otherOrder: 40, otherDesc: "Minions choose which minion holds Lil' Monsta, the storyteller might choose a player to kill", desc: "Each night, Minions choose who babysits Lil' Monsta & \"is the Demon\". Each night*, a player might die\r[+1 Minion]"));
            demons.Add(new("Lleech", firstOrder: 19, firstDesc: "Lleech chooses a player to poison and become their host", otherOrder: 39, otherDesc: "Lleech chooses a player to kill", desc: "Each night*, choose a player: they die. You start by choosing a player: they are poisoned. You die if & only if they are dead"));
            demons.Add(new("Lord of Typhon", rule: true, firstOrder: 0, firstDesc: "The Lord of Typhon's neighbours seperately learn that they are minions and learn their roles", otherOrder: 35, otherDesc: "Lord of Typhon chooses a player to kill", desc: "Each night*, choose a player: they die. Evil characters are in a line. You are in the middle\r[+1 Minion. -? to +? Outsiders]"));
            demons.Add(new("No Dashii", otherOrder: 33, otherDesc: "No Dashii chooses a player to kill", desc: "Each night*, choose a player: they die. Your 2 Townsfolk neighbors are poisoned"));
            demons.Add(new("Ojo", otherOrder: 37, otherDesc: "Ojo chooses a character to kill. If their choice is not in play, the storyteller chooses a player to kill", desc: "Each night*, choose a character: they die. If they are not in play, the Storyteller chooses who dies"));
            demons.Add(new("Po", otherOrder: 31, otherDesc: "Po chooses a player to kill. If they choose nobody, the Po chooses 3 players to kill the next night", desc: "Each night*, you may choose a player: they die. If your last choice was no-one, choose 3 players tonight"));
            demons.Add(new("Pukka", firstOrder: 35, firstDesc: "Pukka chooses a player to poison, their choice dies a day later", otherOrder: 29, otherDesc: "Pukka chooses a player to poison, their choice dies a day later", desc: "Each night, choose a player: they are poisoned. The previously poisoned player dies then becomes healthy"));
            demons.Add(new("Riot", desc: "On day 3, Minions become Riot & nominees die but nominate an alive player immediately. This must happen"));
            demons.Add(new("Shabaloth", otherOrder: 30, otherDesc: "Shabaloth chooses 2 players to kill. A player they chose the night before might come back to life", desc: "Each night*, choose 2 players: they die. A dead player you chose last night might be regurgitated"));
            demons.Add(new("Vigormortis", addingTypes: new(outsiders: new(-1)), otherOrder: 36, otherDesc: "Vigormortis chooses a player to kill", desc: "Each night*, choose a player: they die. Minions you kill keep their ability & poison 1 Townsfolk neighbor\r[-1 Outsider]"));
            demons.Add(new("Vortox", otherOrder: 34, otherDesc: "Vortix chooses a player to kill", desc: "Each night*, choose a player: they die. Townsfolk abilities yield false info. Each day, if no-one is executed, evil wins"));
            demons.Add(new("Yaggababble", firstOrder: 6, firstDesc: "Yaggababble learns their secret phrase", otherOrder: 41, otherDesc: "The storyteller chooses to kill up to as many players as the amount of times the Yaggababble spoke their secret phrase in public the day before", desc: "You start knowing a secret phrase. For each time you said it publicly today, a player might die"));
            demons.Add(new("Zombuul", otherOrder: 28, otherDesc: "If nobody else died today, Zombuul chooses a player to kill", desc: "Each night*, if no-one died today, choose a player: they die. The 1st time you die, you live but register as dead"));
            Dictionary<string, Token> newDict = [];
            for (int i = 0; i < demons.Count; i++)
            {
                Token demon = demons[i];
                demon.characterType = CharacterType.Demon;
                newDict.Add(demon.characterName, demon);
            }
            return newDict;
        }
        public static Dictionary<string, Token> GenerateMinions()
        {
            List<Token> minions = [];
            minions.Add(new("Assassin", otherOrder: 43, otherDesc: "Once per game, Assassin chooses a player to kill. They die even if protected somehow", desc: "Once per game, at night*, choose a player: they die, even if for some reason they could not"));
            minions.Add(new("Baron", addingTypes: new(outsiders: new(2)), desc: "There are extra Outsiders in play\r[+2 Outsiders]"));
            minions.Add(new("Boffin", firstOrder: 2, firstDesc: "Boffin and the demon learn which ability the demon gains", desc: "The Demon (even if drunk or poisoned) has a not-in-play good character's ability. You both know which"));
            minions.Add(new("Boomdandy", desc: "If you are executed, all but 3 players die. After a 10 to 1 countdown, the player with the most players pointing at them, dies"));
            minions.Add(new("Cerenovus", firstOrder: 31, firstDesc: "Cerenovus chooses a player to be \"mad\" that they are another character tomorrow", otherOrder: 17, otherDesc: "Cerenovus chooses a player to be \"mad\" that they are another character tomorrow", desc: "Each night, choose a player & a good character: they are \"mad\" they are this character tomorrow, or might be executed"));
            minions.Add(new("Devil's Advocate", firstOrder: 28, firstDesc: "Devil's Advocate chooses a living player to protect from execution tomorrow", otherOrder: 15, otherDesc: "Devil's Advocate chooses a living player different from the last choice to protect from execution tomorrow", desc: "Each night, choose a living player (different to last night): if executed tomorrow, they don't die"));
            minions.Add(new("Evil Twin", firstOrder: 29, firstDesc: "Both twins learn who eachother are", desc: "You & an opposing player know each other. If the good player is executed, evil wins. Good can't win if you both live"));
            minions.Add(new("Fearmonger", firstOrder: 32, firstDesc: "Fearmonger chooses a player, if they nominate and execute them, the chosen player's team loses", otherOrder: 19, otherDesc: "Fearmonger chooses a player, if they nominate and execute them, the chosen player's team loses. If the new choice is different from the last choice, it is publicly announce that the choice has changed at dawn", desc: "Each night, choose a player: if you nominate & execute them, their team loses. All players know if you choose a new player"));
            minions.Add(new("Goblin", desc: "If you publicly claim to be the Goblin when nominated & are executed that day, your team wins"));
            minions.Add(new("Godfather", addingTypes: new(outsiders: new(-1, 1, true)), removingCharacters: ["Heretic"], firstOrder: 26, firstDesc: "Godfather learns which outsiders are in play", otherOrder: 44, otherDesc: "If an outsider died today, Godfather chooses a player to kill", desc: "You start knowing which Outsiders are in play. If 1 died today, choose a player tonight: they die\r[-1 or +1 Outsider]"));
            minions.Add(new("Harpy", firstOrder: 33, firstDesc: "Harpy chooses a player to be \"mad\" that another player is evil", otherOrder: 20, otherDesc: "Harpy chooses a player to be \"mad\" that another player is evil", desc: "Each night, choose 2 players: tomorrow, the 1st player is mad that the 2nd is evil, or one or both might die"));
            minions.Add(new("Marionette", firstOrder: 15, firstDesc: "The demon learns who the Marionette is", desc: "You think you are a good character, but you are not. The Demon knows who you are. You neighbor the Demon"));
            minions.Add(new("Mastermind", desc: "If the Demon dies by execution (ending the game), play for 1 more day. If a player is then executed, their team loses"));
            minions.Add(new("Mezepheles", firstOrder: 34, firstDesc: "Mezepheles learns their secret phrase", otherOrder: 21, otherDesc: "Once per game, if a good player spoke the Mezepheles phrase today, they become evil", desc: "You start knowing a secret word. The 1st good player to say this word becomes evil that night"));
            minions.Add(new("Organ Grinder", firstOrder: 27, firstDesc: "Organ Grinder chooses whether or not to be drunk the following day", otherOrder: 14, otherDesc: "Organ Grinder chooses whether or not to be drunk the following day", desc: "All players keep their eyes closed when voting and the vote tally is secret. Each night, choose if you are drunk until dusk"));
            minions.Add(new("Pit-Hag", otherOrder: 18, otherDesc: "Pit-Hag chooses a player to become a character", desc: "Each night*, choose a player & a character they become (if not in play). If a Demon is made, deaths tonight are arbitrary"));
            minions.Add(new("Poisoner", firstOrder: 21, firstDesc: "Poisoner chooses a player to poison today", otherOrder: 6, otherDesc: "Poisoner chooses a player to poison today", desc: "Each night, choose a player: they are poisoned tonight and tomorrow day"));
            minions.Add(new("Psychopath", desc: "Each day, before nominations, you may publicly choose a player: they die. If executed, you only die if you lose roshambo"));
            minions.Add(new("Scarlet Woman", desc: "If there are 5 or more players alive & the Demon dies, you become the Demon"));
            minions.Add(new("Spy", removingCharacters: ["Heretic"], firstOrder: 60, firstDesc: "Spy sees the grimoire", otherOrder: 77, otherDesc: "Spy sees the grimoire", desc: "Each night, you see the Grimoire. You might register as good & as a Townsfolk or Outsider, even if dead"));
            minions.Add(new("Summoner", addingTypes: new(demons: new(-1)), firstOrder: 11, firstDesc: "Summoner learns 3 bluffs", otherOrder: 22, otherDesc: "On the 3rd night, Summoner chooses a player to become a demon of their choice", desc: "You get 3 bluffs. On the 3rd night, choose a player: they become an evil Demon of your choice\r[No Demon]"));
            minions.Add(new("Vizier", firstOrder: 67, firstDesc: "At dawn, it is publicly announced that a Vizier is in play and which player it is", desc: "All players know you are the Vizier. You cannot die during the day. If good voted, you may choose to execute immediately"));
            minions.Add(new("Widow", removingCharacters: ["Heretic"], firstOrder: 22, firstDesc: "Widow sees the grimoire and chooses a player to poison. The storyteller chooses a good player to learn the Widow is in play", desc: "On your 1st night, look at the Grimoire & choose a player: they are poisoned. 1 good player knows a Widow is in play"));
            minions.Add(new("Witch", firstOrder: 30, firstDesc: "Witch chooses a player, if the chosen player nominates the following day, they die", otherOrder: 16, otherDesc: "Unless only 3 players remain, Witch chooses a player, if the chosen player nominates the following day, they die", desc: "Each night, choose a player: if they nominate tomorrow, they die. If just 3 players live, you lose this ability"));
            minions.Add(new("Wizard", firstOrder: 24, firstDesc: "Once per game, Wizard may make a wish", otherOrder: 9, otherDesc: "Once per game, Wizard may make a wish", desc: "Once per game, choose to make a wish. If granted, it might have a price & leave a clue as to its nature"));
            minions.Add(new("Wraith", desc: "You may choose to open your eyes at night. You wake when other evil players do"));
            minions.Add(new("Xaan", rule: true, firstOrder: 20, firstDesc: "Note how many outsiders are in play", otherOrder: 5, otherDesc: "On the night equalling the number of outsiders of the 1st night, all townsfolk are poisoned for the day", desc: "On night X, all Townsfolk are poisoned until dusk\r[X Outsiders]"));
            Dictionary<string, Token> newDict = [];
            for (int i = 0; i < minions.Count; i++)
            {
                Token minion = minions[i];
                minion.characterType = CharacterType.Minion;
                newDict.Add(minion.characterName, minion);
            }
            return newDict;
        }
        public static Dictionary<string, Token> GenerateOutsiders()
        {
            List<Token> outsiders = [];
            outsiders.Add(new("Barber", otherOrder: 47, otherDesc: "If Barber died today, the demon can choose 2 players to swap characters", desc: "If you died today or tonight, the Demon may choose 2 players (not another Demon) to swap characters"));
            outsiders.Add(new("Butler", firstOrder: 46, firstDesc: "Butler chooses a player, they can only vote tomorrow if the chosen player votes", otherOrder: 76, otherDesc: "Butler chooses a player, they can only vote tomorrow if the chosen player votes", desc: "Each night, choose a player (not yourself): tomorrow, you may only vote if they are voting too"));
            outsiders.Add(new("Damsel", firstOrder: 38, firstDesc: "The minions seperately learn that the Damsel is in play", otherOrder: 54, otherDesc: "Minions that do not already know seperately learn that the Damsel is in play", desc: "All Minions know a Damsel is in play. If a Minion publicly guesses you (once), your team loses"));
            outsiders.Add(new("Drunk", desc: "You do not know you are the Drunk. You think you are a Townsfolk character, but you are not"));
            outsiders.Add(new("Golem", desc: "You may only nominate once per game. When you do, if the nominee is not the Demon, they die"));
            outsiders.Add(new("Goon", desc: "Each night, the 1st player to choose you with their ability is drunk until dusk. You become their alignment"));
            outsiders.Add(new("Hatter", otherOrder: 46, otherDesc: "If Hatter died today, the minions and the demon may choose new minions and demons to become", desc: "If you died today or tonight, the Minion & Demon players may choose new Minion & Demon characters to be"));
            outsiders.Add(new("Heretic", removingCharacters: ["Godfather", "Spy", "Widow"], rule: true, desc: "Whoever wins, loses & whoever loses, wins, even if you are dead"));
            outsiders.Add(new("Hermit", addingTypes: new(outsiders: new(-1, 0)), desc: "You have all Outsider abilities\r[-0 or -1 Outsider]"));
            outsiders.Add(new("Klutz", desc: "When you learn that you died, publicly choose 1 alive player: if they are evil, your team loses"));
            outsiders.Add(new("Lunatic", firstOrder: 10, firstDesc: "The real demon learns who Lunatic is", otherOrder: 23, otherDesc: "If Lunatic chooses to kill player(s) at night, the real demon learns this choice", desc: "You think you are a Demon, but you are not. The Demon knows who you are & who you choose at night"));
            outsiders.Add(new("Moonchild", otherOrder: 58, otherDesc: "If Moonchild died today, Moonchild chooses 1 alive player. If the chosen player was good, they die", desc: "When you learn that you died, publicly choose 1 alive player. Tonight, if it was a good player, they die"));
            outsiders.Add(new("Mutant", desc: "If you are \"mad\" about being an Outsider, you might be executed"));
            outsiders.Add(new("Ogre", firstOrder: 61, firstDesc: "Ogre chooses a player, they become the chosen player's alignment but do not find out what the alignment is", desc: "On your 1st night, choose a player (not yourself): you become their alignment (you don't know which) even if drunk or poisoned"));
            outsiders.Add(new("Plague Doctor", desc: "When you die, the Storyteller gains a Minion ability"));
            outsiders.Add(new("Politician", desc: "If you were the player most responsible for your team losing, you change alignment & win, even if dead"));
            outsiders.Add(new("Puzzlemaster", desc: "1 player is drunk, even if you die. If you guess (once) who it is, learn the Demon player, but guess wrong & get false info"));
            outsiders.Add(new("Recluse", desc: "You might register as evil & as a Minion or Demon, even if dead"));
            outsiders.Add(new("Saint", desc: "If you die by execution, your team loses"));
            outsiders.Add(new("Snitch", firstOrder: 9, firstDesc: "Each minion seperately learns 3 bluffs", desc: "Each Minion gets 3 bluffs"));
            outsiders.Add(new("Sweetheart", otherOrder: 48, otherDesc: "If Sweetheart died today, the storyteller chooses a player to become drunk for the rest of the game", desc: "When you die, 1 player is drunk from now on"));
            outsiders.Add(new("Tinker", otherOrder: 57, otherDesc: "The storyteller may choose to kill Tinker", desc: "You might die at any time"));
            outsiders.Add(new("Zealot", desc: "If there are 5 or more players alive, you must vote for every nomination"));
            Dictionary<string, Token> newDict = [];
            for (int i = 0; i < outsiders.Count; i++)
            {
                Token outsider = outsiders[i];
                outsider.characterType = CharacterType.Outsider;
                newDict.Add(outsider.characterName, outsider);
            }
            return newDict;
        }
        public static Dictionary<string, Token> GenerateTownsfolks()
        {
            List<Token> townsfolks = [];
            townsfolks.Add(new("Acrobat", otherOrder: 11, otherDesc: "Acrobat chooses a player, if their choice is drunk or poisoned, Acrobat dies", desc: "Each night*, choose a player: if they are or become drunk or poisoned tonight, you die"));
            townsfolks.Add(new("Alchemist", firstOrder: 4, firstDesc: "Alchemist learns what their minion ability is", desc: "You have a Minion ability. When using this, the Storyteller may prompt you to choose differently"));
            townsfolks.Add(new("Alsaahir", desc: "Each day, if you publicly guess which players are Minion(s) and which are Demon(s), good wins"));
            townsfolks.Add(new("Amnesiac", firstOrder: 39, firstDesc: "If Amnesiac's ability wakes them up on the 1st night, they do", otherOrder: 55, otherDesc: "If Amnesiac's ability wakes them up tonight night, they do", desc: "You do not know what your ability is. Each day, privately guess what it is: you learn how accurate you are"));
            townsfolks.Add(new("Artist", desc: "Once per game, during the day, privately ask the Storyteller any yes/no question"));
            townsfolks.Add(new("Atheist", rule: true, desc: "The Storyteller can break the game rules, and if executed, good wins, even if you are dead\r[No evil characters]"));
            townsfolks.Add(new("Balloonist", addingTypes: new(outsiders: new(0, 1)), firstOrder: 54, firstDesc: "Balloonist learns a player", otherOrder: 70, otherDesc: "Balloonist learns a player of a different character type to the last player learned", desc: "Each night, you learn a player of a different character type than last night\r[+0 or +1 Outsider]"));
            townsfolks.Add(new("Banshee", otherOrder: 50, otherDesc: "If Banshee was killed by the demon, it is publicly announced that a Banshee died but not who Banshee was", desc: "If the Demon kills you, all players learn this. From now on, you may nominate twice per day and vote twice per nomination"));
            townsfolks.Add(new("Bounty Hunter", firstOrder: 57, firstDesc: "Bounty Hunter learns an evil player", otherOrder: 73, otherDesc: "If Bounty Hunter's last learned player dies, they learn a new evil player", desc: "You start knowing 1 evil player. If the player you know dies, you learn another evil player tonight\r[1 Townsfolk is evil]"));
            townsfolks.Add(new("Cannibal", desc: "You have the ability of the recently killed executee. If they are evil, you are poisoned until a good player dies by execution"));
            townsfolks.Add(new("Chambermaid", firstOrder: 64, firstDesc: "Chambermaid chooses 2 alive players and learns how many of them woke tonight due to their ability", otherOrder: 80, otherDesc: "Chambermaid chooses 2 alive players and learns how many of them woke tonight due to their ability", desc: "Each night, choose 2 alive players (not yourself): you learn how many woke tonight due to their ability"));
            townsfolks.Add(new("Chef", firstOrder: 43, firstDesc: "Chef learns how many pairs of evil players there are", desc: "You start knowing how many pairs of evil players there are"));
            townsfolks.Add(new("Choirboy", addingCharacters: ["King"], otherOrder: 52, otherDesc: "If the demon killed the king tonight, Choirboy learns who the demon is", desc: "If the Demon kills the King, you learn which player is the Demon\r[+the King]"));
            townsfolks.Add(new("Clockmaker", firstOrder: 48, firstDesc: "Clockmaker learns how many steps the demon is from their nearest minion", desc: "You start knowing how many steps from the Demon to its nearest Minion"));
            townsfolks.Add(new("Courtier", firstOrder: 23, firstDesc: "Once per game, Courtier chooses a character, this character is drunk for the next 3 days", otherOrder: 7, otherDesc: "Once per game, Courtier chooses a character, this character is drunk for the next 3 days", desc: "Once per game, at night, choose a character: they are drunk for 3 nights & 3 days"));
            townsfolks.Add(new("Cult Leader", firstOrder: 59, firstDesc: "Cult Leader becomes the alignment of one of their neighbours, they do not learn which alignment", otherOrder: 75, otherDesc: "Cult Leader becomes the alignment of one of their neighbours, they do not learn which alignment", desc: "Each night, you become the alignment of an alive neighbor. If all good players choose to join your cult, your team wins"));
            townsfolks.Add(new("Dreamer", firstOrder: 49, firstDesc: "Dreamer chooses a player and learns 1 good and 1 evil character, one of which is the chosen player's character", otherOrder: 64, otherDesc: "Dreamer chooses a player and learns 1 good and 1 evil character, one of which is the chosen player's character", desc: "Each night, choose a player (not yourself or Travellers): you learn 1 good & 1 evil character, 1 of which is correct"));
            townsfolks.Add(new("Empath", firstOrder: 44, firstDesc: "Empath learns how many of their alive neighbours are evil", otherOrder: 61, otherDesc: "Empath learns how many of their alive neighbours are evil", desc: "Each night, you learn how many of your 2 alive neighbors are evil"));
            townsfolks.Add(new("Engineer", removingCharacters: ["Legion"], firstOrder: 16, firstDesc: "Once per game, chooses a demon to change the demon to, and chooses minions for the minions to become", otherOrder: 3, otherDesc: "Once per game, chooses a demon to change the demon to, and chooses minions for the minions to become", desc: "Once per game, at night, choose which Minions or which Demon is in play"));
            townsfolks.Add(new("Exorcist", otherOrder: 24, otherDesc: "Exorcist chooses a different player from the last choice. If the demon is chosen, the demon learns who Exorcist is and does not otherwise make choices tonight", desc: "Each night*, choose a player (different to last night): the Demon, if chosen, learns who you are then doesn't wake tonight"));
            townsfolks.Add(new("Farmer", otherOrder: 56, otherDesc: "If Farmer died at night, another alive good player becomes a new Farmer and learns this", desc: "When you die at night, an alive good player becomes a Farmer"));
            townsfolks.Add(new("Fisherman", desc: "Once per game, during the day, visit the Storyteller for some advice to help your team win"));
            townsfolks.Add(new("Flowergirl", otherOrder: 65, otherDesc: "Flowergirl learns whether or not a demon voted today", desc: "Each night*, you learn if a Demon voted today"));
            townsfolks.Add(new("Fool", desc: "The 1st time you die, you don't"));
            townsfolks.Add(new("Fortune Teller", firstOrder: 45, firstDesc: "Fortune Teller chooses 2 players and learns if one of them registers as the demon", otherOrder: 62, otherDesc: "Fortune Teller chooses 2 players and learns if one of them was the demon", desc: "Each night, choose 2 players: you learn if either is a Demon. There is a good player that registers as a Demon to you"));
            townsfolks.Add(new("Gambler", otherOrder: 10, otherDesc: "Gambler chooses a player and guesses their character, if they are incorrect they die", desc: "Each night*, choose a player & guess their character: if you guess wrong, you die"));
            townsfolks.Add(new("General", firstOrder: 63, firstDesc: "General learns which alignment the storyteller currently believes is winning", otherOrder: 79, otherDesc: "General learns which alignment the storyteller currently believes is winning", desc: "Each night, you learn which alignment the Storyteller believes is winning: good, evil, or neither"));
            townsfolks.Add(new("Gossip", otherOrder: 45, otherDesc: "If Gossip's statement today was true, a player dies", desc: "Each day, you may make a public statement. Tonight, if it was true, a player dies"));
            townsfolks.Add(new("Grandmother", firstOrder: 47, firstDesc: "Grandmother learns a good player and their character", otherOrder: 59, otherDesc: "If Grandmother's learned player is killed by the demon, Grandmother dies", desc: "You start knowing a good player & their character. If the Demon kills them, you die too"));
            townsfolks.Add(new("High Priestess", firstOrder: 62, firstDesc: "High Priestess learns which player the storyteller believes they should talk to", otherOrder: 78, otherDesc: "High Priestess learns which player the storyteller believes they should talk to", desc: "Each night, learn which player the Storyteller believes you should talk to most"));
            townsfolks.Add(new("Huntsman", addingCharacters: ["Damsel"], firstOrder: 37, firstDesc: "Once per game, Huntsman chooses a player. If the chosen player is Damsel, they become a not-in-play townsfolk", otherOrder: 53, otherDesc: "Once per game, Huntsman chooses a player. If the chosen player is Damsel, they become a not-in-play townsfolk", desc: "Once per game, at night, choose a living player: the Damsel, if chosen, becomes a not-in-play Townsfolk\r[+the Damsel]"));
            townsfolks.Add(new("Innkeeper", otherOrder: 8, otherDesc: "Innkeeper chooses 2 players, neither can die at night, and one is drunk until tomorrow night", desc: "Each night*, choose 2 players: they can't die tonight, but 1 is drunk until dusk"));
            townsfolks.Add(new("Investigator", firstOrder: 42, firstDesc: "Investigator learns a minion and 2 players, one of these players is that minion", desc: "You start knowing that 1 of 2 players is a particular Minion"));
            townsfolks.Add(new("Juggler", otherOrder: 69, otherDesc: "Juggler learns how many of the guesses they publicly made today were correct", desc: "On your 1st day, publicly guess up to 5 players' characters. That night, you learn how many you got correct"));
            townsfolks.Add(new("King", firstOrder: 13, firstDesc: "The demon learns who King is", otherOrder: 72, otherDesc: "If the number of dead players exceeds living players, King learns an alive player's character", desc: "Each night, if the dead equal or outnumber the living, you learn 1 alive character. The Demon knows you are the King"));
            townsfolks.Add(new("Knight", firstOrder: 52, firstDesc: "Knight learns 2 players that are not the demon", desc: "You start knowing 2 players that are not the Demon"));
            townsfolks.Add(new("Librarian", firstOrder: 41, firstDesc: "Librarian learns an outsider and 2 players, one of these players is that outsider. If there are no outsiders, Librarian learns this", desc: "You start knowing that 1 of 2 players is a particular Outsider (Or that zero are in play)"));
            townsfolks.Add(new("Lycanthrope", otherOrder: 25, otherDesc: "Lycanthrope chooses a player to kill. Unless the chosen player registers as evil, they die and the demon does not kill tonight", desc: "Each night*, choose an alive player. If good, they die & the Demon doesn’t kill tonight. One good player registers as evil"));
            townsfolks.Add(new("Magician", firstOrder: 7, firstDesc: "Minions are shown both Magician and the demon to be their demon, and the demon is additionally shown Magician as one of their minions. This is done instead of minions and the demon learning their teammates", desc: "The Demon thinks you are a Minion. Minions think you are a Demon"));
            townsfolks.Add(new("Mathematician", firstOrder: 65, firstDesc: "Mathematician learns how many character abilities worked abnormally due to another character's ability today", otherOrder: 81, otherDesc: "Mathematician learns how many character abilities worked abnormally due to another character's ability today", desc: "Each night, you learn how many players' abilities worked abnormally (since dawn) due to another character's ability"));
            townsfolks.Add(new("Mayor", desc: "If only 3 players live & no execution occurs, your team wins. If you die at night, another player might die instead"));
            townsfolks.Add(new("Minstrel", desc: "When a Minion dies by execution, all other players (except Travellers) are drunk until dusk tomorrow"));
            townsfolks.Add(new("Monk", otherOrder: 13, otherDesc: "Monk chooses another player, the chosen player cannot be killed by the demon tonight", desc: "Each night*, choose a player (not yourself): they are safe from the Demon tonight"));
            townsfolks.Add(new("Nightwatchman", firstOrder: 58, firstDesc: "Once per game, Nightwatchman chooses a player. The chosen player learns who Nightwatchman is", otherOrder: 74, otherDesc: "Once per game, Nightwatchman chooses a player. The chosen player learns who Nightwatchman is", desc: "Once per game, at night, choose a player: they learn you are the Nightwatchman"));
            townsfolks.Add(new("Noble", firstOrder: 53, firstDesc: "Noble learns 3 players, only 1 of which is evil", desc: "You start knowing 3 players, 1 and only 1 of which is evil"));
            townsfolks.Add(new("Oracle", otherOrder: 67, otherDesc: "Oracle learns how many dead players are evil", desc: "Each night*, you learn how many dead players are evil"));
            townsfolks.Add(new("Pacifist", desc: "Executed good players might not die"));
            townsfolks.Add(new("Philosopher", firstOrder: 3, firstDesc: "Once per game, Philosopher chooses a character and gains its ability. If the chosen character is in play, it becomes drunk", otherOrder: 0, otherDesc: "Once per game, Philosopher chooses a character and gains its ability. If the chosen character is in play, it becomes drunk", desc: "Once per game, at night, choose a good character: gain that ability. If this character is in play, they are drunk"));
            townsfolks.Add(new("Pixie", firstOrder: 36, firstDesc: "Pixie learns an in-play character to be \"mad\" about being", desc: "You start knowing 1 in-play Townsfolk. If you were mad that you were this character, you gain their ability when they die"));
            townsfolks.Add(new("Poppy Grower", firstOrder: 5, firstDesc: "When the demon wakes, they do not learn who the minions are", otherOrder: 1, otherDesc: "If Poppy Grower died today, the demon learns who the minions are and the minions learn who the demon is", desc: "Minions & Demons do not know each other. If you die, they learn who each other are that night"));
            townsfolks.Add(new("Preacher", firstOrder: 17, firstDesc: "Preacher chooses a player. If the chosen player is a minion, their ability stops working and they learn this", otherOrder: 4, otherDesc: "Preacher chooses a player. If the chosen player is a minion, their ability stops working and they learn this", desc: "Each night, choose a player: a Minion, if chosen, learns this. All chosen Minions have no ability"));
            townsfolks.Add(new("Princess", desc: "On your 1st day, if you nominated & executed a player, the Demon doesn’t kill tonight"));
            townsfolks.Add(new("Professor", otherOrder: 51, otherDesc: "Once per game, Professor chooses a dead player. If the chosen player is a townsfolk, they become alive again", desc: "Once per game, at night*, choose a dead player: if they are a Townsfolk, they are resurrected"));
            townsfolks.Add(new("Ravenkeeper", otherOrder: 60, otherDesc: "If Ravenkeeper died tonight they choose a player and learn the chosen player's character", desc: "If you die at night, you are woken to choose a player: you learn their character"));
            townsfolks.Add(new("Sage", otherOrder: 49, otherDesc: "If Sage died to the demon tonight, Sage learns that the demon is one of 2 players", desc: "If the Demon kills you, you learn that it is 1 of 2 players"));
            townsfolks.Add(new("Sailor", firstOrder: 14, firstDesc: "Sailor chooses a player. Either Sailor or the chosen player are drunk until the next night", otherOrder: 2, otherDesc: "Sailor chooses a player. Either Sailor or the chosen player are drunk until the next night", desc: "Each night, choose an alive player: either you or they are drunk until dusk. You can't die"));
            townsfolks.Add(new("Savant", desc: "Each day, you may visit the Storyteller to learn 2 things in private: 1 is true & 1 is false"));
            townsfolks.Add(new("Seamstress", firstOrder: 50, firstDesc: "Once per game, Seamstress chooses 2 other players. Seamstress learns if the chosen players are of the same alignment", otherOrder: 68, otherDesc: "Once per game, Seamstress chooses 2 other players. Seamstress learns if the chosen players are of the same alignment", desc: "Once per game, at night, choose 2 players (not yourself): you learn if they are the same alignment"));
            townsfolks.Add(new("Shugenja", firstOrder: 55, firstDesc: "Shugenja learns if the nearest evil player is clockwise or anti-clockwise. If the 2 closest evil players are equidistant from Shugenja, this information is arbitrary", desc: "You start knowing if your closest evil player is clockwise or anti-clockwise. If equidistant, this info is arbitrary"));
            townsfolks.Add(new("Slayer", desc: "Once per game, during the day, publicly choose a player: if they are the Demon, they die"));
            townsfolks.Add(new("Snake Charmer", firstOrder: 25, firstDesc: "Snake Charmer chooses an alive player. If the chosen player is the demon, the demon and Snake Charmer swap characters and alignments, then the new Snake Charmer is poisoned", otherOrder: 12, otherDesc: "Snake Charmer chooses an alive player. If the chosen player is the demon, the demon and Snake Charmer swap characters and alignments, then the new Snake Charmer is poisoned", desc: "Each night, choose an alive player: a chosen Demon swaps characters & alignments with you & is then poisoned"));
            townsfolks.Add(new("Soldier", desc: "You are safe from the Demon"));
            townsfolks.Add(new("Steward", firstOrder: 51, firstDesc: "Steward learns a good player", desc: "You start knowing 1 good player"));
            townsfolks.Add(new("Tea Lady", desc: "If both your alive neighbors are good, they can't die"));
            townsfolks.Add(new("Town Crier", otherOrder: 66, otherDesc: "Town Crier learns if a minion nominated today", desc: "Each night*, you learn if a Minion nominated today"));
            townsfolks.Add(new("Undertaker", otherOrder: 63, otherDesc: "Undertaker learns the character of today's executed player", desc: "Each night*, you learn which character died by execution today"));
            townsfolks.Add(new("Village Idiot", rule: true, firstOrder: 56, firstDesc: "Village Idiot chooses a player and learns their alignment", otherOrder: 71, otherDesc: "Village Idiot chooses a player and learns their alignment", desc: "Each night, choose a player: you learn their alignment\r[+0 to +2 Village Idiots. 1 of the extras is drunk]"));
            townsfolks.Add(new("Virgin", desc: "The 1st time you are nominated, if the nominator is a Townsfolk, they are executed immediately"));
            townsfolks.Add(new("Washerwoman", firstOrder: 40, firstDesc: "Washerwoman learns a townsfolk and 2 players, one of these players is that townsfolk", desc: "You start knowing that 1 of 2 players is a particular Townsfolk"));
            Dictionary<string, Token> newDict = [];
            for (int i = 0; i < townsfolks.Count; i++)
            {
                Token townsfolk = townsfolks[i];
                newDict.Add(townsfolk.characterName, townsfolk);
            }
            return newDict;
        }
        public int[] BaseCharacterNumbers(int playerCount)
        {
            if (playerCount < 5 || playerCount > 15)
            {
                info.Log("Player count is the wrong size to play BOTC!");
                return null;
            }
            return [ 1, PrimaryMinionCount(playerCount), PrimaryOutsiderCount(playerCount), PrimaryTownsfolkCount(playerCount) ];
        }
        public static int PrimaryMinionCount(int playerCount)
        {
            int count = 1;
            if (playerCount >= 10)
            {
                if (playerCount >= 13)
                {
                    count++;
                }
                count++;
            }
            return count;
        }
        public static int PrimaryOutsiderCount(int playerCount)
        {
            int count = 0;
            if (playerCount == 6 || playerCount == 8 || playerCount == 11 || playerCount == 14)
            {
                count = 1;
            }
            else if (playerCount == 9 || playerCount == 12 || playerCount == 15)
            {
                count = 2;
            }
            return count;
        }
        public static int PrimaryTownsfolkCount(int playerCount)
        {
            int count = 3;
            if (playerCount >= 7)
            {
                if (playerCount >= 10)
                {
                    if (playerCount >= 13)
                    {
                        count += 2;
                    }
                    count += 2;
                }
                count += 2;
            }
            return count;
        }
        public bool ScriptExists(string checkingScript, out string exactScript)
        {
            exactScript = string.Empty;
            bool result = false;
            if (scripts == null)
            {
                return false;
            }
            List<string> scriptNames = [..scripts.Keys];
            for (int i = 0; i < scriptNames.Count; i++)
            {
                result = DeBOTCBot.IsSimilar(checkingScript, scriptNames[i]);
                if (result)
                {
                    exactScript = scriptNames[i];
                    info.Log($"Script: \"{exactScript}\" was found!");
                    break;
                }
            }
            if (!result)
            {
                info.Log($"No script named \"{checkingScript}\" was found!");
            }
            return result;
        }
        public static bool TokenExists(string checkingToken, out string exactToken)
        {
            exactToken = string.Empty;
            bool result = false;
            if (allTokens == null)
            {
                return false;
            }
            List<string> tokenNames = [..allTokens.Keys];
            for (int i = 0; i < tokenNames.Count; i++)
            {
                result = DeBOTCBot.IsSimilar(checkingToken, tokenNames[i]);
                if (result)
                {
                    exactToken = tokenNames[i];
                    break;
                }
            }
            return result;
        }
    }
}