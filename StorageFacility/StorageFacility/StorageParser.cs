using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Zintom.StorageFacility
{
    public sealed partial class Storage
    {

        /// <summary>
        /// Gets the string contents of the given storage file.
        /// </summary>
        private static string? GetStorageFileContents(string path)
        {
            try
            {
                string fileContents = File.ReadAllText(path);
                if (string.IsNullOrEmpty(fileContents))
                {
                    Console.WriteLine("StorageFacility.Storage: The given storage file is empty.");
                    return null;
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
                    Console.WriteLine("StorageFacility.Storage: The given storage file does not exist, creating now.");

                    File.Create(path).Dispose();
                    return null;
                }
                else if (ex is DirectoryNotFoundException ||
                         ex is PathTooLongException ||
                         ex is UnauthorizedAccessException)
                {
                    Console.WriteLine("StorageFacility.Storage: The given path to the storage file is incorrect or inaccessible.");

                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Loads the given storage file into memory and parses each item into their respective String, Array, Integer etc values.
        /// </summary>
        private void ParseStorageFile()
        {
            string? fileContents = GetStorageFileContents(StoragePath);

            if (fileContents == null) return;

            Token[] tokens = Tokenize(fileContents);

            int startIndex = 0;
            int selectionLength = 0;

            // Define sequences to match against.

            Span<Token> ValueAssignmentSequence = new[] {
                new Token(TokenType.String, ""),
                new Token(TokenType.AssignmentOperator, ""),
                new Token(TokenType.TypeDeclaration, ""),
                new Token(TokenType.String, ""),
                new Token(TokenType.ObjectTerminator, "")
                }.AsSpan();

            Span<Token> ArrayAssignmentInitialSequence = new[] {
                new Token(TokenType.String, ""),
                new Token(TokenType.ArrayAssignmentOperator, ""),
                new Token(TokenType.TypeDeclaration, "")
                }.AsSpan();

            Span<Token> ArrayItemSequence = new[] {
                new Token(TokenType.String, ""),
                new Token(TokenType.Seperator, "")
                }.AsSpan();

            while (true)
            {
                // Expand token span selection by one.
                selectionLength++;

                if (startIndex >= tokens.Length)
                    break;

                if (startIndex + selectionLength > tokens.Length)
                {
                    Debug.WriteLine("Parser: Storage format unrecognised; its format may be out of date, or the file has been tampered with or is corrupt.");
                    break;
                }

                // The slice of tokens we are testing.
                Span<Token> testSequence = tokens.AsSpan(startIndex, selectionLength);

                //
                // New single value assignment
                if (testSequence.MatchesSequence(ValueAssignmentSequence))
                {
                    string assignmentKey = testSequence[0].Value;
                    object assignmentValue = testSequence[3].Value;
                    Type assignmentType = GetTypeFromShortHand(testSequence[2].Value);

                    if (assignmentType == typeof(string))
                    {
                        _strings.AddIfNotNull(assignmentKey, Helpers.UnEscapeString(assignmentValue?.ToString() ?? ""));
                    }
                    else if (assignmentType == typeof(bool))
                    {
                        _booleans.AddIfNotNull(assignmentKey, ChangeType<bool>(assignmentValue));
                    }
                    else if (assignmentType == typeof(int))
                    {
                        _integers.AddIfNotNull(assignmentKey, ChangeType<int>(assignmentValue));
                    }
                    else if (assignmentType == typeof(long))
                    {
                        _longs.AddIfNotNull(assignmentKey, ChangeType<long>(assignmentValue));
                    }
                    else if (assignmentType == typeof(float))
                    {
                        _floats.AddIfNotNull(assignmentKey, ChangeType<float>(assignmentValue));
                    }
                    else if (assignmentType == typeof(byte[]))
                    {
                        _raws.AddIfNotNull(assignmentKey, Convert.FromBase64String(assignmentValue?.ToString() ?? "FAILED"));
                    }

                    MoveToNextTokenSequence();

                    continue;
                }
                //
                // New array assignment
                else if (testSequence.Length > ArrayAssignmentInitialSequence.Length
                    && testSequence.Slice(0, ArrayAssignmentInitialSequence.Length).MatchesSequence(ArrayAssignmentInitialSequence)
                    && testSequence.Slice(ArrayAssignmentInitialSequence.Length, testSequence.Length - ArrayAssignmentInitialSequence.Length - 1).FollowsRecurringSequence(ArrayItemSequence)
                    && testSequence[^1].TType == TokenType.ObjectTerminator)
                {
                    string assignmentKey = testSequence[0].Value;
                    Type assignmentType = GetTypeFromShortHand(testSequence[2].Value);

                    // Get just the String and Seperator Tokens
                    Span<Token> justItemsAndSeperators = testSequence[3..];

                    // Because justItemsAndSeperators is all the strings & all the seperators
                    // we need to divide its length by 2 to get the number of elements.
                    int arrayElementsCount = justItemsAndSeperators.Length / 2;

                    // A new empty array of objects
                    // ready to be filled with the items in justItemsAndSeperators
                    object[] arrayElements = new object[arrayElementsCount];

                    for (int i = 0; i < arrayElements.Length; i++)
                    {
                        string newItemValue = justItemsAndSeperators[i * 2].Value;

                        if (assignmentType == typeof(string))
                            newItemValue = Helpers.UnEscapeString(newItemValue);

                        arrayElements[i] = newItemValue;
                    }

                    // Add the newly built array to the correct dictionary.
                    if (assignmentType == typeof(string))
                    {
                        _stringArrays.AddOrReplace(assignmentKey, arrayElements.ChangeElementType<string>());
                    }
                    else if (assignmentType == typeof(int))
                    {
                        _integerArrays.AddOrReplace(assignmentKey, arrayElements.ChangeElementType<int>());
                    }
                    else if (assignmentType == typeof(long))
                    {
                        _longArrays.AddOrReplace(assignmentKey, arrayElements.ChangeElementType<long>());
                    }
                    else if (assignmentType == typeof(float))
                    {
                        _floatArrays.AddOrReplace(assignmentKey, arrayElements.ChangeElementType<float>());
                    }

                    MoveToNextTokenSequence();

                    continue;
                }

                void MoveToNextTokenSequence()
                {
                    startIndex += selectionLength;
                    selectionLength = 0;
                }
            }
        }

        /// <summary>
        /// Displays all the loaded values in the console log.
        /// </summary>
        public void DisplayLoadedValues()
        {
            Console.WriteLine("Strings:");
            DisplayDictionary(_strings);

            Console.WriteLine("Booleans:");
            DisplayDictionary(_booleans);

            Console.WriteLine("Integers:");
            DisplayDictionary(_integers);

            Console.WriteLine("Longs:");
            DisplayDictionary(_longs);

            Console.WriteLine("Floating Point Numbers:");
            DisplayDictionary(_floats);

            Console.WriteLine("Raws (encoded in Base64 for brevity):");
            foreach (var key in _raws.Keys)
            {
                if (_raws[key].Length < 2048)
                    Console.WriteLine(" '" + key + "' => '" + Convert.ToBase64String(_raws[key]) + "'");
                else
                    Console.WriteLine(" '" + key + "' => Value size exceeds safe display limit(" + _raws[key].Length + ")!");
            }

            Console.WriteLine("String arrays:");
            DisplayArray(_stringArrays);

            Console.WriteLine("Integer arrays:");
            DisplayArray(_integerArrays);

            Console.WriteLine("Long arrays:");
            DisplayArray(_longArrays);

            Console.WriteLine("Float arrays:");
            DisplayArray(_floatArrays);

            static void DisplayDictionary<TKey, TValue>(Dictionary<TKey, TValue> pairs) where TKey : notnull
            {
                foreach (var key in pairs.Keys)
                {
                    Console.WriteLine(" '" + key + "' => '" + pairs[key] + "'");
                }
            }

            static void DisplayArray<TValue>(Dictionary<string, TValue[]> keyValuePairs)
            {
                foreach (var key in keyValuePairs.Keys)
                {
                    Console.Write(" '" + key + "' => { ");

                    bool firstValue = true;
                    foreach (var value in keyValuePairs[key])
                    {
                        if (!firstValue) Console.Write(", ");
                        firstValue = false;

                        Console.Write("'" + value + "'");
                    }

                    Console.WriteLine(" }");
                }
            }
        }

        ///// <summary>
        ///// Displays all the loaded Storage values in the console log.
        ///// </summary>
        //public IEnumerable<(Type type, string key, string value)> DumpLoadedValues()
        //{
        //    Debug.WriteLine($"▼ Storage Loaded(\"{StoragePath}\") ▼");

        //    Debug.WriteLine($"Strings:");
        //    foreach (var output in DisplayDictionary(_strings)) yield return output;

        //    Debug.WriteLine($"Booleans:");
        //    foreach (var output in DisplayDictionary(_booleans)) yield return output;

        //    Debug.WriteLine($"Integers:");
        //    foreach (var output in DisplayDictionary(_integers)) yield return output;

        //    Debug.WriteLine($"Longs:");
        //    foreach (var output in DisplayDictionary(_longs)) yield return output;

        //    Debug.WriteLine($"Floating Point Numbers:");
        //    foreach (var output in DisplayDictionary(_floats)) yield return output;

        //    Debug.WriteLine($"Raws (encoded in Base64 for brevity):");
        //    foreach (var key in _raws.Keys)
        //    {
        //        if (_raws[key].Length < 2048)
        //            Debug.WriteLine(" '" + key + "' => '" + Convert.ToBase64String(_raws[key]) + "'");
        //        else
        //            Debug.WriteLine(" '" + key + "' => Value size exceeds safe display limit(" + _raws[key].Length + ")!");
        //    }

        //    Debug.WriteLine($"String arrays:");
        //    foreach (var output in DisplayArray(_stringArrays)) yield return output;

        //    Debug.WriteLine($"Integer arrays:");
        //    foreach (var output in DisplayArray(_integerArrays)) yield return output;

        //    Debug.WriteLine($"Long arrays:");
        //    foreach (var output in DisplayArray(_longArrays)) yield return output;

        //    Debug.WriteLine($"Float arrays:");
        //    foreach (var output in DisplayArray(_floatArrays)) yield return output;

        //    static IEnumerable<(Type type, string key, string value)> DisplayDictionary<TKey, TValue>(Dictionary<TKey, TValue> pairs) where TKey : notnull
        //    {
        //        foreach (var key in pairs.Keys)
        //        {
        //            yield return (key.GetType(), key?.ToString() ?? "", pairs[key]?.ToString() ?? "");
        //        }
        //    }

        //    static IEnumerable<(Type type, string key, TValue[] value)> DisplayArray<TKey, TValue>(Dictionary<TKey, TValue[]> pairs) where TKey : notnull
        //    {
        //        foreach (var key in pairs.Keys)
        //        {
        //            yield return (typeof(TKey), key?.ToString() ?? "", pairs[key]);
        //        }
        //    }
        //}

        //[Obsolete("This will be removed in future versions, strings are now fully qualified by default")]
        //static KeyValuePair<string, string>? MatchDefineAssumeStringAssignment(List<Token> tokens)
        //{
        //    if (tokens.Count != 4) return null;
        //    if (tokens[0].TType != TokenType.String) return null;
        //    if (tokens[1].TType != TokenType.AssignmentOperator) return null;
        //    if (tokens[2].TType != TokenType.String) return null;

        //    // Sequence infers String, so add the String Type Declaration token to the list of tokens and use the Generic method.
        //    tokens.Insert(2, new Token(TokenType.TypeDeclaration, "S"));

        //    Debug.WriteLine("StorageParser: No type was given for object '" + tokens[0].Value + "', so its value '" + tokens[3].Value + "' was assumed to be a String.");

        //    return MatchDefineValueAssignment<string>(tokens);
        //}

        //[Obsolete("This will be removed in future versions, strings are now fully qualified by default")]
        //static KeyValuePair<string, string[]>? MatchDefineAssumeStringArray(List<Token> tokens)
        //{
        //    if (tokens.Count < 4) return null;
        //    if (tokens[0].TType != TokenType.String) return null;
        //    if (tokens[1].TType != TokenType.ArrayAssignmentOperator) return null;
        //    if (tokens[2].TType != TokenType.String) return null;

        //    // Sequence infers String, so add the String Type Declaration token to the list of tokens and use the Generic method.
        //    tokens.Insert(2, new Token(TokenType.TypeDeclaration, "S"));

        //    Debug.WriteLine("StorageParser: No type was given for array '" + tokens[0].Value + "', so it was assumed to be a StringArray.");

        //    return MatchDefineArrayAssignment<string>(tokens);
        //}

        //static KeyValuePair<string, byte[]>? MatchDefineRawValueAssignment(ReadOnlySpan<Token> tokens)
        //{
        //    if (tokens.Length != 5
        //        || tokens[0].TType != TokenType.String
        //        || tokens[1].TType != TokenType.ArrayAssignmentOperator
        //        || tokens[2].TType != TokenType.TypeDeclaration
        //        || GetTypeFromShortHand(tokens[2].Value) != typeof(byte[])
        //        || tokens[3].TType != TokenType.String
        //        || tokens[4].TType != TokenType.ObjectTerminator) return null;

        //    return new KeyValuePair<string, byte[]>(tokens[0].Value, Convert.FromBase64String(tokens[3].Value));
        //}

        //static KeyValuePair<string, T>? MatchDefineValueAssignment<T>(Span<Token> tokens)
        //{
        //    if (tokens.Length != 5
        //        || tokens[0].TType != TokenType.String
        //        || tokens[1].TType != TokenType.AssignmentOperator
        //        || tokens[2].TType != TokenType.TypeDeclaration
        //        || GetTypeFromShortHand(tokens[2].Value) != typeof(T)
        //        || tokens[3].TType != TokenType.String
        //        || tokens[4].TType != TokenType.ObjectTerminator) return null;

        //    if (typeof(T) == typeof(string)) // String
        //    {
        //        // Unescape string before return to caller.
        //        return new KeyValuePair<string, T>(tokens[0].Value, ChangeType<T>(Helpers.UnEscapeString(ChangeType<string>(tokens[3].Value))));
        //    }
        //    else if (typeof(T) == typeof(byte[])) // Raws
        //    {
        //        // Convert from Base64 to byte[] before return to caller.
        //        return new KeyValuePair<string, T>(tokens[0].Value, ChangeType<T>(Convert.FromBase64String(tokens[3].Value)));
        //    }
        //    else
        //    {
        //        return new KeyValuePair<string, T>(tokens[0].Value, ChangeType<T>(tokens[3].Value));
        //    }
        //}

        //static KeyValuePair<string, T[]>? MatchDefineArrayAssignment<T>(ReadOnlySpan<Token> tokens)
        //{
        //    if (tokens.Length < 4) return null;
        //    if (tokens[0].TType != TokenType.String) return null;
        //    if (tokens[1].TType != TokenType.ArrayAssignmentOperator) return null;

        //    if (tokens[2].TType != TokenType.TypeDeclaration) return null;
        //    if (GetTypeFromShortHand(tokens[2].Value) != typeof(T)) return null;

        //    List<T> arrayObjects = new List<T>();
        //    arrayObjects.Add(ChangeType<T>(tokens[3].Value)); // Add first value

        //    for (int i = 3; i < tokens.Length; i++)
        //    {
        //        Token currentToken = tokens[i];
        //        TokenType previousTokenType = tokens[i - 1].TType;

        //        if (previousTokenType == TokenType.Seperator)
        //        {
        //            if (currentToken.TType == TokenType.String)
        //            {
        //                arrayObjects.Add(ChangeType<T>(currentToken.Value));
        //            }
        //            else
        //            {
        //                // Fail
        //                Debug.WriteLine($"MatchDefineArray Failed, expected String following ','({TokenType.Seperator}), found {currentToken.TType}.");
        //                return null;
        //            }
        //        }
        //        else if (previousTokenType == TokenType.String)
        //        {
        //            if (currentToken.TType == TokenType.ObjectTerminator)
        //            {
        //                return new KeyValuePair<string, T[]>(tokens[0].Value, arrayObjects.ToArray());
        //            }
        //            else if (currentToken.TType != TokenType.Seperator)
        //            {
        //                Debug.WriteLine($"MatchDefineArray Failed, expected ','({TokenType.Seperator}) or ';'({TokenType.ObjectTerminator}), found {currentToken.TType}.");
        //            }
        //        }
        //    }

        //    return null;
        //}

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
                "S" => typeof(string),
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

        private sealed class Token : IRepresent<Token>
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

            ///// <summary>
            ///// Determines whether the specified sequences are identical, judged by their <see cref="TType"/>
            ///// </summary>
            ///// <returns>If all sequence member <see cref="TType"/>'s are identical, returns <b>true</b>, if not, returns <b>false</b>.</returns>
            //public static bool SequenceEquals(in Span<Token> sequence1, in Span<Token> sequence2)
            //{
            //    if (sequence1.Length != sequence2.Length) return false;

            //    for (int i = 0; i < sequence1.Length; i++)
            //    {
            //        if (sequence1[i].TType != sequence2[i].TType) return false;
            //    }

            //    return true;
            //}

            public bool Represents(Token obj)
            {
                return obj?.TType == TType;
            }

            ///// <summary>
            ///// Determines whether the given <paramref name="sequenceToMatch"/> follows
            ///// the given recurring sequence exactly.
            ///// </summary>
            ///// <param name="recurringSequence">The template recurring sequence.</param>
            ///// <param name="sequenceToMatch">The sequence to test.</param>
            ///// <returns>If the given <paramref name="sequenceToMatch"/> exactly follows the given <paramref name="recurringSequence"/>, returns <b>true</b>, otherwise <b>false</b>.</returns>
            //public static bool FollowsRecurringSequence(this Span<Token> sequenceToMatch, Span<Token> recurringSequence)
            //{
            //    //if (sequenceToMatch.Length < recurringSequence.Length) return false;

            //    for(int i = 0; i < sequenceToMatch.Length; i++)
            //    {
            //        if (sequenceToMatch[i].TType != recurringSequence[i % recurringSequence.Length].TType) return false;
            //    }

            //    return true;
            //}

            //public override bool Equals(object? obj)
            //{
            //    if (obj == null) return base.Equals(obj);

            //    var objBox = (Token)obj;
            //    return objBox.TType == TType && objBox.Value == Value;
            //}

            //public override int GetHashCode()
            //{
            //    return Tuple.Create(TType, Value).GetHashCode();
            //}
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
    /// <summary>
    /// An object may not be equal but may still <b>'represent'</b> another object.
    /// For example, two boxes may differ in content, but on the outside still represent a 'box'.
    /// </summary>
    /// <remarks>Implement this interface to show the intent that "these two objects are not equal but represent the same or similar thing".
    /// <para>Not the same as <see cref="Type"/> because the <c>Represents(T obj)</c> method transcends the restrictions of <see cref="Type"/>, 
    /// two objects of completely unrelated Type can represent the same thing.</para></remarks>
    /// <typeparam name="T"></typeparam>
    public interface IRepresent<T>
    {
        /// <summary>
        /// Determines whether the specified object <b>represents</b> the current object.
        /// </summary>
        /// <remarks>An object may not be equal but may still <b>'represent'</b> another object.
        /// For example, two boxes may differ in content, but on the outside still represent a 'box'.</remarks>
        /// <returns><b>true</b> if the specified object represents the current object; otherwise, <b>false</b>.</returns>
        bool Represents(T obj);
    }

    public sealed class InvalidTokenException : Exception
    {
        public InvalidTokenException() { }

        public InvalidTokenException(string message) : base(message) { }

        public InvalidTokenException(string message, Exception innerException) : base(message, innerException) { }
    }
}