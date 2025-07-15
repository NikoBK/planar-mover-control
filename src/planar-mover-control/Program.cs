using PMCLIB;
using System.Threading;
using log4net;
using log4net.Config;
using System.IO;
using System.Reflection;

namespace PlanarMoverControl
{
    class Program
    {
        private static SystemCommands _sysCmds = new SystemCommands();
        private static XBotCommands _xbotCmds = new XBotCommands();

        // Key: id, value: mover
        private static Dictionary<int, Mover> Movers = new Dictionary<int, Mover>();

        // Logging
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        public static async Task Main(string[] args)
        {
            // Set up log4net
            var logRepo = LogManager.GetRepository(Assembly.GetEntryAssembly()!);
            XmlConfigurator.Configure(logRepo, new FileInfo("log4net.config"));

            bool pmcStarted = PMCStartup();
            if (pmcStarted)
            {
                var xbotIds = GetXbotIds();
                foreach (var id in xbotIds)
                {
                    Mover mover = new Mover(id, _xbotCmds);
                    Movers.TryAdd(id, mover);
                }
                _log.Info($"All movers initiated and added to dictionary. Total movers: {Movers.Count}");

                // TESTING
                _log.Debug("Running async test procedure...");
                await Test();
                //await Test2(id: 1);
            }
            else {
                _log.Error("Failed to start PMC, false return value");
                return;
            }
            _log.Debug("Program finished running");
        }

        public static List<int> GetXbotIds() {
            return _xbotCmds.GetXBotIDS().XBotIDsArray.ToList();
        }

        private static bool PMCStartup(int expectedMovers = 0) {
            _log.Info("Running PMC startup routine...");

            // Connect to PMC
            _log.Info("Trying to connect to the pmc (1/5)...");
            bool connected = _sysCmds.AutoSearchAndConnectToPMC();
            if (!connected) {
                _log.Error("Failed to connect to the pmc");
                return false;
            }
            _log.Info("Successfully conneced to the pmc");

            // Gain mastership
            _log.Info("Trying to gain pmc mastership (2/5)...");
            PMCRTN res = _sysCmds.GainMastership();
            if (res != PMCRTN.ALLOK) {
                _log.Error($"Failed to gain mastership of the pmc. Return value: {res.ToString()}");
                return false;
            }
            _log.Info("Successfully gained mastership of the pmc");

            // Check PMC status and bring it into operation
            _log.Info("Checking pmc status and initiating operational state (3/3)...");
            PMCSTATUS state = _sysCmds.GetPMCStatus();

            // Check to make sure we only try to start the table in operational state if it is actually ready to.
            if (state != PMCSTATUS.PMC_FULLCTRL && state != PMCSTATUS.PMC_INTELLIGENTCTRL)
            {
                bool operational = false; // indicator for when the pmc is operational
                bool attemptedActivation = false; // Safety measure to prevent infinite looping

                while (!operational)
                {
                    state = _sysCmds.GetPMCStatus();
                    switch(state)
                    {
                        case PMCSTATUS.PMC_ACTIVATING:
                        case PMCSTATUS.PMC_BOOTING:
                        case PMCSTATUS.PMC_DEACTIVATING:
                        case PMCSTATUS.PMC_ERRORHANDLING:
                            operational = false;
                            Thread.Sleep(1000); // Wait 1s before reading pmc status again when the pmc is in a transition state.
                            break;

                        case PMCSTATUS.PMC_ERROR:
                        case PMCSTATUS.PMC_INACTIVE:
                            operational = false;
                            if (!attemptedActivation) {
                                _log.Debug("Activate all movers (xbots)");
                                res = _xbotCmds.ActivateXBOTS();
                                attemptedActivation = true; // Only attempt to send activation command for xbots once
                                if (res != PMCRTN.ALLOK) {
                                    _log.Error($"Failed to activate all movers (xbots). Return value: {res.ToString()}");
                                    return false;
                                }
                            }
                            else {
                                _log.Error("Failed to activate all xbots: Attempt already made");
                                return false;
                            }
                            break;

                        case PMCSTATUS.PMC_FULLCTRL:
                        case PMCSTATUS.PMC_INTELLIGENTCTRL:
                            operational = true;
                            break;

                        default:
                            _log.Error($"Unexpected pmc state. No handler found for state: {state.ToString()}");
                            return false;
                    }
                }
            }

            _log.Info("Succesfully engaged operational pmc state");

            // Check XBOT count
            _log.Info("Checking mover (xbot) count (4/5)...");

            XBotIDs xbotIds = _xbotCmds.GetXBotIDS();
            var xbotCount = xbotIds.XBotCount;
            if (xbotIds.PmcRtn == PMCRTN.ALLOK)
            {
                // Debug
                if (expectedMovers > 0 && xbotCount != expectedMovers) {
                    _log.Error($"Unexpected mover (xbot) count. Expected: {expectedMovers}, detected: {xbotCount}");
                    return false;
                }
            }
            else {
                _log.Error($"Failed to get mover (xbot) ids, pmc returned: {res.ToString()}");
                return false;
            }

            _xbotCmds.StopMotion(0); // Stop all xbot motion as soon as the check is done
            _log.Info("Succesfully checked movers count");

            // Check XBOT states and start XBOT levitation
            _log.Info("Initiating XBOT state and levitation protocol (5/5)...");

            bool xbotsLevitating = false;
            bool levitationAttempted = false;
            bool xbotsTransitioning = false;

            while (!xbotsLevitating)
            {
                xbotsLevitating = true;
                xbotsTransitioning = false;

                for (int i = 0; i < xbotCount; i++)
                {
                    XBotStatus iXBOTStatus = _xbotCmds.GetXbotStatus(xbotIds.XBotIDsArray[i]);

                    switch(iXBOTStatus.XBOTState)
                    {
                        case XBOTSTATE.XBOT_LANDED:
                            xbotsLevitating = false;
                            break;

                        case XBOTSTATE.XBOT_STOPPING:
                        case XBOTSTATE.XBOT_DISCOVERING:
                        case XBOTSTATE.XBOT_MOTION:
                            xbotsLevitating = false;
                            xbotsTransitioning = true;
                            break;

                        case XBOTSTATE.XBOT_IDLE:
                        case XBOTSTATE.XBOT_STOPPED:
                            _log.Warn($"Entered state with no handler implemented. State: {iXBOTStatus.XBOTState.ToString()}");
                            // xbot ready to move
                            break;

                        case XBOTSTATE.XBOT_WAIT:
                        case XBOTSTATE.XBOT_OBSTACLE_DETECTED:
                        case XBOTSTATE.XBOT_HOLDPOSITION:
                            _log.Error($"Failed to stop mover (xbot) motion. State: {iXBOTStatus.XBOTState.ToString()}");
                            return false;

                        case XBOTSTATE.XBOT_DISABLED:
                            _log.Error("Cannot levitate from this XBOT state: {iXBOTStatus.XBOTState.ToString()}");
                            return false;

                        default:
                            _log.Error($"Unexpected mover (xbot) state: {iXBOTStatus.XBOTState.ToString()}");
                            return false;
                    }
                }

                if (!xbotsLevitating && !xbotsTransitioning)
                {
                    if (!levitationAttempted)
                    {
                        _log.Debug("Attempting movers (xbots) levitation...");
                        res = _xbotCmds.LevitationCommand(0, LEVITATEOPTIONS.LEVITATE);
                        levitationAttempted = true;
                        if (res != PMCRTN.ALLOK) {
                            _log.Error($"Failed to levitate movers (xbots). Return value: {res.ToString()}");
                            return false;
                        }
                    }
                    else {
                        _log.Error($"Cannot levitate all xbots, attempt already made. Return value: {res.ToString()}");
                        return false;
                    }
                }
            }
            _log.Info("All xbots succesfully initiated and levitates");
            _log.Info("Start-up routine has completed (5/5)");
            return true;
        }

