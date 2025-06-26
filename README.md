# Virtual Shooting Simulator (Local Multiplayer & Single Player Tower Defense)

This project is a 2D tower defense game developed using the Unity game engine, featuring both local multiplayer and single-player modes. Players defend their castles against enemy waves, utilizing special abilities collected from chests. The game includes multiple difficulty levels and a scoring system integrated with Firebase Realtime Database for a persistent leaderboard.

Contributors to the project:      
https://github.com/Semtomer - Tolga YILMAZ       
https://github.com/BerfuSarihan - Ayşe Berfu SARIHAN         
https://github.com/ZeynepOzer634 - Zeynep ÖZER         

## 1. Project Overview

The primary goal of this project is to implement and showcase several core game development concepts within Unity, focusing on:

*   **2D Game Environment:** An engaging top-down battlefield.
*   **AI Behavior:** NavMeshAgent-driven enemy pathfinding and attack logic.
*   **Player Control & Interaction:** Dedicated keyboard controls for two local players, mouse-based targeting.
*   **Core Game Mechanics:** Castle defense, distinct enemy types, collectible special abilities (Stun, Slow, Weaken Attack) with a FIFO queue and global cooldown, and a distance-based scoring system.
*   **User Interface (UI) System:** In-game HUD (scores, timer, health, abilities), Pause Menu (with audio controls), Game Over Screen (with results and Firebase-powered leaderboard), and a Main Menu system for game setup.
*   **Object-Oriented Programming (OOP):** Structured C# codebase.
*   **Audio Management:** Centralized system for background music and SFX, with persistent volume settings via PlayerPrefs.
*   **Database Integration (Firebase Realtime Database):** Online saving and retrieval of game session results, structured by game mode and difficulty.
*   **Scene and Settings Management:** Separate scenes for modes/difficulties, configured via a static `GameSettings` class.

## 2. Features

*   **Dual Game Modes:** Local Multiplayer and Single Player.
*   **Variable Difficulty Levels:** Easy, Normal, Hard, affecting aspects like chest lifetime and potentially enemy stats (configured per scene/prefab).
*   **Dynamic Enemy & Chest Spawning:** Independent spawners per side, with weighted random ability generation for chests (excluding abilities already held by the relevant player).
*   **Target Point Management:** Enemies navigate to intermediate points before attacking castles.
*   **Detailed Enemy Logic:** Health, attack, stun, death states, and dynamic colliders (`DynamicEnemyCollider.cs`).
*   **Player Abilities & Cooldowns:** FIFO queue for abilities, global cooldown after use, UI feedback.
*   **Comprehensive UI System:** Covers all game states from main menu to game over.
*   **Persistent Audio Settings:** Volume levels saved across game sessions.
*   **Online Firebase Scoreboard:** Separate leaderboards for each game mode and difficulty combination.

## 3. Technologies Used

*   **Game Engine:** Unity (Version **6.0.37f1**)
*   **Programming Language:** C#
*   **Key Unity Features:** NavMeshAgent, Animator, Unity UI (TextMeshPro, Canvas, etc.), Physics2D, PlayerPrefs, Scene Management.
*   **External Services:** Firebase Realtime Database.
*   **Firebase Unity SDK:** Database and its core dependencies (e.g., App, Installations).

## 4. Setup and Running the Project

**Prerequisites:**

*   Unity Hub.
*   Unity Editor (Version **6.0.37f1** or compatible).
*   An IDE (Visual Studio, VS Code with Unity tools, or JetBrains Rider).
*   A Google Account for Firebase.

**Steps:**

1.  **Clone or Download the Repository:**
    ```bash
    git clone https://github.com/Semtomer/VirtualShootingSimulator-SEN4992
    cd VirtualShootingSimulator-SEN4992\VirtualShootingSimulator
    ```

2.  **Open in Unity Hub:**
    *   Launch Unity Hub.
    *   Click "Open" or "Add project from disk".
    *   Navigate to the cloned project folder and select it.

