using System;

namespace DTT.Doctor.Services.Models
{
    // AI Symptom Checker + Escalate to Staff — xem Tai Lieu/ai_chatbot_luong_nghiep_vu.md
    // 1 mục trong hàng chờ tiếp nhận của Lễ tân (GET /api/chat/staff/queue)
    public class ChatQueueItem
    {
        [Newtonsoft.Json.JsonProperty("sessionId")]
        public int SessionId { get; set; }
        [Newtonsoft.Json.JsonProperty("patientId")]
        public int PatientId { get; set; }
        [Newtonsoft.Json.JsonProperty("patientName")]
        public string PatientName { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("suggestedSpecialtyName")]
        public string? SuggestedSpecialtyName { get; set; }
        [Newtonsoft.Json.JsonProperty("lastMessagePreview")]
        public string? LastMessagePreview { get; set; }
        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
        [Newtonsoft.Json.JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    // 1 tin nhắn trong phiên chat (GET /api/chat/sessions/{id}/messages)
    public class ChatMessageModel
    {
        [Newtonsoft.Json.JsonProperty("messageId")]
        public int MessageId { get; set; }
        [Newtonsoft.Json.JsonProperty("senderType")]
        public string SenderType { get; set; } = string.Empty; // Patient | AI | Staff
        [Newtonsoft.Json.JsonProperty("senderUserId")]
        public string? SenderUserId { get; set; }
        [Newtonsoft.Json.JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
