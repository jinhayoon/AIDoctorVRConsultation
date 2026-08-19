# AIDoctorVRConsultation

A virtual reality experimental platform in which a participant holds a spoken medical consultation with an AI doctor, embodied as a talking virtual avatar, that diagnoses hand eczema and recommends treatment. The AI doctor is driven in real time by OpenAI's GPT Realtime API; its speech drives avatar lip-sync via SALSA. The system manipulates the linguistic style of the doctor's communication to measure effects on trust and likelihood to accept treatment.

This repository accompanies my PhD project titled "Effects of Artificial Intelligence Design and Transparency on Patient Trust in Healthcare".  It contains the Unity-side runtime scripts, the Flask experiment-control backend, and the anonymised study data.


The simulation is begun by running the ConditionSelection scene, where the researcher is prompted to put in a Participant Identification Number and the desired Condition Number to be played during the simulation.

# System Architecture

```
┌──────────────────────────┐        ┌──────────────────────────────┐
│  Researcher Control Panel │        │        OpenAI Realtime API    │
│  (browser, Flask :5050)   │        │        gpt-realtime-1.5       │
│  set PID + condition      │        └───────────────▲──────────────┘
└─────────────┬────────────┘                        │ WebRTC (audio + data channel)
              │ HTTP                                  │ ephemeral token
              ▼                                       │
┌──────────────────────────┐   GET /get_token   ┌────┴─────────────────────────┐
│      Flask backend        │◄───────────────────┤        Unity (VR)             │
│  server.py                │   returns token    │  FlaskClient                  │
│  • ephemeral token mint   │   + system prompt  │  RealtimeAPIManager (WebRTC)  │
│  • per-condition prompt   ├───────────────────►│  WebRTCToSALSA (lip-sync)     │
│  • transcript persistence │  POST /save_convo  │  ExperimentalSceneManager     │
│  conversation_history/*.json                   │  SceneFlowManager             │
└──────────────────────────┘                     └───────────────────────────────┘
```
*The OpenAI API key never leaves the server. Unity receives only short-lived ephemeral tokens minted per session via /v1/realtime/client_secrets, then opens a direct WebRTC peer connection to OpenAI.*

### Unity runtime (C#, namespace `AIDoctor.*`)

| Script | Role |
|--------|------|
| `FlaskClient.cs` | HTTP client for the Flask backend; session start, token fetch, transcript save, health check. Singleton, persists across scenes. |
| `RealtimeAPIManager.cs` | Owns the WebRTC peer connection and data channel to OpenAI. Handles session config, mic mute/unmute around AI turns (mic disabled while the AI speaks; 0.4 s delayed un-mute to avoid echo tail), and event parsing/logging. |
| `WebRTCToSALSA.cs` | Bridges decoded WebRTC audio to **SALSA OneClick** (iClone/Character Creator) lip-sync. Computes RMS amplitude from raw samples because `GetSpectrumData`/`GetOutputData` return zeros for WebRTC-`SetTrack` audio, then writes `sayAmount` via reflection. |
| `ExperimentalSceneManager.cs` | Drives the consultation scene: wires up the managers, connects, shows VR status text, and ends on SPACE/ENTER. |
| `SceneFlowManager.cs` | Ordered scene navigation, persisted via `DontDestroyOnLoad`. Branches LLM vs. non-LLM (Control) flow on the `condition` PlayerPref. |

**Scene flow** (from `SceneFlowManager.InitializeFlow`):
`ConditionSelection → AdjustHeadset → HandSelection → WaitingRoom → <experimental scene> → SceneEnd`.
`EMOCalibrationScene` and `ButtonTraining` are present but commented out in the
current flow.

> Third-party Unity dependencies (SALSA OneClick, iClone/Character Creator avatar,
> Unity.WebRTC, Newtonsoft.Json, TextMeshPro) are **not** included. The `.cs` files
> here are the study logic only, not a runnable Unity project on their own.



## Setup & running

### 1. Backend

```bash
pip install flask flask-cors requests python-dotenv
```

Create a `.env` file next to `server.py`:

```
OPENAI_API_KEY=sk-your-key-here
```

Run:

```bash
python server.py
# → http://127.0.0.1:5050  (researcher control panel)
```

### 2. Unity

Open the full Unity project (not included in this repo) with the required
dependencies installed. Point `FlaskClient.serverUrl` at the backend
(default `http://127.0.0.1:5050`). Build/run to the VR headset.

### 3. Per participant

1. In the control panel, enter the participant ID and select the condition.
2. Start the VR session; Unity fetches the token + prompt and connects.
3. Run the consultation. Press **SPACE/ENTER** in-scene to end.
4. Transcript is saved to `conversation_history/LLMconvo_ppt{PID}.json`.
5. End the session in the control panel before the next participant.
