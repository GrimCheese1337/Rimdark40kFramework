namespace Core40k;

/// <summary>
/// A sibling comp that pins decorations or colours on the same thing. CompDecorativeBase asks every lock source
/// before removing a decoration, and the customization tabs show locked entries without letting them be changed.
/// </summary>
public interface IDecorationLockSource
{
    bool LocksDecoration(DecorationDef def);

    string LockReason(DecorationDef def);

    bool LocksDecorationColours(DecorationDef def);

    bool LocksColours { get; }

    string ColourLockReason { get; }
}
