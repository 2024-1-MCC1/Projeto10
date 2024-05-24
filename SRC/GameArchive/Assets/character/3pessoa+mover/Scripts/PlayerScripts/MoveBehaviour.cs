using UnityEngine;
using UnityEngine.Serialization;

// A classe MoveBehaviour herda de GenericBehaviour. Esta classe é responsável por controlar o movimento de caminhada e corrida.
public class MoveBehaviour : GenericBehaviour
{
    public float walkSpeed = 0.15f;                 // Define a velocidade da caminhada
    public float runSpeed = 1.0f;                   // Define a velocidade da corrida
    public float sprintSpeed = 2.0f;                // Define a velocidade do sprint
    public float speedDampTime = 0.1f;              // O tempo de "refresh" para mudar a animação dependendo da velocidade atual
    public string jumpButton = "Jump";              // Botão de pulo
    public float jumpHeight = 1.2f;                 // Altura do pulo
    public float jumpInertialForce = 10f;           // Força inercial horizontal padrão ao pular.

    private float speed, speedSeeker;               // Velocidade de movimento.
    private int jumpBool;                           // Variável do Animator relacionada ao pulo.
    private int groundedBool;                       // Variável do Animator relacionada a se o jogador está no chão ou não.
    private bool jump;                              // Booleano para determinar se o jogador iniciou um pulo.
    private bool isColliding;                       // Booleano para determinar se o jogador colidiu com um obstáculo.

    // Start é sempre chamado após quaisquer funções Awake.
    void Start()
    {
        // Configura as referências.
        jumpBool = Animator.StringToHash("Jump");
        groundedBool = Animator.StringToHash("Grounded");
        behaviourManager.GetAnim.SetBool(groundedBool, true);

        // Inscreve e registra este comportamento como o comportamento padrão.
        behaviourManager.SubscribeBehaviour(this);
        behaviourManager.RegisterDefaultBehaviour(this.behaviourCode);
        speedSeeker = runSpeed;
    }

    // Update é usado para definir recursos independentemente do comportamento ativo.
    void Update()
    {
        // Obter entrada de pulo.
        if (!jump && Input.GetButtonDown(jumpButton) && behaviourManager.IsCurrentBehaviour(this.behaviourCode) && !behaviourManager.IsOverriding())
        {
            jump = true;
        }
    }

    // LocalFixedUpdate substitui a função virtual da classe base.
    public override void LocalFixedUpdate()
    {
        // Chama o gerenciador de movimento básico.
        MovementManagement(behaviourManager.GetH, behaviourManager.GetV);

        // Chama o gerenciador de pulo.
        JumpManagement();
    }

    // Executa os movimentos de pulo ocioso e caminhada/correr.
    void JumpManagement()
    {
        // Inicia um novo pulo.
        if (jump && !behaviourManager.GetAnim.GetBool(jumpBool) && behaviourManager.IsGrounded())
        {
            // Configura os parâmetros relacionados ao pulo.
            behaviourManager.LockTempBehaviour(this.behaviourCode);
            behaviourManager.GetAnim.SetBool(jumpBool, true);
            // É um pulo de locomoção?
            if (behaviourManager.GetAnim.GetFloat(speedFloat) > 0.1)
            {
                // Altera temporariamente a fricção do jogador para passar por obstáculos.
                GetComponent<CapsuleCollider>().material.dynamicFriction = 0f;
                GetComponent<CapsuleCollider>().material.staticFriction = 0f;
                // Remove a velocidade vertical para evitar "super pulos" no final de rampas.
                RemoveVerticalVelocity();
                // Configura a velocidade vertical do impulso do pulo.
                float velocity = 2f * Mathf.Abs(Physics.gravity.y) * jumpHeight;
                velocity = Mathf.Sqrt(velocity);
                behaviourManager.GetRigidBody.AddForce(Vector3.up * velocity, ForceMode.VelocityChange);
            }
        }
        // Já está pulando?
        else if (behaviourManager.GetAnim.GetBool(jumpBool))
        {
            // Mantém o movimento para frente enquanto estiver no ar.
            if (!behaviourManager.IsGrounded() && !isColliding && behaviourManager.GetTempLockStatus())
            {
                behaviourManager.GetRigidBody.AddForce(transform.forward * (jumpInertialForce * Physics.gravity.magnitude * sprintSpeed), ForceMode.Acceleration);
            }
            // Pousou?
            if ((behaviourManager.GetRigidBody.velocity.y < 0) && behaviourManager.IsGrounded())
            {
                behaviourManager.GetAnim.SetBool(groundedBool, true);
                // Restaura a fricção do jogador para o padrão.
                GetComponent<CapsuleCollider>().material.dynamicFriction = 0.6f;
                GetComponent<CapsuleCollider>().material.staticFriction = 0.6f;
                // Configura os parâmetros relacionados ao pulo.
                jump = false;
                behaviourManager.GetAnim.SetBool(jumpBool, false);
                behaviourManager.UnlockTempBehaviour(this.behaviourCode);
            }
        }
    }

