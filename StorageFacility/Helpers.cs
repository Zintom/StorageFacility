﻿using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zintom.Misc
{
    internal static class Helpers
    {

        /// <summary>
        /// Reads the given file and returns it as a <see cref="string"/>.
        /// </summary>
        internal static string ReadFile(string filePath)
        {
            StreamReader streamReader = null;
            string fileContents = "";
            try
            {
                streamReader = new StreamReader(filePath);
                fileContents = streamReader.ReadToEnd();
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            finally
            {
                streamReader?.Close();
                streamReader?.Dispose();
            }

            return fileContents;
        }

        /// <summary>
        /// <inheritdoc cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>
        /// <para>If the key already exists, it is replaced by the new key and value provided.</para>
        /// </summary>
        internal static void AddOrReplace<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary.Remove(key);

            dictionary.Add(key, value);
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

    }
}