3.  **Firebase Setup (CRITICAL for Online Features):**
    This project uses Firebase Realtime Database for online scoreboards. To enable this functionality, you **must** set up your own Firebase project. The `google-services.json` file containing sensitive API keys is **not** included in this repository for security reasons.

    *   **a. Create a Firebase Project:** Go to the [Firebase Console](https://console.firebase.google.com/) and create a new project (or use an existing one).
    *   **b. Register Your Unity App:**
        *   In your Firebase project, click on "Add app" and select the Unity icon.
        *   Follow the registration steps. For a Standalone Windows build, you'll typically register it as a "Desktop" app or by providing a bundle ID (e.g., `com.yourcompany.yourgamename`). The Firebase console will guide you.
        *   **Download `google-services.json`:** After registration, Firebase will provide a `google-services.json` file. Download this file.
    *   **c. Add `google-services.json` to Unity:**
        *   Place the downloaded `google-services.json` file into the `/Assets/` folder of your Unity project. **This file is listed in `.gitignore` and will not be committed to your Git repository.**
    *   **d. Import Firebase SDK:**
        *   Download the latest Firebase Unity SDK from the [official Firebase website](https://firebase.google.com/docs/unity/setup).
        *   From the downloaded `.zip` file, import the following `.unitypackage` files into your Unity project (drag them into the `Assets` folder or use `Assets -> Import Package -> Custom Package...`):
            *   `FirebaseDatabase.unitypackage`
            *   Ensure that any core Firebase packages (like `FirebaseApp.unitypackage`, `FirebaseCore.unitypackage`, `FirebaseInstallations.unitypackage`, `FirebaseAppCheck.unitypackage` if they are separate in your SDK version) are also imported. Often, importing `FirebaseDatabase` will bring its dependencies.
    *   **e.Configure Database URL:**
        *   Open the `FirebaseManager.cs` script (located in `Assets/GameAssets/Scripts/`).
        *   Find the line: `private const string databaseURL = "YOUR_FIREBASE_DATABASE_URL_HERE";`
        *   Replace `"YOUR_FIREBASE_DATABASE_URL_HERE"` with your actual Firebase Realtime Database URL (e.g., `https://your-project-id-default-rtdb.firebaseio.com` or a regional URL like `https://your-project-id-default-rtdb.europe-west1.firebasedatabase.app`). You can find this URL in your Firebase Console under "Realtime Database" -> "Data" tab.
    *   **f. Set Database Rules (for Testing):**
        *   In your Firebase Console, go to "Realtime Database" -> "Rules".
        *   For initial testing, you can set the rules to allow public read/write access. **WARNING: This is insecure for a production app.**
            ```json
            {
              "rules": {
                ".read": true,
                ".write": true
              }
            }
            ```
        *   Click "Publish". Remember to implement proper security rules before releasing your game.
    *   **g. Resolve Dependencies (if prompted or for Android/iOS later):**
        *   If you are targeting Android or iOS in the future, or if Unity prompts you, run the External Dependency Manager: `Assets -> External Dependency Manager -> Android Resolver -> Resolve` (or `iOS Resolver`). For Standalone Windows, this step is often not explicitly required after importing the correct `.unitypackage` files, as the necessary DLLs are usually included.

4.  **Open the Main Menu Scene:**
    *   In the Unity Editor, navigate to `Assets/GameAssets/Scenes/` (or your main scenes folder).
    *   Double-click the `MainMenu` scene file to open it.

5.  **Run the Game:**
    *   Press the Play button at the top of the Unity Editor.
    *   From the Main Menu, configure game mode, difficulty, and player names, then start the game.

## 5. Game Controls

*   **Player 1 (Left Side):**
    *   **Fire:** F
    *   **Use Special Ability:** G (Activates the first ability in the queue)
*   **Player 2 (Right Side - Multiplayer Mode Only):**
    *   **Fire:** O (Default, configurable via Inspector on `Player_Right` GameObject)
    *   **Use Special Ability:** P (Default, configurable via Inspector on `Player_Right` GameObject)
*   **General:**
    *   **Pause/Resume Game:** ESC
    *   **Mouse:** Used for aiming when firing.

## 6. Gameplay

*   **Objective:** Defend your castle(s) from enemy waves. In multiplayer, defeat the opponent's castle or have a higher score at the end of the timer. In single player, survive and achieve a high score.
*   **Special Abilities:** Collect abilities from chests. Use them strategically. A global cooldown applies after an ability is used.
*   **Scoring:** Earn points by defeating enemies, with bonuses for killing them further from their target.
*   **Game End:** Occurs when a castle is destroyed or the timer expires. Results are displayed and saved to Firebase.

## 7. License

This project is open source. Feel free to use, modify, and distribute it as you see fit. This project is licensed under the MIT License - see the LICENSE.md file for details.
