using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour {

    [Header("Player Stats")]
    //Player Stats
    public float health = 100;
    public float stamina = 100;
    
    [Header("Movement Controls")]
    public float walkSpeed = 4;
    public float runSpeed = 10;
    float speed = 4;
    public float jumpHeight = 4;

    Vector3 targetWalkAmount;
    Vector3 walkAmount;
    Vector3 smoothDampMoveRef;
    CharacterController controller;
    
    float gravity = -12;
    float velocityY;
	
    //Crouch
    public LayerMask ground;
    bool isGrounded;
    int crouchToggle = 0;
    float height = .75f;
    float crouchLerpPercent = 0;
    
    [Header("Look Controls")]
	[Range (-10, 10)]
	public float mouseSensitivityX = 6f;
	[Range (-10, 10)]
	public float mouseSensitivityY = 6f;
	float verticalLookRotation;

    //movement modes
    [HideInInspector]
    public enum Mode{walking, flying};
    public Mode mode;

	void Start () {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
	}
	
    void Update() {

    //set movespeed
        if(Input.GetKey(KeyCode.LeftShift)){
        	speed = runSpeed;
		} else {
			speed = walkSpeed;
		}
        
        if (stamina <= 0) {
            speed = walkSpeed;
        } 

	//jump
		if(Input.GetKeyDown(KeyCode.Space)){
	        Jump();
		}
    }

	void LateUpdate () {
        if(mode == Mode.walking){
	    //crouch
		    if(Input.GetKeyDown(KeyCode.C)){
			    crouchToggle = 1 - crouchToggle;
			    StopCoroutine("CrouchToggle");
			    StartCoroutine("CrouchToggle");
		    }

	    //look rotations
            transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * mouseSensitivityX);
	        verticalLookRotation += Input.GetAxis("Mouse Y") * mouseSensitivityY;
		    verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
		    Camera.main.transform.localEulerAngles = Vector3.left * verticalLookRotation;

            Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            Vector3 newRight = Vector3.Cross(Vector3.up, gameObject.transform.forward);
            Vector3 newForward = Vector3.Cross(newRight, Vector3.up);

        //movement
            Vector3 trueMoveDir = (newRight * Input.GetAxisRaw("Horizontal") + newForward * Input.GetAxisRaw("Vertical"));
            targetWalkAmount = trueMoveDir * speed + Vector3.up * velocityY;
		    walkAmount = Vector3.SmoothDamp(walkAmount, targetWalkAmount, ref smoothDampMoveRef, 0.1f);
            controller.Move(walkAmount * Time.fixedDeltaTime);
        
            velocityY += Time.deltaTime * gravity;
        }
	}

    void Jump() {
		if (controller.isGrounded) {
			float jumpVelocity = Mathf.Sqrt (-2 * gravity * jumpHeight);
			velocityY = jumpVelocity;
		}
	}

	IEnumerator CrouchToggle(){
		float time = .25f;
		float speed = 1 / time;

		//crouch
		if(crouchToggle == 1){
			height = .25f;
			while(crouchLerpPercent < 1){
				crouchLerpPercent += Time.fixedDeltaTime * speed;
				gameObject.transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, .5f, 1), crouchLerpPercent);
				yield return null;
			}
			crouchLerpPercent = 1;

		//stand up
		} if(crouchToggle == 0){
			
			StartCoroutine("LiftWhenUncrouching");
			height = .75f;
			while(crouchLerpPercent > 0){
				crouchLerpPercent -= Time.fixedDeltaTime * speed;
				gameObject.transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 1, 1), (1 - crouchLerpPercent));
				yield return null;
			}

			crouchLerpPercent = 0;

		}
	}

	IEnumerator LiftWhenUncrouching(){
		Ray ray = new Ray(transform.position, -transform.up);

		//move character up when uncrouching 
        if(Physics.SphereCast(ray, .5f, height, ground)) {
			float percent = 0;
			float t = .3f;
			float s = 1 / t;
			while(percent < 1){
				percent += Time.fixedDeltaTime * s;
				gameObject.transform.localPosition = Vector3.Lerp(transform.localPosition, transform.localPosition + Vector3.up * .02f, percent);
				yield return null;
			}
        }  
	}

	//push rigidbody objects upon colliding with them
	void OnControllerColliderHit(ControllerColliderHit hit) {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic)
            return;
        
        if (hit.moveDirection.y < -0.3F)
            return;
        
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.velocity = pushDir * 5;
    }

    
}