    // Lidar com o movimento básico do jogador
    void MovementManagement(float horizontal, float vertical)
    {
        // No chão, obedeça à gravidade.
        if (behaviourManager.IsGrounded())
            behaviourManager.GetRigidBody.useGravity = true;

        // Evita decolar quando alcançou o fim de uma rampa.
        else if (!behaviourManager.GetAnim.GetBool(jumpBool) && behaviourManager.GetRigidBody.velocity.y > 0)
        {
            RemoveVerticalVelocity();
        }

        // Chama a função que lida com a orientação do jogador.
        Rotating(horizontal, vertical);

        // Define a velocidade apropriada.
        Vector2 dir = new Vector2(horizontal, vertical);
        speed = Vector2.ClampMagnitude(dir, 1f).magnitude;
        // Isso é apenas para PC, os gamepads controlam a velocidade via stick analógico.
        speedSeeker += Input.GetAxis("Mouse ScrollWheel");
        speedSeeker = Mathf.Clamp(speedSeeker, walkSpeed, runSpeed);
        speed *= speedSeeker;
        if (behaviourManager.IsSprinting())
        {
            speed = sprintSpeed;
        }

        behaviourManager.GetAnim.SetFloat(speedFloat, speed, speedDampTime, Time.deltaTime);
    }

    // Remove a velocidade vertical do rigidbody.
    private void RemoveVerticalVelocity()
    {
        Vector3 horizontalVelocity = behaviourManager.GetRigidBody.velocity;
        horizontalVelocity.y = 0;
        behaviourManager.GetRigidBody.velocity = horizontalVelocity;
    }

    // Rotaciona o jogador para corresponder à orientação correta, de acordo com a câmera e a tecla pressionada.
    Vector3 Rotating(float horizontal, float vertical)
    {
        // Obter a direção para frente da câmera, sem componente vertical.
        Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);

        // O jogador está se movendo no chão, o componente Y do rosto da câmera não é relevante.
        forward.y = 0.0f

;
        forward = forward.normalized;

        // Calcular a direção alvo com base na frente da câmera e na tecla de direção.
        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        Vector3 targetDirection = forward * vertical + right * horizontal;

        // Interpola a direção atual para a direção alvo calculada.
        if ((behaviourManager.IsMoving() && targetDirection != Vector3.zero))
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            Quaternion newRotation = Quaternion.Slerp(behaviourManager.GetRigidBody.rotation, targetRotation, behaviourManager.turnSmoothing);
            behaviourManager.GetRigidBody.MoveRotation(newRotation);
            behaviourManager.SetLastDirection(targetDirection);
        }
        // Se estiver ocioso, ignore a orientação atual da câmera e considere a última direção de movimento.
        if (!(Mathf.Abs(horizontal) > 0.9 || Mathf.Abs(vertical) > 0.9))
        {
            behaviourManager.Repositioning();
        }

        return targetDirection;
    }

    // Detecção de colisão.
    private void OnCollisionStay(Collision collision)
    {
        isColliding = true;
        // Deslize em obstáculos verticais
        if (behaviourManager.IsCurrentBehaviour(this.GetBehaviourCode()) && collision.GetContact(0).normal.y <= 0.1f)
        {
            GetComponent<CapsuleCollider>().material.dynamicFriction = 0f;
            GetComponent<CapsuleCollider>().material.staticFriction = 0f;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        isColliding = false;
        GetComponent<CapsuleCollider>().material.dynamicFriction = 0.6f;
        GetComponent<CapsuleCollider>().material.staticFriction = 0.6f;
    }
}
