using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Zintom.StorageFacility.Helpers;

namespace Zintom.StorageFacility
{
    /// <summary>
    /// Provides an interface to the <see cref="Storage.StorageEditor"/>, allowing you to
    /// put values into a <see cref="Storage"/> and also commit those values to file.
    /// </summary>
    public interface IStorageEditor
    {

        /// <summary>
        /// Puts the given <paramref name="value"/> into <see cref="Storage"/> with the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to use which identifies this value.</param>
        /// <param name="value">The value to be stored.</param>
        IStorageEditor PutValue(string key, bool value);

        /// <inheritdoc cref="PutValue(string, bool)"/>
        IStorageEditor PutValue(string key, int value);

        /// <inheritdoc cref="PutValue(string, bool)"/>
        IStorageEditor PutValue(string key, long value);

        /// <inheritdoc cref="PutValue(string, bool)"/>
        IStorageEditor PutValue(string key, float value);

        /// <inheritdoc cref="PutValue(string, bool)"/>
        IStorageEditor PutValue(string key, string value);

        /// <summary>
        /// Puts the given <paramref name="values"/> into <see cref="Storage"/> with the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to use which identifies this value.</param>
        /// <param name="values">The value to be stored.</param>
        IStorageEditor PutValue(string key, string[] values);

        /// <inheritdoc cref="PutValue(string, string[])"/>
        IStorageEditor PutValue(string key, int[] values);

        /// <inheritdoc cref="PutValue(string, string[])"/>
        IStorageEditor PutValue(string key, long[] values);

        /// <inheritdoc cref="PutValue(string, string[])"/>
        IStorageEditor PutValue(string key, float[] values);

        /// <inheritdoc cref="PutValue(string, string[])"/>
        IStorageEditor PutValue(string key, byte[] values);

        /// <summary>
        /// Save the contents of the <see cref="Storage"/> to disk.
        /// </summary>
        IStorageEditor Commit();

        /// <summary>
        /// Wipes all keys/values from this <see cref="Storage"/>.
        /// <para><c>confirmWipe</c> must be set to <c>true</c> in order for this method to execute, it will return immediately if set to <c>false</c>.</para>
        /// </summary>
        IStorageEditor Clear(bool confirmWipe);
    }

    public partial class Storage
    {

        // The class is private so that only the Storage class can instantiate it.
        private class StorageEditor : IStorageEditor
        {
            readonly Storage Parent;
            /// <summary>
            /// If true, the editor should try and optimize the output file for reading, using new-lines where appropriate.
            /// </summary>
            readonly bool OutputOptimizedForReading;

            internal StorageEditor(Storage parent, bool outputOptimizedForReading)
            {
                Parent = parent;
                OutputOptimizedForReading = outputOptimizedForReading;
            }

            public IStorageEditor PutValue(string key, bool value)
            {
                Parent.booleans.AddOrReplace(EscapeString(key), value);
                return this;
            }

            public IStorageEditor PutValue(string key, int value)
            {
                Parent.integers.AddOrReplace(EscapeString(key), value);
                return this;
            }

            public IStorageEditor PutValue(string key, long value)
            {
                Parent.longs.AddOrReplace(EscapeString(key), value);
                return this;
            }

            public IStorageEditor PutValue(string key, float value)
            {
                Parent.floats.AddOrReplace(EscapeString(key), value);
                return this;
            }

            public IStorageEditor PutValue(string key, string value)
            {
                Parent.strings.AddOrReplace(EscapeString(key), EscapeString(value));
                return this;
            }

            public IStorageEditor PutValue(string key, string[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = EscapeString(values[i]);
                }

                Parent.stringArrays.AddOrReplace(EscapeString(key), values);
                return this;
            }

            public IStorageEditor PutValue(string key, int[] values)
            {
                Parent.integerArrays.AddOrReplace(EscapeString(key), values);
                return this;
            }

            public IStorageEditor PutValue(string key, long[] values)
            {
                Parent.longArrays.AddOrReplace(EscapeString(key), values);
                return this;
            }

            public IStorageEditor PutValue(string key, float[] values)
            {
                Parent.floatArrays.AddOrReplace(EscapeString(key), values);
                return this;
            }

