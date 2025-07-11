using PMCLIB;

namespace PlanarMoverControl
{
    public class Mover
    {
        public int Id { get; private set; }

        public Mover(int id) {
            Console.WriteLine($"Initiated mover with id: {id}!");
            Id = id;
        }
    }
}
