using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections;

public class PlayerName : NetworkBehaviour
{
    [SerializeField]
    private Text nameText; // Inspector���� �Ҵ�

    // �÷��̾� �̸��� �����ϴ� NetworkVariable
    public NetworkVariable<FixedString32Bytes> PlayerNameVar = new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            StartCoroutine(WaitForGameSettings());
        }

        // �̸��� ����� �� UI ������Ʈ
        PlayerNameVar.OnValueChanged += OnPlayerNameChanged;
        UpdateNameText(PlayerNameVar.Value.ToString()); // FixedString32Bytes Ÿ���� string���� ��ȯ

    }

    private IEnumerator WaitForGameSettings()
    {
        while (GameSettings.Instance == null || !GameSettings.Instance.NetworkObject.IsSpawned)
        {
            yield return null;
        }

        // ������ �÷��̾� �̸� ���� ��û
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
        // FixedString32Bytes�� ��ȯ�Ͽ� NetworkVariable�� �Ҵ�
        GameSettings.Instance.SetPlayerNameServerRpc(clientId, name);
        PlayerNameVar.Value = name;
    }

    private void OnPlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        // FixedString32Bytes Ÿ���� string���� ��ȯ�ؼ� ������Ʈ
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
