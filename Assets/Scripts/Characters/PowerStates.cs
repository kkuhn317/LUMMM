public class PowerStates {
    public enum PowerupState {

        small,
        big,
        power
    }

    // Can you break bricks? Are you at least 2 blocks tall?
    public static bool IsBig(PowerupState state) {
        return state is PowerupState.big or PowerupState.power;
    }

    // Can you die in one hit? Are you at most 1 block tall?
    public static bool IsSmall(PowerupState state) {
        return state is PowerupState.small;
    }

}