using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace HttpPersonServer
{
    public class PersonServer
    {
        private HttpListener listener;
        private CancellationTokenSource cts;
        private bool isRunning;

        private readonly List<Person> persons = new List<Person>();
        private readonly object lockObject = new object();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public event EventHandler<string> LogMessage;

        public bool IsRunning => isRunning;
        public string Prefix { get; private set; }

        public PersonServer()
        {
            persons.Add(new Person
            {
                Name = "Иван Петров",
                BirthDate = new DateTime(1990, 5, 12),
                Salary = 50000m
            });
            persons.Add(new Person
            {
                Name = "Мария Сидорова",
                BirthDate = new DateTime(1985, 8, 23),
                Salary = 75000m
            });
        }

        public void Start(string prefix)
        {
            if (isRunning) throw new InvalidOperationException("Уже запущен");

            Prefix = prefix;
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            isRunning = true;
            cts = new CancellationTokenSource();

            RaiseLog($"Сервер запущен: {prefix}");
            RaiseLog($"Пользователей в базе: {persons.Count}");

            Task.Run(() => ListenLoop(cts.Token));
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            cts?.Cancel();

            try { listener?.Stop(); } catch { }
            try { listener?.Close(); } catch { }

            RaiseLog("Сервер остановлен");
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context, token), token);
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                        RaiseLog($"Ошибка приёма: {ex.Message}");
                }
            }
        }
        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken token)
        {
            var request = context.Request;
            var response = context.Response;

            string method = request.HttpMethod;
            string path = request.Url.AbsolutePath.TrimEnd('/');

            RaiseLog($"{method} {request.Url.PathAndQuery}");

            try
            {
                if (method == "GET" && path.Equals("/api/persons", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleGetAllAsync(response);
                    return;
                }
                if (method == "GET" && path.StartsWith("/api/persons/", StringComparison.OrdinalIgnoreCase))
                {
                    string id = path.Substring("/api/persons/".Length);
                    await HandleGetByIdAsync(response, id);
                    return;
                }
                if (method == "POST" && path.Equals("/api/persons", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleCreateAsync(request, response);
                    return;
                }
                if (method == "PUT" && path.StartsWith("/api/persons/", StringComparison.OrdinalIgnoreCase))
                {
                    string id = path.Substring("/api/persons/".Length);
                    await HandleUpdateAsync(request, response, id);
                    return;
                }
                if (method == "DELETE" && path.StartsWith("/api/persons/", StringComparison.OrdinalIgnoreCase))
                {
                    string id = path.Substring("/api/persons/".Length);
                    await HandleDeleteAsync(response, id);
                    return;
                }
                if (method == "GET" && (path == "" || path == "/"))
                {
                    string help =
                        "Доступные эндпоинты:\n" +
                        "  GET    /api/persons         — список\n" +
                        "  GET    /api/persons/{id}    — один\n" +
                        "  POST   /api/persons         — создать\n" +
                        "  PUT    /api/persons/{id}    — обновить\n" +
                        "  DELETE /api/persons/{id}    — удалить";
                    await WriteTextAsync(response, 200, help);
                    return;
                }

                await WriteTextAsync(response, 404, "Не найдено");
            }
            catch (Exception ex)
            {
                RaiseLog($"Ошибка обработки: {ex.Message}");
                try { await WriteTextAsync(response, 500, ex.Message); } catch { }
            }
            finally
            {
                try { response.OutputStream.Close(); } catch { }
            }
        }

        private async Task HandleGetAllAsync(HttpListenerResponse response)
        {
            List<Person> snapshot;
            lock (lockObject) snapshot = new List<Person>(persons);

            RaiseLog($"  -> Отправка списка ({snapshot.Count} чел.)");
            await WriteJsonAsync(response, 200, snapshot);
        }

        private async Task HandleGetByIdAsync(HttpListenerResponse response, string id)
        {
            Person found;
            lock (lockObject) found = persons.FirstOrDefault(p => p.Id == id);

            if (found == null)
            {
                RaiseLog($"  -> Не найден: {id}");
                await WriteTextAsync(response, 404, "Пользователь не найден");
                return;
            }

            RaiseLog($"  -> Найден: {found.Name}");
            await WriteJsonAsync(response, 200, found);
        }

        private async Task HandleCreateAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body = await ReadBodyAsync(request);

            Person person;
            try
            {
                person = JsonSerializer.Deserialize<Person>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                await WriteTextAsync(response, 400, $"Ошибка JSON: {ex.Message}");
                return;
            }

            if (person == null || string.IsNullOrWhiteSpace(person.Name))
            {
                await WriteTextAsync(response, 400, "Неверные данные: имя обязательно");
                return;
            }
            person.Id = Guid.NewGuid().ToString();

            lock (lockObject) persons.Add(person);

            RaiseLog($"  -> Создан: {person.Name} (Id={person.Id})");
            await WriteJsonAsync(response, 201, person);
        }

        private async Task HandleUpdateAsync(HttpListenerRequest request, HttpListenerResponse response, string id)
        {
            string body = await ReadBodyAsync(request);

            Person updated;
            try
            {
                updated = JsonSerializer.Deserialize<Person>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                await WriteTextAsync(response, 400, $"Ошибка JSON: {ex.Message}");
                return;
            }

            if (updated == null)
            {
                await WriteTextAsync(response, 400, "Пустое тело запроса");
                return;
            }

            Person existing;
            lock (lockObject)
            {
                existing = persons.FirstOrDefault(p => p.Id == id);
                if (existing != null)
                {
                    existing.Name = updated.Name ?? existing.Name;
                    existing.BirthDate = updated.BirthDate;
                    existing.Salary = updated.Salary;
                }
            }

            if (existing == null)
            {
                await WriteTextAsync(response, 404, "Пользователь не найден");
                return;
            }

            RaiseLog($"  -> Обновлён: {existing.Name}");
            await WriteJsonAsync(response, 200, existing);
        }

        private async Task HandleDeleteAsync(HttpListenerResponse response, string id)
        {
            bool removed;
            lock (lockObject)
            {
                var p = persons.FirstOrDefault(x => x.Id == id);
                removed = p != null;
                if (removed) persons.Remove(p);
            }

            if (!removed)
            {
                await WriteTextAsync(response, 404, "Пользователь не найден");
                return;
            }

            RaiseLog($"  -> Удалён Id={id}");
            response.StatusCode = 204;
            await Task.CompletedTask;
        }
        private async Task<string> ReadBodyAsync(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task WriteJsonAsync<T>(HttpListenerResponse response, int status, T obj)
        {
            response.StatusCode = status;
            response.ContentType = "application/json; charset=utf-8";

            string json = JsonSerializer.Serialize(obj, JsonOptions);
            byte[] data = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = data.Length;
            await response.OutputStream.WriteAsync(data, 0, data.Length);
        }

        private async Task WriteTextAsync(HttpListenerResponse response, int status, string text)
        {
            response.StatusCode = status;
            response.ContentType = "text/plain; charset=utf-8";

            byte[] data = Encoding.UTF8.GetBytes(text);
            response.ContentLength64 = data.Length;
            await response.OutputStream.WriteAsync(data, 0, data.Length);
        }

        private void RaiseLog(string message)
        {
            LogMessage?.Invoke(this, message);
        }
    }
}
