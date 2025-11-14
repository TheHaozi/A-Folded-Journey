using UnityEngine;

public class Mouse2DRaycast : MonoBehaviour
{
    public Camera mainCamera;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 方法1：使用RaycastHit2D
            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            
            if (hit.collider != null)
            {
                Debug.Log("点击了: " + hit.collider.gameObject.name);
                Debug.Log("点击位置: " + hit.point);
                
                // 获取点击物体的位置
                Debug.Log("物体位置: " + hit.transform.position);
            }
        }
    }
}
