# JsonBinary: JSON to Compact Binary Format

🚀 **JsonBinary**는 JSON 데이터를 효율적인 바이너리 포맷으로 변환하고 다시 복원할 수 있는 C# 라이브러리입니다.  
텍스트 기반 JSON보다 **더 작은 크기와 빠른 로딩 속도**를 제공합니다.

이 프로젝트는 2012년쯤 작업했던 프로젝트에서 사용했던 방식을 다시 작성한 버전입니다.
현재 코드에서는 오래된 `System.Json` 패키지를 사용하고 있으므로, 최신 JSON 처리 라이브러리(예: `System.Text.Json` 또는 `Newtonsoft.Json`)를 활용하는 것이 좋습니다.

---

## 🔹 주요 특징

- **JSON → 바이너리 변환**: `ConvertToBinary()`
- **바이너리 → JSON 복원**: `ReadFromBinary()`
- **데이터 타입 최적화**: `char`, `short`, `int`, `double` 등 다양한 숫자 타입 지원
- **최대 255개 키 리스트 저장**: 중복 제거를 통한 효율적인 키 저장 방식
- **빠른 변환 속도**: `HashSet<string>`을 활용한 키 탐색 최적화
- **데이터 손실 없는 복원**: 바이너리 데이터에서 JSON 객체로 변환 가능

---

## 📌 사용 예제

### 1️⃣ JSON을 바이너리로 변환

```csharp
JsonBinary.ConvertToBinary("data.json", "data.bin", overwrite: true);
```

### 2️⃣ 바이너리를 JSON으로 복원

```csharp
JsonValue json = JsonBinary.ReadFromBinary("data.bin");
Console.WriteLine(json.ToString());
```

---

## 💡 내부 동작 방식

### ✔ JSON → 바이너리 변환
1. JSON 파일을 읽고 `BinaryWriter`를 사용하여 바이너리 포맷으로 저장  
2. **키 리스트를 따로 저장**하여 중복을 줄이고 크기 최적화  
3. 데이터 타입(`string`, `number`, `boolean`, `array`, `object`)을 구분하여 저장  

### ✔ 바이너리 → JSON 복원
1. 파일 헤더(`BinaryJson_V0.01`)를 확인 후 데이터 파싱  
2. **키 리스트를 로드하여 참조**  
3. 바이너리 데이터를 JSON 객체로 변환  

---

## 🛠 최적화 및 개선 사항

- **문자열 압축**: GZip을 활용한 문자열 크기 축소 가능
- **숫자 타입 확장**: `float`, `long`, `decimal` 지원 추가
- **파일 헤더 검증 강화**: 잘못된 파일 형식 예외 처리
- **키 탐색 속도 개선**: `HashSet<string>` 사용하여 빠른 탐색
- **최신 JSON 라이브러리 적용 권장**: `System.Json` 대신 `System.Text.Json` 또는 `Newtonsoft.Json` 사용 고려

---

