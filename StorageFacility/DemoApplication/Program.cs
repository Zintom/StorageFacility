using System;
using System.Text;
using Zintom.StorageFacility;

namespace DemoApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Storage storage = Storage.GetStorage("DemoApp.dat");

            storage.DisplayLoadedValues();

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
            editor.PutValue("number1", int.MaxValue);
            editor.PutValue("number2", long.MaxValue);
            editor.PutValue("number3", float.MaxValue);
            editor.PutValue("onOffSwitch", true);

            //editor.Commit();

            Console.ReadKey();
        }
    }
}
