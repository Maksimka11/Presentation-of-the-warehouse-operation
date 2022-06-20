using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Wildberries
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            dgOrderToStorage.ItemsSource = StorageOrders;

        }


        private void btnStart_Click(object sender, RoutedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(CheckFields()))
            {
                MessageBox.Show(CheckFields(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TbOff();
            Activated = true;
            tcMain.SelectedIndex = 1;
            FirstLoad();
            Summ = 0;
            Markdown = 0;
            salvage = 0;
        }

        //Список товаров на складе по упаковкам
        public List<ProductOnStorage> storage = new List<ProductOnStorage>();
        //Список товаров на складе суммарно
        public List<StorageStatistics> storageDesc = new List<StorageStatistics>();
        //Общий списко продуктов
        public List<Product> products = new List<Product>();
        //Данные о статистике
        public List<StorageStatistics> statistics = new List<StorageStatistics>();
        //Список заказов магазина
        public List<OrderToShop> orderToShops = new List<OrderToShop>();
        //Список поставок в магазины
        public List<DeliverToShop> deliverToShops = new List<DeliverToShop>();
        //Список продуктов, которые следует заказать у поставщиков
        public List<OrderToStorage> StorageOrders = new List<OrderToStorage>();
        //Список текущих доставок на склад
        public List<CurrentOrdersToStorage> currentOrders = new List<CurrentOrdersToStorage>();
        //Журнал работы
        public List<string> History = new List<string>();

        private void FirstLoad()
        {
            int TypesCount = Convert.ToInt32(tbProductsType.Text);
            Random random = new Random();
            List<int> mas = new List<int>();
            for (int j = 0; j < products.Count; j++) mas.Add(j);
            for (int i = 0; i < TypesCount; i++)
            {
                int ProductId = random.Next(1, mas.Count);
                bool Correct = false;
                while (Correct == false)
                {
                    if (mas.IndexOf(ProductId) != -1) Correct = true;
                    else
                    {
                        ProductId = random.Next(0, products.Count);
                    }
                }
                StorageStatistics statistic = new StorageStatistics(ProductId, products[ProductId].Name, products[ProductId].Unit);
                statistics.Add(statistic);
                statistic = new StorageStatistics(ProductId, products[ProductId].Name, products[ProductId].Unit, products[ProductId].StartCount);
                storageDesc.Add(statistic);
                mas.Remove(ProductId);

                for (int j = 0; j < products[ProductId].StartCount; j++)
                {
                    ProductOnStorage product = new ProductOnStorage(ProductId, products[ProductId].Name, products[ProductId].Unit, products[ProductId].UnitPrice, products[ProductId].Term, 0, 0);
                    storage.Add(product);
                }

            }
            dgStorageProduct.ItemsSource = storage;
            dgStatistics.ItemsSource = statistics;
        }


        bool Activated = false;

        public void LoadHistory()
        {
            tbHistory.Text = "";
            for (int i = 0; i < History.Count; i++)
            {
                tbHistory.Text += History[i] + Environment.NewLine;
            }

        }

        private void TbOff()
        {
            tbPeriod.IsEnabled = false;
            tbShops.IsEnabled = false;
            tbProductsType.IsEnabled = false;
            tbChance.IsEnabled = false;
            tbTypeCountMin.IsEnabled = false;
            tbTypeCountMax.IsEnabled = false;
            tbProductCountMin.IsEnabled = false;
            tbProductCountMax.IsEnabled = false;
            tbSaleTerm.IsEnabled = false;
            tbSaleProc.IsEnabled = false;
        }

        private void TbOn()
        {
            tbPeriod.IsEnabled = true;
            tbShops.IsEnabled = true;
            tbProductsType.IsEnabled = true;
            tbChance.IsEnabled = true;
            tbTypeCountMin.IsEnabled = true;
            tbTypeCountMax.IsEnabled = true;
            tbProductCountMin.IsEnabled = true;
            tbProductCountMax.IsEnabled = true;
            tbSaleTerm.IsEnabled = true;
            tbSaleProc.IsEnabled = true;
        }

        int StepCount = 0;

        private void btnSingle_Click(object sender, RoutedEventArgs e)
        {
            if (Activated == false) return;
            Step();
            StepCount++;
            lblStepCount.Content = StepCount;
            for (int i = 0; i < currentOrders.Count; i++)
            {
                currentOrders[i].CurrentTime++;
            }
            if (StepCount == Convert.ToInt32(tbPeriod.Text))
            {
                MessageBox.Show("Период моделирования истек", "Информация", MessageBoxButton.OK);
                Stop();
            }
        }

        private void LoadAllLists()
        {
            dgStorageProduct.Items.Refresh();
            dgStatistics.Items.Refresh();
            dgCurrentOrdersToStorage.Items.Refresh();
            dgDeliverToShop.Items.Refresh();
            dgOrderToShop.Items.Refresh();
            dgOrderToStorage.Items.Refresh();
        }

        private void DgClear()
        {
            dgStatistics.ItemsSource = null;
            dgCurrentOrdersToStorage.ItemsSource = null;
            dgDeliverToShop.ItemsSource = null;
            dgOrderToShop.ItemsSource = null;
            dgOrderToStorage.ItemsSource = null;
            dgStorageProduct.ItemsSource = null;
        }

        private void Step()
        {
            UpdateStorage();
            DeliverFinish();
            CheckCurrentDelivery();
            CheckProducts();
            OrderToShop();
            DeliverToShop();
            LoadHistory();

            LoadAllLists();
            CalculateStatistic();
        }

        private void UpdateStorage()
        {
            for (int i = 0; i < storage.Count; i++)
            {
                storage[i].StorageTime++;
                if (storage[i].StorageTime >= Convert.ToInt32(tbSaleTerm.Text)) storage[i].Sale = Convert.ToInt32(tbSaleProc.Text);
            }

            foreach (ProductOnStorage product in storage.ToArray())
            {
                if (product.Term == product.StorageTime)
                {
                    storage.Remove(product);
                    salvage += product.UnitPrice;
                    foreach (StorageStatistics statistic in storageDesc)
                    {
                        if (statistic.Name == product.Name) statistic.Count--;
                    }

                }
            }

            lblLostProducts.Content = salvage;
        }


        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            TbOn();
            storage = new List<ProductOnStorage>();
            storageDesc = new List<StorageStatistics>();
            statistics = new List<StorageStatistics>();
            orderToShops = new List<OrderToShop>();
            deliverToShops = new List<DeliverToShop>();
            StorageOrders = new List<OrderToStorage>();
            currentOrders = new List<CurrentOrdersToStorage>();
            History = new List<string>();
            tcMain.SelectedIndex = 0;
            LoadAllLists();
            Activated = false;
            DgClear();
            StepCount = 0;
            tbHistory.Text = "";
            lblStepCount.Content = 0;
        }

        public double Summ = 0;
        public double Markdown = 0;
        public double salvage = 0;

        private void CalculateStatistic()
        {
            lblSoldProducts.Content = Summ.ToString();
            lblSaleProducts.Content = Markdown.ToString();
            lblLostProducts.Content = salvage.ToString();

        }

        private void OrderToShop()
        {
            int ShopCount = Convert.ToInt32(tbShops.Text);
            int TypeCountMin = Convert.ToInt32(tbTypeCountMin.Text);
            int TypeCountMax = Convert.ToInt32(tbTypeCountMax.Text);
            int CountMin = Convert.ToInt32(tbProductCountMin.Text);
            int CountMax = Convert.ToInt32(tbProductCountMax.Text);
            int Chance = Convert.ToInt32(tbChance.Text);

            Random rand = new Random();
            orderToShops = new List<OrderToShop>();

            for (int i = 0; i < ShopCount; i++)
            {
                if (rand.Next(1, 100) > Chance) continue;
                List<int> mas = new List<int>();
                for (int j = 0; j < statistics.Count; j++) mas.Add(j);
                List<Order> order = new List<Order>();
                int TypeCount = rand.Next(TypeCountMin, TypeCountMax);
                for (int j = 0; j < TypeCount; j++)
                {
                    int id = rand.Next(0, statistics.Count);
                    bool Correct = false;
                    while (Correct == false)
                    {
                        if (mas.IndexOf(id) != -1) Correct = true;
                        else
                        {
                            id = rand.Next(0, statistics.Count);
                        }
                    }
                    int Count = rand.Next(CountMin, CountMax);
                    Order product = new Order(statistics[id].Name, Count, statistics[id].Unit);
                    mas.Remove(id);
                    order.Add(product);
                }
                OrderToShop deliver = new OrderToShop(i + 1, order);
                orderToShops.Add(deliver);
            }
            dgOrderToShop.ItemsSource = orderToShops;
        }

        private void DeliverToShop()
        {
            deliverToShops = new List<DeliverToShop>();
            List<StorageStatistics> st = new List<StorageStatistics>();
            foreach (StorageStatistics st1 in storageDesc) st.Add(st1);

            for (int i = 0; i < orderToShops.Count; i++)
            {

                List<Deliver> deliverList = new List<Deliver>();
                foreach (Order Order in orderToShops[i].Order)
                {
                    Deliver d = new Deliver(Order.Name, Order.Count, Order.Unit);
                    deliverList.Add(d);
                }

                DeliverToShop deliver = new DeliverToShop(orderToShops[i].ShopNumber, deliverList);
                for (int j = 0; j < deliver.Deliver.Count; j++)
                {
                    int id = 0;
                    foreach (StorageStatistics storage in st)
                    {
                        if (storage.Name == deliver.Deliver[j].Name) id = st.IndexOf(storage);
                    }
                    deliver.Deliver[j].Count = GetCount(st[id].Count, deliver.Deliver[j].Count);
                }
                deliverToShops.Add(deliver);
            }
            dgDeliverToShop.ItemsSource = deliverToShops;


        }

        private int GetCount(int stCount, int delCount)
        {
            Random rand = new Random();
            int Count = rand.Next(delCount - 5, delCount + 5);

            if (stCount <= 0) return 0;
            if (delCount >= stCount) return stCount;
            else
            {
                while (Count > stCount | Count <= 0)
                {
                    Count = rand.Next(delCount - 5, delCount + 5);
                }
            }            
            return Count;
        }


        private void DeliverFinish()
        {
            if (deliverToShops.Count != 0)
            {
                for (int i = 0; i < deliverToShops.Count; i++)
                {
                    if (deliverToShops[i].Approved == false) continue;
                    History.Add($"Перевезены заказаные продукты со склада в магазин № {deliverToShops[i].ShopNumber}");
                    for (int j = 0; j < deliverToShops[i].Deliver.Count; j++)
                    {
                        History.Add($"{deliverToShops[i].Deliver[j].Name} - {deliverToShops[i].Deliver[j].Count} {deliverToShops[i].Deliver[j].Unit}");
                        int id = 0;
                        foreach (StorageStatistics storage in storageDesc)
                        {
                            if (storage.Name == deliverToShops[i].Deliver[j].Name) id = storageDesc.IndexOf(storage);
                        }

                        storageDesc[id].Count -= deliverToShops[i].Deliver[j].Count;

                        foreach (StorageStatistics storage in statistics)
                        {
                            if (storage.Name == deliverToShops[i].Deliver[j].Name) id = statistics.IndexOf(storage);
                        }
                        statistics[id].Count += deliverToShops[i].Deliver[j].Count;


                        for (int k = 0; k < deliverToShops[i].Deliver[j].Count; k++)
                        {
                            string Name = deliverToShops[i].Deliver[j].Name;
                            foreach (ProductOnStorage product in storage.ToArray())
                            {
                                if (product.Name == Name)
                                {
                                    Summ = Summ + product.UnitPrice * (100 - product.Sale) * 0.01;
                                    Markdown = Markdown + product.UnitPrice * (product.Sale * 0.01);
                                    storage.Remove(product);
                                    break;
                                }
                            }
                        }
                        dgStorageProduct.Items.Refresh();
                    }
                }
            }
        }

        private void CheckProducts()
        {
            Random rand = new Random();
            StorageOrders = new List<OrderToStorage>();
            foreach (StorageStatistics storage in storageDesc)
            {
                int id = 0;
                foreach (Product product in products)
                {
                    if (product.Name == storage.Name) id = products.IndexOf(product);
                }
                if (storage.Count < products[id].MinCount)
                {
                    int Count = rand.Next(products[id].MinCount, products[id].MaxCount);
                    OrderToStorage order = new OrderToStorage(products[id].Name, Count);
                    StorageOrders.Add(order);
                }
            }

            foreach (CurrentOrdersToStorage current in currentOrders)
            {
                foreach (OrderToStorage order in StorageOrders.ToArray())
                {
                    if (current.Name == order.Name) StorageOrders.Remove(order);
                }
            }
            dgOrderToStorage.ItemsSource = StorageOrders;
        }


        private void CheckCurrentDelivery()
        {
            Random rand = new Random();

            foreach (CurrentOrdersToStorage current in currentOrders.ToArray())
            {
                if (current.CurrentTime == current.DeliveryTime)
                {
                    History.Add("Перевезены заканные продукты со склада поставщика к нам на склад");
                    int id = 0;
                    foreach (Product product in products)
                    {
                        if (product.Name == current.Name) id = products.IndexOf(product);
                    }

                    for (int j = 0; j < current.Count; j++)
                    {
                        ProductOnStorage product = new ProductOnStorage(id, products[id].Name, products[id].Unit, products[id].UnitPrice, products[id].Term, 0, 0);
                        storage.Add(product);
                        History.Add($"{current.Name} - {current.Count}");
                    }

                    foreach (StorageStatistics storageStatistics in storageDesc)
                    {
                        if (storageStatistics.Name == current.Name) id = storageDesc.IndexOf(storageStatistics);
                    }


                    storageDesc[id].Count += current.Count;
                    currentOrders.Remove(current);
                }
            }

            foreach (OrderToStorage order in StorageOrders)
            {
                if (order.Approved == true)
                {
                    int Time = rand.Next(1, 5);
                    CurrentOrdersToStorage current = new CurrentOrdersToStorage(order.Name, order.Count, Time);
                    currentOrders.Add(current);
                }
            }
            dgCurrentOrdersToStorage.ItemsSource = currentOrders;

        }



        private string CheckFields()
        {
            string message = "";

            if (string.IsNullOrWhiteSpace(tbPeriod.Text)) message += "Введите период моделирования!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbPeriod.Text) < 10 | Convert.ToInt32(tbPeriod.Text) > 30) message += "Период моделирования не может быть меньше 10 дней и больше 30 дней!" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(tbShops.Text)) message += "Введите количество магазинов!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbShops.Text) < 3 | Convert.ToInt32(tbShops.Text) > 9) message += "Количество магазинов не может быть меньше 3 и больше 9!" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(tbProductsType.Text)) message += "Введите количество видов продуктов!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbProductsType.Text) < 12 | Convert.ToInt32(tbProductsType.Text) > 20) message += "Количество видов продуктов не может быть меньше 12 и больше 20!" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(tbChance.Text)) message += "Введите вероятность заказа продуктов!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbChance.Text) < 1 | Convert.ToInt32(tbChance.Text) > 100) message += "Вероятность заказа продуктов не может быть меньше 1 и больше 100!" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(tbTypeCountMin.Text)) message += "Введите количество видов заказываемых продуктов, мин.!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbTypeCountMin.Text) < 1 | Convert.ToInt32(tbTypeCountMin.Text) > Convert.ToInt32(tbTypeCountMax.Text)) message += "Минимальное количество видов не может быть меньше 1 и больше максимального!" + Environment.NewLine;
            }
            if (string.IsNullOrWhiteSpace(tbTypeCountMax.Text)) message += "Введите количество видов заказываемых продуктов, макс.!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbTypeCountMax.Text) < Convert.ToInt32(tbTypeCountMin.Text) | Convert.ToInt32(tbTypeCountMax.Text) > 10) message += "Максимальное количество видов не может быть меньше минимального и больше 10!" + Environment.NewLine;
            }
            if (string.IsNullOrWhiteSpace(tbProductCountMin.Text)) message += "Введите количество единиц заказываемых продуктов, мин.!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbProductCountMin.Text) < 1 | Convert.ToInt32(tbProductCountMin.Text) > Convert.ToInt32(tbProductCountMax.Text)) message += "Минимальное количество товаров в заказе не может быть меньше 1 и больше максимального количества!" + Environment.NewLine;
            }
            if (string.IsNullOrWhiteSpace(tbProductCountMax.Text)) message += "Введите количество единиц заказываемых продуктов, макс.!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbProductCountMax.Text) < Convert.ToInt32(tbProductCountMin.Text) | Convert.ToInt32(tbProductCountMax.Text) > 100) message += "Максимальное количество товаров в заказе не может быть меньше минимального и больше 100!" + Environment.NewLine;
            }
            if (string.IsNullOrWhiteSpace(tbSaleTerm.Text)) message += "Введите cрок уценки!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbSaleTerm.Text) < 1 | Convert.ToInt32(tbSaleTerm.Text) > 10) message += "Срок уценки не может быть меньше 1 и больше 10!" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(tbSaleProc.Text)) message += "Введите процент уценки!" + Environment.NewLine;
            else
            {
                if (Convert.ToInt32(tbSaleProc.Text) < 0 | Convert.ToInt32(tbSaleTerm.Text) > 10) message += "Процент уценки не может быть меньше 0 и больше 50!" + Environment.NewLine;
            }

            return message;
        }

        private void PreviewText(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Product product = new Product(1, "Картофель", "кг", 50, 15, 50, 20, 10, 100);
            products.Add(product);
            product = new Product(2, "Морковь", "кг", 60, 15, 50, 20, 10, 100);
            products.Add(product);
            product = new Product(3, "Капуста", "кг", 70, 15, 50, 20, 10, 100);
            products.Add(product);
            product = new Product(4, "Подсолнечное масло", "л", 100, 15, 10, 20, 10, 100);
            products.Add(product);
            product = new Product(5, "Молоко", "л", 70, 10, 10, 20, 10, 100);
            products.Add(product);
            product = new Product(6, "Кефир", "л", 60, 10, 10, 20, 10, 100);
            products.Add(product);
            product = new Product(7, "Рыба", "кг", 500, 10, 20, 20, 10, 100);
            products.Add(product);
            product = new Product(8, "Говядина", "кг", 400, 10, 20, 20, 10, 100);
            products.Add(product);
            product = new Product(9, "Свинина", "кг", 300, 10, 20, 20, 10, 100);
            products.Add(product);
            product = new Product(10, "Яблоки", "кг", 50, 15, 30, 20, 10, 100);
            products.Add(product);
            product = new Product(11, "Бананы", "кг", 60, 15, 30, 20, 10, 100);
            products.Add(product);
            product = new Product(12, "Груши", "кг", 70, 15, 30, 20, 10, 100);
            products.Add(product);
            product = new Product(13, "Гречка", "кг", 150, 20, 50, 20, 10, 100);
            products.Add(product);
            product = new Product(14, "Рис", "кг", 100, 20, 50, 20, 10, 100);
            products.Add(product);
            product = new Product(15, "Сахар", "кг", 100, 30, 50, 20, 10, 100);
            products.Add(product);
            product = new Product(16, "Соль", "кг", 50, 30, 50, 20, 10, 100);
            products.Add(product);
            product = new Product(17, "Макароны", "кг", 100, 20, 30, 20, 10, 100);
            products.Add(product);
            product = new Product(18, "Мука", "кг", 50, 20, 30, 20, 10, 100);
            products.Add(product);
            product = new Product(19, "Кофе", "уп", 250, 25, 10, 20, 10, 100);
            products.Add(product);
            product = new Product(20, "Чай", "уп", 150, 25, 10, 20, 10, 100);
            products.Add(product);

            dgProducts.ItemsSource = products;
        }
    }

    public class Product
    {

        public Product(int id, string name, string unit, int unitPrice, int term, int wholesaleSize, int startCount, int minCount, int maxCount)
        {
            Id = id;
            Name = name;
            Unit = unit;
            UnitPrice = unitPrice;
            Term = term;
            WholesaleSize = wholesaleSize;
            StartCount = startCount;
            MinCount = minCount;
            MaxCount = maxCount;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public int UnitPrice { get; set; }
        public int Term { get; set; }
        public int WholesaleSize { get; set; }
        public int StartCount { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
    }


    public class OrderToStorage
    {

        public OrderToStorage(string name, int count, bool approved = false)
        {
            Name = name;
            Count = count;
            Approved = approved;
        }

        public string Name { get; set; }
        public int Count { get; set; }
        public bool Approved { get; set; }
    }

    public class CurrentOrdersToStorage
    {
        public CurrentOrdersToStorage(string name, int count, int deliveryTime, int currentTime = 0)
        {
            Name = name;
            Count = count;
            DeliveryTime = deliveryTime;
            CurrentTime = currentTime;
        }
        public string Name { get; set; }
        public int Count { get; set; }
        public int DeliveryTime { get; set; }
        public int CurrentTime { get; set; }
    }

    public class StorageStatistics
    {

        public StorageStatistics(int Id, string name, string unit, int count = 0)
        {
            ProductId = Id;
            Name = name;
            Unit = unit;
            Count = count;
        }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public string Unit { get; set; }
    }

    public class Order
    {
        public Order(string name, int count, string unit)
        {
            Name = name;
            Count = count;
            Unit = unit;
        }

        public string Name { get; set; }
        public int Count { get; set; }
        public string Unit { get; set; }
    }

    public class Deliver
    {
        public Deliver(string name, int count, string unit)
        {
            Name = name;
            Count = count;
            Unit = unit;
        }

        public string Name { get; set; }
        public int Count { get; set; }
        public string Unit { get; set; }
    }

    public class ProductOnStorage
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public int UnitPrice { get; set; }
        public int Term { get; set; }
        public int StorageTime { get; set; }
        public int Sale { get; set; }

        public ProductOnStorage(int Id, string name, string unit, int unitPrice, int term, int storageTime, int sale)
        {
            ProductId = Id;
            Name = name;
            Unit = unit;
            UnitPrice = unitPrice;
            Term = term;
            StorageTime = storageTime;
            Sale = sale;
        }
    }

    public class DeliverToShop
    {
        public DeliverToShop(int number, List<Deliver> order, bool approved = true)
        {
            ShopNumber = number;
            Deliver = order;
            Approved = approved;
        }

        public int ShopNumber { get; set; }
        public List<Deliver> Deliver { get; set; }
        public bool Approved { get; set; }
    }

    public class OrderToShop
    {
        public OrderToShop(int number, List<Order> order)
        {
            ShopNumber = number;
            Order = order;
        }

        public int ShopNumber { get; set; }
        public List<Order> Order { get; set; }
    }
}
