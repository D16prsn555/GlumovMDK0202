using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Glumov0202
{
    public partial class PartnerEditWindow : Window
    {
        private int _partnerId;

        private bool _isEditMode;

        private List<ProductInRequest> _productsInRequest;

        public event EventHandler PartnerSaved;
        public PartnerEditWindow(int partnerId = 0)
        {
            InitializeComponent();

            _partnerId = partnerId;
            _isEditMode = partnerId > 0;
            _productsInRequest = new List<ProductInRequest>();

            // Устанавливаем заголовок окна в зависимости от режима
            if (_isEditMode)
            {
                Title = "Редактирование заявки партнера";
            }
            else
            {
                Title = "Добавление заявки партнера";
            }

            // Обработчик загрузки окна
            Loaded += PartnerEditWindow_Loaded;
        }

        /// <summary>
        /// Загрузка данных при открытии окна.
        /// </summary>
        private void PartnerEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPartnerTypes();
                LoadProducts();

                // Если редактирование — загружаем данные партнера и его заявки
                if (_isEditMode)
                {
                    LoadPartnerData();
                    LoadPartnerRequests();
                }

                // Обновляем общую стоимость заявки
                UpdateTotalCost();
            }
            catch
            {
                MessageBox.Show("Ошибка при загрузке данных");
                Close();
            }
        }

        /// <summary>
        /// Загрузка типов партнеров в комбобокс.
        /// </summary>
        private void LoadPartnerTypes()
        {
            using (var context = new Entities())
            {
                var partnerTypes = context.Partner_type.ToList();
                PartnerTypeComboBox.ItemsSource = partnerTypes;

                // По умолчанию выбираем первый тип
                if (partnerTypes.Any())
                {
                    PartnerTypeComboBox.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Загрузка списка продукции в комбобокс.
        /// </summary>
        private void LoadProducts()
        {
            using (var context = new Entities())
            {
                var products = context.Products.ToList();
                ProductComboBox.ItemsSource = products;

                // По умолчанию выбираем первый продукт и обновляем цену
                if (products.Any())
                {
                    ProductComboBox.SelectedIndex = 0;
                    UpdateUnitCost();
                }
            }
        }

        /// <summary>
        /// Загрузка данных партнера по его идентификатору.
        /// </summary>
        private void LoadPartnerData()
        {
            using (var context = new Entities())
            {
                var partner = context.Partners
                    .Include("Partner_type")
                    .FirstOrDefault(p => p.ID == _partnerId);

                if (partner == null)
                {
                    MessageBox.Show("Партнер не найден!");
                    Close();
                    return;
                }

                PartnerTypeComboBox.SelectedValue = partner.ID_Partner_type;
                NameTextBox.Text = partner.Name;
                DirectorTextBox.Text = partner.Director;
                AddressTextBox.Text = partner.Address;
                RatingTextBox.Text = partner.Rating?.ToString() ?? "0";
                PhoneTextBox.Text = partner.Phone;
                EmailTextBox.Text = partner.Email;
            }
        }

        /// <summary>
        /// Загрузка существующих заявок партнера и заполнение списка продуктов.
        /// </summary>
        private void LoadPartnerRequests()
        {
            using (var context = new Entities())
            {
                var requests = context.Partner_products_request
                    .Include("Products")
                    .Where(r => r.ID_Partner == _partnerId)
                    .ToList();

                foreach (var request in requests)
                {
                    if (request.Products != null && request.Count.HasValue)
                    {
                        _productsInRequest.Add(new ProductInRequest
                        {
                            ProductId = request.Products.ID,
                            ProductName = request.Products.Name,
                            Count = request.Count.Value,
                            UnitCost = request.Products.Minimal_cost_for_partner ?? 0,
                            TotalCost = Math.Round((request.Products.Minimal_cost_for_partner ?? 0) * request.Count.Value, 2)
                        });
                    }
                }

                ProductsDataGrid.ItemsSource = _productsInRequest;
            }
        }

        /// <summary>
        /// Обработчик смены выбранного продукта.
        /// </summary>
        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUnitCost();
        }

        /// <summary>
        /// Обновление отображения минимальной стоимости выбранного продукта.
        /// </summary>
        private void UpdateUnitCost()
        {
            var selectedProduct = ProductComboBox.SelectedItem as Products;
            if (selectedProduct != null)
            {
                double unitCost = selectedProduct.Minimal_cost_for_partner ?? 0;
                UnitCostTextBlock.Text = $"{unitCost:N2} руб.";

                // Пересчет общей стоимости заявки
                UpdateTotalCost();
            }
        }

        /// <summary>
        /// Пересчет общей стоимости всех продуктов в заявке.
        /// </summary>
        private void UpdateTotalCost()
        {
            double totalCost = _productsInRequest.Sum(p => p.TotalCost);
            TotalCostTextBlock.Text = $"{totalCost:N2} руб.";
        }

        /// <summary>
        /// Ограничение ввода рейтинга — только цифры.
        /// </summary>
        private void RatingTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Ограничение ввода количества продукта — только цифры.
        /// </summary>
        private void CountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Добавление выбранного продукта в заявку.
        /// </summary>
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProduct = ProductComboBox.SelectedItem as Products;
                if (selectedProduct == null)
                {
                    MessageBox.Show("Выберите продукт!");
                    return;
                }

                if (!int.TryParse(CountTextBox.Text, out int count) || count <= 0)
                {
                    MessageBox.Show("Введите корректное количество!");
                    return;
                }

                double unitCost = selectedProduct.Minimal_cost_for_partner ?? 0;
                double totalCost = Math.Round(unitCost * count, 2);

                // Формируем запись о продукте в заявке
                var productInRequest = new ProductInRequest
                {
                    ProductId = selectedProduct.ID,
                    ProductName = selectedProduct.Name,
                    Count = count,
                    UnitCost = unitCost,
                    TotalCost = totalCost
                };

                // Добавляем в список и обновляем таблицу
                _productsInRequest.Add(productInRequest);
                ProductsDataGrid.ItemsSource = null;
                ProductsDataGrid.ItemsSource = _productsInRequest;

                // Обновляем общую сумму
                UpdateTotalCost();

                // Сбрасываем количество к значению по умолчанию
                CountTextBox.Text = "1";
            }
            catch
            {
                MessageBox.Show("Ошибка при добавлении продукта");
            }
        }

        /// <summary>
        /// Удаление продукта из заявки по нажатию кнопки в строке таблицы.
        /// </summary>
        private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductInRequest product)
            {
                _productsInRequest.Remove(product);
                ProductsDataGrid.ItemsSource = null;
                ProductsDataGrid.ItemsSource = _productsInRequest;

                // Обновляем общую стоимость
                UpdateTotalCost();
            }
        }

        /// <summary>
        /// Обработка нажатия кнопки "Сохранить заявку".
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка корректности данных партнера
                if (!ValidatePartnerInput())
                {
                    return;
                }

                // Проверка заполненности заявки продуктами
                if (!ValidateRequestInput())
                {
                    return;
                }

                // Сохранение партнера и списка заявок
                SavePartnerAndRequests();

                DialogResult = true;
                PartnerSaved?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch
            {
                MessageBox.Show("Ошибка при сохранении");
            }
        }

        /// <summary>
        /// Проверка введенных данных о партнере.
        /// </summary>
        private bool ValidatePartnerInput()
        {
            if (PartnerTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип партнера!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите наименование компании!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DirectorTextBox.Text))
            {
                MessageBox.Show("Введите ФИО директора!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                MessageBox.Show("Введите юридический адрес!");
                return false;
            }

            if (!int.TryParse(RatingTextBox.Text, out int rating) || rating < 0)
            {
                MessageBox.Show("Рейтинг должен быть целым неотрицательным числом!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Проверка наличия хотя бы одного продукта в заявке.
        /// </summary>
        private bool ValidateRequestInput()
        {
            if (_productsInRequest.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один продукт в заявку!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Сохранение данных партнера и его заявки в базу данных.
        /// </summary>
        private void SavePartnerAndRequests()
        {
            using (var context = new Entities())
            {
                Partners partner;

                if (_isEditMode)
                {
                    // Режим редактирования: загружаем уже существующего партнера
                    partner = context.Partners.FirstOrDefault(p => p.ID == _partnerId);
                    if (partner == null)
                    {
                        throw new Exception("Партнер не найден в базе данных");
                    }

                    // Удаляем старые записи по заявке, чтобы записать новые
                    var oldRequests = context.Partner_products_request
                        .Where(r => r.ID_Partner == _partnerId)
                        .ToList();

                    foreach (var request in oldRequests)
                    {
                        context.Partner_products_request.Remove(request);
                    }
                }
                else
                {
                    // Режим добавления нового партнера
                    partner = new Partners();
                    context.Partners.Add(partner);
                }

                // Обновляем данные партнера
                partner.ID_Partner_type = (int?)PartnerTypeComboBox.SelectedValue;
                partner.Name = NameTextBox.Text.Trim();
                partner.Director = DirectorTextBox.Text.Trim();
                partner.Address = AddressTextBox.Text.Trim();
                partner.Rating = int.Parse(RatingTextBox.Text);
                partner.Phone = PhoneTextBox.Text.Trim();
                partner.Email = EmailTextBox.Text.Trim();

                // Сохраняем партнера
                context.SaveChanges();

                // Создаем новые записи по заявке
                foreach (var productInRequest in _productsInRequest)
                {
                    var request = new Partner_products_request
                    {
                        ID_Partner = partner.ID,
                        ID_Product = productInRequest.ProductId,
                        Count = productInRequest.Count
                    };

                    context.Partner_products_request.Add(request);
                }

                // Сохраняем заявки
                context.SaveChanges();

                MessageBox.Show("Данные партнера и заявки успешно сохранены!");
            }
        }

        /// <summary>
        /// Обработка нажатия кнопки "Отмена".
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Обработка нажатия кнопки "Удалить заявку".
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить эту заявку? Это действие нельзя отменить.",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    DeletePartnerAndRequests();
                    DialogResult = true;
                    PartnerSaved?.Invoke(this, EventArgs.Empty);
                    Close();
                }
            }
            catch
            {
                MessageBox.Show("Ошибка при удалении заявки");
            }
        }

        /// <summary>
        /// Удаление партнера и всех его заявок из базы данных.
        /// </summary>
        private void DeletePartnerAndRequests()
        {
            using (var context = new Entities())
            {
                // Удаляем все связанные заявки
                var requests = context.Partner_products_request
                    .Where(r => r.ID_Partner == _partnerId)
                    .ToList();

                foreach (var request in requests)
                {
                    context.Partner_products_request.Remove(request);
                }

                // Удаляем самого партнера
                var partner = context.Partners.FirstOrDefault(p => p.ID == _partnerId);
                if (partner != null)
                {
                    context.Partners.Remove(partner);
                }

                context.SaveChanges();

                MessageBox.Show("Заявка и данные партнера успешно удалены!");
            }
        }
    }

    /// <summary>
    /// Модель продукта внутри заявки партнера.
    /// </summary>
    public class ProductInRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Count { get; set; }
        public double UnitCost { get; set; }
        public double TotalCost { get; set; }
    }
}
