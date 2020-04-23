using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Prime31;

public class PlayerController : MonoBehaviour
{

    public CharacterController2D.CharacterCollisionState2D flags;
    public float walkSpeed = 4.0f;     // Depois de incluido, alterar no Unity Editor
    public float jumpSpeed = 8.0f;     // Depois de incluido, alterar no Unity Editor
    public float doubleJumpSpeed = 6.0f; //Depois de incluido, alterar no Editor
    public float gravity = 9.8f;       // Depois de incluido, alterar no Unity Editor
    public int coins = 0;

    public bool doubleJumped; // informa se foi feito um pulo duplo
    public bool isDucking;
    public bool isFalling;      // Se estiver caindo
    public bool isGrounded;     // Se está no chão
    public bool isJumping;      // Se está pulando
    public bool isFacingRight;      // Se está olhando para a direita
    public bool IsInputEnabled = true;
    public bool isPushed;
    public AudioClip coin;
    public AudioClip victory;

    private Vector3 moveDirection = Vector3.zero; // direção que o personagem se move
    private CharacterController2D characterController;  //Componente do Char. Controller

    private BoxCollider2D boxCollider;
    private float colliderSizeY;
    private float colliderOffsetY;
    private Animator animator;
    public LayerMask mask;  // para filtrar os layers a serem analisados
    public AudioSource footstep;
    public AudioSource _AudioSource1;
    public AudioSource _AudioSource2;


    [SerializeField] private Text coinsText;


    private void OnTriggerEnter2D(Collider2D other)
    {
        //if (other.gameObject.layer == LayerMask.NameToLayer("Coins"))
        if (other.gameObject.tag == "Coin")

        {
            AudioSource.PlayClipAtPoint(coin, this.gameObject.transform.position);
            coins += 1;
            coinsText.text = coins.ToString() + "/9";
            Destroy(other.gameObject);

            if (coins == 9)
            {
                IsInputEnabled = false; //disable all inputs
                animator.SetBool("IsInputEnabled", IsInputEnabled);
                isDucking = false;
                isFalling =false;     
                isGrounded =false;    
                isJumping = false;      
                isFacingRight = false;
                _AudioSource1.Stop();
                StartCoroutine(WaitAnimation());
                
            }
        }
        if (other.gameObject.tag == "Fall")
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
        }
        
        if (other.gameObject.tag == "Dungeon")
        {
            if (!_AudioSource2.isPlaying)
            {
            _AudioSource1.Stop();
            _AudioSource2.Play();

            }
        }
        if (other.gameObject.tag == "Grass")
        {
            if (!_AudioSource1.isPlaying)
            {

            _AudioSource2.Stop();

            _AudioSource1.Play();
            }
        }




    }

    void Start()
    {
        characterController = GetComponent<CharacterController2D>(); //identif. o componente
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        colliderSizeY = boxCollider.size.y;
        colliderOffsetY = boxCollider.offset.y;
        footstep = GetComponent<AudioSource>();


    }

    void Update()
    {
        if (IsInputEnabled)
        {

        // Atualizando Animator com estados do jogo
        animator.SetFloat("movementX", Mathf.Abs(moveDirection.x / walkSpeed)); // +Normalizado
        animator.SetFloat("movementY", moveDirection.y);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isJumping", isJumping);
        animator.SetBool("isDucking", isDucking);
        animator.SetBool("isFalling", isFalling);
        moveDirection.x = Input.GetAxis("Horizontal"); // recupera valor dos controles
        moveDirection.x *= walkSpeed;

        if (moveDirection.y < 0)
            isFalling = true;
        else
            isFalling = false;
        // Conforme direção do personagem girar ele no eixo Y
        if (moveDirection.x < 0)
        {
            transform.eulerAngles = new Vector3(0, 180, 0);
            isFacingRight = false;
        }
        else if (moveDirection.x > 0)
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            isFacingRight = true;
        } // se direção em x == 0 mantenha como está a rotação
        if (isGrounded)
        {            // caso esteja no chão
            moveDirection.y = 0.0f;    // se no chão nem subir nem descer
            doubleJumped = false; // se voltou ao chão pode faz pulo duplo
            isJumping = false;

            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpSpeed;
                isJumping = true;
            }
        }
        else
        {            // caso esteja pulando 
            if (Input.GetButtonUp("Jump") && moveDirection.y > 0) // Soltando botão diminui pulo
                moveDirection.y *= 0.5f;
        if (Input.GetButtonDown("Jump") && !doubleJumped) // Segundo clique faz pulo duplo
        {
            moveDirection.y = doubleJumpSpeed;
            doubleJumped = true;
        }
        }
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -Vector2.up, 4f, mask);
        if (hit.collider != null && isGrounded)
        {
            transform.SetParent(hit.transform);
            if (Input.GetAxis("Vertical") < 0 && Input.GetButtonDown("Jump"))
            {
                moveDirection.y = -jumpSpeed;
                StartCoroutine(PassPlatform(hit.transform.gameObject));
            }
        }
        else
        {
            transform.SetParent(null);
        }




        moveDirection.y -= gravity * Time.deltaTime;    // aplica a gravidade
        characterController.move(moveDirection * Time.deltaTime);   // move personagem	

        flags = characterController.collisionState;     // recupera flags
        isGrounded = flags.below;				// define flag de chão
        if (Input.GetAxis("Vertical") < 0 && moveDirection.x == 0)
        {
            if (!isDucking)
            {
                boxCollider.size = new Vector2(boxCollider.size.x, 2 * colliderSizeY / 3);
                boxCollider.offset = new Vector2(boxCollider.offset.x, colliderOffsetY - colliderSizeY / 6);
                characterController.recalculateDistanceBetweenRays();
            }
            isDucking = true;
        }
        else
        {
            if (isDucking)
            {
                boxCollider.size = new Vector2(boxCollider.size.x, colliderSizeY);
                boxCollider.offset = new Vector2(boxCollider.offset.x, colliderOffsetY);
                characterController.recalculateDistanceBetweenRays();
                isDucking = false;
            }
        }
        }
        IEnumerator PassPlatform(GameObject platform)
        {
            platform.GetComponent<EdgeCollider2D>().enabled = false;
            yield return new WaitForSeconds(1.0f);
            platform.GetComponent<EdgeCollider2D>().enabled = true;
        }




    }

    private void Footstep()
    {
        footstep.Play();
    }
    IEnumerator WaitAnimation()
    {
        //Print the time of when the function is first called.
        AudioSource.PlayClipAtPoint(victory, this.gameObject.transform.position);
        Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(5);

        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

}
