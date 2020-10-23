using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zintom.IO.StorageFacility;

namespace DemoApp
{
    class Program
    {
        static void Main(string[] args)
        {

            Storage storage = Storage.GetStorage("DemoApp.dat");

            var editor = storage.Edit();

            editor.Clear(true);
            editor.PutValue("testKey", @"test""""Value");
            editor.PutValue("testKey2", @"test""Value2");
            editor.PutValue(@"strArray1", new string[] { "testValues1", "testValues2" });
            editor.PutValue(@"strArray2", new string[] { "helloyou", "hellotwo" });
            editor.PutValue(@"intArray1", new int[] { 1, int.MaxValue });
            editor.PutValue(@"longArray1", new long[] { 1, long.MaxValue });
            editor.PutValue(@"floatArray1", new float[] { 1, float.MaxValue });

            editor.PutValue("testRaw", Encoding.Default.GetBytes("HELLO WORLD!"));
            editor.PutValue("testRaw2", Encoding.Default.GetBytes("HELLO WORLD!File.WriteAllBytes(testAudio.mp3, storage.Raws[testRaw]);HELLO WORLD!File.WriteAllBytes(testAudio.mp3, storage.Raws[testRaw]);HELLO WORLD!File.WriteAllBytes(testAudio.mp3, storage.Raws[testRaw]);HELLO WORLD!File.WriteAllBytes(testAudio.mp3, storage.Raws[testRaw]);"));

            //byte[] song = new byte[99999 * 5];
            //Array.Copy(File.ReadAllBytes(@"D:\All Files\Music\An0maly - I Like Trump.mp3"), 0, song, 0, 99999 * 5);

            //editor.PutValue("testRaw", song);

            editor.PutValue("number1", int.MaxValue);
            editor.PutValue("number2", long.MaxValue);
            editor.PutValue("number3", float.MaxValue);
            editor.PutValue("onOffSwitch", true);

            editor.Commit();

            // Write audio to file as test:
            //File.WriteAllBytes("testAudio.mp3", storage.Raws["testRaw"]);

            Console.ReadKey();
        }

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
    }
}