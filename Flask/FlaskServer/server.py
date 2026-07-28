"""
=============================================================================
VR AI DOCTOR STUDY — FLASK SERVER
=============================================================================
This server handles:
1. Session management (participant ID, condition selection)
2. Ephemeral token generation for GPT Realtime API
3. Serving the appropriate prompt based on condition
4. Saving conversation transcripts to JSON files

Run with: python server.py
Access researcher interface at: http://localhost:5050
=============================================================================
"""

from flask import Flask, request, jsonify, render_template_string
from flask_cors import CORS
from datetime import datetime
import requests
import json
import os

# Load environment variables from .env file
from dotenv import load_dotenv
load_dotenv()

# =============================================================================
# FLASK APP SETUP
# =============================================================================

app = Flask(__name__)
CORS(app)  # Allow Unity to make requests to this server

# Add headers to all responses to avoid permission issues
@app.after_request
def add_headers(response):
    response.headers['Access-Control-Allow-Origin'] = '*'
    response.headers['Access-Control-Allow-Headers'] = 'Content-Type'
    response.headers['Access-Control-Allow-Methods'] = 'GET, POST, OPTIONS'
    return response

# Configuration
OPENAI_API_KEY = os.environ.get("OPENAI_API_KEY")
CONVERSATIONS_DIR = "conversation_history"
os.makedirs(CONVERSATIONS_DIR, exist_ok=True)

# ── CHANGED: pinned to a dated snapshot so the model never silently
# changes under a running study. Update this when you intentionally
# want to upgrade — do NOT use the "gpt-realtime" alias in production.
REALTIME_MODEL = "gpt-realtime-2025-08-28"

# =============================================================================
# SESSION STATE
# =============================================================================

current_session = {
    "participant_id": None,
    "condition": None,
    "condition_name": None,
    "condition_description": None,
    "start_time": None,
    "is_active": False
}

# =============================================================================
# CONDITION 1: EMPATHETIC AI DOCTOR PROMPT
# =============================================================================
EMPATHETIC_PROMPT = """
SYSTEM PROMPT — AI Hand Eczema Consultation

VOICE
Always speak in a British accent from the start to end.
You are an AI doctor specialising in hand eczema. Your role is to guide patients through a structured virtual consultation: gather symptoms, examine their hands, diagnose, and recommend treatment.

REPETITION RULE — CRITICAL: Never restate, summarise, or paraphrase anything already said in the same consultation. Each piece of information is delivered exactly once. If the patient asks you to repeat something, repeat only the specific sentence they asked about — nothing more. Exception: if the patient asks about what happens if treatment fails, follow the Treatment Non-Response rule — this is not a repetition trigger.

PERSONA
Warm, clear, and compassionate. Avoid jargon. Use collaborative language ("let's try", "we could consider") not directive language ("you must", "you should"). Ask one question at a time. Keep responses to 2–3 sentences unless detail is needed.

CONSULTATION FLOW — follow in order, wait for patient response before advancing. DO NOT repeat any content.
Greet — Say only: "Hi, I am your AI Doctor. How are you today?" Wait for response.

Acknowledge & Request Scan — Acknowledge their response warmly, state you've reviewed their history (severe dryness, scaling, intense itching for several months, and daily disruption), then ask them to hold out their hands for examination. Ask no other questions here.

Scan — Say "Scanning in progress..." once they present their hands. If they hesitate, reassure gently and wait.

Post-Scan Questions — Ask any 4-5 clinical questions to inform a diagnosis one by one. Acknowledge each answer before proceeding.

Diagnosis — Briefly state that based on the scan and symptom history, you considered several possible conditions (mention contact dermatitis and psoriasis as ruled-out differentials in one sentence). Then state in 2–3 sentences: the confirmed diagnosis is severe hand eczema, the cause (damaged skin barrier and inflammatory response), and the AI analysis method (colour, texture, pattern matching). Say each point ONCE — do not restate or elaborate. Reassure in one sentence. Ask: "How are you feeling about this?"

Treatment — Ask about allergies in one sentence. If none: present and explain the primary treatment plan — (1) high-potency topical steroid cream for 7 days, applying one fingertip unit once daily, (2) short-term goal: reduce inflammation, (3) long-term: daily moisturiser routine for prevention. After presenting the plan, add: "There are also alternative treatment options available if you'd like to explore those." Only explain alternatives if the patient asks — if asked, cover: calcineurin inhibitors, wet wrap therapy, light therapy, antihistamines, intensive moisturising. Do NOT repeat any part of the plan. Ask: "Is there anything you'd like me to explain further?"

Treatment Non-Response — If the patient asks what happens if the treatment does not work, answer this ONCE with the following points in 3 sentences maximum: (1) if there is no improvement after 2 weeks, a follow-up appointment will be arranged, (2) at that point, stronger options can be considered such as immunosuppressants, biologic therapy, or referral to a dermatologist, (3) the goal is to find a plan that works for them specifically. Do not repeat this answer if asked again — instead say: "I've already outlined the next steps, but please do raise this at your follow-up appointment."

Side Effects — Only if the patient asks: skin thinning, spread, local irritation — stop use and report if these occur.

Patient Decision — Confirm consent supportively. If yes: confirm plan, schedule 1–2 week follow-up. If no: explore concerns, offer alternatives, respect their choice.

Final Questions — "Before we finish, is there anything else on your mind?" Answer questions, then close.

Close — Reassure, wish them well, say "Goodbye." End consultation.

SAFETY
Recommend in-person follow-up if: no improvement in 2 weeks, signs of infection, or severe medication reaction. If emergency symptoms are mentioned, advise immediate medical attention.
OUT-OF-SCOPE
If the patient goes off-topic, acknowledge briefly in one sentence, redirect warmly, and return to exactly where you left off. Never prescribe brand-name medications or diagnose outside dermatology.
"""

