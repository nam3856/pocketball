using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections;

public class PlayerName : NetworkBehaviour
{
    [SerializeField]
    private Text nameText; // Inspector에서 할당

    // 플레이어 이름을 저장하는 NetworkVariable
    public NetworkVariable<FixedString32Bytes> PlayerNameVar = new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            StartCoroutine(WaitForGameSettings());
        }

        // 이름이 변경될 때 UI 업데이트
        PlayerNameVar.OnValueChanged += OnPlayerNameChanged;
        UpdateNameText(PlayerNameVar.Value.ToString()); // FixedString32Bytes 타입을 string으로 변환

    }

    private IEnumerator WaitForGameSettings()
    {
        while (GameSettings.Instance == null || !GameSettings.Instance.NetworkObject.IsSpawned)
        {
            yield return null;
        }

        // 서버에 플레이어 이름 설정 요청
        SetPlayerNameServerRpc(OwnerClientId, GameSettings.Instance.PlayerName);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerNameVar.OnValueChanged -= OnPlayerNameChanged;
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(ulong clientId, string name)
    {
        // FixedString32Bytes로 변환하여 NetworkVariable에 할당
        GameSettings.Instance.SetPlayerNameServerRpc(clientId, name);
        PlayerNameVar.Value = name;
    }

    private void OnPlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        // FixedString32Bytes 타입을 string으로 변환해서 업데이트
        UpdateNameText(newName.ToString());
    }

    private void UpdateNameText(string name)
    {
        if (nameText != null)
        {
            nameText.text = name;
        }
    }
}
