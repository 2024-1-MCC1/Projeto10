using System.Collections.Generic;
using UnityEngine;


// Essa classe gerencia qual comportamento do jogador está ativo ou sobrescrito e chama suas funções locais.
// Contém configuração básica e funções comuns usadas por todos os comportamentos do jogador.
public class BasicBehaviour : MonoBehaviour
{
    public Transform playerCamera;                        // referência para a câmera que seguirá o jogador
    public float turnSmoothing = 0.06f;                   // suavização de rotação da câmera
    public float sprintFOV = 100f;                        // o FOV para quando o jogador correr
    public string sprintButton = "Correr";                // nome do input para correr

    private float h;                                      // Eixo Horizontal
    private float v;                                      // Eixo Vertical
    private int currentBehaviour;                         // Referência para o comportamento atual do jogador
    private int defaultBehaviour;                         // Comportamento padrão do jogador (quando nenhum outro está ativo)
    private int behaviourLocked;                          // Referência temporária para o comportamento que não pode ser sobreposto
    private Vector3 lastDirection;                        // Última direção que o jogador estava mirando
    private Animator anim;                                // Referência para o componente do animator
    private ThirdPersonOrbitCamBasic camScript;           // Referência para a câmera de terceira pessoa
    private bool sprint;                                  // Bool para determinar se o jogador está correndo ou não
    private bool changedFOV;                              // Criado para armazenar a bool de se o FOV está ativo após o jogador ter corrido
    private int hFloat;                                   // Variável do animator relacionada ao eixo horizontal
    private int vFloat;                                   // Variável do animator relacionada ao eixo vertical
    private List<GenericBehaviour> behaviours;            // A lista de todos os comportamentos
    private List<GenericBehaviour> overridingBehaviours;  // Lista dos atuais comportamentos sobrepostos
    private Rigidbody rBody;                              // Referência para o rigidbody do jogador
    private int groundedBool;                             // Variável do animator de se o jogador está no chão ou não (contato com algum objeto tangível)
    private Vector3 colExtents;                           // Teste de colisão com o chão

    // Define os eixos horizontais e verticais atuais
    public float GetH => h;
    public float GetV => v;

    // Obtém o script da câmera
    public ThirdPersonOrbitCamBasic GetCamScript => camScript;

    // Obtém o rigidbody
    public Rigidbody GetRigidBody => rBody;

    // Obtém o controlador de animações
    public Animator GetAnim => anim;

    // Obtém o comportamento padrão atual
    public int GetDefaultBehaviour => defaultBehaviour;

    void Awake()
    {
        // Define as funções e referências mencionadas acima
        behaviours = new List<GenericBehaviour>();
        overridingBehaviours = new List<GenericBehaviour>();
        anim = GetComponent<Animator>();
        hFloat = Animator.StringToHash("H");
        vFloat = Animator.StringToHash("V");
        camScript = playerCamera.GetComponent<ThirdPersonOrbitCamBasic>();
        rBody = GetComponent<Rigidbody>();

        // Verifica a colisão com o chão
        groundedBool = Animator.StringToHash("Grounded");
        colExtents = GetComponent<Collider>().bounds.extents;
    }

    void Update()
    {
        // Armazena os eixos a partir da entrada do usuário
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        // Envia a entrada para o controlador de animações
        anim.SetFloat(hFloat, h, 0.1f, Time.deltaTime);
        anim.SetFloat(vFloat, v, 0.1f, Time.deltaTime);

        // Ativa a corrida pela entrada
        sprint = Input.GetButton(sprintButton);

        // Ao correr, ativar o FOV alterado e mudar o vetor X da câmera
        if (IsSprinting())
        {
            changedFOV = true;
            camScript.SetFOV(sprintFOV);
        }
        else if (changedFOV)
        {
            camScript.ResetFOV();
            changedFOV = false;
        }
        // Envia a informação de colisão para o controlador de animações definindo o teste de estar no chão no Animator Controller
        anim.SetBool(groundedBool, IsGrounded());
    }

