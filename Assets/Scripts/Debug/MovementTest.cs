using UnityEngine;

public class MovementTest : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotateSpeed = 120f;

    void Update()
    {
        // đặt ở đầu Update()
        bool wPressed = false, sPressed = false, aPressed = false, dPressed = false;

        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        using UnityEngine.InputSystem;
        if (Keyboard.current != null) {
            wPressed = Keyboard.current.wKey.isPressed;
            sPressed = Keyboard.current.sKey.isPressed;
            aPressed = Keyboard.current.aKey.isPressed;
            dPressed = Keyboard.current.dKey.isPressed;
        }
        #else
        wPressed = Input.GetKey(KeyCode.W);
        sPressed = Input.GetKey(KeyCode.S);
        aPressed = Input.GetKey(KeyCode.A);
        dPressed = Input.GetKey(KeyCode.D);
        #endif

        Debug.Log($"Keys: W={wPressed} S={sPressed} A={aPressed} D={dPressed}");

        float move = 0f;
        float turn = 0f;
        if (Input.GetKey(KeyCode.W)) move = 1f;
        if (Input.GetKey(KeyCode.S)) move = -1f;
        if (Input.GetKey(KeyCode.A)) turn = -1f;
        if (Input.GetKey(KeyCode.D)) turn = 1f;

        transform.Rotate(0f, turn * rotateSpeed * Time.deltaTime, 0f);
        transform.position += transform.forward * move * moveSpeed * Time.deltaTime;
    }
}
