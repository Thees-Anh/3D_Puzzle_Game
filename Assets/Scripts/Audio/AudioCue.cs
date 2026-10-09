namespace PuzzleRoom.Audio
{
    public enum AudioCategory { Music, Sfx, Ambience, UI }

    public enum AudioCue
    {
        UIHover, UIClick, UIOpen, UIClose, Pause, Resume, Confirm, Error,
        FootstepWood,
        ChairSit, ChairStand,
        PickupUV, PickupKey, PickupFuse, UVOn, UVOff,
        SafeKey, SafeClear, SafeSubmit, SafeWrong, SafeCorrect, SafeUnlock, SafeDoor,
        SymbolPress, SymbolWrong, SymbolComplete, MechanismUnlock,
        BookSelect, BookPlace, InvalidMove, PuzzleReset, HanoiComplete, DrawerOpen,
        CabinetLocked, CabinetUnlock, CabinetOpen, CabinetClose,
        FuseInsert, CircuitRotate, GridComplete, OutputActivate, WrongActivation, PowerRestored,
        DoorLocked, DoorOpen, DoorClose, ExitUnlock,
        RoomAmbience, PoweredAmbience, GameplayMusic, Victory, MenuMusic
    }
}
