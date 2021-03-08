using System;

namespace GrassPls
{
    class GrassStats {
        // Maps from GrassState (ex: Cut) to number of grass in that state. I'm
        // curious if there's a way to create a mutable-tuple-of-sorts with the
        // correct size (the number of enum values in GrassState)... but I
        // don't think there is.
        private int[] GrassInState;

        public GrassStats() {
            GrassInState = new int[Enum.GetNames(typeof(GrassState)).Length];
        }

        public int Total() {
            int sum = 0;
            foreach (int numGrass in GrassInState) {
                sum += numGrass;
            }
            return sum;
        }

        public int GetNumGrassInState(GrassState state) {
            return GrassInState[(int)state];
        }

        public int this[GrassState state] {
            get => GetNumGrassInState(state);
        }

        public void HandleUpdate(GrassState? oldState, GrassState newState) {
            if (oldState is GrassState oldStateValue) {
                GrassInState[(int)oldStateValue] -= 1;
            }

            GrassInState[(int)newState] += 1;
        }

        public override string ToString() {
            string result = "GrassStats(";
            foreach (GrassState state in Enum.GetValues(typeof(GrassState))) {
                result += $"{Enum.GetName(typeof(GrassState), state)}=" +
                          $"{GrassInState[(int)state]}, ";
            }
            return result + ")";
        }
    }
}
