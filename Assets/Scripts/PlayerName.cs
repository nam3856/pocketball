using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;

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
            // ������ �÷��̾� �̸� ���� ��û
            SetPlayerNameServerRpc(GameSettings.Instance.PlayerName);
        }

        // �̸��� ����� �� UI ������Ʈ
        PlayerNameVar.OnValueChanged += OnPlayerNameChanged;
        UpdateNameText(PlayerNameVar.Value.ToString()); // FixedString32Bytes Ÿ���� string���� ��ȯ
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerNameVar.OnValueChanged -= OnPlayerNameChanged;
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(string name)
    {
        // FixedString32Bytes�� ��ȯ�Ͽ� NetworkVariable�� �Ҵ�
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