        private static async Task Test2(int id) {
            _log.Debug($"Running single mover test for mover: {id}");

            for (int i = 0; i < 5; i++) {
                _ = Movers[id].MoveTo(Constants.MovePointsTest[0]);
                await Task.Delay(1500); // test time
                _ = Movers[id].MoveTo(Constants.MovePointsTest[2]);
                await Task.Delay(1500); // test time
                _ = Movers[id].MoveTo(Constants.MovePointsTest[4]);
                await Task.Delay(1500); // test time
                _ = Movers[id].MoveTo(Constants.MovePointsTest[5]);
                await Task.Delay(1500); // test time
                _ = Movers[id].MoveTo(Constants.MovePointsTest[3]);
                await Task.Delay(1500); // test time
                _ = Movers[id].MoveTo(Constants.MovePointsTest[1]);
                await Task.Delay(1500); // test time
            }

            //Movers[id].MoveTo(new System.Numerics.Vector2(0.060f, 0.060f));
            _log.Debug($"Single mover test for mover: {id} has concluded");
        }

        private static async Task Test() { //TODO eventually remove this
            _log.Debug("Running movement test...");
            bool running = true;
            int i = 0;
            int delay = 1000;

            while(running)
            {
                /*
                 *
                 * Optional but something I might want to do later:
                 * var t1 = Movers[1].MoveTo(...);
                 * var t2 = Movers[2].MoveTo(...);
                 * await Task.WhenAll(t1, t2);
                 *
                 */
                _ = Movers[1].MoveTo(Constants.MovePointsTest[0]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[1]);
                await Task.Delay(delay); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[2]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[0]);
                await Task.Delay(delay); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[4]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[2]);
                await Task.Delay(delay); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[5]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[4]);
                await Task.Delay(delay); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[3]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[5]);
                await Task.Delay(delay); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[1]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[3]);
                await Task.Delay(delay); // test time

                // Manage the while loop
                i++;
                if (i > 5) {
                    running = false;
                }
            }
            _log.Debug("Movement test has concluded");
        }
    }
}