    // Chama as funções FixedUpdate dos comportamentos ativos ou sobrepostos
    void FixedUpdate()
    {
        // Chama o comportamento ativo se nenhum outro estiver sobreposto.
        bool isAnyBehaviourActive = false;
        if (behaviourLocked > 0 || overridingBehaviours.Count == 0)
        {
            foreach (GenericBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode())
                {
                    isAnyBehaviourActive = true;
                    behaviour.LocalFixedUpdate();
                }
            }
        }
        // Chama os comportamentos sobrepostos se houver algum.
        else
        {
            foreach (GenericBehaviour behaviour in overridingBehaviours)
            {
                behaviour.LocalFixedUpdate();
            }
        }

        // Garante que o jogador ficará em pé no chão se nenhum comportamento estiver ativo ou sobreposto.
        if (!isAnyBehaviourActive && overridingBehaviours.Count == 0)
        {
            rBody.useGravity = true;
            Repositioning();
        }
    }

    // Chama as funções LateUpdate dos comportamentos ativos ou sobrepostos
    private void LateUpdate()
    {
        // Chama o comportamento ativo se nenhum outro estiver sobreposto.
        if (behaviourLocked > 0 || overridingBehaviours.Count == 0)
        {
            foreach (GenericBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && currentBehaviour == behaviour.GetBehaviourCode())
                {
                    behaviour.LocalLateUpdate();
                }
            }
        }
        // Chama os comportamentos sobrepostos se houver algum.
        else
        {
            foreach (GenericBehaviour behaviour in overridingBehaviours)
            {
                behaviour.LocalLateUpdate();
            }
        }

    }

    // Adiciona um novo comportamento à lista de comportamentos monitorados.
    public void SubscribeBehaviour(GenericBehaviour behaviour)
    {
        behaviours.Add(behaviour);
    }

    // Define o comportamento padrão do jogador.
    public void RegisterDefaultBehaviour(int behaviourCode)
    {
        defaultBehaviour = behaviourCode;
        currentBehaviour = behaviourCode;
    }

    // Tenta definir um comportamento personalizado como o ativo.
    // Sempre muda do comportamento padrão para o passado.
    public void RegisterBehaviour(int behaviourCode)
    {
        if (currentBehaviour == defaultBehaviour)
        {
            currentBehaviour = behaviourCode;


        }
    }

    // Tenta desativar um comportamento do jogador e retornar ao padrão.
    public void UnregisterBehaviour(int behaviourCode)
    {
        if (currentBehaviour == behaviourCode)
        {
            currentBehaviour = defaultBehaviour;
        }
    }

    // Tenta sobrepor qualquer comportamento ativo com os comportamentos na fila.
    // Usado para mudar para um ou mais comportamentos que devem se sobrepor ao ativo (por exemplo, comportamento de mira).
    public bool OverrideWithBehaviour(GenericBehaviour behaviour)
    {
        // O comportamento não está na fila.
        if (!overridingBehaviours.Contains(behaviour))
        {
            // Nenhum comportamento está sendo atualmente sobreposto.
            if (overridingBehaviours.Count == 0)
            {
                // Chama a função OnOverride do comportamento ativo antes de sobrepor.
                foreach (GenericBehaviour overriddenBehaviour in behaviours)
                {
                    if (overriddenBehaviour.isActiveAndEnabled && currentBehaviour == overriddenBehaviour.GetBehaviourCode())
                    {
                        overriddenBehaviour.OnOverride();
                        break;
                    }
                }
            }
            // Adiciona o comportamento de sobreposição à fila.
            overridingBehaviours.Add(behaviour);
            return true;
        }
        return false;
    }

    // Tenta revogar o comportamento de sobreposição e retornar ao ativo.
    // Chamado ao sair do comportamento de sobreposição (por exemplo, parar de mirar).
    public bool RevokeOverridingBehaviour(GenericBehaviour behaviour)
    {
        if (overridingBehaviours.Contains(behaviour))
        {
            overridingBehaviours.Remove(behaviour);
            return true;
        }
        return false;
    }

    // Verifica se algum ou um comportamento específico está atualmente sobrepondo o ativo.
    public bool IsOverriding(GenericBehaviour behaviour = null)
    {
        if (behaviour == null)
            return overridingBehaviours.Count > 0;
        return overridingBehaviours.Contains(behaviour);
    }

    // Verifica se o comportamento ativo é o passado.
    public bool IsCurrentBehaviour(int behaviourCode)
    {
        return this.currentBehaviour == behaviourCode;
    }

    // Verifica se algum outro comportamento está temporariamente bloqueado.
    public bool GetTempLockStatus(int behaviourCodeIgnoreSelf = 0)
    {
        return (behaviourLocked != 0 && behaviourLocked != behaviourCodeIgnoreSelf);
    }

    // Tenta bloquear em um comportamento específico.
    // Nenhum outro comportamento pode sobrepor durante o bloqueio temporário.
    // Usado para transições temporárias como pular, entrar/sair do modo de mira, etc.
    public void LockTempBehaviour(int behaviourCode)
    {
        if (behaviourLocked == 0)
        {
            behaviourLocked = behaviourCode;
        }
    }

    // Tenta desbloquear o comportamento bloqueado atual.
    // Usado após o término de uma transição temporária.
    public void UnlockTempBehaviour(int behaviourCode)
    {
        if (behaviourLocked == behaviourCode)
        {
            behaviourLocked = 0;
        }
    }

    // Funções comuns a qualquer comportamento:

    // Verifica se o jogador está correndo.
    public virtual bool IsSprinting()
    {
        return sprint && IsMoving() && CanSprint();
    }

    // Verifica se o jogador pode correr (todos os comportamentos devem permitir).
    public bool CanSprint()
    {
        foreach (GenericBehaviour behaviour in behaviours)
        {
            if (!behaviour.AllowSprint())
                return false;
        }
        foreach (GenericBehaviour behaviour in overridingBehaviours)
        {
            if (!behaviour.AllowSprint())
                return false;
        }
        return true;
    }

    // Verifica se o jogador está se movendo no plano horizontal.
    public bool IsHorizontalMoving()
    {
        return h != 0;
    }

    // Verifica se o jogador está se movendo.
    public bool IsMoving()
    {
        return (h != 0) || (v != 0);
    }

    // Obtém a última direção do jogador.
    public Vector3 GetLastDirection()
    {
        return lastDirection;
    }

    // Define a última direção do jogador.
    public void SetLastDirection(Vector3 direction)
    {
        lastDirection = direction;
    }

    // Coloca o jogador em uma posição em pé com base na última direção enfrentada.
    public void Repositioning()
    {
        if (lastDirection != Vector3.zero)
        {
            lastDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(lastDirection);
            Quaternion newRotation = Quaternion.Slerp(rBody.rotation, targetRotation, turnSmoothing);
            rBody.MoveRotation(newRotation);
        }
    }

    // Função para verificar se o jogador está no chão.
    public bool IsGrounded()
    {
        Ray ray = new Ray(this.transform.position + Vector3.up * (2 * colExtents.x), Vector3.down);
        return Physics.SphereCast(ray, colExtents.x, colExtents.x + 0.2f);
    }
}

