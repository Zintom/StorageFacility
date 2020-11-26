using System.Collections.Generic;
using System.Threading;

namespace Zintom.StorageFacility
{
    /// <summary>
    /// A class which simplifies data storage to disk allowing for easy saving, loading and access of a key-value data structure.
    /// </summary>
    public sealed partial class Storage
    {

        private string StoragePath = "";

        private readonly object _editorLocker = new object();
        private IStorageEditor? _editor;

        private readonly static object _storagePoolLocker = new object();
        private readonly static List<Storage> _storagePool = new List<Storage>();

        // To add more types, ensure the following:
        // - StorageParser checks for the new type.
        // - The GetTypeFromShortHand() method is updated with the new type.
        // - The IStorageEditor has a PutValue method for the new type, commits the type to file, and clears on Clear().
        // - Make sure to add a Public Property here.
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _booleans = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _integers = new Dictionary<string, int>();
        private readonly Dictionary<string, long> _longs = new Dictionary<string, long>();
        private readonly Dictionary<string, float> _floats = new Dictionary<string, float>();

        private readonly Dictionary<string, string[]> _stringArrays = new Dictionary<string, string[]>();
        private readonly Dictionary<string, int[]> _integerArrays = new Dictionary<string, int[]>();
        private readonly Dictionary<string, long[]> _longArrays = new Dictionary<string, long[]>();
        private readonly Dictionary<string, float[]> _floatArrays = new Dictionary<string, float[]>();
        private readonly Dictionary<string, byte[]> _raws = new Dictionary<string, byte[]>();

        #region Public Properties
        /// <summary>
        /// A dictionary of all <see cref="bool"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, bool> Booleans { get => _booleans; }

        /// <summary>
        /// A dictionary of all <see cref="int"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, int> Integers { get => _integers; }

        /// <summary>
        /// A dictionary of all <see cref="long"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, long> Longs { get => _longs; }

        /// <summary>
        /// A dictionary of all <see cref="float"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, float> Floats { get => _floats; }

        /// <summary>
        /// A dictionary of all <see cref="string"/>'s contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> Strings { get => _strings; }

        /// <summary>
        /// A dictionary of all string arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> StringArrays { get => _stringArrays; }

        /// <summary>
        /// A dictionary of all integer arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, int[]> IntegerArrays { get => _integerArrays; }

        /// <summary>
        /// A dictionary of all <see cref="long"/> arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, long[]> LongArrays { get => _longArrays; }

        /// <summary>
        /// A dictionary of all <see cref="float"/> arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, float[]> FloatArrays { get => _floatArrays; }

        /// <summary>
        /// A dictionary of all <see cref="byte"/> arrays contained within this <see cref="Storage"/>.
        /// </summary>
        public IReadOnlyDictionary<string, byte[]> Raws { get => _raws; }
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
            Monitor.Enter(_storagePoolLocker);

            // Check all loaded storages to see if we already have one for this file.
            for (int i = 0; i < _storagePool.Count; i++)
            {
                if (_storagePool[i].StoragePath == filePath)
                {
                    Monitor.Exit(_storagePoolLocker);
                    return _storagePool[i];
                }
            }

            // Storage does not exist in memory yet, so create it.
            Storage s = new Storage();
            s.StoragePath = filePath;
            _storagePool.Add(s);

            // Release lock before we start parsing the storage file
            // as we shouldn't be holding onto the lock for too long.
            Monitor.Exit(_storagePoolLocker);

            s.ParseStorageFile();

            return s;
        }

        /// <summary>
        /// Gets the <see cref="IStorageEditor"/> for this <see cref="Storage"/>, which can be used to modify the values that this storage manages.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="IStorageEditor"/> persists for the life-time of this <see cref="Storage"/> object and can be called up through multiple calls to <c>Edit()</c>,
        /// the object returned is an identical reference to the underlying editor.
        /// <para/>
        /// You must call <see cref="IStorageEditor.Commit"/> to apply the changes to disk.
        /// </remarks>
        /// <param name="outputOptimizedForReading">If true, the editor should try and optimize the output file for reading, using new-lines where appropriate.</param>
        public IStorageEditor Edit(bool outputOptimizedForReading = true)
        {
            // If the editor is not null, skip the lock code as the editor is definitely not null
            // and is only ever modified by this block so we can guarantee
            // it will never turn null.
            if (_editor != null) return _editor;

            // Only allow one editor to exist at one time.
            lock (_editorLocker)
            {
                if (_editor == null)
                    _editor = new StorageEditor(this, outputOptimizedForReading);
            }

            return _editor;
        }
    }
}