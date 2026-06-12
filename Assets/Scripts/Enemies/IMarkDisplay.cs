using UnityEngine;

/// <summary>
/// Implemented by every status mark that renders a sigil sprite above an enemy.
/// <see cref="MarkDisplayController"/> queries this interface each frame to lay out
/// all active marks at a uniform size in a centered row.
/// </summary>
public interface IMarkDisplay
{
    /// <summary>True while this mark's sprite should be visible.</summary>
    bool IsMarkVisible { get; }

    /// <summary>The SpriteRenderer used to draw this mark's icon.</summary>
    SpriteRenderer MarkSpriteRenderer { get; }
}
