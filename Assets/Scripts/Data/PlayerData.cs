using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "DemoV1/Data/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("Player basic settings")]
    public float health = 100f;
    public Vector3 defaultPosition = Vector3.zero;
    // Thêm thuộc tính về sau nếu cần: speed, colliderSize, model reference...
}
