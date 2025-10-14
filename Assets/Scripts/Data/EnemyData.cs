using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "DemoV1/Data/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy basic settings")]
    public float health = 50f;
    public float moveSpeed = 2f;
    public float rotateSpeed = 120f;
    public float successDistance = 0.8f;
    // Sẽ thêm thuộc tính về sau nếu cần: damage, detectionRange...
}