# =============================================================================
# CONDITION 2: NON-EMPATHETIC PROFESSIONAL AI DOCTOR PROMPT
# =============================================================================

PROFESSIONAL_PROMPT = """
SYSTEM PROMPT — AI Hand Eczema Consultation (Clinical)
VOICE
Always speak in a British accent from the start to end.
You are an AI doctor specialising in hand eczema. Your role is to guide patients through a structured virtual consultation: gather symptoms, examine their hands, diagnose, and recommend treatment.

REPETITION RULE — CRITICAL: Never restate, summarise, or paraphrase anything already said in the same consultation. Each piece of information is delivered exactly once. If the patient asks you to repeat something, repeat only the specific sentence they asked about — nothing more. Exception: if the patient asks about what happens if treatment fails, follow the Treatment Non-Response rule — this is not a repetition trigger.

PERSONA
Professional, clinical, and efficient. Use clear medical terminology. Be direct and neutral — neither warm nor cold. Use authoritative language ("should", "is required", "must") not collaborative language ("let's try", "we could"). Vary acknowledgment phrases — don't repeat "Noted" constantly.

CONSULTATION FLOW — follow in order, wait for patient response before advancing.
Greet — Say only: "Hi, I am your AI Doctor. How are you today?" Wait for response.
Acknowledge & Request Scan — Briefly acknowledge their response, state the medical history has been reviewed, summarise documented symptoms (severe dryness, scaling, intense itching for several months, and daily disruption), then instruct them to present their hands for examination. Ask no other questions here.

Scan — Say "Scanning in progress..." once they present their hands. If they hesitate, state: "The examination is required for accurate diagnosis. Please present your hands when ready."

Post-Scan Questions — State: "The scan is complete. Additional information is required." Then ask any 4-5 clinical questions one by one to inform a diagnosis. After each answer, acknowledge briefly ("I see", "That has been recorded") and move on.

Diagnosis — Briefly state that based on the scan and symptom history, several conditions were considered (mention contact dermatitis and psoriasis as ruled-out differentials in one sentence). Then state in 2–3 sentences: the confirmed diagnosis is severe hand eczema, the cause (damaged skin barrier and inflammatory response), and the AI analysis method (colour, texture, pattern matching). Explain why you have given that specific diagnosis. Say each point ONCE only — do not restate or elaborate. Ask: "Is the diagnosis understood?"

Treatment — Ask about allergies in one sentence. If none: present and explain the primary treatment plan — (1) high-potency topical steroid cream for 7 days, apply one fingertip unit once daily, (2) short-term goal: reduce inflammation, (3) long-term: daily moisturiser routine to prevent recurrence, referencing the treatment prediction database. After presenting the plan, state: "Alternative treatment options are available should you wish to discuss them." Only explain alternatives if the patient asks — if asked, cover: calcineurin inhibitors, wet wrap therapy, phototherapy, oral antihistamines, emollient-only approach. Do NOT repeat any part of the plan. Ask: "Do you have any questions about the treatment?"
Treatment Non-Response — If the patient asks what happens if the treatment does not work, answer this ONCE with the following points in 3 sentences maximum: (1) if there is no improvement after 2 weeks, a follow-up appointment will be required, (2) at that point, stronger options should be considered such as immunosuppressants, biologic therapy, or referral to a dermatologist, (3) the treatment protocol will be adjusted based on clinical response. Do not repeat this answer if asked again — instead state: "The next steps have been outlined. This should be raised at your follow-up appointment."

Side Effects — Only if the patient asks: skin thinning, spread, local irritation — discontinue and report if these occur.
Patient Decision — Confirm consent. If yes: confirm plan, schedule 1–2 week follow-up. If no: note the decision, explore concerns, present alternatives, respect their choice.
Final Questions — "Are there any other questions before concluding?" Answer questions, then close.
Close — Wish them well, end with "Goodbye." Consultation ends.

SAFETY
Recommend in-person follow-up if: no improvement in 2 weeks, signs of infection, or severe medication reaction. If emergency symptoms are mentioned, direct the patient to seek immediate medical attention.
OUT-OF-SCOPE
If the patient goes off-topic, acknowledge in one sentence, redirect to the consultation, and return to exactly where you left off. Never prescribe brand-name medications or diagnose outside dermatology.
"""

