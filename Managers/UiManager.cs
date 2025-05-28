using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace White_Knuckle_Multiplayer.Managers;

public class UiManager : MonoBehaviour
{
    private static UiManager _instance;
    private GameObject playerCardTemplate;
    private Button startGame;
    private TMP_InputField inputField;

    private void Awake()
    {
        playerCardTemplate = transform.Find("Container/Viewport/Content/Player Card").gameObject;
        startGame = transform.Find("Start").gameObject.GetComponent<Button>(); // This is SetActive(false); by default
        inputField = transform.Find("LobbyInput").gameObject.GetComponent<TMP_InputField>();

        inputField.onValueChanged.AddListener((string value) =>
        {
            inputField.text = value;
            inputField.transform.Find("Text").GetComponent<TMP_Text>().text = value;
            // This is the text that shows up in the input field
        });
    }

    private void HiGalfarLookHere()
    {
        // Basically you want to run this when someone joins on the both sides
        // If joining as client, you can run this as many times as there is people in the lobby
        
        // Create a player card and parent it to the list
        var currentPlayerCard = Instantiate(playerCardTemplate, transform.Find("Container/Viewport/Content"));
        
        // Setting player name
        var playerName = currentPlayerCard.transform.Find("Name").transform.GetComponent<TMP_Text>();
        playerName.text = "Hi Galfar";
        
        // Setting player avatar
        var avatar = currentPlayerCard.transform.Find("Avatar").transform.GetComponent<Image>();
        avatar.sprite = Resources.Load<Sprite>("PathToYourSprite");

        // Setting kick button
        var kickButton = currentPlayerCard.transform.Find("Kick").transform.GetComponent<Button>();
        
        // If host, show kick button
        var host = false;
        if (host)
        {
           kickButton.gameObject.SetActive(true); 
        }
        
        kickButton.onClick.AddListener(() => 
        {
            // Kick logic
        });
    }
}