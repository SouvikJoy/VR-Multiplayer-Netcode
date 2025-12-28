using Unity.Netcode;
using UnityEngine;
using Unity.XR.CoreUtils;

public class NetPlayer : NetworkBehaviour
{
    private XROrigin _xr;
    private Transform _hmd;

    private readonly NetworkVariable<Vector3> _pos = new(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<Quaternion> _rot = new(writePerm: NetworkVariableWritePermission.Owner);

    private void TryBindRig()
    {
        if (_xr == null) _xr = FindFirstObjectByType<XROrigin>();
        if (_hmd == null && Camera.main != null) _hmd = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            if (_xr == null || _hmd == null) TryBindRig();
            if (_xr == null || _hmd == null) return;

            float floorY = _xr.transform.position.y;

            Vector3 p = new Vector3(_hmd.position.x, floorY, _hmd.position.z);
            Quaternion r = Quaternion.Euler(0f, _hmd.eulerAngles.y, 0f);

            transform.SetPositionAndRotation(p, r);

            _pos.Value = p;
            _rot.Value = r;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, _pos.Value, 20f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _rot.Value, 20f * Time.deltaTime);
        }
    }
}
