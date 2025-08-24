namespace DeBOTCBot
{
    public enum CharacterType
    {
        Demon,
        Minion,
        Outsider,
        Townsfolk
    }
    public class Range2D(int minimumValue = 0, int maximumValue = 0, bool minMax = false)
    {
        public int min = minimumValue;
        public int max = maximumValue;
        public bool onlyMinMax = minMax;
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
        public Token(string name, CharacterType type = CharacterType.Townsfolk, TypeRanges addingTypes = null, string[] addingCharacters = null, string[] removingCharacters = null, bool rule = false)
        {
            characterName = name;
            characterType = type;
            addingTypes ??= new();
            typesToAdd = addingTypes;
            characterToAdd = addingCharacters;
            forbiddenPair = removingCharacters;
            specialRule = rule;
        }
    }
    public class BOTCCharacters
    {
        public static readonly Dictionary<string, Token> allTokens = GenerateTokens();
        public ServerInfo info;
        public Dictionary<string, string[]> scripts;
        public List<Token> scriptTokens;
        public List<Token> chosenCharacters;
        public void Initialize(ServerInfo newInfo = null)
        {
            if (newInfo != null)
            {
                info = newInfo;
            }
            info?.Log("Making default BOTC scripts");
            scripts = [];
            scripts.Add("Trouble Brewing", ["Imp", "Baron", "Scarlet Woman", "Spy", "Poisoner", "Saint", "Recluse", "Drunk", "Butler", "Mayor", "Soldier", "Slayer", "Virgin", "Ravenkeeper", "Monk", "Undertaker", "Fortune Teller", "Empath", "Chef", "Investigator", "Librarian", "Washerwoman"]);
            scripts.Add("Sects & Violets", ["Vortox", "No Dashii", "Vigormortis", "Fang Gu", "Pit-Hag", "Cerenovus", "Witch", "Evil Twin", "Klutz", "Barber", "Sweetheart", "Mutant", "Sage", "Juggler", "Artist", "Philosopher", "Seamstress", "Savant", "Oracle", "Town Crier", "Flowergirl", "Mathematician", "Snake Charmer", "Dreamer", "Clockmaker"]);
            scripts.Add("Bad Moon Rising", ["Po", "Shabaloth", "Pukka", "Zombuul", "Mastermind", "Assassin", "Devil's Advocate", "Godfather", "Moonchild", "Tinker", "Lunatic", "Goon", "Fool", "Pacifist", "Tea Lady", "Minstrel", "Professor", "Courtier", "Gossip", "Gambler", "Innkeeper", "Exorcist", "Chambermaid", "Sailor", "Grandmother"]);
        }
        public string[] RollTokens(string[] script, int playerCount)
        {
            scriptTokens = [];
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
            chosenCharacters = ChooseCharacters(playerCount);
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
        public List<Token> ChooseCharacters(int playerCount)
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
                initialPicks = SpecialRulesAdjustments(initialPicks, completedSpecialRules, out completedSpecialRules, counts, out counts);
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
        public List<Token> SpecialRulesAdjustments(List<Token> adjustedList, List<string> completedSpecialRules, out List<string> newCompleted, int[] originalCounts, out int[] specialCounts)
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
                                info.Log($"(Kazali) Adding a {script[index].characterName}");
                                script.RemoveAt(index);
                                removed--;
                            }
                            List<Token> townsfolks = adjustedList.Where((x) => x.characterType == CharacterType.Townsfolk).ToList();
                            List<Token> availableOutsiders = script.Where((x) => x.characterType == CharacterType.Outsider && !adjustedList.Contains(x)).ToList();
                            for (int j = 0; j < availableOutsiders.Count && j < townsfolks.Count; j++)
                            {
                                if (Random.Shared.NextSingle() >= (availableOutsiders.Count * 1.5f) / townsfolks.Count)
                                {
                                    continue;
                                }
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
                            for (int j = 0; j < availableOutsiders.Count && j < townsfolks.Count; j++)
                            {
                                if (Random.Shared.NextSingle() >= (availableOutsiders.Count * 1.5f) / townsfolks.Count)
                                {
                                    continue;
                                }
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
                            for (int j = 0; j < availableOutsiders.Count && j < townsfolks.Count; j++)
                            {
                                if (Random.Shared.NextSingle() >= (availableOutsiders.Count * 1.5f) / townsfolks.Count)
                                {
                                    continue;
                                }
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
            demons.Add(new("Al-Hadikhia"));
            demons.Add(new("Fang Gu", addingTypes: new(outsiders: new(1, 1))));
            demons.Add(new("Imp"));
            demons.Add(new("Kazali", rule: true));
            demons.Add(new("Legion", removingCharacters: ["Engineer"], rule: true));
            demons.Add(new("Leviathan"));
            demons.Add(new("Lil' Monsta", rule: true));
            demons.Add(new("Lleech"));
            demons.Add(new("Lord of Typhon", rule: true));
            demons.Add(new("No Dashii"));
            demons.Add(new("Ojo"));
            demons.Add(new("Po"));
            demons.Add(new("Pukka"));
            demons.Add(new("Riot"));
            demons.Add(new("Shabaloth"));
            demons.Add(new("Vigormortis", addingTypes: new(outsiders: new(-1, -1))));
            demons.Add(new("Vortox"));
            demons.Add(new("Yaggababble"));
            demons.Add(new("Zombuul"));
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
            minions.Add(new("Assassin"));
            minions.Add(new("Baron", addingTypes: new(outsiders: new(2, 2))));
            minions.Add(new("Boffin"));
            minions.Add(new("Boomdandy"));
            minions.Add(new("Cerenovus"));
            minions.Add(new("Devil's Advocate"));
            minions.Add(new("Evil Twin"));
            minions.Add(new("Fearmonger"));
            minions.Add(new("Goblin"));
            minions.Add(new("Godfather", addingTypes: new(outsiders: new(-1, 1, true)), removingCharacters: ["Heretic"]));
            minions.Add(new("Harpy"));
            minions.Add(new("Marionette"));
            minions.Add(new("Mastermind"));
            minions.Add(new("Mezepheles"));
            minions.Add(new("Organ Grinder"));
            minions.Add(new("Pit-Hag"));
            minions.Add(new("Poisoner"));
            minions.Add(new("Psychopath"));
            minions.Add(new("Scarlet Woman"));
            minions.Add(new("Spy", removingCharacters: ["Heretic"]));
            minions.Add(new("Summoner", addingTypes: new(demons: new(-1, -1))));
            minions.Add(new("Vizier"));
            minions.Add(new("Widow", removingCharacters: ["Heretic"]));
            minions.Add(new("Witch"));
            minions.Add(new("Wizard"));
            minions.Add(new("Wraith"));
            minions.Add(new("Xaan", rule: true));
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
            outsiders.Add(new("Barber"));
            outsiders.Add(new("Butler"));
            outsiders.Add(new("Damsel"));
            outsiders.Add(new("Drunk"));
            outsiders.Add(new("Golem"));
            outsiders.Add(new("Goon"));
            outsiders.Add(new("Hatter"));
            outsiders.Add(new("Heretic", removingCharacters: ["Godfather", "Spy", "Widow"], rule: true));
            outsiders.Add(new("Hermit", addingTypes: new(outsiders: new(-1, 0))));
            outsiders.Add(new("Klutz"));
            outsiders.Add(new("Lunatic"));
            outsiders.Add(new("Moonchild"));
            outsiders.Add(new("Mutant"));
            outsiders.Add(new("Ogre"));
            outsiders.Add(new("Plague Doctor"));
            outsiders.Add(new("Politician"));
            outsiders.Add(new("Puzzlemaster"));
            outsiders.Add(new("Recluse"));
            outsiders.Add(new("Saint"));
            outsiders.Add(new("Snitch"));
            outsiders.Add(new("Sweetheart"));
            outsiders.Add(new("Tinker"));
            outsiders.Add(new("Zealot"));
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
            townsfolks.Add(new("Acrobat"));
            townsfolks.Add(new("Alchemist"));
            townsfolks.Add(new("Alsaahir"));
            townsfolks.Add(new("Amnesiac"));
            townsfolks.Add(new("Artist"));
            townsfolks.Add(new("Atheist", rule: true));
            townsfolks.Add(new("Balloonist", addingTypes: new(outsiders: new(0, 1))));
            townsfolks.Add(new("Banshee"));
            townsfolks.Add(new("Bounty Hunter"));
            townsfolks.Add(new("Cannibal"));
            townsfolks.Add(new("Chambermaid"));
            townsfolks.Add(new("Chef"));
            townsfolks.Add(new("Choirboy", addingCharacters: ["King"]));
            townsfolks.Add(new("Clockmaker"));
            townsfolks.Add(new("Courtier"));
            townsfolks.Add(new("Cult Leader"));
            townsfolks.Add(new("Dreamer"));
            townsfolks.Add(new("Empath"));
            townsfolks.Add(new("Engineer", removingCharacters: ["Legion"]));
            townsfolks.Add(new("Exorcist"));
            townsfolks.Add(new("Farmer"));
            townsfolks.Add(new("Fisherman"));
            townsfolks.Add(new("Flowergirl"));
            townsfolks.Add(new("Fool"));
            townsfolks.Add(new("Fortune Teller"));
            townsfolks.Add(new("Gambler"));
            townsfolks.Add(new("General"));
            townsfolks.Add(new("Gossip"));
            townsfolks.Add(new("Grandmother"));
            townsfolks.Add(new("High Priestess"));
            townsfolks.Add(new("Huntsman", addingCharacters: ["Damsel"]));
            townsfolks.Add(new("Innkeeper"));
            townsfolks.Add(new("Investigator"));
            townsfolks.Add(new("Juggler"));
            townsfolks.Add(new("King"));
            townsfolks.Add(new("Knight"));
            townsfolks.Add(new("Librarian"));
            townsfolks.Add(new("Lycanthrope"));
            townsfolks.Add(new("Magician"));
            townsfolks.Add(new("Mathematician"));
            townsfolks.Add(new("Mayor"));
            townsfolks.Add(new("Minstrel"));
            townsfolks.Add(new("Monk"));
            townsfolks.Add(new("Nightwatchman"));
            townsfolks.Add(new("Noble"));
            townsfolks.Add(new("Oracle"));
            townsfolks.Add(new("Pacifist"));
            townsfolks.Add(new("Philosopher"));
            townsfolks.Add(new("Pixie"));
            townsfolks.Add(new("Poppy Grower"));
            townsfolks.Add(new("Preacher"));
            townsfolks.Add(new("Princess"));
            townsfolks.Add(new("Professor"));
            townsfolks.Add(new("Ravenkeeper"));
            townsfolks.Add(new("Sage"));
            townsfolks.Add(new("Sailor"));
            townsfolks.Add(new("Savant"));
            townsfolks.Add(new("Seamstress"));
            townsfolks.Add(new("Shugenja"));
            townsfolks.Add(new("Slayer"));
            townsfolks.Add(new("Snake Charmer"));
            townsfolks.Add(new("Soldier"));
            townsfolks.Add(new("Steward"));
            townsfolks.Add(new("Tea Lady"));
            townsfolks.Add(new("Town Crier"));
            townsfolks.Add(new("Undertaker"));
            townsfolks.Add(new("Village Idiot", rule: true));
            townsfolks.Add(new("Virgin"));
            townsfolks.Add(new("Washerwoman"));
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