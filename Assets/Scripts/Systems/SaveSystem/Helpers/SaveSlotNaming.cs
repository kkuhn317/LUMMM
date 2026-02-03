public static class SaveSlotNaming
{
    public static char Letter(SaveSlotId id) => (char)('A' + (int)id);

    public static string DefaultNameFor(SaveSlotId id) => $"PLAYER {Letter(id)}";

    // Accepts any default: PLAYER A/B/C (not just the same slot)
    public static bool IsDefaultPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true; // treat empty as default
        name = name.Trim();
        return name == "PLAYER A" || name == "PLAYER B" || name == "PLAYER C";
    }

    public static void EnsureCorrectDefaultNameForSlot(ref string name, SaveSlotId targetSlot)
    {
        if (IsDefaultPlayerName(name))
            name = DefaultNameFor(targetSlot);
    }
}