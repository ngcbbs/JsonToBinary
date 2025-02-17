using System.Text;
using System.Json;
using JsonPair = System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>;

namespace JsonToBinary {
	public static class JsonBinary {
		const string kBinaryFileHeader = "BinaryJson_V0.01";
        const int kMaxKeyLength = 255;

        private static byte TypeToByte(JsonType type) {
	        return type switch {
		        JsonType.String => 0xf0,
		        JsonType.Number => 0xf1,
		        JsonType.Array => 0xf2,
		        JsonType.Object => 0xf3,
		        JsonType.Boolean => 0xf4,
		        _ => throw new Exception($"invalid input type = {type}")
	        };
        }

        private static JsonType ByteToType(byte value) {
	        return value switch {
		        0xf0 => JsonType.String,
		        0xf1 => JsonType.Number,
		        0xf2 => JsonType.Array,
		        0xf3 => JsonType.Object,
		        0xf4 => JsonType.Boolean,
		        _ => throw new ArgumentOutOfRangeException($"invalid input value = {value}")
	        };
        }
        
        #region write
        
        public static void ConvertToBinary(string jsonFilename, string binaryFilename, bool overwrite) {
			using var streamReader = new StreamReader(jsonFilename, Encoding.UTF8);

			if (!overwrite && File.Exists(binaryFilename))
				throw new Exception("바이너리 파일을 덮어 쓸 수 없습니다. (overwrite=" + overwrite + ")");

			using var filestream = new FileStream(binaryFilename, FileMode.Create);
			using var writer = new BinaryWriter(filestream);
			var keyList = new List<string> { "<null>" };
			
			writer.Write(Encoding.UTF8.GetBytes(kBinaryFileHeader));

			Convert(null, JsonValue.Load(streamReader), writer, keyList);
			
			if (keyList.Count > kMaxKeyLength) {
				writer.Close();
				throw new Exception($"사용되는 키의 최대치는 {kMaxKeyLength}개 입니다.");
			}

			var offset = writer.BaseStream.Position; // 키 시작 오프셋
			writer.Write((byte)keyList.Count); // 키 갯수 제한 255개
			foreach (var key in keyList)
				writer.Write(key);
			writer.Write(offset);

			writer.Close();
		}

		private static void WriteNumber(BinaryWriter writer, JsonValue jsonValue) {
			var stringValue = jsonValue.ToString();
			if (char.TryParse(stringValue, out var charValue)) {
				writer.Write((byte)0x1);
				writer.Write(charValue);
			} else if (short.TryParse(stringValue, out var shortValue)) {
				writer.Write((byte)0x2);
				writer.Write(shortValue);
			} else if (int.TryParse(stringValue, out var intValue)) {
				writer.Write((byte)0x3);
				writer.Write(intValue);
			} else if (double.TryParse(stringValue, out var doubleValue)) {
				writer.Write((byte)0x4);
				writer.Write(doubleValue);
			}
			else {
				throw new Exception($"숫자 분류 실패 {stringValue}");
			}
		}

		private static void Convert(string? key, JsonValue? jValue, BinaryWriter writer, List<string> keyList) {
			var typeByte = jValue == null ? (byte)0xff : TypeToByte(jValue.JsonType);

			writer.Write(typeByte);
			var keyIndex = key != null ? (byte)keyList.IndexOf(key) : (byte)0; 
			writer.Write(keyIndex);

			if (jValue == null)
				return;

			switch (jValue.JsonType) {
				case JsonType.Array:
					if (jValue is JsonArray array) {
						var count = array.Count;
						writer.Write(count);
						for (var i = 0; i < count; ++i)
							Convert(key, array[i], writer, keyList);
					}
					break;
				
				case JsonType.Object:
					if (jValue is JsonObject obj) {
						var count = obj.Count;
						writer.Write(count);
						foreach (var objKey in obj.Keys) {
							if (objKey == null)
								continue;
							if (!keyList.Contains(objKey)) // slow
								keyList.Add(objKey);
							Convert(objKey, obj[objKey], writer, keyList);
						}
					}
					break;

				case JsonType.Boolean: {
					writer.Write((bool)jValue);
					break;
				}

				case JsonType.Number: {
					WriteNumber(writer, jValue);
					break;
				}

				case JsonType.String: {
					writer.Write((string)jValue); // 압축!?
					break;
				}
			}
		}
		
		#endregion // write
		
		#region read
		
		private static double ReadNumber(BinaryReader reader) {
			var valueType = reader.ReadByte();
			switch (valueType) {
				case 0x1:
					return reader.ReadByte();
				case 0x2:
					return reader.ReadInt16();
				case 0x3:
					return reader.ReadInt32();
				case 0x4:
					return reader.ReadDouble();
				default:
					throw new Exception($"숫자 타입 분류 실패 {valueType}");
			}
		}
		
		public static JsonValue? ReadFromBinary(string filename) {
			using var filestream = new FileStream(filename, FileMode.Open);
			using var reader = new BinaryReader(filestream);

			var root = new JsonObject();
			var keyList = new List<string>();

			// 헤더 읽기
			var fileHeader = Encoding.UTF8.GetString(reader.ReadBytes(16)); // todo: 예외처리
			if (!string.Equals(kBinaryFileHeader, fileHeader))
				return null;

			// 키값 목록 읽기...
			reader.BaseStream.Seek(-8, SeekOrigin.End);
			var offset = reader.ReadInt64();
			reader.BaseStream.Seek(offset, SeekOrigin.Begin);
			var keyCount = reader.ReadByte();
			for (byte i = 0; i < keyCount; ++i) {
				var key = reader.ReadString();
				keyList.Add(key);
			}

			// json 재구성...
			reader.BaseStream.Seek(16, SeekOrigin.Begin);
			var readObject = ReadBinary(reader, keyList);

			reader.Close();

			return readObject.value;
		}
		
		private static (string key, JsonValue value) ReadBinary(BinaryReader reader, List<string> keyList, bool isArrayElement = false) {
			var byteType = reader.ReadByte();
			var keyIndex = reader.ReadByte();
			var keyString = keyList[keyIndex];

			// null 객체..
			if (byteType == 0xff)
				return (keyString, null)!;
			
			var jsonType = ByteToType(byteType);
			
			switch (jsonType) {
				case JsonType.Array: {
					var count = reader.ReadInt32();
					var array = new List<JsonValue>();
					for (var i = 0; i < count; ++i)
						array.Add(ReadBinary(reader, keyList).value);
					var result = (keyString, new JsonArray(array));
					array.Clear();
					return result;
				}
				
				case JsonType.Object: {
					var count = reader.ReadInt32();
					var items = new List<JsonPair>();
					for (var i = 0; i < count; ++i) {
						var item = ReadBinary(reader, keyList);
						
						items.Add(new JsonPair(item.key, item.value));
					}
					var result = (keyString, new JsonObject(items));
					items.Clear();
					return result;
				}
				
				case JsonType.Boolean:
					return (keyString, reader.ReadBoolean());

				case JsonType.Number:
					return (keyString, ReadNumber(reader));

				case JsonType.String:
					return (keyString, reader.ReadString());
			}
			
			return (null, null)!;
		}

		#endregion
	}
}