using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class FlightController : MonoBehaviour {
   
    public LayerMask environment;

    bool backpanning = false;

    int flightToggle = 0;

    PlayerController playerController;
    
    Vector3 targetFlyAmount;
    Vector3 flyAmount;
    Vector3 smoothDampMoveRef;
    
    [Header("Movement Controls")]
    public float slowSpeed = 8;
    public float fastSpeed = 26;
    float velocityY = 0;
    float speed;

    [Header("Look Controls")]
	[Range (-10, 10)]
	public float mouseSensitivityX = 3f;
	[Range (-10, 10)]
	public float mouseSensitivityY = 3f;
	float verticalLookRotation;

    float yaw;
    float pitch;

    [Header("Transition Controls")]    
    [Range(15, 300)]
    public float transitionHeight = 20;

    float transitionTime = .01125f;
    int transitionSmoothness = 50;
    Vector3[] positions = new Vector3[50];
    Vector3 velocity = Vector3.zero;

	// Use this for initialization
	void Start () {
		playerController = FindObjectOfType<PlayerController>();
        playerController.gameObject.transform.Find("TempMesh").Find("PlayerIcon").gameObject.SetActive(false);

	}

    void Update() {

        if(Input.GetKey(KeyCode.LeftShift)){
            speed = fastSpeed;
        } else {
            speed = slowSpeed;
        }
    }

    void LateUpdate () {

        if(playerController.mode == PlayerController.Mode.flying && !backpanning){
            yaw += mouseSensitivityX * Input.GetAxis("Mouse X");
            pitch -= mouseSensitivityY * Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            

            transform.localEulerAngles = new Vector3(pitch, yaw, 0.0f);

            Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            Vector3 newRight = Vector3.Cross(Vector3.up, transform.worldToLocalMatrix.MultiplyVector(transform.forward));
            Vector3 newForward = Vector3.Cross(newRight, Vector3.up);	

            Vector3 trueMoveDir = (newRight * Input.GetAxisRaw("Horizontal") + newForward * Input.GetAxisRaw("Vertical"));
            targetFlyAmount = trueMoveDir * speed + Vector3.up * velocityY;
            flyAmount = Vector3.SmoothDamp(flyAmount, targetFlyAmount, ref smoothDampMoveRef, 0.1f);
            transform.Translate(flyAmount * Time.fixedDeltaTime);


            if(Input.GetKey(KeyCode.E)){
                transform.Translate(Vector3.up * speed * Time.fixedDeltaTime, Space.Self);
            }

            if(Input.GetKey(KeyCode.Q)){
                transform.Translate(-Vector3.up * speed * Time.fixedDeltaTime, Space.Self);
            }
            
            if(Input.GetMouseButton(0)){
                ChangePlayerLocation();
            }
            
        }
        if(Input.GetKeyDown(KeyCode.T)){
            ToggleFlightMode();
        }
    }

    void ToggleFlightMode(){
        flightToggle = 1 - flightToggle;

        //flight on
        if(flightToggle == 1){
            playerController.mode = PlayerController.Mode.flying; 
            
            playerController.gameObject.transform.Find("TempMesh").GetComponent<MeshRenderer>().enabled = true;
            playerController.gameObject.transform.Find("TempMesh").Find("PlayerIcon").gameObject.SetActive(true);
            
            Vector3 targetRot = transform.eulerAngles;
            transform.parent = null;
            
            if(transform.eulerAngles.x > 90){
                pitch = -(360 - transform.eulerAngles.x);
            } else {
                pitch = transform.eulerAngles.x;
            }
            yaw = transform.eulerAngles.y;
            
            StartCoroutine("BackPan");
           
        
        //flight off
        } else if (flightToggle == 0){ 

            playerController.gameObject.transform.Find("TempMesh").Find("PlayerIcon").gameObject.SetActive(false);

            StopCoroutine("BackPan");
            playerController.mode = PlayerController.Mode.walking; 
            Camera.main.transform.localEulerAngles = Vector3.zero;
            Camera.main.transform.position = GameObject.Find("CameraDock").transform.position;
            playerController.gameObject.transform.Find("TempMesh").GetComponent<MeshRenderer>().enabled = false;
            transform.parent = GameObject.Find("CameraDock").transform;

        }

    }

    //IEnumerator PanBack(){
    //    float speed = 1/transitionTime;
    //    float percent = 0;
    //    Vector3 targetPos = gameObject.transform.position + -transform.forward * 4;
    //    while (percent < 1){
    //        backpanning = true;
    //        percent += Time.deltaTime * speed;
    //        gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, targetPos, percent);
    //        yield return null;
    //    }
    //    backpanning = false;

    //}

    IEnumerator BackPan(){
        Vector3[] points = new Vector3[4];
        points[0] = gameObject.transform.position + -transform.forward * 3;
        points[1] = (points[0] + -transform.forward * 3) + Vector3.up * 2;
        points[2] = (points[1] + -transform.forward * 1.5f) + Vector3.up * 4; 
        Vector3 p3Horizontal = points[2] + -transform.forward * .5f;
        points[3] = new Vector3(p3Horizontal.x, transitionHeight, p3Horizontal.z);
        LoadPositions(points[0], points[1], points[2], points[3]);
        for(int i = 0; i < transitionSmoothness; i++){
            float speed = 1/transitionTime;
            float percent = 0;
            gameObject.transform.position = Vector3.SmoothDamp(gameObject.transform.position, positions[i], ref velocity, transitionTime);

            while (percent < 1){
                backpanning = true;
                percent += Time.fixedUnscaledDeltaTime * speed;
                gameObject.transform.position = Vector3.SmoothDamp(gameObject.transform.position, positions[i], ref velocity, percent);
                gameObject.transform.LookAt(FindObjectOfType<PlayerController>().gameObject.transform);
                yield return null;
            }
        }
        yaw = gameObject.transform.eulerAngles.y;
        pitch = gameObject.transform.eulerAngles.x;
        backpanning = false;
    }

    void ChangePlayerLocation(){
        Vector3 ray = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if(Physics.SphereCast(ray, 1f, Camera.main.transform.forward, out hit, Mathf.Infinity, environment) && !backpanning){
            playerController.gameObject.transform.position = hit.point + hit.normal * 1.25f;
        }
    }

    Vector3 CalculateCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t){
        float r = 1f - t;
        float f0 = r * r * r;
        float f1 = r * r * t * 3;
        float f2 = r * t * t * 3;
        float f3 = t * t * t;
        return f0*p0 + f1*p1 + f2*p2 + f3*p3;
    }

    void LoadPositions(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3){
        for (int i = 1; i < transitionSmoothness + 1; i ++){
            float t = i / (float)transitionSmoothness;
            positions[i - 1] = CalculateCubicBezierPoint(p0, p1, p2, p3, t);
        }
    }

    
}