// Esta é a classe base para todos os comportamentos do jogador, qualquer comportamento personalizado deve herdar desta.
// Contém referências a componentes locais que podem diferir de acordo com o próprio comportamento.
public abstract class GenericBehaviour : MonoBehaviour
{
    //protected Animator anim;                       // Referência ao componente Animator.
    protected int speedFloat;                      // Parâmetro de velocidade no Animator.
    protected BasicBehaviour behaviourManager;     // Referência ao gerenciador básico de comportamentos.
    protected int behaviourCode;                   // O código que identifica um comportamento.
    protected bool canSprint;                      // Booleano para armazenar se o comportamento permite o jogador correr.

    void Awake()
    {
        // Configura as referências.
        behaviourManager = GetComponent<BasicBehaviour>();
        speedFloat = Animator.StringToHash("Speed");
        canSprint = true;

        // Define o código de comportamento com base na classe herdada.
        behaviourCode = this.GetType().GetHashCode();
    }

    // Funções protegidas e virtuais podem ser substituídas por classes herdadas.
    // O comportamento ativo controlará as ações do jogador com essas funções:

    // A versão local da função FixedUpdate do MonoBehaviour.
    public virtual void LocalFixedUpdate() { }
    // A versão local da função LateUpdate do MonoBehaviour.
    public virtual void LocalLateUpdate() { }
    // Esta função é chamada quando outro comportamento substitui o atual.
    public virtual void OnOverride() { }

    // Obtém o código de comportamento.
    public int GetBehaviourCode()
    {
        return behaviourCode;
    }

    // Verifica se o comportamento permite correr.
    public bool AllowSprint()
    {
        return canSprint;
    }
}
