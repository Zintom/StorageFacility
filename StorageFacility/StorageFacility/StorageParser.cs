using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata.Ecma335;
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

                //TODO MOVE TO Array Slice.

                // Check if tokens read so far match a sequence

                // Match assumed strings (assumed strings are those without a Type prefix, we assume these are String types).
                if (AddToDictionaryIfKeyPairNotNull(strings, MatchDefineAssumeStringAssignment(sequenceBuilder))) continue;

                // Match strings
                else if (AddToDictionaryIfKeyPairNotNull(strings, MatchDefineValueAssignment<string>(sequenceBuilder))) continue;

                // Match booleans
                else if (AddToDictionaryIfKeyPairNotNull(booleans, MatchDefineValueAssignment<bool>(sequenceBuilder))) continue;

                // Match integers
                else if (AddToDictionaryIfKeyPairNotNull(integers, MatchDefineValueAssignment<int>(sequenceBuilder))) continue;

                // Match longs
                else if (AddToDictionaryIfKeyPairNotNull(longs, MatchDefineValueAssignment<long>(sequenceBuilder))) continue;

                // Match floats
                else if (AddToDictionaryIfKeyPairNotNull(floats, MatchDefineValueAssignment<float>(sequenceBuilder))) continue;

                // Match raws
                else if (AddToDictionaryIfKeyPairNotNull(raws, MatchDefineRawValueAssignment(sequenceBuilder))) continue;

                // Match assumed string arrays
                else if (AddToDictionaryIfKeyPairNotNull(stringArrays, MatchDefineAssumeStringArray(sequenceBuilder))) continue;

                // Match string arrays
                else if (AddToDictionaryIfKeyPairNotNull(stringArrays, MatchDefineArrayAssignment<string>(sequenceBuilder))) continue;

                // Match integer arrays
                else if (AddToDictionaryIfKeyPairNotNull(integerArrays, MatchDefineArrayAssignment<int>(sequenceBuilder))) continue;

                // Match long arrays
                else if (AddToDictionaryIfKeyPairNotNull(longArrays, MatchDefineArrayAssignment<long>(sequenceBuilder))) continue;

                // Match float arrays
                else if (AddToDictionaryIfKeyPairNotNull(floatArrays, MatchDefineArrayAssignment<float>(sequenceBuilder))) continue;

                bool AddToDictionaryIfKeyPairNotNull<T>(Dictionary<string, T> targetDictionary, KeyValuePair<string, T>? keyPair)
                {
                    if (keyPair.HasValue)
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

        [Obsolete("This will be removed in future versions, strings are now fully qualified by default")]
        static KeyValuePair<string, string>? MatchDefineAssumeStringAssignment(List<Token> tokens)
        {
            if (tokens.Count != 4) return null;
            if (tokens[0].TType != TokenType.String) return null;
            if (tokens[1].TType != TokenType.AssignmentOperator) return null;
            if (tokens[2].TType != TokenType.String) return null;

            // Sequence infers String, so add the String Type Declaration token to the list of tokens and use the Generic method.
            tokens.Insert(2, new Token(TokenType.TypeDeclaration, "S"));

            Debug.WriteLine("StorageParser: No type was given for object '" + tokens[0].Value + "', so its value '" + tokens[3].Value + "' was assumed to be a String.");

            return MatchDefineValueAssignment<string>(tokens);
        }

        [Obsolete("This will be removed in future versions, strings are now fully qualified by default")]
        static KeyValuePair<string, string[]>? MatchDefineAssumeStringArray(List<Token> tokens)
        {
            if (tokens.Count < 4) return null;
            if (tokens[0].TType != TokenType.String) return null;
            if (tokens[1].TType != TokenType.ArrayAssignmentOperator) return null;
            if (tokens[2].TType != TokenType.String) return null;

            // Sequence infers String, so add the String Type Declaration token to the list of tokens and use the Generic method.
            tokens.Insert(2, new Token(TokenType.TypeDeclaration, "S"));

            Debug.WriteLine("StorageParser: No type was given for array '" + tokens[0].Value + "', so it was assumed to be a StringArray.");

            return MatchDefineArrayAssignment<string>(tokens);
        }

        static KeyValuePair<string, byte[]>? MatchDefineRawValueAssignment(List<Token> tokens)
        {
            if (tokens.Count != 5
                || tokens[0].TType != TokenType.String
                || tokens[1].TType != TokenType.ArrayAssignmentOperator
                || tokens[2].TType != TokenType.TypeDeclaration
                || GetTypeFromShortHand(tokens[2].Value) != typeof(byte[])
                || tokens[3].TType != TokenType.String
                || tokens[4].TType != TokenType.ObjectTerminator) return null;

            return new KeyValuePair<string, byte[]>(tokens[0].Value, Convert.FromBase64String(tokens[3].Value));
        }

        static KeyValuePair<string, T>? MatchDefineValueAssignment<T>(List<Token> tokens)
        {
            if (tokens.Count != 5
                || tokens[0].TType != TokenType.String
                || tokens[1].TType != TokenType.AssignmentOperator
                || tokens[2].TType != TokenType.TypeDeclaration
                || GetTypeFromShortHand(tokens[2].Value) != typeof(T)
                || tokens[3].TType != TokenType.String
                || tokens[4].TType != TokenType.ObjectTerminator) return null;

            return new KeyValuePair<string, T>(tokens[0].Value, ChangeType<T>(tokens[3].Value));
        }

        static KeyValuePair<string, T[]>? MatchDefineArrayAssignment<T>(List<Token> tokens)
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
        /// Converts the short-hand <see cref="string"/> representation of a type to its object <see cref="Type"/> version.
        /// <para/>
        /// </summary>
        /// <remarks>For example <b>F</b> returns <see cref="float"/> and <b>B</b> returns <see cref="bool"/>,
        /// if unrecognised, will return <see cref="string"/>.</remarks>
        private static Type GetTypeFromShortHand(string shortHandType)
        {
            return shortHandType switch
            {
                "B" => typeof(bool),
                "I" => typeof(int),
                "L" => typeof(long),
                "F" => typeof(float),
                "RAW" => typeof(byte[]),
                _ => typeof(string)
            };
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

            Token? previousMatchedToken = null;
            var tokenValueBuilder = new StringBuilder();

            // Loop through all characters
            for (int c = 0; c < chars.Length; c++)
            {
                if (state == TokenizerState.None)
                {
                    if (chars[c] == '\r' || chars[c] == '\n' || chars[c] == ' ')
                        continue;

                    Token? matchedToken = null;
                    bool partialMatch = false;

                    // Add the current char to the token value to be checked.
                    tokenValueBuilder.Append(chars[c]);
                    string tokenValue = tokenValueBuilder.ToString();

                    TryMatchToken(new Token(TokenType.Seperator, TokenStrings.Seperator));
                    TryMatchToken(new Token(TokenType.ObjectTerminator, TokenStrings.ObjectTerminator));
                    TryMatchToken(new Token(TokenType.ArrayAssignmentOperator, TokenStrings.ArrayAssignmentOperator));
                    TryMatchToken(new Token(TokenType.AssignmentOperator, TokenStrings.AssignmentOperator));

                    TryMatchToken(new Token(TokenType.TypeDeclaration, "S"));
                    TryMatchToken(new Token(TokenType.TypeDeclaration, "B"));
                    TryMatchToken(new Token(TokenType.TypeDeclaration, "I"));
                    TryMatchToken(new Token(TokenType.TypeDeclaration, "L"));
                    TryMatchToken(new Token(TokenType.TypeDeclaration, "F"));
                    TryMatchToken(new Token(TokenType.TypeDeclaration, "RAW"));

                    TryMatchToken(new Token(TokenType.String, TokenStrings.StringEnclosure));

                    // If we have a partial or full match then test next character
                    // to see if this remains true.
                    if (partialMatch || matchedToken != null)
                    {
                        previousMatchedToken = matchedToken;
                        continue;
                    }

                    // Move back one character as this will need to be evaluated again
                    // as it was the last character to not match anything.
                    c--;
                    tokenValueBuilder.Clear();

                    // At this point, previousMatchedToken is actually the token we add to the tokens list
                    // as we now have no partial matches left and no full matches(matchedToken) left.

                    if (previousMatchedToken == null) throw new InvalidTokenException($"The token '{tokenValue[0..^1]}' is not valid.");

                    if (previousMatchedToken.TType == TokenType.String)
                    {
                        state = TokenizerState.BuildingString;
                        valueBuilder.Clear();
                        continue;
                    }
                    else if (previousMatchedToken.Value == "RAW")
                    {
                        tokens.Add(new Token(TokenType.TypeDeclaration, "RAW"));

                        state = TokenizerState.BuildingByteString;
                        valueBuilder.Clear();
                        continue;
                    }

                    tokens.Add(previousMatchedToken);

                    // Checks to see whether the given targetTokenString
                    // matches the current tokenValue. Setting partialMatch or matchedToken as appropriate.
                    void TryMatchToken(Token targetToken)//TokenType targetTokenType, string targetTokenString)
                    {
                        // If the tokenValue startsWith the first character of the target string
                        // then this is at least a partial match.
                        if (tokenValue.StartsWith(targetToken.Value[0]))
                        {
                            // If the tokenValue and target string are identical then
                            // this is an exact match
                            if (tokenValue == targetToken.Value)
                            {
                                if (matchedToken == null || matchedToken.Value.Length < targetToken.Value.Length)
                                    matchedToken = targetToken;
                            }
                            // Else we register that there has been a partial,
                            // but not full match.
                            else if (tokenValue.Length < targetToken.Value.Length)
                            {
                                partialMatch = true;
                            }
                        }
                    }
                }
                else if (state == TokenizerState.BuildingString)
                {
                    // If end quote detected
                    if (chars[c] == '"' && !chars.ContainsAt('\\', c - 1))
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

                        if (c + arrayLength > input.Length)
                            throw new InvalidDataException("RAW length-prefix extends beyond the size of the file.");

                        tokens.Add(new Token(TokenType.String, input.Substring(c, arrayLength)));
                        c += arrayLength; // Move past last quote.

                        state = TokenizerState.None;
                        continue;
                    }
                }
            }

            if (previousMatchedToken != null)
                tokens.Add(previousMatchedToken);

            if (state == TokenizerState.BuildingString)
                throw new FormatException("String literal was declared however does not close before the end of file.");
            if (state == TokenizerState.BuildingByteString)
                throw new FormatException("RAW was declared however the file ends before the length-prefix finishes.");

            return tokens.ToArray();
        }

        ///// <summary>
        ///// Parses a given input converting to parsable <see cref="Token"/>'s.
        ///// </summary>
        //private static Token[] Tokenize(string input)
        //{
        //    List<Token> tokens = new List<Token>();

        //    char[] chars = input.ToCharArray();

        //    var state = TokenizerState.None;
        //    var valueBuilder = new StringBuilder();

        //    // Loop through all characters
        //    for (int c = 0; c < chars.Length; c++)
        //    {
        //        if (state == TokenizerState.None)
        //        {
        //            // Start string definition
        //            if (chars[c] == '"')
        //            {
        //                state = TokenizerState.BuildingString;
        //                valueBuilder.Clear();
        //                continue;
        //            }
        //            // Array item seperator
        //            else if (chars[c] == ',')
        //            {
        //                tokens.Add(new Token(TokenType.Seperator, ""));
        //                continue;
        //            }
        //            // End of Object
        //            else if (chars[c] == ';')
        //            {
        //                tokens.Add(new Token(TokenType.ObjectTerminator, ""));
        //                continue;
        //            }
        //            // Assign Array
        //            else if (chars[c] == ':' && chars.ContainsAt(':', c + 1))//&& (c + 1 < chars.Length) && chars[c + 1] == ':')
        //            {
        //                tokens.Add(new Token(TokenType.ArrayAssignmentOperator, ""));
        //                c++;
        //                continue;
        //            }
        //            // Assign Single Value
        //            else if (chars[c] == ':')
        //            {
        //                tokens.Add(new Token(TokenType.SingleValueAssignmentOperator, ""));
        //                continue;
        //            }
        //            // Type Declaration
        //            else if (chars[c] == 'R'
        //                && chars.ContainsAt('A', c + 1) 
        //                && chars.ContainsAt('W', c + 2))//c < chars.Length - 2 && chars[c + 1] == 'A' && chars[c + 2] == 'W')
        //            {
        //                tokens.Add(new Token(TokenType.TypeDeclaration, "RAW"));
        //                state = TokenizerState.BuildingByteString;
        //                valueBuilder.Clear();

        //                // Increment past the two 'A' and 'W' characters.
        //                c += 2;

        //                continue;
        //            }
        //            else if (chars[c] == 'S' || chars[c] == 'B' || chars[c] == 'I' || chars[c] == 'L' || chars[c] == 'F')
        //            {
        //                tokens.Add(new Token(TokenType.TypeDeclaration, chars[c].ToString()));
        //                continue;
        //            }
        //            // Ignore newline and space characters
        //            else if (chars[c] == '\r' || chars[c] == '\n' || chars[c] == ' ')
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                throw new InvalidDataException($"Tokenizer: Did not expect '{chars[c]}' at position {c + 1}");
        //            }
        //        }
        //        else if (state == TokenizerState.BuildingString)
        //        {
        //            // If end quote detected
        //            if (chars[c] == '"' && !chars.ContainsAt('\\', c - 1))//chars[c - 1] != '\\')
        //            {
        //                // Add the built string to the token list.
        //                tokens.Add(new Token(TokenType.String, valueBuilder.ToString()));

        //                // Reset the state.
        //                state = TokenizerState.None;
        //                continue;
        //            }
        //            else
        //            {
        //                // Add the character to the string builder
        //                valueBuilder.Append(chars[c]);
        //                continue;
        //            }
        //        }
        //        else if (state == TokenizerState.BuildingByteString)
        //        {
        //            // Ignore open bracket
        //            if (chars[c] == '<') continue;

        //            if (chars[c] != '>')
        //                valueBuilder.Append(chars[c]);

        //            if (chars[c] == '>')
        //            {
        //                c += 2;
        //                //int arrayLength = BitConverter.ToInt32(Convert.FromBase64String(valueBuilder.ToString()), 0);
        //                int arrayLength = Convert.ToInt32(valueBuilder.ToString(), 16);

        //                if (input.Length < c + arrayLength)
        //                    throw new InvalidDataException("RAW length-prefix extends beyond the size of the file.");

        //                tokens.Add(new Token(TokenType.String, input.Substring(c, arrayLength)));
        //                c += arrayLength; // Move past last quote.

        //                state = TokenizerState.None;
        //                continue;
        //            }
        //        }
        //    }

        //    if (state == TokenizerState.BuildingString)
        //        throw new FormatException("String literal was declared however does not close before the end of file.");
        //    if (state == TokenizerState.BuildingByteString)
        //        throw new FormatException("RAW was declared however the file ends before the length-prefix finishes.");

        //    return tokens.ToArray();
        //}

        private class Token
        {

            /// <summary>
            /// This tokens type <see cref="TokenType"/>.
            /// </summary>
            public readonly TokenType TType;

            /// <summary>
            /// The string value that this token holds.
            /// </summary>
            public readonly string Value;

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
            /// Represents <see cref="TokenStrings.Seperator"/>
            /// </summary>
            Seperator,
            /// <summary>
            /// Represents the <see cref="Type"/> stored for an assignment, i.e L(<see cref="long"/>), I(<see cref="int"/>) etc.
            /// </summary>
            TypeDeclaration,
            /// <summary>
            /// Represents <see cref="TokenStrings.AssignmentOperator"/>
            /// </summary>
            AssignmentOperator,
            /// <summary>
            /// Represents <see cref="TokenStrings.ArrayAssignmentOperator"/>
            /// </summary>
            ArrayAssignmentOperator,
            /// <summary>
            /// Represents <see cref="TokenStrings.ObjectTerminator"/>
            /// </summary>
            ObjectTerminator
        }

        private static class TokenStrings
        {
            internal const string StringEnclosure = "\"";

            internal const string Seperator = ",";

            internal const string AssignmentOperator = ":";

            internal const string ArrayAssignmentOperator = "::";

            internal const string ObjectTerminator = ";";
        }

    }

    public class InvalidTokenException : Exception
    {
        public InvalidTokenException() { }

        public InvalidTokenException(string message) : base(message) { }

        public InvalidTokenException(string message, Exception innerException) : base(message, innerException) { }
    }
}