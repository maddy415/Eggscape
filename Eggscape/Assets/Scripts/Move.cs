using UnityEngine;

public class Move : MonoBehaviour
{
    
    public float moveSpeed;
    void Update()
    {
        float moveInputX = Input.GetAxisRaw("Horizontal"); 
        float moveInputY = Input.GetAxisRaw("Vertical"); 

        transform.position += new Vector3(moveInputX, moveInputY, 0) * moveSpeed * Time.deltaTime;
    }
}
