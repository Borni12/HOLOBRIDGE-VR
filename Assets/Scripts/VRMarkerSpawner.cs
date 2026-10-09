using UnityEngine;
using UnityEngine.InputSystem;

public class VRMarkerSpawner : MonoBehaviour
{
    [Header("Marker Prefab")]
    [SerializeField] private GameObject markerPrefab;

    void Update()
    {
        // মাউসের লেফট ক্লিক অথবা কীবোর্ডের Space বার চাপলে মার্কার পড়বে
        bool clicked = false;

        // ১. New Input System চেক
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
        }

        // ২. ক্লাসিক ইনপুট চেক
        if (Input.GetMouseButtonDown(0))
        {
            clicked = true;
        }

        // ৩. কিবোর্ডের স্পেস বার (Space) বিকল্প হিসেবে
        if (Input.GetKeyDown(KeyCode.Space) || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            clicked = true;
        }

        if (clicked)
        {
            SpawnDirectly();
        }
    }

    private void SpawnDirectly()
    {
        if (markerPrefab == null)
        {
            Debug.LogError("Marker Prefab Assign করা হয়নি!");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        Vector3 spawnPosition;
        if (cam != null)
        {
            // মাউস পজিশন থেকে মেঝেতে হিট করার চেষ্টা করবে
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
            Ray ray = cam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                spawnPosition = hit.point + hit.normal * 0.05f;
            }
            else
            {
                // রে-কাস্ট ব্যর্থ হলেও সরাসরি ক্যামেরার সামনে ১.৫ মিটার দূরে দৃশ্যমান স্থানে স্পন করবে
                spawnPosition = cam.transform.position + cam.transform.forward * 1.5f;
            }
        }
        else
        {
            spawnPosition = new Vector3(0, 1f, 1f);
        }

        GameObject spawned = Instantiate(markerPrefab, spawnPosition, Quaternion.identity);
        spawned.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // সাইজ যাতে চোখে পড়ার মতো বড় থাকে
        Debug.Log("Marker Successfully Spawned at: " + spawnPosition);
    }
}
