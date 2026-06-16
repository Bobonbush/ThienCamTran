# Hệ thống hội thoại phân nhánh (Dialog System)

Hệ thống thoại cho phép **rẽ nhánh**: ở một câu hỏi, chọn A thì ra câu C, chọn B thì ra câu D.
Hỗ trợ 2 kiểu hiển thị: **khung chữ nhật dưới đáy** và **bong bóng bám trên đầu nhân vật**.

> Toàn bộ code nằm trong `Assets/Scripts/Dialog/`.

---

## 1. Ý tưởng

Một đoạn thoại là một **cây node**. Mỗi node là một câu nói, có một danh sách **lựa chọn (choice)**,
và **mỗi lựa chọn trỏ tới một node kế tiếp**. Rẽ nhánh chính là: choice A trỏ tới node C, choice B trỏ tới node D.

```
          ┌─ chọn A ──► Node C
Node Start ┤
          └─ chọn B ──► Node D
```

Node nào để danh sách lựa chọn **rỗng** thì được coi là câu kết thúc — hệ thống tự hiện nút **Close**.

---

## 2. Các thành phần

| File | Vai trò |
|------|---------|
| `DialogNode.cs` | Một câu thoại (ScriptableObject). Chứa tên người nói, nội dung, và danh sách lựa chọn. |
| `DialogChoice` (trong `DialogNode.cs`) | Một lựa chọn: chữ trên nút + node kế tiếp. |
| `DialogView.cs` | "Bộ mặt" — lo phần hiển thị (text + nút). Dùng cho cả kiểu khung lẫn bong bóng. |
| `DialogManager.cs` | "Bộ não" — lo logic rẽ nhánh. Là singleton, gọi qua `DialogManager.Instance`. |
| `DialogTrigger.cs` | Gắn lên Boss/NPC để bắt đầu thoại (khi player chạm vào, hoặc gọi từ code). |
| `DialogTester.cs` | Script tạm để test bằng phím **F**. Dùng xong xoá được. |

---

## 3. Cách hoạt động

1. Gọi `DialogManager.Instance.StartDialog(node, style, target)`.
2. Manager hiện nội dung của node và sinh ra một nút cho mỗi lựa chọn.
3. Bấm một nút → nhảy tới `nextNode` của lựa chọn đó (đây là phần rẽ nhánh).
4. Tới node không còn lựa chọn → hiện nút **Close** → kết thúc, bắn sự kiện `DialogEnded`.

---

## 4. Tạo nội dung thoại

1. Trong cửa sổ **Project**, chuột phải → **Create > Dialog > Dialog Node**.
2. Mở node vừa tạo, điền:
   - **Speaker Name**: tên người nói (vd `Boss`).
   - **Text**: nội dung câu thoại.
   - **Choices**: bấm `+` để thêm lựa chọn. Mỗi lựa chọn gồm:
     - **Choice Text**: chữ hiện trên nút.
     - **Next Node**: kéo thả node muốn nhảy tới. *Để trống = kết thúc thoại.*
3. Tạo nhiều node và nối chúng lại với nhau để thành cây thoại.

> Mẹo: tạo các node "đích" (C, D...) trước, rồi mới mở node gốc để kéo chúng vào ô Next Node.

---

## 5. Setup trong scene (làm 1 lần)

### Canvas
Dùng một **Canvas** ở chế độ **Screen Space - Overlay**.
EventSystem phải dùng **Input System UI Input Module** (vì dự án xài Input System mới) — nếu thấy
nút "Replace with InputSystemUIInputModule" trên EventSystem thì bấm nó, nếu không các nút sẽ không click được.

### Khung chữ nhật (Box)
- Tạo một Panel `BoxView`, bên trong có: `SpeakerName` (TMP Text), `DialogText` (TMP Text),
  `ChoicesContainer` (empty + **Vertical Layout Group**).
- Gắn script `DialogView` lên `BoxView`, nối các ô: Root, Speaker Name Text, Dialog Text,
  Choices Container, Choice Button Prefab.
- `Follow Target` = **tắt**.

### Bong bóng (Bubble)
- Tạo một Image `BubbleView` (gán sprite bong bóng), bên trong có `DialogText` và `ChoicesContainer`.
- Gắn `DialogView`, nối các ô tương tự (Speaker Name có thể để trống).
- `Follow Target` = **bật**.
- `World Offset` = `(0, 2, 0)` → chỉnh số Y cho bong bóng cao hơn đầu nhân vật.

