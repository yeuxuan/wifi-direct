namespace ConsoleApp27
{
    internal class Program
    {
        static void Main(string[] args)
        {

            ClientSocket clientSocket = new ClientSocket("192.168.137.31", "0");

            clientSocket.Start().GetAwaiter();

            clientSocket.MsgReceivedAction = (x) =>
            {
                Console.WriteLine("1");
            };

            clientSocket.OnStartFailed += (x) =>
            {
                Console.WriteLine("2");
            };

            clientSocket.OnStartSuccess += () => 
            {
                Console.WriteLine("3");
            };

            Console.WriteLine("Hello, World!");

            Console.ReadKey();
        }
    }
}