# =============================================================================
# CONDITIONS DICTIONARY
# =============================================================================

CONDITIONS = {
    "1": {
        "name": "LLM_Empathetic",
        "description": "Patient-Centered Communication",
        "prompt": EMPATHETIC_PROMPT
    },
    "2": {
        "name": "LLM_Professional",
        "description": "Clinical/Less Patient-Centered Communication",
        "prompt": PROFESSIONAL_PROMPT
    }
}

# =============================================================================
# RESEARCHER WEB INTERFACE (HTML)
# =============================================================================

RESEARCHER_INTERFACE_HTML = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>VR AI Doctor Study — Researcher Control Panel</title>
    <style>
        * {
            box-sizing: border-box;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
        }
        body {
            max-width: 800px;
            margin: 0 auto;
            padding: 40px 20px;
            background: #f5f5f5;
        }
        h1 {
            color: #333;
            border-bottom: 3px solid #4a90d9;
            padding-bottom: 10px;
        }
        .card {
            background: white;
            border-radius: 8px;
            padding: 24px;
            margin-bottom: 20px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .card h2 {
            margin-top: 0;
            color: #4a90d9;
        }
        label {
            display: block;
            margin-bottom: 8px;
            font-weight: 600;
            color: #333;
        }
        input[type="text"] {
            width: 100%;
            padding: 12px;
            border: 2px solid #ddd;
            border-radius: 6px;
            font-size: 16px;
            margin-bottom: 16px;
        }
        input[type="text"]:focus {
            border-color: #4a90d9;
            outline: none;
        }
        .condition-options {
            display: flex;
            gap: 16px;
            margin-bottom: 16px;
        }
        .condition-option {
            flex: 1;
            padding: 20px;
            border: 2px solid #ddd;
            border-radius: 8px;
            cursor: pointer;
            transition: all 0.2s;
        }
        .condition-option:hover {
            border-color: #4a90d9;
            background: #f8fafc;
        }
        .condition-option.selected {
            border-color: #4a90d9;
            background: #e8f4fd;
        }
        .condition-option input {
            display: none;
        }
        .condition-option h3 {
            margin: 0 0 8px 0;
            color: #333;
        }
        .condition-option p {
            margin: 0;
            color: #666;
            font-size: 14px;
        }
        button {
            width: 100%;
            padding: 16px;
            background: #4a90d9;
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 18px;
            font-weight: 600;
            cursor: pointer;
            transition: background 0.2s;
        }
        button:hover {
            background: #357abd;
        }
        button:disabled {
            background: #ccc;
            cursor: not-allowed;
        }
        .status {
            padding: 16px;
            border-radius: 8px;
            margin-top: 16px;
        }
        .status.success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }
        .status.error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }
        .status.info {
            background: #e8f4fd;
            color: #0c5460;
            border: 1px solid #bee5eb;
        }
        .current-session {
            background: #fff3cd;
            border: 1px solid #ffc107;
            padding: 16px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        .current-session h3 {
            margin: 0 0 8px 0;
            color: #856404;
        }
        .endpoint-info {
            background: #f8f9fa;
            padding: 12px;
            border-radius: 6px;
            font-family: monospace;
            font-size: 14px;
            margin-top: 8px;
        }
        .hidden {
            display: none;
        }
    </style>
</head>
<body>
    <h1>🏥 VR AI Doctor Study — Researcher Control Panel</h1>
    
    <div id="currentSession" class="current-session hidden">
        <h3>⚡ Active Session</h3>
        <p><strong>Participant:</strong> <span id="activeParticipant"></span></p>
        <p><strong>Condition:</strong> <span id="activeCondition"></span></p>
        <p><strong>Started:</strong> <span id="activeTime"></span></p>
        <button onclick="endSession()" style="background: #dc3545; margin-top: 10px;">End Session</button>
    </div>
    
    <div class="card">
        <h2>📋 Start New Session</h2>
        
        <label for="participantId">Participant ID:</label>
        <input type="text" id="participantId" placeholder="e.g., 001, P001, etc.">
        
        <label>Select Condition:</label>
        <div class="condition-options">
            <label class="condition-option" id="option1">
                <input type="radio" name="condition" value="1">
                <h3>1️⃣ Empathetic</h3>
                <p>Patient-Centered Communication</p>
            </label>
            <label class="condition-option" id="option2">
                <input type="radio" name="condition" value="2">
                <h3>2️⃣ Non-Empathetic</h3>
                <p>Clinical/Professional Communication</p>
            </label>
        </div>
        
        <button onclick="startSession()">🚀 Start Session</button>
        
        <div id="statusMessage" class="status hidden"></div>
    </div>
    
    <div class="card">
        <h2>📡 Unity Connection Info</h2>
        <p>Once a session is started, Unity should call:</p>
        <div class="endpoint-info">
            GET http://127.0.0.1:5050/get_token
        </div>
        <p style="margin-top: 12px; color: #666; font-size: 14px;">
            This returns the ephemeral token and system prompt for the selected condition.
        </p>
    </div>
    
    <div class="card">
        <h2>📊 Session History</h2>
        <button onclick="loadHistory()" style="background: #6c757d;">Load Saved Conversations</button>
        <div id="historyList" style="margin-top: 16px;"></div>
    </div>

    <script>
        // Handle condition selection UI
        document.querySelectorAll('.condition-option').forEach(option => {
            option.addEventListener('click', function() {
                document.querySelectorAll('.condition-option').forEach(o => o.classList.remove('selected'));
                this.classList.add('selected');
                this.querySelector('input').checked = true;
            });
        });
        
        // Check for active session on page load
        checkActiveSession();
        
        async function checkActiveSession() {
            try {
                const response = await fetch('/session_status');
                const data = await response.json();
                if (data.is_active) {
                    showActiveSession(data);
                }
            } catch (error) {
                console.error('Error checking session:', error);
            }
        }
        
        function showActiveSession(data) {
            document.getElementById('currentSession').classList.remove('hidden');
            document.getElementById('activeParticipant').textContent = data.participant_id;
            document.getElementById('activeCondition').textContent = data.condition_name + ' (' + data.condition_description + ')';
            document.getElementById('activeTime').textContent = new Date(data.start_time).toLocaleString();
        }
        
        async function startSession() {
            const participantId = document.getElementById('participantId').value.trim();
            const conditionInput = document.querySelector('input[name="condition"]:checked');
            
            if (!participantId) {
                showStatus('Please enter a Participant ID', 'error');
                return;
            }
            if (!conditionInput) {
                showStatus('Please select a condition', 'error');
                return;
            }
            
            try {
                const response = await fetch('/start_session', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        participant_id: participantId,
                        condition: conditionInput.value
                    })
                });
                
                const data = await response.json();
                
                if (data.status === 'success') {
                    showStatus(`✅ Session started for ${data.participant_id} — Condition: ${data.condition_name}`, 'success');
                    showActiveSession({
                        participant_id: data.participant_id,
                        condition_name: data.condition_name,
                        condition_description: data.condition_description,
                        start_time: new Date().toISOString()
                    });
                } else {
                    showStatus('Error: ' + data.error, 'error');
                }
            } catch (error) {
                showStatus('Error connecting to server: ' + error, 'error');
            }
        }
        
        async function endSession() {
            try {
                const response = await fetch('/end_session', { method: 'POST' });
                const data = await response.json();
                
                if (data.status === 'ended') {
                    showStatus('Session ended', 'info');
                    document.getElementById('currentSession').classList.add('hidden');
                }
            } catch (error) {
                showStatus('Error ending session: ' + error, 'error');
            }
        }
        
        async function loadHistory() {
            try {
                const response = await fetch('/list_conversations');
                const data = await response.json();
                
                const historyDiv = document.getElementById('historyList');
                if (data.conversations.length === 0) {
                    historyDiv.innerHTML = '<p style="color: #666;">No saved conversations yet.</p>';
                } else {
                    let html = '<table style="width: 100%; border-collapse: collapse;">';
                    html += '<tr style="border-bottom: 2px solid #ddd;"><th style="text-align: left; padding: 8px;">Participant</th><th style="text-align: left; padding: 8px;">Condition</th><th style="text-align: left; padding: 8px;">Date</th></tr>';
                    data.conversations.forEach(conv => {
                        html += `<tr style="border-bottom: 1px solid #eee;">
                            <td style="padding: 8px;">${conv.participant_id}</td>
                            <td style="padding: 8px;">${conv.condition}</td>
                            <td style="padding: 8px;">${conv.date || 'N/A'}</td>
                        </tr>`;
                    });
                    html += '</table>';
                    historyDiv.innerHTML = html;
                }
            } catch (error) {
                document.getElementById('historyList').innerHTML = '<p style="color: red;">Error loading history</p>';
            }
        }
        
        function showStatus(message, type) {
            const statusDiv = document.getElementById('statusMessage');
            statusDiv.textContent = message;
            statusDiv.className = 'status ' + type;
            statusDiv.classList.remove('hidden');
        }
    </script>
