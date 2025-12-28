using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ServerGrabCube : NetworkBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    // current owner
    private readonly NetworkVariable<ulong> grabbingClientId =
        new NetworkVariable<ulong>(ulong.MaxValue);

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    private void OnDestroy()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // local player grabbing
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening) return;

        var localId = NetworkManager.Singleton.LocalClientId;
        RequestGrabServerRpc(localId);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening) return;

        var localId = NetworkManager.Singleton.LocalClientId;
        ReleaseGrabServerRpc(localId);
    }

    private void FixedUpdate()
    {
        if (!IsSpawned) return;
        if (grabbingClientId.Value == NetworkManager.Singleton.LocalClientId)
        {
            var interactor = grab.firstInteractorSelecting as IXRSelectInteractor;
            if (interactor == null) return;

            Transform hand = interactor.transform;
            SubmitPoseServerRpc(hand.position, hand.rotation);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRpc(ulong requesterId)
    {
        // client handle
        if (grabbingClientId.Value != ulong.MaxValue) return;

        grabbingClientId.Value = requesterId;
        rb.isKinematic = true; // motion
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseGrabServerRpc(ulong requesterId)
    {
        if (grabbingClientId.Value != requesterId) return;

        grabbingClientId.Value = ulong.MaxValue;
        rb.isKinematic = false; // motion off
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPoseServerRpc(Vector3 pos, Quaternion rot)
    {
        if (grabbingClientId.Value == ulong.MaxValue) return;
        rb.MovePosition(pos);
        rb.MoveRotation(rot);
    }
}
