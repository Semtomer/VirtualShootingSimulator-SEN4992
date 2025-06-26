using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public static bool IsInitialized { get; private set; } = false;

    public static event Action OnFirebaseInitialized;

    private DatabaseReference dbReference;
    private FirebaseApp app;

    private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;

    private bool isLocalInstanceInitialized = false;
    private const string databaseURL = "https://virtualshootingsimulator-default-rtdb.europe-west1.firebasedatabase.app";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            IsInitialized = false;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        Debug.Log("Checking Firebase Dependencies...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                Debug.Log("Firebase Dependencies Available. Initializing Database...");

                try
                {
                    FirebaseDatabase database = FirebaseDatabase.GetInstance(app, databaseURL);
                    Debug.Log($"Firebase Database Instance obtained for URL: {databaseURL}");

                    dbReference = database.RootReference;
                    isLocalInstanceInitialized = true;

                    IsInitialized = true;
                    Debug.Log("Firebase Fully Initialized. Invoking OnFirebaseInitialized event.");
                    OnFirebaseInitialized?.Invoke();

                    Debug.Log("Firebase Database Initialized and RootReference obtained.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error getting Database instance or RootReference for URL {databaseURL}: {e.Message} \nStack Trace: {e.StackTrace}");
                    dependencyStatus = DependencyStatus.UnavailableOther;
                    IsInitialized = false;
                }
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}. Firebase Database features will be unavailable.");
                IsInitialized = false;
            }
        });
    }

    private string GetSessionPath(GameModeType mode, GameDifficulty difficulty)
    {
        return $"GameSessions_{mode}_{difficulty}";
    }

    public void SaveMultiplayerSession(MultiplayerSessionData sessionData, GameDifficulty difficulty)
    {
        InternalSaveSession(sessionData, GameModeType.Multiplayer, difficulty);
    }

    public void SaveSinglePlayerSession(SinglePlayerSessionData sessionData, GameDifficulty difficulty)
    {
        InternalSaveSession(sessionData, GameModeType.SinglePlayer, difficulty);
    }

    public void InternalSaveSession(object sessionDataObject, GameModeType mode, GameDifficulty difficulty)
    {
        if (!isLocalInstanceInitialized || dbReference == null)
        {
            Debug.LogError("Firebase is not initialized correctly (dbReference is null or status not Available). Cannot save game session.");
            return;
        }

        string path = GetSessionPath(mode, difficulty);

        string sessionKey = dbReference.Child(path).Push().Key;

        if (sessionDataObject is MultiplayerSessionData mpData)
            mpData.sessionID = sessionKey;
        else if (sessionDataObject is SinglePlayerSessionData spData)
            spData.sessionID = sessionKey;

        string jsonData = JsonUtility.ToJson(sessionDataObject);

        dbReference.Child(path).Child(sessionKey).SetRawJsonValueAsync(jsonData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"Session {sessionKey} saved successfully to {path}.");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError($"Failed to save session {sessionKey} to {path}: {task.Exception}");
            }
            else if (task.IsCanceled)
            {
                Debug.LogWarning($"Saving game session {sessionKey} was canceled.");
            }
        });
    }

    public void FetchSpecificSessionHistory(GameModeType mode, GameDifficulty difficulty, Action<List<object>> onDataReceived)
    {
        if (!isLocalInstanceInitialized || dbReference == null)
        {
            Debug.LogError("Firebase not initialized. Cannot fetch history.");
            onDataReceived?.Invoke(null);
            return;
        }

        string path = GetSessionPath(mode, difficulty);
        Debug.Log($"Fetching session history from specific path: {path}");

        dbReference.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            List<object> sessionList = new List<object>();

            if (task.IsFaulted)
            {
                Debug.LogError($"Failed to fetch session history from {path}: {task.Exception}");
                onDataReceived?.Invoke(null);
                return;
            }

            if (task.IsCompletedSuccessfully)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot != null && snapshot.Exists && snapshot.HasChildren)
                {
                    foreach (DataSnapshot sessionSnapshot in snapshot.Children)
                    {
                        try
                        {
                            string json = sessionSnapshot.GetRawJsonValue();
                            if (!string.IsNullOrEmpty(json))
                            {
                                object session = null;

                                if (mode == GameModeType.Multiplayer)
                                {
                                    MultiplayerSessionData mpSession = JsonUtility.FromJson<MultiplayerSessionData>(json);

                                    if (string.IsNullOrEmpty(mpSession.sessionID))
                                        mpSession.sessionID = sessionSnapshot.Key;

                                    session = mpSession;
                                }
                                else
                                {
                                    SinglePlayerSessionData spSession = JsonUtility.FromJson<SinglePlayerSessionData>(json);

                                    if (string.IsNullOrEmpty(spSession.sessionID))
                                        spSession.sessionID = sessionSnapshot.Key;

                                    session = spSession;
                                }
                                if (session != null)
                                    sessionList.Add(session);
                            }
                        }
                        catch (Exception e) { Debug.LogError($"Error parsing session {sessionSnapshot.Key} from {path}: {e.Message}"); }
                    }
                    Debug.Log($"Fetched {sessionList.Count} session(s) from {path}.");
                }
                else
                {
                    Debug.Log($"No session history found at {path}.");
                }
            }

            onDataReceived?.Invoke(sessionList);
        });
    }
}