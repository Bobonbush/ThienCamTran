- Prefab lưu hết vào trong cái mục Character nhớ phân chia mục con rõ ràng.
- Scene : Có test scene để có gì đó mà test còn lại là scene dùng cho gameplay cũng nên phân ra thư mục.
- Scripts : Các script global như damagable nên để ở thư mục cha còn lại phải nhét vào thư mục con tương ứng để phân loại Player, Enemies, Traps,...
- Art : Nên phân mục.

### Giải thích một số Prefab hay Code có thể gây khó hiểu : 
- SafeGround : Là một cái object rỗng có hình chữ nhật, khi người chơi đi qua nó sẽ lưu lại đây là chỗ có thể quay lại nếu dẫm phải trap.
- Architecture của mình nên ưu tiên OOP nếu có thể, làm như thế này cũng được nhưng phải kiểm soát code cho tốt đọc không quá hiểu.


### Giải thích Animator Layers.
...