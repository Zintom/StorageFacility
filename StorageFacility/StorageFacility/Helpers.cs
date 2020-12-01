using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace Zintom.StorageFacility
{
    public static class Helpers
    {

        /// <summary>
        /// Retreives a <typeparamref name="TValue"/> from the dictionary with the given <paramref name="key"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key for the value to retrieve.</param>
        /// <param name="defaultValue">The value to return if the key does not exist.</param>
        /// <returns>The <typeparamref name="TValue"/> for the given <paramref name="key"/>, or <paramref name="defaultValue"/> if it does not exist.</returns>
        public static TValue? GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue) where TKey : notnull
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            else
            {
                return defaultValue;
            }
        }

        /// <inheritdoc cref="GetValue{TKey, TValue}(IDictionary{TKey, TValue}, TKey, TValue)"/>
        public static TValue? GetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue) where TKey : notnull
        {
            return GetValue((IDictionary<TKey, TValue>)dictionary, key, defaultValue);
        }

        /// <summary>
        /// <inheritdoc cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>
        /// <para>If the key already exists, it is replaced by the new key and value provided.</para>
        /// </summary>
        internal static void AddOrReplace<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
        {
            if (dictionary.ContainsKey(key))
                dictionary.Remove(key);

            dictionary.Add(key, value);
        }

        /// <summary>
        /// Adds the given key and value pair to the given <see cref="Dictionary{TKey, TValue}"/> <b>if</b> the value is <b>not</b> <see langword="null"/>
        /// </summary>
        /// <returns>A <see cref="bool"/> value representing whether the value was added or not.</returns>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. Null values will not be added.</param>
        internal static bool AddIfNotNull<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
        {
            if (value == null) return false;

            dictionary.AddOrReplace(key, value);

            return true;
        }

        /// <inheritdoc cref="AddIfNotNull{TKey, TValue}(Dictionary{TKey, TValue}, TKey, TValue)"/>
        internal static bool AddIfNotNull<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue>? keyValuePair) where TKey : notnull
        {
            if (keyValuePair == null) return false;

            dictionary.AddOrReplace(keyValuePair.Value.Key, keyValuePair.Value.Value);

            return true;
        }

        internal static string EscapeString(string input)
        {
            StringBuilder escaped = new StringBuilder();
            char[] chars = input.ToCharArray();

            for (int c = 0; c < chars.Length; c++)
            {
                // If the character is not a special character, move on.
                if (chars[c] != '"')
                {
                    escaped.Append(chars[c]);
                    continue;
                }

                // If we are at the start of the string or the previous
                // character was not an escape string.
                if (c == 0 || chars[c - 1] != '\\')
                    escaped.Append('\\' + "" + chars[c]);
                else
                    escaped.Append(chars[c]);
            }

            return escaped.ToString();
        }

        /// <summary>
        /// Removes all escape characters from the Double Quote character.
        /// </summary>
        internal static string UnEscapeString(string input)
        {
            StringBuilder unescaped = new StringBuilder();
            char[] chars = input.ToCharArray();

            for (int c = 0; c < chars.Length; c++)
            {
                if (c + 1 < chars.Length && chars[c] == '\\' && chars[c + 1] == '"')
                    continue; // Don't add the escape character
                else
                    unescaped.Append(chars[c]);
            }

            return unescaped.ToString();
        }

        /// <summary>
        /// Checks that the array contains the specified <paramref name="expectedCharacter"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="expectedCharacter"></param>
        /// <param name="index"></param>
        /// <returns><b>true</b> if the given <paramref name="expectedCharacter"/> exists at the given <paramref name="index"/>, or if not, returns <b>false</b>.</returns>
        internal static bool ContainsAt(this char[] array, char expectedCharacter, int index)
        {
            return index > 0 && index < array.Length && array[index] == expectedCharacter;
        }

        /// <summary>
        /// Determines whether the first sequence of <see cref="Span{T}"/> matches
        /// the second sequence of <see cref="Span{T}"/> exactly.
        /// </summary>
        /// <param name="sequence">The sequence to test.</param>
        /// <returns>If the first sequence exactly matches the second, returns <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        internal static bool MatchesSequence<T>(this Span<T> @this, Span<T> sequence) where T : IRepresent<T>
        {
            if (@this.Length != sequence.Length) return false;

            for (int i = 0; i < @this.Length; i++)
            {
                if (!@this[i].Represents(sequence[i])) return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the given sequence of <see cref="Span{T}"/> follows
        /// the given recurring sequence of <see cref="Span{T}"/> exactly.
        /// </summary>
        /// <param name="recurringSequence">The template recurring sequence.</param>
        /// <param name="sequenceToMatch">The sequence to test.</param>
        /// <returns>If the given <paramref name="sequenceToMatch"/> exactly follows the given <paramref name="recurringSequence"/>, returns <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        internal static bool FollowsRecurringSequence<T>(this Span<T> sequenceToMatch, Span<T> recurringSequence) where T : IRepresent<T>
        {
            for (int i = 0; i < sequenceToMatch.Length; i++)
            {
                if (!sequenceToMatch[i]!.Represents(recurringSequence[i % recurringSequence.Length])) return false;
            }

            return true;
        }

        /// <summary>
        /// Changes the <see cref="Type"/> of each of the source array elements to that of <typeparamref name="TTargetType"/>.
        /// </summary>
        /// <typeparam name="TTargetType">The <see cref="Type"/> that the array items are to be converted to.</typeparam>
        /// <param name="sourceArray"></param>
        /// <returns>A new <typeparamref name="TTargetType"/> array.</returns>
        internal static TTargetType[] ChangeElementType<TTargetType>(this object[] sourceArray)
        {
            TTargetType[] outputArray = new TTargetType[sourceArray.Length];

            for (int i = 0; i < outputArray.Length; i++)
            {
                outputArray[i] = (TTargetType)Convert.ChangeType(sourceArray[i], typeof(TTargetType), CultureInfo.InvariantCulture);
            }

            return outputArray;
        }
    }
}