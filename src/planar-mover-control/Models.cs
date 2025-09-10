using PMCLIB;
using System.Numerics;
using log4net;

namespace PlanarMoverControl
{
    public class Mover
    {
        public int Id { get; private set; }
        private XBotCommands? _cmds;

        // Logging
        private static readonly ILog _log = LogManager.GetLogger(typeof(Mover));

        public Mover(int id, XBotCommands? cmds = null, bool verbose = false) {
            _log.Info($"Initiated mover with id: {id}!");
            Id = id;
            _cmds = cmds;
            if (_cmds != null && verbose) {
                _log.Debug($"Mover ({id}) initialized at coords: (X:{(float)_cmds.GetAllXbotInfo().AllXbotInfoList[id - 1].XPos}; Y:{(float)_cmds.GetAllXbotInfo().AllXbotInfoList[id - 1].YPos})");
            }
        }

        public void GetPosition() {
            if (_cmds != null) {
                _log.Debug($"Mover ({Id}) position: (X:{(float)_cmds.GetAllXbotInfo().AllXbotInfoList[Id - 1].XPos}; Y:{(float)_cmds.GetAllXbotInfo().AllXbotInfoList[Id - 1].YPos})");
            }
        }

        private void LoopedMovementTest()
        {
            if (_cmds == null) {
                _log.Warn($"Failed to execute movement for mover with id: {Id}, xbot commands reference is null!");
                return;
            }
        }

        public async Task MoveTo(Vector2 pos,
                            ushort cmdLabel = 0, POSITIONMODE posMode = POSITIONMODE.ABSOLUTE, LINEARPATHTYPE pathType = LINEARPATHTYPE.DIRECT,
                            double finalSpdMetersPs = 0, double maxSpdMetersPs = 0.5, double maxAccelerationMetersPs2 = 10) {
            if (_cmds == null) {
                _log.Warn($"Failed to execute movement for mover with id: {Id}, xbot commands reference is null!");
                return;
            }
            _log.Debug($"Shuttle {Id} is moving!");
            _cmds.LinearMotionSI(cmdLabel, Id, posMode, pathType, pos.X, pos.Y, finalSpdMetersPs, maxSpdMetersPs, maxAccelerationMetersPs2);
            _log.Debug("finished moving");

            await Task.Delay(1000); // Buffer time to get the mover moving.
            _log.Debug("time delay of 1s passed (async task)");
        }
    }
}
