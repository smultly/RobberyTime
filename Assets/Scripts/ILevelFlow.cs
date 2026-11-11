namespace LibraryEscape
{
    /// <summary>
    /// Общее состояние уровня, которое нужно игроку: можно ли двигаться и идёт ли игра.
    /// Реализуется Level1Game и Level2Game, чтобы PlayerMover2D работал на любом уровне.
    /// </summary>
    public interface ILevelFlow
    {
        bool IsRunning { get; }
        bool IsPaused { get; }
        bool MovementBlocked { get; }
    }
}
