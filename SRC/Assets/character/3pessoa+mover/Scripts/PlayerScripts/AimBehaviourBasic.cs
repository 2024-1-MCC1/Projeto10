using UnityEngine;
using System.Collections;

// essa classe contém o codigo para a mira do personagem e seu movimento
public class AimBehaviourBasic : GenericBehaviour
{
	public string aimButton = "Aim", shoulderButton = "Aim Shoulder";     // define os botoes para mirar
	public Texture2D crosshair;                                           // a textura do crosshair
	public float aimTurnSmoothing = 0.15f;                                // velocidade da rotação da camera ao virar
	public Vector3 aimPivotOffset = new Vector3(0.5f, 1.2f,  0f);         // offset pra aonde a camera vai apontar ao mirar
	public Vector3 aimCamOffset   = new Vector3(0f, 0.4f, -0.7f);         // nova posição da camera para quando se mira

	private int aimBool;                                                  // variavel do animator da mira
	private bool aim;                                                     // Bool pra determinar quando o jogador esta mirando ou nao

	void Start ()
	{
		// referencia do animador para o aimbool
		aimBool = Animator.StringToHash("Aim");
	}


	void Update ()
	{
		// ativa e desativa a mira pelo input.
		if (Input.GetAxisRaw(aimButton) != 0 && !aim)
		{
			StartCoroutine(ToggleAimOn());
		}
		else if (aim && Input.GetAxisRaw(aimButton) == 0)
		{
			StartCoroutine(ToggleAimOff());
		}

		// codigo para definir que ao mirar, não se pode correr
		canSprint = !aim;

		// Toggle camera aim position left or right, switching shoulders.
		if (aim && Input.GetButtonDown (shoulderButton))
		{
			aimCamOffset.x = aimCamOffset.x * (-1);
			aimPivotOffset.x = aimPivotOffset.x * (-1);
		}

		// coloca a boolean da mira no controlador de animação
		behaviourManager.GetAnim.SetBool (aimBool, aim);
	}

	// Codigo para ativar a mira com um pequeno delay.
	private IEnumerator ToggleAimOn()
	{
		yield return new WaitForSeconds(0.05f);
		// se mirar não for possivel, não mirar
		if (behaviourManager.GetTempLockStatus(this.behaviourCode) || behaviourManager.IsOverriding(this))
			yield return false;

		// codigo para começar a mirar
		else
		{
			aim = true;
			int signal = 1;
			aimCamOffset.x = Mathf.Abs(aimCamOffset.x) * signal;
			aimPivotOffset.x = Mathf.Abs(aimPivotOffset.x) * signal;
			yield return new WaitForSeconds(0.1f);
			behaviourManager.GetAnim.SetFloat(speedFloat, 0);
			// sufixo para sobrepor a animação atual com a da mira
			behaviourManager.OverrideWithBehaviour(this);
		}
	}

	// codigo para parar de mirar com um pequeno delay
	private IEnumerator ToggleAimOff()
	{
		aim = false;
		yield return new WaitForSeconds(0.3f);
		behaviourManager.GetCamScript.ResetTargetOffsets();
		behaviourManager.GetCamScript.ResetMaxVerticalAngle();
		yield return new WaitForSeconds(0.05f);
		behaviourManager.RevokeOverridingBehaviour(this);
	}


	public override void LocalFixedUpdate()
	{
		// coloca a camera e a sua orientação para os parametros do modo de mira ( mirando ou não )
		if(aim)
			behaviourManager.GetCamScript.SetTargetOffsets (aimPivotOffset, aimCamOffset);
	}

	// o locallateupdate é chamado aqui pra definir a rotação do jogador após a rotação da camera, pra evitar "flickering"
	public override void LocalLateUpdate()
	{
		AimManagement();
	}

	// parametros da mira ao mirar
	void AimManagement()
	{
		// ajusta a orientação do player quando se mira
		Rotating();
	}

	// Codigo para a rotação do jogador ao mecher na mira
	void Rotating()
	{
		Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);
		// ja que o jogador sempre está no chão, o componente Y da camera não importa.
		forward.y = 0.0f;
		forward = forward.normalized;

		// sempre rotaciona o jogador de acordo com a rotação horizontal da camera no modo de mira.    
		Quaternion targetRotation =  Quaternion.Euler(0, behaviourManager.GetCamScript.GetH, 0);

		float minSpeed = Quaternion.Angle(transform.rotation, targetRotation) * aimTurnSmoothing;

		
		behaviourManager.SetLastDirection(forward);
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, minSpeed * Time.deltaTime);

	}

 	// chama a crosshair ao mirar
	void OnGUI () 
	{
		if (crosshair)
		{
			float mag = behaviourManager.GetCamScript.GetCurrentPivotMagnitude(aimPivotOffset);
			if (mag < 0.05f)
				GUI.DrawTexture(new Rect(Screen.width / 2.0f - (crosshair.width * 0.5f),
										 Screen.height / 2.0f - (crosshair.height * 0.5f),
										 crosshair.width, crosshair.height), crosshair);
		}
	}
}
