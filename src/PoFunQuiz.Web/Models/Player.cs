namespace PoFunQuiz.Web.Models;

/// <summary>
/// Represents a quiz player within a game session.
/// </summary>
public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}
