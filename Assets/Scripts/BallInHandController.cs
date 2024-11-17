using Cysharp.Threading.Tasks;
using System.Drawing;
using Unity.Netcode;
using UnityEngine;
using Color = UnityEngine.Color;

public class BallInHandController : NetworkBehaviour
{
    public MeshRenderer meshRenderer;

    private readonly float tableMinX = -7.5f;
    private readonly float tableMaxX = 7.5f;
    private readonly float tableMinZ = -3.5f;
    private readonly float tableMaxZ = 3.5f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        meshRenderer = GetComponent<MeshRenderer>();
    }
    public void SetTransparency(Color color, float alpha)
    {
        if (meshRenderer != null)
        {
            Material material = meshRenderer.material;

            material.shader = Shader.Find("Standard");
            material.SetFloat("_Mode", 3);

            // 블렌딩 모드와 키워드 활성화
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            //material.renderQueue = 3000;

            // 색상 및 투명도 설정
            color.a = alpha;
            material.color = color;
        }
    }

    public async UniTaskVoid StartFreeBallPlacement()
    {
        await UniTask.Delay(100);
        int count = 0;
        ShowHelper();
        while (GameManager.Instance.GetMyPlayerNumber() != GameManager.Instance.playerTurn.Value || !IsOwner)
        {
            Debug.Log($"not your turn{GameManager.Instance.GetMyPlayerNumber()} {GameManager.Instance.playerTurn.Value}");
            count++;

            await UniTask.Delay(100);
            if (count >= 10) return;
        }
        if (!GameManager.Instance.freeBall.Value)
        {
            return;
        }

        while (GameManager.Instance.freeBall.Value)
        {
            // 마우스 위치를 가져옴
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.y = transform.position.y;

            // 마우스 위치를 테이블 영역 내로 제한
            float xPos = Mathf.Clamp(mousePos.x, tableMinX, tableMaxX);
            float zPos = Mathf.Clamp(mousePos.z, tableMinZ, tableMaxZ);
            Vector3 newPosition = new Vector3(xPos, 0.3f, zPos);

            // 해당 위치에 다른 공이 있는지 검사 (클라이언트에서 임시로 검사)
            if (IsPositionValid(newPosition))
            {
                if (meshRenderer.material.color != Color.white) UpdateColorServerRpc(Color.white, 0.5f);
                transform.position = newPosition; // 큐볼 위치 업데이트
                UpdateCuePositionServerRpc(newPosition);
            }
            else
            {
                if(meshRenderer.material.color!=Color.red) UpdateColorServerRpc(Color.red, 0.5f);
            }

            // 마우스 왼쪽 버튼 클릭 시 위치 확정
            if (Input.GetMouseButtonDown(0) && IsPositionValid(newPosition))
            {
                // 서버에 큐볼 위치 변경 요청
                
                RequestFreeBallPlacementServerRpc(newPosition);
                // freeBall 상태 변경을 서버에 요청
                GameManager.Instance.SetFreeBallServerRpc(false);

                transform.position = new Vector3(0, -2, 0);
                break;
            }

            await UniTask.Yield();
        }
        HideHelper();
    }
    [ServerRpc(RequireOwnership = false)]
    private void UpdateColorServerRpc(Color color, float alpha)
    {
        UpdateColorClientRpc(color, alpha);
    }
    [ClientRpc]
    private void UpdateColorClientRpc(Color color, float alpha)
    {
        SetTransparency(color, alpha);
    }
    [ServerRpc(RequireOwnership = false)]
    private void UpdateCuePositionServerRpc(Vector3 position)
    {
        UpdateCueBallPositionClientRpc(position);
    }
    [ClientRpc]
    private void UpdateCueBallPositionClientRpc(Vector3 position)
    {
        transform.position = position;
    }

    bool IsPositionValidOnServer(Vector3 position)
    {
        float radius = 0.32f;
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != transform.gameObject && (collider.gameObject.CompareTag("SolidBall") || collider.gameObject.CompareTag("StripedBall") || collider.gameObject.CompareTag("EightBall")))
            {
                return false;
            }
        }
        return true;
    }

    bool IsPositionValid(Vector3 position)
    {
        float radius = 0.32f;
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != transform.gameObject && (collider.gameObject.CompareTag("SolidBall") || collider.gameObject.CompareTag("StripedBall") || collider.gameObject.CompareTag("EightBall")))
            {
                return false;
            }
        }
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestFreeBallPlacementServerRpc(Vector3 newPosition)
    {
        // 해당 위치에 다른 공이 있는지 서버에서 검사
        if (!IsPositionValidOnServer(newPosition))
            return;
        GameManager.Instance.CompleteBallInHandServerRpc(newPosition);

        transform.position = new Vector3(0, -2, 0);
    }

    public void SetOwnerClientId(ulong clientId)
    {
        GetComponent<NetworkObject>().ChangeOwnership(clientId);
    }

    internal void HideHelper()
    {
        HideServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    void ShowServerRpc()
    {
        ShowHelperClientRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    void HideServerRpc()
    {
        HideHelperClientRpc();
    }
    internal void ShowHelper()
    {
        ShowServerRpc();
    }
    [ClientRpc]
    private void ShowHelperClientRpc()
    {
        meshRenderer.enabled = true;
    }

    [ClientRpc]
    private void HideHelperClientRpc()
    {
        meshRenderer.enabled = false;
    }
}
