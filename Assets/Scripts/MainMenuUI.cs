using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private TMP_Text statusText;

    [Header("Game")]
    [SerializeField] private string gameplaySceneName = "GamePlay";
    [SerializeField] private int maxClients = 1;
    [SerializeField] private string connectionType = "dtls";

    private bool _busy;

    private void Awake()
    {
        hostButton.onClick.AddListener(() => _ = HostAsync());
        joinButton.onClick.AddListener(() => _ = JoinAsync());
        startButton.onClick.AddListener(StartGame);
        leaveButton.onClick.AddListener(Leave);

        RefreshUI();
    }

    private async Task EnsureServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync(); // needed before relay

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync(); // also needed before relay
    }

    private UnityTransport TransportOrThrow()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) throw new Exception("Nm not found");

        var t = nm.GetComponent<UnityTransport>();
        if (t == null) throw new Exception("Ut not found");

        return t;
    }

    private async Task HostAsync()
    {
        if (_busy) return;
        _busy = true;
        SetStatus("Starting host...");
        RefreshUI();

        try
        {
            await EnsureServicesAsync();

            // transport config
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxClients);
            var relayData = AllocationUtils.ToRelayServerData(allocation, connectionType);
            TransportOrThrow().SetRelayServerData(relayData);

            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCodeText.text = joinCode;

            GUIUtility.systemCopyBuffer = joinCode; // clipboard copy
            SetStatus($"Host started. Code copied: {joinCode}");

            if (!NetworkManager.Singleton.StartHost())
                SetStatus("Failed to start host (NetworkManager.StartHost returned false).");
        }
        catch (Exception e)
        {
            SetStatus("Host error: " + e.Message);
        }
        finally
        {
            _busy = false;
            RefreshUI();
        }
    }

    private async Task JoinAsync()
    {
        if (_busy) return;
        _busy = true;
        SetStatus("Joining...");
        RefreshUI();

        try
        {
            var code = (joinCodeInput.text ?? "").Trim();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Enter a join code first.");
                return;
            }

            await EnsureServicesAsync();

            // transport config
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: code);
            var relayData = AllocationUtils.ToRelayServerData(allocation, connectionType);
            TransportOrThrow().SetRelayServerData(relayData);

            if (NetworkManager.Singleton.StartClient())
                SetStatus("Client started. Connecting...");
            else
                SetStatus("Failed to start client (NetworkManager.StartClient returned false).");
        }
        catch (Exception e)
        {
            SetStatus("Join error: " + e.Message);
        }
        finally
        {
            _busy = false;
            RefreshUI();
        }
    }

    private void StartGame()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
        {
            SetStatus("Only the Host can start the game.");
            return;
        }
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        SetStatus("Loading gameplay...");
    }

    private void Leave()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        joinCodeText.text = "";
        SetStatus("Left session.");
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool hasNM = NetworkManager.Singleton != null;
        bool isListening = hasNM && NetworkManager.Singleton.IsListening;
        bool isHost = hasNM && NetworkManager.Singleton.IsHost;
        bool isClient = hasNM && NetworkManager.Singleton.IsClient;

        hostButton.interactable = !_busy && !isListening;
        joinButton.interactable = !_busy && !isListening;
        startButton.interactable = !_busy && isHost;
        leaveButton.interactable = !_busy && isListening;

        if (joinCodeInput) joinCodeInput.interactable = !_busy && !isListening;
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log(msg);
    }
}
