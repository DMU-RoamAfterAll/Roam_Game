using System;

public static class EventManager {
    public static event Action AreaMoveFinished;
    public static void RaiseAreaMoveFinished() => AreaMoveFinished?.Invoke();
}