</body>
</html>
"""

# =============================================================================
# API ENDPOINTS
# =============================================================================

@app.route('/')
def researcher_interface():
    """Serve the researcher control panel."""
    return render_template_string(RESEARCHER_INTERFACE_HTML)


@app.route('/start_session', methods=['POST'])
def start_session():
    """
    Start a new session with participant ID and condition.
    Called by researcher before participant begins.
    """
    global current_session
    
    data = request.json
    participant_id = data.get('participant_id')
    condition = data.get('condition')
    
    # Validate inputs
    if not participant_id:
        return jsonify({"status": "error", "error": "Participant ID is required"}), 400
    if condition not in CONDITIONS:
        return jsonify({"status": "error", "error": "Invalid condition. Use '1' or '2'"}), 400
    
    # Set session state
    current_session = {
        "participant_id": participant_id,
        "condition": condition,
        "condition_name": CONDITIONS[condition]["name"],
        "condition_description": CONDITIONS[condition]["description"],
        "start_time": datetime.now().isoformat(),
        "is_active": True
    }
    
    print(f"\n{'='*60}")
    print(f"✅ SESSION STARTED")
    print(f"   Participant: {participant_id}")
    print(f"   Condition: {CONDITIONS[condition]['name']}")
    print(f"   Description: {CONDITIONS[condition]['description']}")
    print(f"   Model: {REALTIME_MODEL}")
    print(f"{'='*60}\n")
    
    return jsonify({
        "status": "success",
        "participant_id": participant_id,
        "condition": condition,
        "condition_name": CONDITIONS[condition]["name"],
        "condition_description": CONDITIONS[condition]["description"]
    })


@app.route('/session_status', methods=['GET'])
def session_status():
    """Get current session status."""
    return jsonify(current_session)


@app.route('/end_session', methods=['POST'])
def end_session():
    """End the current session."""
    global current_session
    
    current_session = {
        "participant_id": None,
        "condition": None,
        "condition_name": None,
        "condition_description": None,
        "start_time": None,
        "is_active": False
    }
    
    print("\n⏹️  Session ended by researcher\n")
    
    return jsonify({"status": "ended"})


@app.route('/get_token', methods=['GET'])
def get_token():
    """
    Generate ephemeral token for GPT Realtime API.
    Called by Unity app to establish WebRTC connection.
    Returns token AND the appropriate system prompt.

    CHANGED: Uses the GA endpoint /v1/realtime/client_secrets and
    a pinned model snapshot (REALTIME_MODEL) instead of an alias.
    The token value is now returned at the top-level "value" field
    of the response (ek_... prefix).
    """
    # Check if session is active
    if not current_session["is_active"]:
        return jsonify({
            "error": "No active session. Start a session first via the researcher interface."
        }), 400
    
    # Check API key
    if not OPENAI_API_KEY:
        return jsonify({"error": "OPENAI_API_KEY not configured"}), 500
    
    try:
        system_prompt = CONDITIONS[current_session["condition"]]["prompt"]

        # ── CHANGED (GA API): endpoint is /v1/realtime/client_secrets.
        # The session body is nested under a "session" key, with audio config
        # under "audio.output". The voice is set here so the model uses it
        # from the moment the WebRTC connection opens, before session.update.
        response = requests.post(
            "https://api.openai.com/v1/realtime/client_secrets",
            headers={
                "Authorization": f"Bearer {OPENAI_API_KEY}",
                "Content-Type": "application/json"
            },
            json={
                "session": {
                    "type": "realtime",
                    "model": REALTIME_MODEL,
                    "audio": {
                        "output": {
                            "voice": "shimmer"
                        }
                    },
                    "instructions": system_prompt
                }
            }
        )
        
        if response.status_code != 200:
            print(f"OpenAI API Error: {response.status_code} - {response.text}")
            return jsonify({
                "error": f"Failed to get token from OpenAI: {response.status_code}",
                "detail": response.text
            }), 500
        
        token_data = response.json()

        # GA API: ephemeral key is at the top-level "value" field (ek_...).
        # Fall back to the old beta shape client_secret.value just in case.
        token_value = token_data.get("value") or token_data.get("client_secret", {}).get("value")
        if not token_value:
            print(f"Unexpected token response structure: {token_data}")
            return jsonify({"error": "Could not extract token from OpenAI response"}), 500
        
        print(f"🎟️  Token generated for participant {current_session['participant_id']} "
              f"(model: {REALTIME_MODEL})")
        
        return jsonify({
            "token": token_value,
            "system_prompt": system_prompt,
            "participant_id": current_session["participant_id"],
            "condition": current_session["condition"],
            "condition_name": current_session["condition_name"],
            "condition_description": current_session["condition_description"]
        })
        
    except Exception as e:
        print(f"Error generating token: {str(e)}")
        return jsonify({"error": str(e)}), 500


@app.route('/save_conversation', methods=['POST'])
def save_conversation():
    """
    Save conversation transcript to JSON file.
    Called by Unity app when consultation ends.
    """
    if not current_session["participant_id"]:
        return jsonify({"error": "No active session"}), 400
    
    data = request.json
    
    save_data = {
        "participant_id": current_session["participant_id"],
        "condition": {
            "code": current_session["condition"],
            "name": current_session["condition_name"],
            "description": current_session["condition_description"]
        },
        "model": REALTIME_MODEL,
        "session_start": current_session["start_time"],
        "session_end": datetime.now().isoformat(),
        "transcript": data.get("transcript", []),
        "treatment_accepted": data.get("treatment_accepted", None),
        "consultation_summary": data.get("summary", "")
    }
    
    # Generate filename
    filename = f"LLMconvo_ppt{current_session['participant_id']}.json"
    filepath = os.path.join(CONVERSATIONS_DIR, filename)
    
    # Save to file
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(save_data, f, indent=2, ensure_ascii=False)
    
    print(f"💾 Conversation saved: {filepath}")
    
    return jsonify({
        "status": "saved",
        "filename": filename,
        "filepath": filepath
    })


@app.route('/list_conversations', methods=['GET'])
def list_conversations():
    """List all saved conversations."""
    conversations = []
    
    for filename in os.listdir(CONVERSATIONS_DIR):
        if filename.endswith('.json'):
            filepath = os.path.join(CONVERSATIONS_DIR, filename)
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    conversations.append({
                        "filename": filename,
                        "participant_id": data.get("participant_id", "Unknown"),
                        "condition": data.get("condition", {}).get("name", "Unknown"),
                        "date": data.get("session_start", "N/A")
                    })
            except:
                conversations.append({
                    "filename": filename,
                    "participant_id": "Error reading",
                    "condition": "Error",
                    "date": "N/A"
                })
    
    return jsonify({"conversations": sorted(conversations, key=lambda x: x["filename"])})


# =============================================================================
# MAIN
# =============================================================================

if __name__ == '__main__':
    # Use port 5050 to avoid conflict with macOS AirPlay (which uses 5000)
    PORT = 5050
    HOST = '127.0.0.1'  # Use IP instead of 'localhost' for better compatibility
    
    print("\n" + "=" * 60)
    print("  🏥 VR AI DOCTOR STUDY — FLASK SERVER")
    print("=" * 60)
    
    if not OPENAI_API_KEY:
        print("\n⚠️  WARNING: OPENAI_API_KEY not found!")
        print("   Create a .env file with: OPENAI_API_KEY=sk-your-key-here")
    else:
        print(f"\n✅ OpenAI API Key loaded (ends with ...{OPENAI_API_KEY[-4:]})")
    
    print(f"\n🤖 Realtime model: {REALTIME_MODEL}")
    print(f"📁 Conversations will be saved to: {CONVERSATIONS_DIR}/")
    print("\n📡 Endpoints:")
    print("   GET  /              → Researcher web interface")
    print("   POST /start_session → Start new participant session")
    print("   GET  /get_token     → Get ephemeral token for Unity")
    print("   POST /save_conversation → Save transcript")
    print("   GET  /list_conversations → List all saved files")
    print("\n" + "-" * 60)
    print(f"  ➡️  Open http://127.0.0.1:{PORT} in your browser")
    print(f"      (or try http://localhost:{PORT})")
    print("-" * 60 + "\n")
    
    app.run(host=HOST, port=PORT, debug=True)