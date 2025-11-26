using UnityEngine;

public class IceSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public IceData iceData; // 使用的碎冰数据
    public GameObject icePrefab; // 碎冰预制体
    
    [Header("生成位置")]
    public Transform spawnParent; // 父物体
    public Vector3 spawnPosition = Vector3.zero; // 生成位置
    
    /// <summary>
    /// 生成碎冰物体
    /// </summary>
    public GameObject SpawnIce()
    {
        if (icePrefab == null)
        {
            Debug.LogError("未设置碎冰预制体！");
            return null;
        }
        
        GameObject iceObject = Instantiate(icePrefab, spawnPosition, Quaternion.identity, spawnParent);
        IceBehavior iceBehavior = iceObject.GetComponent<IceBehavior>();
        
        if (iceBehavior != null && iceData != null)
        {
            iceBehavior.SetIceData(iceData);
        }
        
        return iceObject;
    }
    
    /// <summary>
    /// 在指定位置生成碎冰
    /// </summary>
    public GameObject SpawnIceAtPosition(Vector3 position)
    {
        spawnPosition = position;
        return SpawnIce();
    }
    
    /// <summary>
    /// 使用指定数据生成碎冰
    /// </summary>
    public GameObject SpawnIceWithData(IceData customIceData, Vector3 position)
    {
        iceData = customIceData;
        spawnPosition = position;
        return SpawnIce();
    }
}