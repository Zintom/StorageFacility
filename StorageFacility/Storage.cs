using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;

namespace Zintom.IO.StorageFacility
{
    /// <summary>
    /// A class which simplifies data storage to disk allowing for easy saving, loading and access of a key-value data structure.
    /// </summary>
    public partial class Storage
    {

        private string StoragePath = "";

        private readonly object editor_locker = new object();
        private IStorageEditor editor = null;

        private readonly static object storagePool_locker = new object();
        private readonly static List<Storage> storagePool = new List<Storage>();

        // To add more types, ensure the following:
        // - StorageParser checks for the new type.
        // - The GetTypeFromShortHand() method is updated with the new type.
        // - The IStorageEditor has a PutValue method for the new type, commits the type to file, and clears on Clear().
        // - Make sure to add a Public Property here.
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> booleans = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> integers = new Dictionary<string, int>();
        private readonly Dictionary<string, long> longs = new Dictionary<string, long>();
        private readonly Dictionary<string, float> floats = new Dictionary<string, float>();

        private readonly Dictionary<string, string[]> stringArrays = new Dictionary<string, string[]>();
        private readonly Dictionary<string, int[]> integerArrays = new Dictionary<string, int[]>();
        private readonly Dictionary<string, long[]> longArrays = new Dictionary<string, long[]>();
        private readonly Dictionary<string, float[]> floatArrays = new Dictionary<string, float[]>();
        private readonly Dictionary<string, byte[]> raws = new Dictionary<string, byte[]>();

        #region Public Properties
        /// <summary>
        /// A dictionary of all <see cref="bool"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, bool> Booleans { get => booleans; }

        /// <summary>
        /// A dictionary of all <see cref="int"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, int> Integers { get => integers; }

        /// <summary>
        /// A dictionary of all <see cref="long"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, long> Longs { get => longs; }

        /// <summary>
        /// A dictionary of all <see cref="float"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, float> Floats { get => floats; }

        /// <summary>
        /// A dictionary of all <see cref="string"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> Strings { get => strings; }

        /// <summary>
        /// A dictionary of all string arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> StringArrays { get => stringArrays; }

        /// <summary>
        /// A dictionary of all integer arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, int[]> IntegerArrays { get => integerArrays; }

        /// <summary>
        /// A dictionary of all <see cref="long"/> arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, long[]> LongArrays { get => longArrays; }

        /// <summary>
        /// A dictionary of all <see cref="float"/> arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, float[]> FloatArrays { get => floatArrays; }

        /// <summary>
        /// A dictionary of all <see cref="byte"/> arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, byte[]> Raws { get => raws; }
        #endregion

        private Storage()
        {
            // Private constructor as this follows the factory pattern.
        }

        /// <summary>
        /// Gets the associated <see cref="Storage"/> object for the given <paramref name="filePath"/>.
        /// <para>If there is no <see cref="Storage"/> for this <paramref name="filePath"/> in the pool, a new <see cref="Storage"/>
        /// is created and parsed before control is returned, this may be an expensive operation depending on the size of the file.</para>
        /// </summary>
        public static Storage GetStorage(string filePath)
        {
            // Lock so that we don't create more than one of the same storage.
            Monitor.Enter(storagePool_locker);

            // Check all loaded storages to see if we already have one for this file.
            for (int i = 0; i < storagePool.Count; i++)
            {
                if (storagePool[i].StoragePath == filePath)
                {
                    Monitor.Exit(storagePool_locker);
                    return storagePool[i];
                }
            }

            // Storage does not exist in memory yet, so create it.
            Storage s = new Storage();
            s.StoragePath = filePath;
            storagePool.Add(s);

            // Release lock before we start parsing the storage file
            // as we shouldn't be holding onto the lock for too long.
            Monitor.Exit(storagePool_locker);

            s.ParseStorageFile();

            return s;
        }

        /// <summary>
        /// Returns an <see cref="IStorageEditor"/> which can be used to modify the values that this storage manages.
        /// <para/>
        /// You must call <see cref="IStorageEditor.Commit"/> to apply the changes to disk.
        /// </summary>
        /// <param name="outputOptimizedForReading">If true, the editor should try and optimize the output file for reading, using new-lines where appropriate.</param>
        public IStorageEditor Edit(bool outputOptimizedForReading = true)
        {
            // Only allow one editor to exist at one time.
            lock (editor_locker)
            {
                if (editor == null)
                    editor = new StorageEditor(this, outputOptimizedForReading);
            }

            return editor;
        }
    }
}