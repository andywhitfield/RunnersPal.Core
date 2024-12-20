using System.ComponentModel.DataAnnotations;

namespace RunnersPal.Core.Models;

public class UserAccountAuthentication
{
    public int Id { get; set; }
    public int UserAccountId { get; set; }
    [Required]
    public required UserAccount UserAccount { get; set; }
    public byte[]? CredentialId { get; set; }
    public byte[]? PublicKey { get; set; }
    public byte[]? UserHandle { get; set; }
    public uint? SignatureCount { get; set; }
}