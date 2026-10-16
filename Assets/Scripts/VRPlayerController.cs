using UnityEngine;
using Fusion;

public class VRPlayerController : NetworkBehaviour
{
    [Header("Visual Transforms (Sync Objects)")]
    public Transform headVisual;
    public Transform leftHandVisual;
    public Transform rightHandVisual;

    // ক্যামেরা ও অডিও ট্র্যাক রেফারেন্স
    private Camera localCamera;
    private AudioListener localAudio;

    public override void Spawned()
    {
        // ১. রিমোট প্লেয়ারের ক্যামেরা ও অডিও নিষ্ক্রিয় করা
        if (!HasInputAuthority)
        {
            Camera remoteCam = GetComponentInChildren<Camera>();
            if (remoteCam != null)
            {
                remoteCam.enabled = false;
            }

            AudioListener remoteAudio = GetComponentInChildren<AudioListener>();
            if (remoteAudio != null)
            {
                remoteAudio.enabled = false;
            }
        }
        else
        {
            // ২. লোকাল প্লেয়ারের ক্ষেত্রে নিশ্চিত করুন ক্যামেরা সক্রিয় আছে
            localCamera = GetComponentInChildren<Camera>();
            if (localCamera != null)
            {
                localCamera.enabled = true;
            }

            localAudio = GetComponentInChildren<AudioListener>();
            if (localAudio != null)
            {
                localAudio.enabled = true;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // শুধুমাত্র লোকাল প্লেয়ার তার নিজস্ব পজিশন ও রোটেশন আপডেট করবে
        if (HasInputAuthority)
        {
            // যদি ক্যামেরা খুঁজে পাওয়া যায় তবে তার পজিশন হেড মডেলে সেট করা
            if (localCamera != null && headVisual != null)
            {
                headVisual.position = localCamera.transform.position;
                headVisual.rotation = localCamera.transform.rotation;
            }
        }
    }
}
