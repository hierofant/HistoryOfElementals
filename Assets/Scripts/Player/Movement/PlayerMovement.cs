using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using ANU.IngameDebug.Console;

namespace Player
{
    [DebugCommandPrefix("Player")]
    public class PlayerController2D : MonoBehaviour
    {
        [DebugCommand] //добавление в консоль с возможностью редактирования
        [SerializeField, Range(0, 10)] private float movementSpeed = 5f;

        [DebugCommand] //добавление в консоль с возможностью редактирования
        [SerializeField, Range(0, 20)] private float acceleration = 10f;

        [DebugCommand] //добавление в консоль с возможностью редактирования
        [SerializeField, Range(0, 15)] private float deceleration = 10f;

        [DebugCommand] //добавление в консоль с возможностью редактирования
        [SerializeField, Range(0, 10)] private float jumpForce = 5f;

        [DebugCommand] //добавление в консоль с возможностью редактирования
        [SerializeField, Range(0, 10)] private float SlideSpeed = 2f;

        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Animator animator;

        private InputManager inputManager;
        private Rigidbody2D rb;
        private Vector2 movementInput;
        private bool isGrounded;

        private void Awake()
        {
            inputManager = new InputManager();
            inputManager.Player.Enable();
        }

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        void FixedUpdate()
        {

            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);


            movementInput = inputManager.Player.Movement.ReadValue<Vector2>();


            float targetSpeed = movementInput.x * movementSpeed;


            float newSpeedX = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, (movementInput.x != 0 ? acceleration : deceleration) * Time.fixedDeltaTime);


            rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);
        }

        void Update()
        {

            if (inputManager.Player.Jump.triggered && isGrounded)
            {
                Jump();
            }


            Physics2D.queriesStartInColliders = false;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, movementInput, 0.6f);
            if (!isGrounded && hit.collider != null)
            {

                rb.linearVelocity = new Vector2(0, -SlideSpeed);


                animator.SetFloat("Velocity", 0f);


                VibrateController(0.1f, 0.2f);
            }
            else
            {

                animator.SetFloat("Velocity", Mathf.Abs(rb.linearVelocity.x));
            }


            if (rb.linearVelocity.x > 0.1f)
            {

                gameObject.transform.localScale = new Vector3(1, 1, 1);
            }
            else if (rb.linearVelocity.x < -0.1f)
            {

                gameObject.transform.localScale = new Vector3(-1, 1, 1);
            }
        }
        private void Jump()
        {

            rb.AddForce(new Vector2(0f, jumpForce * rb.mass), ForceMode2D.Impulse);


            VibrateController(0.2f, 0.1f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {

            if (collision.relativeVelocity.magnitude > 10f)
            {

                VibrateController(0.7f, 0.2f);
            }
            else if (collision.relativeVelocity.magnitude > 7f)
            {

                VibrateController(0.3f, 0.2f);
            }
        }

        private void VibrateController(float intensity, float duration)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(intensity, intensity);
                StartCoroutine(StopVibrationAfterDelay(duration));
            }
        }

        private IEnumerator StopVibrationAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
        }

        private void OnDestroy()
        {
            inputManager.Player.Disable();
        }

        public void DisableInput()
        {
            inputManager.Player.Disable();
        }

        public void EnableInput()
        {
            inputManager.Player.Enable();
        }
    }

}
