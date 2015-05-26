using UnityEngine;
using System.Collections;

public class PongController : MonoBehaviour {

    public const float TIMEOUT = 3.0f;

    private float _timer = 0;
    public GameObject ballPrefab;
    public GameObject CenterEyeAnchor;
    public bool test = false;
    public Vector3 test2 = Vector3.zero;

    private bool _visible = true;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        _timer += Time.deltaTime;

        if (_timer > TIMEOUT)
        {
            _timer = 0f;

            Vector3 pos = CenterEyeAnchor.transform.forward * 2.6f;
            pos.y = 0.1f;
            GameObject ball = Instantiate(ballPrefab, pos, new Quaternion()) as GameObject;

            Vector3 dir = -CenterEyeAnchor.transform.forward;
            dir.Normalize();
            dir *= 0.165f;
            dir.y = 0.5f;


            ball.GetComponent<Rigidbody>().AddForce(dir, ForceMode.Impulse);
        }

        GameObject[] list = GameObject.FindGameObjectsWithTag("ball");
        foreach (GameObject marker in list)
        {
            if (marker.transform.position.y < -0.5)
            {
                Destroy(marker);
            }
        }

        if(Input.GetKeyDown(KeyCode.Z))
        {
            _visible = !_visible;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach(Renderer renderer in renderers)
            {
                renderer.enabled = _visible;
            }
        }
	}
}
