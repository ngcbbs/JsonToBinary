using System.Diagnostics;
using System.Json;
using System.Text;

namespace JsonToBinary
{
    internal class Program
    {
        static void Main(string[] args) {
	        //var target = "data_tiny";
            //var target = "data_mini";
            //var target = "array_test";
            var target = "data_big";
            var outFile = $"{target}_out.json";
            var testCount = 1000;

            Console.WriteLine("바이너리 파일 변환 준비...");
            JsonBinary.ConvertToBinary($"data\\{target}.json", $"{target}.bin", true);
            Console.WriteLine("바이너리 파일 쓰기 준비...");
            var result = JsonBinary.ReadFromBinary($"{target}.bin");
            if (result != null) {
	            Console.WriteLine("바이너리 포멧 읽기 성공...");
	            var jsonText = result.ToString().Replace("\\\"", ""); // note: JsonValue.ToString() has strange string output.. :(
	            if (File.Exists(outFile))
		            File.Delete(outFile);
	            File.WriteAllText($"{target}_out.json", jsonText, Encoding.UTF8);
            }
            else {
	            Console.WriteLine("바너리 포멧 읽기 실패...");
	            return;
            }
            
            Console.WriteLine("테스트1...");
            long result1 = Test1($"data\\{target}.json", testCount);
            Console.WriteLine("테스트2...");
            long result2 = Test2($"data\\{target}.json", testCount);
            Console.WriteLine("테스트3...");
            long result3 = Test3($"{target}.bin", testCount);
			Console.WriteLine($"테스트 결과 = {result1}, {result2}, {result3}");
        }

        // 텍스트에서 읽기...
        private static long Test1(string filename, int testCount) {
	        Stopwatch watch = new Stopwatch();
	        watch.Start();
	        for (var i = 0; i < testCount; ++i)
	        {
		        var jsonString = File.ReadAllText(filename);
		        JsonValue? jv = JsonValue.Parse(jsonString);
		        //var json = jv?.ToString();
	        }
	        watch.Stop();
	        return watch.ElapsedMilliseconds;
        }
        
        // 텍스트에서 읽기(파일스트림)...
        private static long Test2(string filename, int testCount) {
	        Stopwatch watch = new Stopwatch();
	        watch.Start();
	        for (var i = 0; i < testCount; ++i) {
		        using FileStream fs = new FileStream(filename, FileMode.Open);
		        JsonValue? jv = JsonValue.Load(fs);
		        //var json = jv?.ToString();
	        }
	        watch.Stop();
	        return watch.ElapsedMilliseconds;
        }

        private static long Test3(string filename, int testCount) {
	        Stopwatch watch = new Stopwatch();
	        watch.Start();
	        for (var i = 0; i < testCount; ++i) {
		        JsonValue? jv = JsonBinary.ReadFromBinary(filename);
		        //var json = jv?.ToString();
	        }
	        watch.Stop();
	        return watch.ElapsedMilliseconds;
        }
        
        
    }
}
