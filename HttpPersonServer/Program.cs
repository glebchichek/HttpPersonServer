using System.Text;

namespace HttpPersonServer
{
    class Program
    {
        static PersonServer server;
        static readonly object consoleLock = new object();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("============================================================");
            Console.WriteLine("HTTP-СЕРВЕР CRUD (HttpListener)");
            Console.WriteLine("============================================================");
            Console.WriteLine();

            Console.Write("Префикс (по умолчанию http://localhost:5000/): ");
            string input = Console.ReadLine();
            string prefix = string.IsNullOrWhiteSpace(input)
                ? "http://localhost:5000/"
                : input.Trim();
            if (!prefix.EndsWith("/")) prefix += "/";

            server = new PersonServer();
            server.LogMessage += Server_LogMessage;

            try
            {
                server.Start(prefix);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось запустить: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Возможные причины:");
                Console.WriteLine("  • Порт занят — попробуй другой порт");
                Console.WriteLine("  • Для http://+:5000/ или http://*:5000/ нужны права администратора");
                Console.WriteLine("  • Префикс должен заканчиваться на /");
                Console.WriteLine();
                Console.WriteLine("Нажмите Enter для выхода...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Доступные эндпоинты:");
            Console.WriteLine($"  GET    {prefix}api/persons");
            Console.WriteLine($"  GET    {prefix}api/persons/{{id}}");
            Console.WriteLine($"  POST   {prefix}api/persons");
            Console.WriteLine($"  PUT    {prefix}api/persons/{{id}}");
            Console.WriteLine($"  DELETE {prefix}api/persons/{{id}}");
            Console.WriteLine();
            Console.WriteLine("Команды сервера:");
            Console.WriteLine("  status  — показать количество пользователей");
            Console.WriteLine("  stop    — остановить сервер");
            Console.WriteLine();

            while (server.IsRunning)
            {
                string cmd = Console.ReadLine();
                if (cmd == null) continue;

                switch (cmd.Trim().ToLower())
                {
                    case "status":
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Сервер работает на {server.Prefix}");
                        break;

                    case "stop":
                        server.Stop();
                        break;

                    case "exit":
                        if (server.IsRunning) server.Stop();
                        return;

                    default:
                        if (!string.IsNullOrWhiteSpace(cmd))
                            Console.WriteLine("Неизвестная команда. Доступно: status, stop, exit");
                        break;
                }
            }

            Console.WriteLine("Сервер завершил работу. Нажмите Enter...");
            Console.ReadLine();
        }

        static void Server_LogMessage(object sender, string message)
        {
            lock (consoleLock)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }
    }
}
