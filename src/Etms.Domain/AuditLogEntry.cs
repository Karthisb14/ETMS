namespace Etms.Domain;

// Durable audit trail (KAN-12 — HIPAA/PII accountability). The legacy app had no
// audit trail at all.
public class AuditLogEntry
{
    public long AuditLogEntryId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string User { get; set; } = "anonymous";
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}
