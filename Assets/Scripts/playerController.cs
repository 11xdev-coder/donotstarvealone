using UnityEngine;
using UnityEngine.Assertions.Must;
using Vector2 = UnityEngine.Vector2;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public Animator animator;
    public Camera camera;

    private Rigidbody2D _rb;
    public Vector2 movement;
    public Vector2 targetPos;
    public Vector2 direction;
    private bool _moveToMouse;
    
    public float horizontal;
    public float vertical;
    public bool diagonal;
    public bool playsidewaysAnim;

    public InventoryManager inventory;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void PlayAnimation()
    {
        if (horizontal == 0 & vertical > 0)
            if(!diagonal) animator.Play("walkbackward");
        
        if (horizontal == 0 & vertical < 0)
            if(!diagonal) animator.Play("walkfront");
        
        if (vertical == 0 & horizontal > 0 || vertical != 0 & horizontal > 0) animator.Play("walkright");
        if (vertical == 0 & horizontal < 0 || vertical != 0 & horizontal < 0) animator.Play("walkleft");

        if (vertical == 0 & horizontal == 0)
        {
            if (playsidewaysAnim) animator.Play("idlesideways");
            else animator.Play("idle");
        }
    }

    private void Update()
    {
        // Get input from the W, A, S, and D keys
        horizontal = 0f;
        vertical = 0f;
        // if w pressed
        if (Input.GetKey(KeyCode.W))
        {
            playsidewaysAnim = false;
            vertical = 1f;
            PlayAnimation();
        }
        // if a pressed
        if (Input.GetKey(KeyCode.A))
        {
            playsidewaysAnim = true;
            diagonal = true;
            horizontal = -1f;
            PlayAnimation();
        }
        // if s pressed
        if (Input.GetKey(KeyCode.S))
        {
            playsidewaysAnim = false;
            vertical = -1f;
            PlayAnimation();
        }
        // if d pressed
        if (Input.GetKey(KeyCode.D))
        {
            playsidewaysAnim = true;
            diagonal = true;
            horizontal = 1f;
            PlayAnimation();
        }
        // if left clicked pressed
        if (Input.GetMouseButton(0))
        {
            //_moveToMouse = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            _moveToMouse = false;
        }
        else
        {
            // idle check
            if (!Input.GetKey(KeyCode.A) | !Input.GetKey(KeyCode.W) | !Input.GetKey(KeyCode.S) | !Input.GetKey(KeyCode.D))
            {
                PlayAnimation();
            }
            // is s or d pressed
            if (Input.GetKeyUp(KeyCode.S) | Input.GetKeyUp(KeyCode.D))
            {
                diagonal = false;
            }
        }
        movement = new Vector2(horizontal, vertical);
        
        if (_moveToMouse)
        {
            //MoveToMouse();
        }
        else
        {
            _rb.MovePosition(_rb.position + movement * moveSpeed * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // use selected item
            // TO-DO: MAKE TOOL-EQUIP SLOT
            if(inventory.equippedTool != null)
                inventory.equippedTool.GetItem().Use(this);
        }
    }

    public void MoveToMouse()
    {
        targetPos = camera.ScreenToWorldPoint(Input.mousePosition);
        direction = (targetPos - (Vector2)transform.position).normalized;
        Debug.DrawLine(targetPos, (Vector2)transform.position, Color.gray, 2f);
        _rb.MovePosition((Vector2)transform.position + direction * moveSpeed * Time.deltaTime);
    }
}