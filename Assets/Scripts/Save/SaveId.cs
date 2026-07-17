using System.Text;
using UnityEngine;

/// <summary>
/// ID định danh cho object cần save (rương, tường vỡ, puzzle, quái, boss).
/// - Gắn component này và tự đặt tên (vd "OutSkirt-1_Chest_001") khi muốn
///   ID đọc được lúc debug.
/// - KHÔNG gắn cũng chạy được: hệ thống tự sinh ID ổn định dạng
///   "TênScene_TênObject_đường-dẫn-sibling" — vẫn có tên scene ở đầu để dễ
///   truy vết, và không phải sửa tay từng scene.
/// ID tự sinh chỉ đổi khi object bị đổi tên/di chuyển trong hierarchy.
/// </summary>
public class SaveId : MonoBehaviour
{
    [Tooltip("Bỏ trống = tự sinh theo scene + hierarchy. Đặt tay khi cần ID đẹp để debug, vd OutSkirt-1_Chest_001")]
    [SerializeField] private string id;

    public string Id
    {
        get { return string.IsNullOrWhiteSpace(id) ? SaveIdUtility.AutoId(transform) : id; }
    }
}

public static class SaveIdUtility
{
    /// <summary>
    /// ID cho component bất kỳ: ưu tiên SaveId gắn tay, không có thì tự sinh.
    /// </summary>
    public static string For(Component component)
    {
        if (component == null)
            return "";

        SaveId explicitId = component.GetComponentInParent<SaveId>();
        if (explicitId != null)
            return explicitId.Id;

        return AutoId(component.transform);
    }

    /// <summary>
    /// "TênScene_TênGốc_a.b.c" với a.b.c là chuỗi sibling index từ root xuống —
    /// ổn định qua các lần load, không trùng giữa hai object cùng tên.
    /// </summary>
    public static string AutoId(Transform target)
    {
        if (target == null)
            return "";

        StringBuilder path = new StringBuilder();
        Transform current = target;
        while (current != null)
        {
            if (path.Length > 0)
                path.Insert(0, '.');
            path.Insert(0, current.GetSiblingIndex());
            current = current.parent;
        }

        return target.gameObject.scene.name + "_" + target.name + "_" + path;
    }
}
