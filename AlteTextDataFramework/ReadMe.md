# AlteTextDataFramework 仕様書 & README

Unity向けの超軽量・ゼロアロケーション（Zero-Allocation）多言語テキスト管理システムです。
ゲーム内の全テキストをバイナリ形式へ集約し、ランタイムのメモリ効率を極限まで最適化することを目的としています。

---

## 🛠 1. 全体構造とデータフロー

本システムは、ゲーム全体で常駐する共通テキスト（**Master**）と、画面やステージごとに排他ロードするテキスト（**Scene**）の2レイヤー構造で成り立っています。

### バイナリファイルの役割（5つの `FileKinds`）

コンバーターによってシリアライズされたデータは、以下の5つのファイルに切り分けられて出力されます。

1. **`lang.dat` (Lang)**: 言語ごとのメタデータ。全シーン中での「最大チャンク数」「最大文字列数」「最大charデータ数」を固定長（int×3）で記録。ランタイムのバッファ事前確保に使用します。
2. **`scene.dat` (Scene)**: 対象シーン（またはMaster）内に含まれる総チャンク数を記録。
3. **`chunk.dat` (Chunk)**: チャンクごとの文字列オフセット（各チャンクが何番目の文字列から始まるか）を記録。
4. **`offsets.dat` (Offsets)**: 文字列ごとの文字オフセット（各文字列が `data.dat` の何文字目から始まるか）を記録。
5. **`data.dat` (Data)**: すべてのテキストを隙間なく連結した、純粋な `char`（文字）のバイナリ配列。

---

## 💾 2. メモリ割り当ての具体仕様（ゼロアロケーションの仕組み）

### ランタイムバッファ（`char[] buffer`）のメモリマップ

システム初期化時（`Initialize`）に、`lang.dat` から読み込んだ「最大シーン文字数（`maxSceneDataLength`）」と「Masterの文字数」を合算したサイズの配列を**一度だけ一括確保**します。

```
◆ AlteTextDataFramework 内部の char[] buffer 構造

  [================= Master 領域 =================][================== Scene 領域 (上書きエリア) ==================]
  0                                              border                                                       buffer.Length
  └─ 固定永続（初期化時にコピー）                    └─ シーン遷移時にクリアされ、新しいシーンデータを上書きコピー

```

* **`border`（境界線）**: Master領域の総文字数であり、Scene領域の開始オフセット（底上げ値）となります。
* **シーン切り替え（`SetSceneData`）**: バッファ後半のScene領域を `Clear` し、新しいシーンの `char` データをそのまま上書きコピー（`CopyTo`）します。**ゲームプレイ中にメモリの再確保（アロケーション）が一切発生しない**ため、GCスパイクを完全に防ぎます。

---

## 🔍 3. テキスト走査（ルックアップ）の内部仕様

テキストの取得は、文字列の複製を行わず、`buffer` 配列の特定の範囲を指す `ReadOnlySpan<char>` を切り出すことで実現しています。

`TextPointer` から指定された `chunk` と `index` を基に、以下の計算式でバッファを切り出します。

### ① Masterテキストの場合 (`isMaster == true`)

1. `masterChunkOffsets[chunk]` から、対象チャンクの開始文字列インデックスを取得。
2. そこに `pointer.index` を足して、全体の絶対文字列番号 `offsetIndex` を算出。
3. `masterOffsets[offsetIndex]`（開始位置）から、次の要素との差分（文字数）の長さを、`buffer` の `0` から切り出す。

### ② シーンテキストの場合 (`isMaster == false`)

1. `sceneChunkOffsets[chunk] + pointer.index` から、現在のシーン内での絶対文字列番号 `offsetIndex` を算出。
2. `sceneOffsets[offsetIndex]` でシーン内ローカルの開始位置を取得し、そこに **`border`** を足すことで、`buffer` 全体における絶対開始位置を特定。
3. 文字数分（次の要素との差分）の長さを切り出す。

