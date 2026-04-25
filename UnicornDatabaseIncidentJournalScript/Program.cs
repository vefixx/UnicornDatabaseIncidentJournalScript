using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using UnicornDatabaseIncidentJournalScript.Entities;
using UnicornDatabaseIncidentJournalScript.Models;

namespace UnicornDatabaseIncidentJournalScript;

class Program
{
    static void Main(string[] args)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        if (!File.Exists("config.json"))
        {
            Console.WriteLine($"Конфигурационный файл config.json не найден в корневой директории скрипта");
            return;
        }
        
        
        Config? config;
        try
        {
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            if (config is null)
            {
                Console.WriteLine("Ошибка при чтении конфигурации config.json");
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка при чтении конфигурации config.json");
            return;
        }

        var connectionString = $"Data Source={config.DatabasePath}";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            // Создаем таблицу инцидентов, если она не существует
            connection.Execute(
                """
                CREATE TABLE IF NOT EXISTS incidents_journal (
                    incident_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ts TEXT NOT NULL,
                    building_id INTEGER NOT NULL,
                    building TEXT NOT NULL,
                    complex TEXT NOT NULL,
                    apartment_id INTEGER NOT NULL,
                    apartment_no TEXT NOT NULL,
                    category TEXT NOT NULL,
                    message TEXT NOT NULL,
                    FOREIGN KEY (building_id) REFERENCES building(building_id),
                    FOREIGN KEY (apartment_id) REFERENCES apartment(apartment_id)
                )
                """
            );

            // Получаем последнюю строку в инцидентах, извлекаем из нее дату -
            // сортируем по возрастанию ключа, получаем первую строку
            var getLastIncidentSql = "SELECT * FROM incidents_journal ORDER BY incident_id DESC LIMIT 1";
            var lastIncident = connection.QuerySingleOrDefault<Incident>(getLastIncidentSql);

            // Если таблица пустая (т.е. lastIncident == null), то дату устанавливаем на null. В таких случаях мы будем извлекать все записи из представлений
            DateTime? targetDateTime = null;
            if (lastIncident is not null)
                targetDateTime = DateTime.Parse(lastIncident.Ts);
            Console.WriteLine($"targetDataTime is null = {targetDateTime is null}");

            // Инициализируем список объектов, которые мы добавим в журнал
            List<Incident> newIncidents = new List<Incident>();


            // Получаем список всех новых инцидентов в категории опасных сценариев потребления электроэнергии при отсутствии движения
            // Тут два случая - когда отправная точка (дата) указана и когда нет
            string energySql;
            object? dbParams = null;
            if (targetDateTime != null)
            {
                energySql = "SELECT * FROM view_danger_energy_with_no_motion_scen WHERE ts > @TargetDate";
                dbParams = new { TargetDate = targetDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") };
            }
            else
                energySql = "SELECT ts, apartment_id, apartment_no, building, building_id, complex FROM view_danger_energy_with_no_motion_scen";

            var scenaries = connection.Query<DangerEnergyNoMotionScen>(energySql, dbParams).ToList();
            Console.WriteLine($"Всего строк из таблицы view_danger_energy_with_no_motion_scen: {scenaries.Count}");
            
            newIncidents.AddRange(
                scenaries.Where(sc => sc.IsDanger).Select(sc => new Incident()
                {
                    Ts = sc.Ts,
                    ApartmentId = sc.ApartmentId,
                    ApartmentNo = sc.ApartmentNo,
                    Building = sc.Building,
                    BuildingId = sc.BuildingId,
                    Complex = sc.Complex,
                    Category = $"Повышение электроэнергии при отсутствии движения",
                    Message = "Обнаружено повышение электроэнергии при отсутствии движения в квартире"
                })
            );


            // Добавляем строки в журнал
            var addSql = """
                         INSERT INTO incidents_journal (ts, building_id, building, complex, apartment_id, apartment_no, category, message)
                         VALUES (@Ts, @BuildingId, @Building, @Complex, @ApartmentId, @ApartmentNo, @Category, @Message)
                         """;
            
            Console.WriteLine($"Вставка {newIncidents.Count} в журнал");

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    connection.Execute(addSql, newIncidents, transaction);
                    transaction.Commit();
                    Console.WriteLine("Вставка завершена");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Ошибка во время вставки {ex.Message}");
                }
            }
        }

        Console.WriteLine("close");
    }
}