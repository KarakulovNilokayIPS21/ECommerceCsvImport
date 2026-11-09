namespace ECommerceCsvImport.Services;

internal static class ImportConstants
{
    internal static class ImportMessages
    {
        public const string FileNotFound = "Файл не найден";
        public const string NoData = "Данные для импорта отсутствуют";
        public const string InvalidHeaders = "Неверные заголовки CSV";
        public const string WrongFieldCount = "Неверное количество полей в строке";
        public const string EmptySku = "Поле sku не может быть пустым";
        public const string EmptyName = "Поле name не может быть пустым";
        public const string InvalidPrice = "Поле price должно быть числом больше 0";
        public const string InvalidStock = "Остаток на складе должен быть целым числом не меньше 0";
        public const string InvalidIsActive = "Поле is_active должно иметь значение true, false, 1 или 0";
        public const string DuplicateSku = "Артикул товара уже существует";
        public const string EmptyBrand = "Поле brand обязательно для варианта «Электроника»";
        public const string InvalidWarranty = "Гарантия должна быть целым числом от 0 до 60 месяцев";
        public const string FileReadFailed = "Не удалось прочитать файл";
    }

    internal static class ImportDefaults
    {
        public const string Currency = "RUB";
        public const string CategoryName = "Без категории";
        public const int MinStock = 0;
        public const int MinWarrantyMonths = 0;
        public const int MaxWarrantyMonths = 60;
        public const int NoRowNumber = 0;
        public const int ByteOrderMarkCode = 0xFEFF;
        public const char ByteOrderMark = (char)ByteOrderMarkCode;
    }

    internal static class CsvColumns
    {
        public const string Sku = "sku";
        public const string Name = "name";
        public const string Description = "description";
        public const string Price = "price";
        public const string Currency = "currency";
        public const string CategoryName = "category_name";
        public const string Stock = "stock";
        public const string ImageUrl = "image_url";
        public const string IsActive = "is_active";
        public const string Brand = "brand";
        public const string WarrantyMonths = "warranty_months";
        public const string ModelNumber = "model_number";

        public static readonly string[] Required =
        [
            Sku,
            Name,
            Description,
            Price,
            Currency,
            CategoryName,
            Stock,
            ImageUrl,
            IsActive,
            Brand,
            WarrantyMonths
        ];
    }
}
