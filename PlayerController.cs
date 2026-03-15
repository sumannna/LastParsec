using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float thrustForce = 8f;
    public float maxSpeed = 6f;
    public float mouseSensitivity = 2f;
    public float bounceFactor = 0.1f;
    public float inertiaDamping = 0.9995f;
    public float stopThreshold = 0.001f;

    [Header("参照")]
    public FuelSystem fuelSystem;
    public OxygenSystem oxygenSystem;
    public VitalSystem vitalSystem;
    public InventoryUI inventoryUI;
    public Transform cameraRig;
    public Transform cameraTransform;

    private CharacterController controller;
    private float verticalRotation = 0f;
    private Vector3 velocity = Vector3.zero;

    [Header("重力設定")]
    public float gravityStrength = 9.8f;
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpForce = 4f;
    public float crouchHeight = 0.5f;
    public float standHeight = 2f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("[PlayerController] CharacterController が見つかりません。");
            enabled = false;
            return;
        }

        if (cameraRig == null)
        {
            Debug.LogError("[PlayerController] cameraRig が未設定です。");
            enabled = false;
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("[PlayerController] cameraTransform が未設定です。");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        bool isDead = (oxygenSystem != null && oxygenSystem.IsGameOver)
                   || (vitalSystem != null && vitalSystem.IsDead);

        bool inventoryOpen = (inventoryUI != null && inventoryUI.IsOpen)
          || (CraftUI.Instance != null && CraftUI.Instance.IsOpen)
          || (CraftTreeUI.Instance != null && CraftTreeUI.Instance.IsOpen);

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        if (!isDead && !inventoryOpen)
        {
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            cameraRig.Rotate(Vector3.up * mouseX);
        }

        bool hasGravity = EnvironmentSystem.Instance != null && EnvironmentSystem.Instance.HasCentrifugalGravity;

        if (hasGravity)
        {
            if (!isDead)
            {
                bool shift = Input.GetKey(KeyCode.LeftShift);
                bool crouch = Input.GetKey(KeyCode.LeftControl);
                float speed = crouch ? walkSpeed * 0.5f : (shift ? runSpeed : walkSpeed);

                controller.height = crouch ? crouchHeight : standHeight;

                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.W)) move += cameraRig.forward;
                if (Input.GetKey(KeyCode.S)) move -= cameraRig.forward;
                if (Input.GetKey(KeyCode.A)) move -= cameraRig.right;
                if (Input.GetKey(KeyCode.D)) move += cameraRig.right;
                move.y = 0f;
                if (move.magnitude > 1f) move.Normalize();

                velocity.x = move.x * speed;
                velocity.z = move.z * speed;

                if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
                    velocity.y = jumpForce;
            }
            else
            {
                velocity.x = 0f;
                velocity.z = 0f;
            }

            // 重力は常に1箇所のみで処理
            if (controller.isGrounded && velocity.y < 0f)
                velocity.y = -2f;
            else
                velocity.y -= gravityStrength * Time.deltaTime;
        }
        else
        {
            // 無重力：スラスター移動
            bool isThrusting = false;
            Vector3 thrust = Vector3.zero;

            if (!isDead)
            {
                if (Input.GetKey(KeyCode.W)) { thrust += cameraTransform.forward; isThrusting = true; }
                if (Input.GetKey(KeyCode.S)) { thrust -= cameraTransform.forward; isThrusting = true; }
                if (Input.GetKey(KeyCode.A)) { thrust -= cameraTransform.right; isThrusting = true; }
                if (Input.GetKey(KeyCode.D)) { thrust += cameraTransform.right; isThrusting = true; }
                if (Input.GetKey(KeyCode.Space)) { thrust += Vector3.up; isThrusting = true; }
                if (Input.GetKey(KeyCode.LeftControl)) { thrust += Vector3.down; isThrusting = true; }
            }

            if (!isDead && isThrusting && thrust != Vector3.zero && (fuelSystem == null || fuelSystem.HasFuel()))
            {
                velocity += thrust.normalized * thrustForce * Time.deltaTime;
                if (velocity.magnitude > maxSpeed)
                    velocity = velocity.normalized * maxSpeed;
                if (fuelSystem != null)
                    fuelSystem.UseFuel(Time.deltaTime);
            }

            if (!isDead && !isThrusting)
            {
                velocity *= Mathf.Pow(inertiaDamping, Time.deltaTime * 60f);
                if (velocity.magnitude < stopThreshold)
                    velocity = Vector3.zero;
            }
        }

        CollisionFlags flags = controller.Move(velocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0)
            velocity.y = -velocity.y * bounceFactor;

        if (!hasGravity && (flags & CollisionFlags.Below) != 0 && velocity.y < 0)
            velocity.y = -velocity.y * bounceFactor;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y is > -0.1f and < 0.1f)
        {
            Vector3 reflected = Vector3.Reflect(velocity, hit.normal) * bounceFactor;
            velocity.x = reflected.x;
            velocity.z = reflected.z;
        }
    }

    public void ResetVelocity(bool keepPosition = false)
    {
        velocity = Vector3.zero;

        if (!keepPosition)
        {
            controller.enabled = false;
            transform.position = new Vector3(0f, 0f, 0f);
            controller.enabled = true;
        }
    }
}