            public IStorageEditor PutValue(string key, byte[] values)
            {
                Parent.raws.AddOrReplace(EscapeString(key), values);
                return this;
            }

            public IStorageEditor Commit()
            {
                // Save all Storage contents to file.
                using (var file = File.Create(Parent.StoragePath))
                using (StreamWriter writer = new StreamWriter(file, Encoding.UTF8))
                {
                    // Write single value objects to file:
                    WriteKeyPairValues(Parent.strings, 'S');
                    WriteKeyPairValues(Parent.booleans, 'B');
                    WriteKeyPairValues(Parent.integers, 'I');
                    WriteKeyPairValues(Parent.longs, 'L');
                    WriteKeyPairValues(Parent.floats, 'F');

                    void WriteKeyPairValues<TValue>(Dictionary<string, TValue> keyValuePairs, char? typeShortHand)
                    {
                        foreach (var key in keyValuePairs.Keys)
                        {
                            writer.Write(TokenStrings.StringEnclosure +
                                         key +
                                         TokenStrings.StringEnclosure +
                                         TokenStrings.AssignmentOperator +
                                         typeShortHand +
                                         TokenStrings.StringEnclosure +
                                         keyValuePairs[key] +
                                         TokenStrings.StringEnclosure +
                                         TokenStrings.ObjectTerminator +
                                         (OutputOptimizedForReading ? "\n" : ""));
                        }
                    }

                    // Write all Arrays to file.
                    WriteArray(Parent.stringArrays, 'S');
                    WriteArray(Parent.integerArrays, 'I');
                    WriteArray(Parent.longArrays, 'L');
                    WriteArray(Parent.floatArrays, 'F');

                    void WriteArray<TValue>(Dictionary<string, TValue[]> keyValuePairs, char? typeShortHand)
                    {
                        foreach (var arrayName in keyValuePairs.Keys)
                        {
                            StringBuilder arrayBuilder = new StringBuilder();
                            arrayBuilder.Append(TokenStrings.StringEnclosure + 
                                                arrayName +
                                                TokenStrings.StringEnclosure +
                                                TokenStrings.ArrayAssignmentOperator + 
                                                typeShortHand);

                            foreach (var value in keyValuePairs[arrayName])
                            {
                                arrayBuilder.Append(TokenStrings.StringEnclosure + 
                                                    value + 
                                                    TokenStrings.StringEnclosure + 
                                                    TokenStrings.Seperator);
                            }

                            // Overwrite the final 'seperator' at the end of the array builder.
                            arrayBuilder.Remove(arrayBuilder.Length - TokenStrings.Seperator.Length, TokenStrings.Seperator.Length);
                            // Append object terminator
                            arrayBuilder.Append(TokenStrings.ObjectTerminator);
                            // Write to file.
                            writer.Write(arrayBuilder.ToString() + (OutputOptimizedForReading ? "\n" : ""));
                        }
                    }

                    // Write raws
                    foreach (var name in Parent.raws.Keys)
                    {
                        string value = Convert.ToBase64String(Parent.raws[name]);

                        writer.Write(TokenStrings.StringEnclosure +
                                     name +
                                     TokenStrings.StringEnclosure +
                                     TokenStrings.AssignmentOperator +
                                     "RAW<" + Convert.ToString(value.Length, 16) + ">" +
                                     TokenStrings.StringEnclosure +
                                     value +
                                     TokenStrings.StringEnclosure +
                                     TokenStrings.ObjectTerminator +
                                     (OutputOptimizedForReading ? "\n" : ""));
                    }
                }
                return this;
            }

            public IStorageEditor Clear(bool confirmWipe)
            {
                if (!confirmWipe) return this;

                Parent.strings.Clear();
                Parent.booleans.Clear();
                Parent.integers.Clear();
                Parent.longs.Clear();
                Parent.floats.Clear();

                Parent.stringArrays.Clear();
                Parent.integerArrays.Clear();
                Parent.longArrays.Clear();
                Parent.floatArrays.Clear();

                Parent.raws.Clear();

                return this;
            }
        }

    }
}