using System.Numerics;

namespace PlanarMoverControl
{
    public class Constants
    {
        public static Dictionary<int, Vector2> MovePointsTest = new Dictionary<int, Vector2>() {
            // Key: Station Position, Value: Pos (X, Y)
            {0, new Vector2(0.060f, 0.060f)},
            {1, new Vector2(0.660f, 0.060f)},
            {2, new Vector2(0.060f, 0.450f)},
            {3, new Vector2(0.660f, 0.450f)},
            {4, new Vector2(0.060f, 0.900f)},
            {5, new Vector2(0.660f, 0.900f)},
        };
    }
}