### Animation mở/đóng (có sẵn trong DialogView)
Mỗi `DialogView` tự chạy animation khi mở và khi đóng. Chỉnh trong Inspector:
- **Use Fade**: hiện/ẩn theo độ trong suốt (alpha).
- **Use Slide**: trượt ngang từ trái sang phải.
- Tick **cả hai** = kết hợp (vừa mờ dần vừa trượt).
- **Anim Duration**: thời gian (giây).
- **Slide Distance**: trượt bắt đầu cách vị trí gốc bao nhiêu pixel về bên trái.

> Script tự thêm `CanvasGroup` vào root để làm fade — không cần gắn tay.
> Animation chạy bằng `Time.unscaledDeltaTime` nên vẫn mượt kể cả khi game đang pause (`timeScale = 0`).

#### Typewriter (chữ hiện từ trái sang phải)
Phần thân thoại hiện dần từng ký tự từ trái sang phải, mỗi ký tự fade theo alpha cho mượt:
- **Use Typewriter**: bật/tắt hiệu ứng. Tắt = chữ hiện ngay lập tức.
- **Type Speed**: số ký tự hiện ra mỗi giây (mặc định 30).
- **Type Fade Chars**: độ rộng vùng fade của mỗi ký tự, tính theo số ký tự (mặc định 3).
  Để `0` = chữ "nhảy" cứng từng ký tự; số lớn = làn sóng mờ mượt hơn.

> Hiệu ứng dùng vertex alpha của TMP nên chữ giữ nguyên vị trí, chỉ mờ → rõ dần (không bị giật layout).
> Slide (trượt khung) và typewriter (chữ) độc lập nhau — tắt slide vẫn giữ được typewriter.

#### Nền đen
"Nền đen" chỉ là màu của component **Image** trên `BoxView`/`BubbleView`:
chọn view đó → component **Image** → ô **Color** → chỉnh sang **đen**, kéo **Alpha** xuống ~`200`
nếu muốn nền hơi trong. Nhớ để **chữ màu sáng** (trắng) cho nổi trên nền đen.

### Manager
- Tạo empty object `DialogManager`, gắn script `DialogManager`.
- Nối `Box View` ← `BoxView`, `Bubble View` ← `BubbleView` (cái nào không dùng để trống cũng được).

### Prefab nút lựa chọn
- Tạo một **Button - TextMeshPro** (bên trong sẵn có 1 TMP Text), kéo vào folder `Prefabs` thành prefab.
- Gán prefab này vào ô `Choice Button Prefab` của các DialogView.

---

## 6. Kích hoạt thoại

### Cách 1 — DialogTrigger (khuyên dùng cho Boss/NPC)
Gắn `DialogTrigger` lên đối tượng:
- **Start Node**: node đầu tiên.
- **Style**: `Box` hoặc `Bubble`.
- **Bubble Target**: nhân vật để bong bóng bám (để trống = chính object này).
- **Trigger Once**: chỉ chạy 1 lần.

Tự chạy khi Player (tag `Player`) chạm vào (cần một Collider2D bật **Is Trigger**),
hoặc gọi `triggerComponent.TriggerDialog()` từ code.

### Cách 2 — Gọi trực tiếp từ code
```csharp
// Kiểu khung chữ nhật
DialogManager.Instance.StartDialog(myStartNode, DialogStyle.Box);

// Kiểu bong bóng bám theo một nhân vật
DialogManager.Instance.StartDialog(myStartNode, DialogStyle.Bubble, character.transform);
```

### Cách 3 — Test nhanh
Gắn `DialogTester` lên Player, gán `Start Node`, chọn `Style`, bấm **Play** rồi nhấn **F**.

---

## 7. Làm gì đó sau khi thoại kết thúc

`DialogManager` bắn sự kiện `DialogEnded` khi đóng thoại. Ví dụ cho Boss bắt đầu đánh:

```csharp
private void Start()
{
    DialogManager.Instance.DialogEnded += StartFight;
}

private void StartFight()
{
    DialogManager.Instance.DialogEnded -= StartFight; // bỏ đăng ký để khỏi gọi lại
    // ... bật AI, cho boss tấn công
}
```

---

## 8. Gợi ý mở rộng

- **Khoá điều khiển player khi đang thoại**: tắt input/di chuyển trong lúc thoại, bật lại ở `DialogEnded`.
- **Gõ xong chữ mới hiện nút**: trong `DialogManager.ShowNode`, chờ typewriter chạy xong rồi mới spawn nút (kiểu visual novel).
- **Hậu quả theo nhánh** (vd chọn A boss nổi giận): cho lựa chọn dẫn tới node kết thúc riêng,
  rồi đọc node cuối để biết player đã đi nhánh nào.
- **Chân dung nhân vật**: thêm một ô Image vào `DialogView` và gán theo node.
