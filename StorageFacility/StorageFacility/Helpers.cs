using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Zintom.StorageFacility
{
    internal static class Helpers
    {

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
        /// Adds the given <paramref name="keyValuePair"/> to the given <see cref="Dictionary{TKey, TValue}"/>
        /// <b>if</b> <paramref name="keyValuePair"/> is <b>not</b> null.
        /// </summary>
        /// <returns>A boolean value representing whether the value was added or not.</returns>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. Null values will not be added.</param>
        internal static bool AddIfNotNull<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue>? keyValuePair) where TKey : notnull
        {
            if (keyValuePair == null) return false;

            dictionary.Add(keyValuePair.Value.Key, keyValuePair.Value.Value);

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

    }
}