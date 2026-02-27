using UnityEngine;
using System.Text;
using BeauData;
using BeauUtil;
using FieldDay;
using FieldDay.Data;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;

public class AssignPlayerCode : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshPro _codeText;

    // Start is called before the first frame update
    void Start()
    {
        OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
    }

    public string GetTempCode() { return _codeText.text; }

    private void HandleNewPlayerId(string id)
    {
        _codeText.text = id;
        //m_BeginButton.interactable = true;
        //HandlePlayerCodeUpdated(id);
    }

    private void HandleNewPlayerIdError(OGD.Core.Error err)
    {
        OGD.Player.NewId(HandleNewPlayerId, HandleNewPlayerIdError);
    }

    private void HandlePlayerCodeUpdated(string text)
    {
        //m_BeginButton.interactable = text.Length > 1;
    }
}
