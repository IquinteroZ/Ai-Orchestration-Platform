using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class CreateTicketRequest
{
    [Required, MinLength(3), MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(5), MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;
}