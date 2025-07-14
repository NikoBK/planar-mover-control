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
            var logRepo = LogManager.GetRepository(Assembly.GetEntryAssembly()!);
            XmlConfigurator.Configure(logRepo, new FileInfo("log4net.config"));

            _log.Info("info test");
            _log.Warn("warn test");
            _log.Error("error test");

            bool pmcStarted = PMCStartup();
            if (pmcStarted)
            {
                var xbotIds = GetXbotIds();
                foreach (var id in xbotIds)
                {
                    Mover mover = new Mover(id, _xbotCmds);
                    Movers.TryAdd(id, mover);
                }
                Console.WriteLine($"All movers initiated and added to dictionary. Total movers: {Movers.Count}");

                // TESTING
                //Test();
                await Test2(id: 1);
            }
            else {
                Console.WriteLine("Failed to start PMC, false return value");
                return;
            }
        }

        public static List<int> GetXbotIds() {
            return _xbotCmds.GetXBotIDS().XBotIDsArray.ToList();
        }

        private static bool PMCStartup(int expectedMovers = 0) {
            Console.WriteLine("Running PMC startup routine...");

            // Connect to PMC
            Console.WriteLine("Trying to connect to the pmc (1/5)...");
            bool connected = _sysCmds.AutoSearchAndConnectToPMC();
            if (!connected) {
                Console.WriteLine("Failed to connect to the pmc");
                return false;
            }
            Console.WriteLine("Successfully conneced to the pmc");

            // Gain mastership
            Console.WriteLine("Trying to gain pmc mastership (2/5)...");
            PMCRTN res = _sysCmds.GainMastership();
            if (res != PMCRTN.ALLOK) {
                Console.WriteLine($"Failed to gain mastership of the pmc. Return value: {res.ToString()}");
                return false;
            }
            Console.WriteLine("Successfully gained mastership of the pmc");

            // Check PMC status and bring it into operation
            Console.WriteLine("Checking pmc status and initiating operational state (3/3)...");
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
                                Console.WriteLine("Activate all movers (xbots)");
                                res = _xbotCmds.ActivateXBOTS();
                                attemptedActivation = true; // Only attempt to send activation command for xbots once
                                if (res != PMCRTN.ALLOK) {
                                    Console.WriteLine($"Failed to activate all movers (xbots). Return value: {res.ToString()}");
                                    return false;
                                }
                            }
                            else {
                                Console.WriteLine("Failed to activate all xbots: Attempt already made");
                                return false;
                            }
                            break;

                        case PMCSTATUS.PMC_FULLCTRL:
                        case PMCSTATUS.PMC_INTELLIGENTCTRL:
                            operational = true;
                            break;

                        default:
                            Console.WriteLine($"Unexpected pmc state. No handler found for state: {state.ToString()}");
                            return false;
                    }
                }
            }

            Console.WriteLine("Succesfully engaged operational pmc state");

            // Check XBOT count
            Console.WriteLine("Checking mover (xbot) count (4/5)...");

            XBotIDs xbotIds = _xbotCmds.GetXBotIDS();
            var xbotCount = xbotIds.XBotCount;
            if (xbotIds.PmcRtn == PMCRTN.ALLOK)
            {
                // Debug
                if (expectedMovers > 0 && xbotCount != expectedMovers) {
                    Console.WriteLine($"Unexpected mover (xbot) count. Expected: {expectedMovers}, detected: {xbotCount}");
                    return false;
                }
            }
            else {
                Console.WriteLine($"Failed to get mover (xbot) ids, pmc returned: {res.ToString()}");
                return false;
            }

            _xbotCmds.StopMotion(0); // Stop all xbot motion as soon as the check is done
            Console.WriteLine("Succesfully checked movers count");

            // Check XBOT states and start XBOT levitation
            Console.WriteLine("Initiating XBOT state and levitation protocol (5/5)...");

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
                            Console.WriteLine($"WARNING: Entered state with no handler implemented. State: {iXBOTStatus.XBOTState.ToString()}");
                            // xbot ready to move
                            break;

                        case XBOTSTATE.XBOT_WAIT:
                        case XBOTSTATE.XBOT_OBSTACLE_DETECTED:
                        case XBOTSTATE.XBOT_HOLDPOSITION:
                            Console.WriteLine($"Error: Failed to stop mover (xbot) motion. State: {iXBOTStatus.XBOTState.ToString()}");
                            return false;

                        case XBOTSTATE.XBOT_DISABLED:
                            Console.WriteLine("Error: Cannot levitate from this XBOT state: {iXBOTStatus.XBOTState.ToString()}");
                            return false;

                        default:
                            Console.WriteLine($"Error: Unexpected mover (xbot) state: {iXBOTStatus.XBOTState.ToString()}");
                            return false;
                    }
                }

                if (!xbotsLevitating && !xbotsTransitioning)
                {
                    if (!levitationAttempted)
                    {
                        Console.WriteLine("Attempting movers (xbots) levitation...");
                        res = _xbotCmds.LevitationCommand(0, LEVITATEOPTIONS.LEVITATE);
                        levitationAttempted = true;
                        if (res != PMCRTN.ALLOK) {
                            Console.WriteLine($"Error: Failed to levitate movers (xbots). Return value: {res.ToString()}");
                            return false;
                        }
                    }
                    else {
                        Console.WriteLine($"Error: cannot levitate all xbots, attempt already made. Return value: {res.ToString()}");
                        return false;
                    }
                }
            }
            Console.WriteLine("All xbots succesfully initiated and levitates");
            Console.WriteLine("Start-up routine has completed (5/5)");
            return true;
        }

        private static async Task Test2(int id) {
            Console.WriteLine($"Running single mover test for mover: {id}");

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
            Console.WriteLine($"Single mover test for mover: {id} has concluded");
        }

        private static async Task Test() { //TODO eventually remove this
            Console.WriteLine("Running movement test...");
            bool running = true;
            int i = 0;

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
                _ = Movers[1].MoveTo(Constants.MovePointsTest[1]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[0]);
                await Task.Delay(2000); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[2]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[1]);
                await Task.Delay(2000); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[3]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[2]);
                await Task.Delay(2000); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[4]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[3]);
                await Task.Delay(2000); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[5]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[4]);
                await Task.Delay(2000); // test time
                _ = Movers[1].MoveTo(Constants.MovePointsTest[0]);
                _ = Movers[2].MoveTo(Constants.MovePointsTest[5]);
                await Task.Delay(2000); // test time

                // Manage the while loop
                i++;
                if (i > 5) {
                    running = false;
                }
            }
            Console.WriteLine("Movement test has concluded");
        }
    }
}
