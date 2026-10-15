using System;
using System.Collections.Generic;
using System.Linq;

namespace Pizzeria
{
    public abstract class MenuItem
    {
        private string _name;
        protected decimal _basePrice;

        public string Name => _name;

        protected MenuItem(string name, decimal basePrice)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название не может быть пустым.");

            if (basePrice < 0)
                throw new ArgumentException("Цена не может быть отрицательной.");

            _name = name;
            _basePrice = basePrice;
        }

        public abstract decimal GetFinalPrice();
        public abstract string GetInfo();

        public virtual string GetCategory() => "Позиция меню";
    }

    public interface IDiscountable
    {
        decimal ApplyDiscount(decimal percent);
        string GetDiscountInfo();
    }

    public class Pizza : MenuItem, IDiscountable
    {
        public int ToppingsCount { get; }

        public Pizza(string name, decimal basePrice, int toppingsCount)
            : base(name, basePrice)
        {
            if (toppingsCount < 0)
                throw new ArgumentException("Количество топпингов не может быть отрицательным.");

            ToppingsCount = toppingsCount;
        }

        public override decimal GetFinalPrice()
        {
            return _basePrice + ToppingsCount * 50m;
        }

        public override string GetInfo()
        {
            return $"Пицца «{Name}», топпингов: {ToppingsCount}, " +
                   $"итого: {GetFinalPrice():F2} руб.";
        }

        public override string GetCategory() => "Пицца";

        public decimal ApplyDiscount(decimal percent)
        {
            CheckPercent(percent);

            decimal price = GetFinalPrice() * (1m - percent / 100m);

            if (ToppingsCount > 3)
                price *= 0.95m;

            return Math.Round(price, 2);
        }

        public string GetDiscountInfo()
        {
            return ToppingsCount > 3
                ? "Дополнительная скидка 5% за топпинги"
                : "Дополнительная скидка не применяется";
        }

        private static void CheckPercent(decimal percent)
        {
            if (percent < 0 || percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent));
        }
    }

    public class Drink : MenuItem
    {
        public int VolumeMl { get; }

        public Drink(string name, decimal basePrice, int volumeMl)
            : base(name, basePrice)
        {
            if (volumeMl < 0)
                throw new ArgumentException("Объём не может быть отрицательным.");

            VolumeMl = volumeMl;
        }

        public override decimal GetFinalPrice()
        {
            return _basePrice + VolumeMl * 0.5m;
        }

        public override string GetInfo()
        {
            return $"Напиток «{Name}», {VolumeMl} мл, " +
                   $"итого: {GetFinalPrice():F2} руб.";
        }

        public override string GetCategory() => "Напиток";
    }

    public class Dessert : MenuItem, IDiscountable
    {
        public bool IsTodayFresh { get; }

        public Dessert(string name, decimal basePrice, bool isTodayFresh)
            : base(name, basePrice)
        {
            IsTodayFresh = isTodayFresh;
        }

        public override decimal GetFinalPrice()
        {
            return IsTodayFresh ? _basePrice : _basePrice * 0.7m;
        }

        public override string GetInfo()
        {
            string freshness = IsTodayFresh ? "свежий" : "не свежий";

            return $"Десерт «{Name}», {freshness}, " +
                   $"итого: {GetFinalPrice():F2} руб.";
        }

        public override string GetCategory() => "Десерт";

        public decimal ApplyDiscount(decimal percent)
        {
            if (percent < 0 || percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent));

            decimal price = GetFinalPrice() * (1m - percent / 100m);

            if (!IsTodayFresh)
                price *= 0.90m;

            return Math.Round(price, 2);
        }

        public string GetDiscountInfo()
        {
            return IsTodayFresh
                ? "Дополнительная скидка не применяется"
                : "Дополнительная скидка 10% на несвежий десерт";
        }
    }

    public class Combo : MenuItem
    {
        private readonly IReadOnlyList<MenuItem> _items;

        public Combo(string name, IEnumerable<MenuItem> items)
            : base(name, 0m)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            List<MenuItem> copy = items.ToList();

            if (copy.Count == 0 || copy.Any(item => item == null))
                throw new ArgumentException("Комбо должно содержать позиции меню.");

            _items = copy.AsReadOnly();
        }

        public override decimal GetFinalPrice()
        {
            decimal total = _items.Sum(item => item.GetFinalPrice());
            return Math.Round(total * 0.85m, 2);
        }

        public override string GetInfo()
        {
            return $"Комбо «{Name}», позиций: {_items.Count}, скидка 15%, итого: {GetFinalPrice():F2} руб.";
        }

        public override string GetCategory() => "Комбо";
    }

    public class Order
    {
        private readonly List<MenuItem> _items = new();
        private const int MaxItems = 20;

        public void Add(MenuItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_items.Count >= MaxItems)
                throw new InvalidOperationException($"Нельзя добавить больше {MaxItems} позиций.");

            _items.Add(item);
        }

        public IReadOnlyList<MenuItem> GetItems()
        {
            return _items.AsReadOnly();
        }

        public decimal GetTotal()
        {
            return _items.Sum(item => item.GetFinalPrice());
        }

        public void PrintReceipt()
        {
            Console.WriteLine("\n========== ЧЕК ==========");
            if (_items.Count == 0)
            {
                Console.WriteLine("Заказ пуст.");
            }
            else
            {
                foreach (MenuItem item in _items)
                {
                    Console.WriteLine($"[{item.GetCategory()}] {item.GetInfo()}");
                }
            }
            Console.WriteLine("-------------------------");
            Console.WriteLine($"Итого: {GetTotal():F2} руб.");
            Console.WriteLine("=========================");
        }

        public decimal GetTotalWithDiscount(decimal percent)
        {
            if (percent < 0 || percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent));

            decimal total = 0m;
            foreach (MenuItem item in _items)
            {
                if (item is IDiscountable discountable)
                    total += discountable.ApplyDiscount(percent);
                else
                    total += item.GetFinalPrice();
            }
            return Math.Round(total, 2);
        }

        public MenuItem? FindMostExpensive()
        {
            return _items.OrderByDescending(item => item.GetFinalPrice()).FirstOrDefault();
        }

        public IReadOnlyList<MenuItem> GetItemsSortedByPrice()
        {
            return _items.OrderBy(item => item.GetFinalPrice()).ToList().AsReadOnly();
        }
    }

    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var margarita = new Pizza("Маргарита", 400m, 3);
            var pepperoni = new Pizza("Пепперони", 300m, 2);
            var fourCheeses = new Pizza("Четыре сыра", 450m, 4);

            var cola = new Drink("Кола", 100m, 200);
            var juice = new Drink("Апельсиновый сок", 80m, 250);

            var tiramisu = new Dessert("Тирамису", 320m, true);
            var cheesecake = new Dessert("Чизкейк", 280m, false);

            var combo = new Combo("Обеденный", new MenuItem[] { cola, cheesecake });

            List<MenuItem> menu = new()
            {
                margarita, pepperoni, fourCheeses, cola, juice, tiramisu, cheesecake, combo
            };

            Order order = new Order();

            Console.WriteLine("*****************************");
            Console.WriteLine("      ДОБРО ПОЖАЛОВАТЬ!      ");
            Console.WriteLine("          ПИЦЦЕРИЯ           ");
            Console.WriteLine("*****************************");

            bool addMore = true;
            while (addMore)
            {
                Console.WriteLine("\n========= МЕНЮ =========");
                for (int i = 0; i < menu.Count; i++)
                {
                    MenuItem item = menu[i];
                    Console.WriteLine($"{i + 1}. [{item.GetCategory()}] {item.Name} — {item.GetFinalPrice():F2} руб.");
                }
                Console.WriteLine("========================");

                int choice;
                while (true)
                {
                    Console.Write("Введите номер позиции: ");
                    string? input = Console.ReadLine();
                    if (int.TryParse(input, out choice) && choice >= 1 && choice <= menu.Count)
                    {
                        break;
                    }
                    Console.WriteLine($"Ошибка! Введите целое число от 1 до {menu.Count}.");
                }

                try
                {
                    MenuItem selectedItem = menu[choice - 1];
                    order.Add(selectedItem);
                    Console.WriteLine($"Добавлено: {selectedItem.Name} — {selectedItem.GetFinalPrice():F2} руб.");
                    Console.WriteLine($"Позиций в заказе: {order.GetItems().Count}/20");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }

                if (order.GetItems().Count >= 20)
                {
                    Console.WriteLine("Заказ достиг максимального количества позиций.");
                    break;
                }

                addMore = AskYesNo("Добавить ещё? (д/н): ");
            }

            order.PrintReceipt();

            if (AskYesNo("\nПрименить скидку 10%? (д/н): "))
            {
                decimal discountedTotal = order.GetTotalWithDiscount(10m);
                Console.WriteLine("\n======= СКИДКА =======");
                Console.WriteLine($"До скидки: {order.GetTotal():F2} руб.");
                Console.WriteLine($"После скидки 10%: {discountedTotal:F2} руб.");
                Console.WriteLine("======================");
            }

            MenuItem? expensive = order.FindMostExpensive();
            if (expensive != null)
            {
                Console.WriteLine($"\nСамая дорогая позиция: {expensive.Name} — {expensive.GetFinalPrice():F2} руб.");
            }

            Console.WriteLine("\n=== Позиции по возрастанию цены ===");
            foreach (MenuItem item in order.GetItemsSortedByPrice())
            {
                Console.WriteLine($"{item.Name} — {item.GetFinalPrice():F2} руб.");
            }

            Console.WriteLine("\nСписок заказа доступен только для чтения.");
            IReadOnlyList<MenuItem> readOnlyItems = order.GetItems();
            Console.WriteLine($"Количество позиций в заказе: {readOnlyItems.Count}");

            Console.WriteLine("\nСпасибо за заказ! До свидания!");
        }

        private static bool AskYesNo(string question)
        {
            while (true)
            {
                Console.Write(question);
                string? answer = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (answer == "д" || answer == "да") return true;
                if (answer == "н" || answer == "нет") return false;
                Console.WriteLine("Ошибка! Введите «д» (да) или «н» (нет).");
            }
        }
    }
}
