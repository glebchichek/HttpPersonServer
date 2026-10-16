using System.Globalization;
using System.Text;

namespace HttpPersonClient
{
    class Program
    {
        static PersonApiClient api;

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("============================================================");
            Console.WriteLine("HTTP-КЛИЕНТ CRUD (HttpClient)");
            Console.WriteLine("============================================================");
            Console.WriteLine();

            Console.Write("Адрес сервера (по умолчанию http://localhost:5000/): ");
            string input = Console.ReadLine();
            string baseUrl = string.IsNullOrWhiteSpace(input)
                ? "http://localhost:5000/"
                : input.Trim();

            api = new PersonApiClient(baseUrl);

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("============================================================");
                Console.WriteLine("МЕНЮ:");
                Console.WriteLine("  1. Показать всех пользователей  (GET)");
                Console.WriteLine("  2. Найти по Id                   (GET)");
                Console.WriteLine("  3. Добавить                      (POST)");
                Console.WriteLine("  4. Обновить                      (PUT)");
                Console.WriteLine("  5. Удалить                       (DELETE)");
                Console.WriteLine("  0. Выход");
                Console.WriteLine("============================================================");
                Console.Write("Ваш выбор: ");

                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1": await ShowAllAsync(); break;
                        case "2": await ShowByIdAsync(); break;
                        case "3": await CreateAsync(); break;
                        case "4": await UpdateAsync(); break;
                        case "5": await DeleteAsync(); break;
                        case "0": exit = true; break;
                        default:
                            Console.WriteLine("Неверный выбор");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ОШИБКА: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("До свидания!");
        }
        static async Task ShowAllAsync()
        {
            Console.WriteLine();
            Console.WriteLine("Загрузка списка...");

            var persons = await api.GetAllAsync();

            Console.WriteLine();
            Console.WriteLine($"{"Id",-38} {"Имя",-25} {"Дата рожд.",-12} {"Зарплата",12}");
            Console.WriteLine(new string('-', 92));

            foreach (var p in persons)
            {
                Console.WriteLine($"{p.Id,-38} {p.Name,-25} " +
                                  $"{p.BirthDate:dd.MM.yyyy,-12} " +
                                  $"{p.Salary,12:N2}");
            }

            Console.WriteLine(new string('-', 92));
            Console.WriteLine($"Всего: {persons.Count}");
        }
        static async Task ShowByIdAsync()
        {
            Console.Write("Введите Id: ");
            string id = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(id)) return;

            var p = await api.GetByIdAsync(id);

            Console.WriteLine();
            Console.WriteLine("Найден:");
            Console.WriteLine($"  Id:           {p.Id}");
            Console.WriteLine($"  Имя:          {p.Name}");
            Console.WriteLine($"  Дата рожд.:   {p.BirthDate:dd.MM.yyyy}");
            Console.WriteLine($"  Зарплата:     {p.Salary:N2}");
        }
        static async Task CreateAsync()
        {
            Console.WriteLine();
            Console.WriteLine("--- СОЗДАНИЕ ---");

            Console.Write("Имя: ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) { Console.WriteLine("Имя обязательно."); return; }

            DateTime birth = ReadDate("Дата рождения");
            decimal salary = ReadDecimal("Зарплата");

            var p = new Person
            {
                Name = name,
                BirthDate = birth,
                Salary = salary
            };

            var created = await api.CreateAsync(p);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Создан: Id = {created.Id}");
            Console.ResetColor();
        }
        static async Task UpdateAsync()
        {
            Console.WriteLine();
            Console.WriteLine("--- ОБНОВЛЕНИЕ ---");

            Console.Write("Id для обновления: ");
            string id = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(id)) return;

            var current = await api.GetByIdAsync(id);
            Console.WriteLine($"Текущие данные: {current.Name}, " +
                              $"{current.BirthDate:dd.MM.yyyy}, {current.Salary:N2}");

            Console.Write($"Новое имя (Enter — оставить \"{current.Name}\"): ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) name = current.Name;

            Console.Write($"Новая дата (Enter — оставить \"{current.BirthDate:dd.MM.yyyy}\"): ");
            string dateStr = Console.ReadLine()?.Trim();
            DateTime birth = string.IsNullOrEmpty(dateStr)
                ? current.BirthDate
                : ParseDate(dateStr);

            Console.Write($"Новая зарплата (Enter — оставить \"{current.Salary:N2}\"): ");
            string salStr = Console.ReadLine()?.Trim();
            decimal salary = string.IsNullOrEmpty(salStr)
                ? current.Salary
                : ParseDecimal(salStr);

            var updated = new Person
            {
                Name = name,
                BirthDate = birth,
                Salary = salary
            };

            var result = await api.UpdateAsync(id, updated);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Обновлено: {result.Name}, " +
                              $"{result.BirthDate:dd.MM.yyyy}, {result.Salary:N2}");
            Console.ResetColor();
        }
        static async Task DeleteAsync()
        {
            Console.WriteLine();
            Console.WriteLine("--- УДАЛЕНИЕ ---");

            Console.Write("Id для удаления: ");
            string id = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(id)) return;

            var p = await api.GetByIdAsync(id);
            Console.WriteLine($"Будет удалён: {p.Name} ({p.BirthDate:dd.MM.yyyy}, {p.Salary:N2})");
            Console.Write("Подтвердить? (y/n): ");
            string confirm = Console.ReadLine()?.Trim().ToLower();
            if (confirm != "y" && confirm != "yes" && confirm != "д" && confirm != "да")
            {
                Console.WriteLine("Отменено.");
                return;
            }

            await api.DeleteAsync(id);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Удалено.");
            Console.ResetColor();
        }
        static DateTime ReadDate(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (дд.мм.гггг): ");
                string s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s)) continue;
                try { return ParseDate(s); }
                catch { Console.WriteLine("Неверный формат. Пример: 15.05.1990"); }
            }
        }

        static DateTime ParseDate(string s)
        {
            return DateTime.ParseExact(s, "dd.MM.yyyy",
                CultureInfo.InvariantCulture);
        }

        static decimal ReadDecimal(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (например, 50000): ");
                string s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s)) continue;
                try { return ParseDecimal(s); }
                catch { Console.WriteLine("Неверное число."); }
            }
        }

        static decimal ParseDecimal(string s)
        {
            return decimal.Parse(s.Replace(',', '.'),
                CultureInfo.InvariantCulture);
        }
    }
}
