using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Zintom.StorageFacility
{
    public partial class Storage
    {

        /// <summary>
        /// Gets the string contents of the given storage file.
        /// </summary>
        private static string GetStorageFileContents(string path)
        {
            try
            {
                string fileContents = File.ReadAllText(path);
                if (string.IsNullOrEmpty(fileContents))
                {
                    Debug.WriteLine("StorageFacility.Storage: The given storage file is empty.");
                    return "";
                }
                else
                {
                    return fileContents;
                }
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException)
                {
                    Debug.WriteLine("StorageFacility.Storage: The given storage file does not exist, creating now.");

                    File.Create(path).Dispose();
                    return "";
                }
                else if (ex is DirectoryNotFoundException ||
                         ex is PathTooLongException ||
                         ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine("StorageFacility.Storage: The given path to the storage file is incorrect or inaccessible.");

                    return "";
                }

                throw;
            }
        }

        /// <summary>
        /// Loads the given storage file into memory and parses each item into their respective String, Array, Integer etc values.
        /// </summary>
        private void ParseStorageFile()
        {
            string fileContents = GetStorageFileContents(StoragePath);

            Token[] tokens = Tokenize(fileContents);

            List<Token> sequenceBuilder = new List<Token>();
            for (int t = 0; t < tokens.Length; t++)
            {
                // Add the next token to the builder of tokens
                sequenceBuilder.Add(tokens[t]);

                // Check if tokens read so far match a sequence

                // Match assumed strings (assumed strings are those without a Type prefix, we assume these are String types).
                if (AddToDictionaryIfKeyPairNotNull(strings, MatchDefineAssumeStringAssignment(sequenceBuilder))) continue;
                // Match strings
                else if (AddToDictionaryIfKeyPairNotNull(strings, MatchDefineSingleValueAssignment<string>(sequenceBuilder))) continue;
                // Match booleans
                else if (AddToDictionaryIfKeyPairNotNull(booleans, MatchDefineSingleValueAssignment<bool>(sequenceBuilder))) continue;
                // Match integers
                else if (AddToDictionaryIfKeyPairNotNull(integers, MatchDefineSingleValueAssignment<int>(sequenceBuilder))) continue;
                // Match longs
                else if (AddToDictionaryIfKeyPairNotNull(longs, MatchDefineSingleValueAssignment<long>(sequenceBuilder))) continue;
                // Match floats
                else if (AddToDictionaryIfKeyPairNotNull(floats, MatchDefineSingleValueAssignment<float>(sequenceBuilder))) continue;
                // Match raws
                else if (AddToDictionaryIfKeyPairNotNull(raws, MatchDefineRawValueAssignment(sequenceBuilder))) continue;
                // Match assumed string arrays
                else if (AddToDictionaryIfKeyPairNotNull(stringArrays, MatchDefineAssumeStringArray(sequenceBuilder))) continue;
                // Match string arrays
                else if (AddToDictionaryIfKeyPairNotNull(stringArrays, MatchDefineArray<string>(sequenceBuilder))) continue;
                // Match integer arrays
                else if (AddToDictionaryIfKeyPairNotNull(integerArrays, MatchDefineArray<int>(sequenceBuilder))) continue;
                // Match long arrays
                else if (AddToDictionaryIfKeyPairNotNull(longArrays, MatchDefineArray<long>(sequenceBuilder))) continue;
                // Match float arrays
                else if (AddToDictionaryIfKeyPairNotNull(floatArrays, MatchDefineArray<float>(sequenceBuilder))) continue;

                bool AddToDictionaryIfKeyPairNotNull<T>(Dictionary<string, T> targetDictionary,
                                           KeyValuePair<string, T>? keyPair) where T : notnull
                {
                    if (keyPair != null
                        && keyPair.HasValue)
                    {
                        // If the object is string, ensure it gets unescaped.
                        if (typeof(T) == typeof(string))
                            targetDictionary.Add(keyPair.Value.Key, ChangeType<T>(Helpers.UnEscapeString(ChangeType<string>(keyPair.Value.Value))));
                        else
                            targetDictionary.Add(keyPair.Value.Key, keyPair.Value.Value);

                        sequenceBuilder.Clear();
                        return true;
                    }

                    return false;
                }

                if (t == tokens.Length - 1)
                    throw new FormatException("Parser: Invalid storage format, file has been tampered with or is corrupt.");
            }

            DisplayLoadedValues();
        }

        /// <summary>
        /// Displays all the loaded Storage values in the console log.
        /// </summary>
        private void DisplayLoadedValues()
        {
            Debug.WriteLine($"▼ Storage Loaded(\"{StoragePath}\") ▼");

            Debug.WriteLine($"Strings:");
            DisplayDictionary(strings);

            Debug.WriteLine($"Booleans:");
            DisplayDictionary(booleans);

            Debug.WriteLine($"Integers:");
            DisplayDictionary(integers);

            Debug.WriteLine($"Longs:");
            DisplayDictionary(longs);

            Debug.WriteLine($"Floating Point Numbers:");
            DisplayDictionary(floats);

            Debug.WriteLine($"Raws (encoded in Base64 for brevity):");
            foreach (var key in raws.Keys)
            {
                if (raws[key].Length < 2048)
                    Debug.WriteLine(" '" + key + "' => '" + Convert.ToBase64String(raws[key]) + "'");
                else
                    Debug.WriteLine(" '" + key + "' => Value size exceeds safe display limit(" + raws[key].Length + ")!");
            }

            Debug.WriteLine($"String arrays:");
            DisplayArray(stringArrays);

            Debug.WriteLine($"Integer arrays:");
            DisplayArray(integerArrays);

            Debug.WriteLine($"Long arrays:");
            DisplayArray(longArrays);

            Debug.WriteLine($"Float arrays:");
            DisplayArray(floatArrays);

            static void DisplayDictionary<TKey, TValue>(Dictionary<TKey, TValue> pairs) where TKey : notnull
            {
                foreach (var key in pairs.Keys)
                {
                    Debug.WriteLine(" '" + key + "' => '" + pairs[key] + "'");
                }
            }

            static void DisplayArray<TValue>(Dictionary<string, TValue[]> keyValuePairs)
            {
                foreach (var key in keyValuePairs.Keys)
                {
                    Debug.Write(" '" + key + "' => { ");

                    bool firstValue = true;
                    foreach (var value in keyValuePairs[key])
                    {
                        if (!firstValue) Debug.Write(", ");
                        firstValue = false;

                        Debug.Write("'" + value + "'");
                    }

                    Debug.WriteLine(" }");
                }
            }

        }

        ///// <summary>
        ///// Displays all the loaded Storage values in the console log.
        ///// </summary>
        //public IEnumerable<(Type type, string value)> DumpLoadedValues()
        //{
        //    Debug.WriteLine($"▼ Storage Loaded(\"{StoragePath}\") ▼");

        //    Debug.WriteLine($"Strings:");
        //    foreach (var output in DisplayDictionary(strings)) yield return output;

        //    Debug.WriteLine($"Booleans:");
        //    DisplayDictionary(booleans);

        //    Debug.WriteLine($"Integers:");
        //    DisplayDictionary(integers);

        //    Debug.WriteLine($"Longs:");
        //    DisplayDictionary(longs);

        //    Debug.WriteLine($"Floating Point Numbers:");
        //    DisplayDictionary(floats);

        //    Debug.WriteLine($"Raws (encoded in Base64 for brevity):");
        //    foreach (var key in raws.Keys)
        //    {
        //        if (raws[key].Length < 2048)
        //            Debug.WriteLine(" '" + key + "' => '" + Convert.ToBase64String(raws[key]) + "'");
        //        else
        //            Debug.WriteLine(" '" + key + "' => Value size exceeds safe display limit(" + raws[key].Length + ")!");
        //    }

        //    Debug.WriteLine($"String arrays:");
        //    DisplayArray(stringArrays);

        //    Debug.WriteLine($"Integer arrays:");
        //    DisplayArray(integerArrays);

        //    Debug.WriteLine($"Long arrays:");
        //    DisplayArray(longArrays);

        //    Debug.WriteLine($"Float arrays:");
        //    DisplayArray(floatArrays);

        //    static IEnumerable<(Type, string)> DisplayDictionary<TKey, TValue>(Dictionary<TKey, TValue> pairs) where TKey : notnull
        //    {
        //        foreach (var key in pairs.Keys)
        //        {
        //            yield return (key.GetType(), pairs[key]!.ToString() ?? "");
        //            //Debug.WriteLine(" '" + key + "' => '" + pairs[key] + "'");
        //        }
        //    }

        //    static void DisplayArray<TValue>(Dictionary<string, TValue[]> keyValuePairs)
        //    {
        //        foreach (var key in keyValuePairs.Keys)
        //        {
        //            Debug.Write(" '" + key + "' => { ");

        //            bool firstValue = true;
        //            foreach (var value in keyValuePairs[key])
        //            {
        //                if (!firstValue) Debug.Write(", ");
        //                firstValue = false;

        //                Debug.Write("'" + value + "'");
        //            }

        //            Debug.WriteLine(" }");
        //        }
        //    }

        //}

        static KeyValuePair<string, string>? MatchDefineAssumeStringAssignment(List<Token> tokens)
        {
            if (tokens.Count != 4) return null;
            if (tokens[0].TType != TokenType.String) return null;
            if (tokens[1].TType != TokenType.SingleValueAssignmentOperator) return null;
            if (tokens[2].TType != TokenType.String) return null;

            // Sequence infers String, so add the String Type Declaration token to the list of tokens and use the Generic method.
            tokens.Insert(2, new Token(TokenType.TypeDeclaration, "S"));

            Debug.WriteLine("StorageParser: No type was given for object '" + tokens[0].Value + "', so its value '" + tokens[3].Value + "' was assumed to be a String.");

            return MatchDefineSingleValueAssignment<string>(tokens);
        }

        static KeyValuePair<string, string[]>? MatchDefineAssumeStringArray(List<Token> tokens)
        {
            if (tokens.Count < 4) return null;
            if (tokens[0].TType != TokenType.String) return null;
            if (tokens[1].TType != TokenType.ArrayAssignmentOperator) return null;
            if (tokens[2].TType != TokenType.String) return null;

            // Sequence infers String, so add the String Type Declaration token to the list of tokens and use the Generic method.
            tokens.Insert(2, new Token(TokenType.TypeDeclaration, "S"));

            Debug.WriteLine("StorageParser: No type was given for array '" + tokens[0].Value + "', so it was assumed to be a StringArray.");

            return MatchDefineArray<string>(tokens);
        }

        static KeyValuePair<string, byte[]>? MatchDefineRawValueAssignment(List<Token> tokens)
        {
            if (tokens.Count != 5) return null;
            if (tokens[0].TType != TokenType.String) return null;
            if (tokens[1].TType != TokenType.ArrayAssignmentOperator) return null;

            if (tokens[2].TType != TokenType.TypeDeclaration) return null;
            if (GetTypeFromShortHand(tokens[2].Value) != typeof(byte[])) return null;

            if (tokens[3].TType != TokenType.String) return null;
            if (tokens[4].TType != TokenType.ObjectTerminator) return null;

            return new KeyValuePair<string, byte[]>(tokens[0].Value, Convert.FromBase64String(tokens[3].Value));
        }

        static KeyValuePair<string, T>? MatchDefineSingleValueAssignment<T>(List<Token> tokens)
        {
            if (tokens.Count != 5) return null;
            if (tokens[0].TType != TokenType.String) return null;
            if (tokens[1].TType != TokenType.SingleValueAssignmentOperator) return null;

            if (tokens[2].TType != TokenType.TypeDeclaration) return null;
            if (GetTypeFromShortHand(tokens[2].Value) != typeof(T)) return null;

            if (tokens[3].TType != TokenType.String) return null;
            if (tokens[4].TType != TokenType.ObjectTerminator) return null;

            return new KeyValuePair<string, T>(tokens[0].Value, ChangeType<T>(tokens[3].Value));
            //return new KeyValuePair<string, T>(tokens[0].Value, (T)Convert.ChangeType(tokens[3].Value, typeof(T), CultureInfo.InvariantCulture));
        }

        static KeyValuePair<string, T[]>? MatchDefineArray<T>(List<Token> tokens)
        {
            if (tokens.Count < 4) return null;
            if (tokens[0].TType != TokenType.String) return null;
            if (tokens[1].TType != TokenType.ArrayAssignmentOperator) return null;

            if (tokens[2].TType != TokenType.TypeDeclaration) return null;
            if (GetTypeFromShortHand(tokens[2].Value) != typeof(T)) return null;

            List<T> arrayObjects = new List<T>();
            arrayObjects.Add(ChangeType<T>(tokens[3].Value)); // Add first value

            for (int i = 3; i < tokens.Count; i++)
            {
                Token currentToken = tokens[i];
                TokenType previousTokenType = tokens[i - 1].TType;

                if (previousTokenType == TokenType.Seperator)
                {
                    if (currentToken.TType == TokenType.String)
                    {
                        arrayObjects.Add(ChangeType<T>(currentToken.Value));
                    }
                    else
                    {
                        // Fail
                        Debug.WriteLine($"MatchDefineArray Failed, expected String following ','({TokenType.Seperator}), found {currentToken.TType}.");
                        return null;
                    }
                }
                else if (previousTokenType == TokenType.String)
                {
                    if (currentToken.TType == TokenType.ObjectTerminator)
                    {
                        return new KeyValuePair<string, T[]>(tokens[0].Value, arrayObjects.ToArray());
                    }
                    else if (currentToken.TType != TokenType.Seperator)
                    {
                        Debug.WriteLine($"MatchDefineArray Failed, expected ','({TokenType.Seperator}) or ';'({TokenType.ObjectTerminator}), found {currentToken.TType}.");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Converts the short-hand representation of a Type to its long-hand version.
        /// <para/>
        /// For example <c>'L'</c> returns <see cref="long"/> and <c>'B'</c> returns <see cref="bool"/>.
        /// </summary>
        private static Type GetTypeFromShortHand(string shortHandType)
        {
            if (shortHandType == "B")
                return typeof(bool);
            if (shortHandType == "I")
                return typeof(int);
            if (shortHandType == "L")
                return typeof(long);
            if (shortHandType == "F")
                return typeof(float);
            if (shortHandType == "RAW")
                return typeof(byte[]);

            return typeof(string);
        }

        /// <summary>
        /// Changes the <see cref="Type"/> of the given <see cref="object"/> to the given <see cref="Type"/>.
        /// </summary>
        private static TRequiredType ChangeType<TRequiredType>(object value)
        {
            return (TRequiredType)Convert.ChangeType(value, typeof(TRequiredType), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses a given input converting to parsable <see cref="Token"/>'s.
        /// </summary>
        private static Token[] Tokenize(string input)
        {
            List<Token> tokens = new List<Token>();

            char[] chars = input.ToCharArray();

            var state = TokenizerState.None;
            var valueBuilder = new StringBuilder();

            // Loop through all characters
            for (int c = 0; c < chars.Length; c++)
            {
                if (state == TokenizerState.None)
                {
                    // Start string definition
                    if (chars[c] == '"')
                    {
                        state = TokenizerState.BuildingString;
                        valueBuilder.Clear();
                        continue;
                    }
                    // Array item seperator
                    else if (chars[c] == ',')
                    {
                        tokens.Add(new Token(TokenType.Seperator, ""));
                        continue;
                    }
                    // End of Object
                    else if (chars[c] == ';')
                    {
                        tokens.Add(new Token(TokenType.ObjectTerminator, ""));
                        continue;
                    }
                    // Assign Array
                    else if (chars[c] == ':' && chars.ContainsAt(':', c + 1))//&& (c + 1 < chars.Length) && chars[c + 1] == ':')
                    {
                        tokens.Add(new Token(TokenType.ArrayAssignmentOperator, ""));
                        c++;
                        continue;
                    }
                    // Assign Single Value
                    else if (chars[c] == ':')
                    {
                        tokens.Add(new Token(TokenType.SingleValueAssignmentOperator, ""));
                        continue;
                    }
                    // Type Declaration
                    else if (chars[c] == 'R'
                        && chars.ContainsAt('A', c + 1) 
                        && chars.ContainsAt('W', c + 2))//c < chars.Length - 2 && chars[c + 1] == 'A' && chars[c + 2] == 'W')
                    {
                        tokens.Add(new Token(TokenType.TypeDeclaration, "RAW"));
                        state = TokenizerState.BuildingByteString;
                        valueBuilder.Clear();

                        // Increment past the two 'A' and 'W' characters.
                        c += 2;

                        continue;
                    }
                    else if (chars[c] == 'S' || chars[c] == 'B' || chars[c] == 'I' || chars[c] == 'L' || chars[c] == 'F')
                    {
                        tokens.Add(new Token(TokenType.TypeDeclaration, chars[c].ToString()));
                        continue;
                    }
                    // Ignore newline and space characters
                    else if (chars[c] == '\r' || chars[c] == '\n' || chars[c] == ' ')
                    {
                        continue;
                    }
                    else
                    {
                        throw new InvalidDataException($"Tokenizer: Did not expect '{chars[c]}' at position {c + 1}");
                    }
                }
                else if (state == TokenizerState.BuildingString)
                {
                    // If end quote detected
                    if (chars[c] == '"' && !chars.ContainsAt('\\', c - 1))//chars[c - 1] != '\\')
                    {
                        // Add the built string to the token list.
                        tokens.Add(new Token(TokenType.String, valueBuilder.ToString()));

                        // Reset the state.
                        state = TokenizerState.None;
                        continue;
                    }
                    else
                    {
                        // Add the character to the string builder
                        valueBuilder.Append(chars[c]);
                        continue;
                    }
                }
                else if (state == TokenizerState.BuildingByteString)
                {
                    // Ignore open bracket
                    if (chars[c] == '<') continue;

                    if (chars[c] != '>')
                        valueBuilder.Append(chars[c]);

                    if (chars[c] == '>')
                    {
                        c += 2;
                        //int arrayLength = BitConverter.ToInt32(Convert.FromBase64String(valueBuilder.ToString()), 0);
                        int arrayLength = Convert.ToInt32(valueBuilder.ToString(), 16);

                        if (input.Length < c + arrayLength)
                            throw new InvalidDataException("RAW length-prefix extends beyond the size of the file.");

                        tokens.Add(new Token(TokenType.String, input.Substring(c, arrayLength)));
                        c += arrayLength; // Move past last quote.

                        state = TokenizerState.None;
                        continue;
                    }
                }
            }

            if (state == TokenizerState.BuildingString)
                throw new FormatException("String literal was declared however does not close before the end of file.");
            if (state == TokenizerState.BuildingByteString)
                throw new FormatException("RAW was declared however the file ends before the length-prefix finishes.");

            return tokens.ToArray();
        }

        private struct Token
        {
            public TokenType TType;
            public string Value;

            public Token(TokenType type, string value)
            {
                TType = type;
                Value = value;
            }
        }

        private enum TokenizerState
        {
            None,
            BuildingString,
            BuildingByteString
        }

        private enum TokenType
        {
            /// <summary>
            /// Represents an array of characters, i.e "hello".
            /// </summary>
            String,
            /// <summary>
            /// Represents ','
            /// </summary>
            Seperator,
            /// <summary>
            /// Represents the <see cref="Type"/> stored for an assignment, i.e L(<see cref="long"/>), I(<see cref="int"/>) etc.
            /// </summary>
            TypeDeclaration,
            /// <summary>
            /// Represents ':'
            /// </summary>
            SingleValueAssignmentOperator,
            /// <summary>
            /// Represents '::'
            /// </summary>
            ArrayAssignmentOperator,
            /// <summary>
            /// Represents ';'
            /// </summary>
            ObjectTerminator
        }

    }
}