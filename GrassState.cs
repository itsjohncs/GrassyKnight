namespace GrassPls
{
    enum GrassState {
        Uncut,
        // A special state that grass might enter if it is struck with the
        // nail but not actually cut in game.
        ShouldBeCut,
        Cut,
    }
}
