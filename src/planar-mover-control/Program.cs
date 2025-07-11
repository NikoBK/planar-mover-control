using PMCLIB;

namespace PlanarMoverControl
{
    class Program
    {
        private static SystemCommands _sysCmds = new SystemCommands();

        public static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            PMCConnect();
        }

        private static bool PMCConnect() {
            Console.WriteLine("Trying to connect to the pmc...");
            bool connected = _sysCmds.AutoSearchAndConnectToPMC();
            if (!connected) {
                Console.WriteLine("Failed to connect to the pmc");
                return false;
            }
            return true;
        }
    }
}
