using System.ComponentModel.DataAnnotations;

namespace UTH.Library.Api.Contracts.Notifications;

public sealed record SmtpTestRequest(
    [Required, EmailAddress, StringLength(256)] string Recipient,
    bool SendMessage = true);
