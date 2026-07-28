using System;
using System.Collections.Generic;

namespace AIDoctor.Data
{
    [Serializable]
    public class TokenResponse
    {
        public string token;
        public string system_prompt;
        public string participant_id;
        public string condition;
        public string condition_name;
        public string error;
    }

    [Serializable]
    public class SessionStartRequest
    {
        public string participant_id;
        public string condition;
    }

    [Serializable]
    public class SessionStartResponse
    {
        public string status;
        public string participant_id;
        public string condition;
        public string condition_name;
        public string error;
    }

    [Serializable]
    public class ConversationMessage
    {
        public string role;      // "user" or "assistant"
        public string content;
        public string timestamp;
    }

    [Serializable]
    public class SaveConversationRequest
    {
        public List<ConversationMessage> transcript;
        public bool treatment_accepted;
        public string summary;
    }

    [Serializable]
    public class SaveConversationResponse
    {
        public string status;
        public string filename;
        public string error;
    }
}
