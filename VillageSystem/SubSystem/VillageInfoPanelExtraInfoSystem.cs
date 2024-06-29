using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillageInfoPanelExtraInfoSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI homeName;
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI skillExtraInfo;
    [SerializeField] private Button exitButton;

    public void Load(string homeName , string skillName , string extraInfo)
    {
        this.homeName.text = homeName;
        this.skillName.text = skillName;
        this.skillExtraInfo.text = extraInfo;
        
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(()=> gameObject.SetActive(false));
    }
}