---

## 📝 4. 導入・実装手順

### Step 1: 列挙型の定義 (`TextLanguage`, `TextScene`)

`AlteTextDataFramework.cs` 内の列挙型に、プロジェクトで必要な言語とシーンを定義します。

> ⚠️ **重要**: 末尾の `Count` はシステムが要素数を自動取得するためのベンチマークです。必ず**末尾のまま変更せず、新しい要素はその上に追記**してください。

```csharp
public enum TextLanguage
{
    Japanese,
    English,
    Count // 触らない
}

public enum TextScene
{
    Title,
    InGame,
    Count // 触らない
}

```

### Step 2: エディタコンバーターのパス記述

`AlteTextBinaryConverter.cs` の `GetMasterPath` および `GetScenePath` に、元となるテキストファイルのパス（1行＝1テキストのテキストファイルなど）を返すロジックを記述します。

* 1つのシーンに対し、複数のパス（配列）を返した場合は、それがそのまま「**チャンク（Chunk）**」として分割管理されます。

```csharp
private Span<string> GetMasterPath(int lang)
{
    // 例：言語ごとの共通テキストソースのパスを返す
    return new string[] { Path.Combine(UnityEngine.Application.dataPath, "RawTexts/Master.txt") };
}

private Span<string> GetScenePath(int lang, int scene)
{
    // 例：各シーン・言語に応じたテキストソースのパスを返す
    string sceneName = ((TextScene)scene).ToString();
    return new string[] { Path.Combine(UnityEngine.Application.dataPath, $"RawTexts/{sceneName}.txt") };
}

```

### Step 3: ランタイムでの呼び出しフロー

ゲーム起動時に初期化を行い、画面遷移に合わせてシーンデータをロードします。

```csharp
using Alte.Data.Text;
using UnityEngine;

public class TextSystemDriver : MonoBehaviour
{
    void Awake()
    {
        // 1. ゲーム起動時に初期化（言語インデックスを指定。自動でMasterデータがロードされる）
        AlteTextDataIO.Initialize(TextLanguage.Japanese); 
    }

    void Start()
    {
        // 2. シーンのロード（インゲーム用テキストをメモリに展開）
        AlteTextDataIO.LoadScene(TextScene.InGame);

        // 3. テキストの参照
        // 例: シーンテキスト / チャンク0 / インデックス12 の文字列ポインタを作成
        TextPointer greetingPtr = new TextPointer(isMaster: false, chunk: 0, index: 12);

        // stringとして取得（アロケーションが発生します）
        string textStr = greetingPtr.String; 

        // ReadOnlySpan<char> として取得（★完全ゼロアロケーション）
        ReadOnlySpan<char> textSpan = greetingPtr.Char;
        
        // TextMeshProなど、Spanに対応したUIコンポーネントへ直接流し込むのが最も効果的です
        // myTextMeshPro.SetText(textSpan);
    }
}

```

---

## 💡 5. コンポーネントおよび技術仕様メモ

### `AlteTextDataIO` (入出力管理)

* **`SceneDataStruct`**: `ArrayPool<T>.Shared.Rent` で取得したアンパブリックな配列バッファを、安全かつ確実に解放（`Return`）するための `ref struct` です。`IDisposable` の機構を利用して `try-finally` で確実に回収されます。
* **`AlteTextDataInput.BinaryReader`**: `MemoryMarshal.AsBytes(result)` を用いて、ファイルストリームからダイレクトに `Span<T>` 領域にバイナリデータを流し込むため、読み込み中の中間アロケーションも排除されています。

### `AlteTextBinaryConverter` (エディタコンバーター)

* Unityエディタでの起動時に、自動でインスタンス化されシリアライズを実行します。
* `WriteOffset` などのバッファ確保には `stackalloc` を利用しており、高速かつクリーンにバイナリ生成を行います。
