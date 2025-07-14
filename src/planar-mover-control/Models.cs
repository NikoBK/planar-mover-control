using PMCLIB;
using System.Numerics;

namespace PlanarMoverControl
{
    public class Mover
    {
        public int Id { get; private set; }
        private XBotCommands? _cmds;

        public Mover(int id, XBotCommands? cmds = null) {
            Console.WriteLine($"Initiated mover with id: {id}!");
            Id = id;
            _cmds = cmds;
            if (_cmds != null) {
                Console.WriteLine($"Mover ({id}) initialized at coords: (X:{(float)_cmds.GetAllXbotInfo().AllXbotInfoList[id - 1].XPos}; Y:{(float)_cmds.GetAllXbotInfo().AllXbotInfoList[id - 1].YPos})");
            }
        }

        private void LoopedMovementTest()
        {
            if (_cmds == null) {
                Console.WriteLine($"<Mover{Id}>: Failed to execute movement, xbot commands reference is null!");
                return;
            }
        }

        public async Task MoveTo(Vector2 pos,
                            ushort cmdLabel = 0, POSITIONMODE posMode = POSITIONMODE.ABSOLUTE, LINEARPATHTYPE pathType = LINEARPATHTYPE.DIRECT,
                            double finalSpdMetersPs = 0, double maxSpdMetersPs = 0.5, double maxAccelerationMetersPs2 = 10) {
            if (_cmds == null) {
                Console.WriteLine($"<Mover{Id}>: Failed to execute movement, xbot commands reference is null!");
                return;
            }
            Console.WriteLine($"Shuttle {Id} is moving!");
            _cmds.LinearMotionSI(cmdLabel, Id, posMode, pathType, pos.X, pos.Y, finalSpdMetersPs, maxSpdMetersPs, maxAccelerationMetersPs2);
            Console.WriteLine("finished moving");

            await Task.Delay(1000); // Buffer time to get the mover moving.
            Console.WriteLine("time delay of 1s passed (async task)");
        }
    }
}
