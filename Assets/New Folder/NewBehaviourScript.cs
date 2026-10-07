using UnityEngine;
using Fusion;

public class NewBehaviourScript : MonoBehaviour
{
    public float moveSpeed = 6.0f;
    public float lookSpeed = 2.0f;
    public NetworkPrefabRef markerNetworkPrefab;

    private float yaw = 0.0f;
    private float pitch = 25.0f;
    private Vector3 lastMousePos;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        // মাউসের রাইট-ক্লিক দিয়ে ভিউ ঘোরানো
        Vector3 currentMousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(1))
        {
            lastMousePos = currentMousePos;
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 delta = currentMousePos - lastMousePos;
            yaw += delta.x * lookSpeed * 0.1f;
            pitch -= delta.y * lookSpeed * 0.1f;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
            lastMousePos = currentMousePos;
        }

        // কিবোর্ড মুভমেন্ট
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= transform.forward;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += transform.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= transform.right;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftShift)) move += Vector3.down;

        transform.position += move.normalized * moveSpeed * Time.deltaTime;

        // মাউসের লেফট-ক্লিকে নেটওয়ার্ক মার্কার স্পন করা
        if (Input.GetMouseButtonDown(0))
        {
            SpawnNetworkMarker();
        }
    }

    void SpawnNetworkMarker()
    {
        Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            NetworkRunner runner = FindObjectOfType<NetworkRunner>();
            if (runner != null && runner.IsRunning && markerNetworkPrefab.IsValid)
            {
                runner.Spawn(markerNetworkPrefab, hit.point + hit.normal * 0.05f, Quaternion.LookRotation(hit.normal));
            }
        }
    }
}