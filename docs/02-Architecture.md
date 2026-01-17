# Архитектура

## 1. Общий подход
Архитектура на базе MVVM с четким разделением UI, бизнес-логики и инфраструктуры.

**Слои:**
- **Presentation**: Avalonia Views + ViewModels.
- **Application**: сервисы перевода, настройки, локализация.
- **Domain**: модели и интерфейсы (языки, задания перевода, результаты).
- **Infrastructure**: Ollama HTTP-клиент, файловые обработчики, хранилище настроек.

## 2. Основные модули
### 2.1 Translation Pipeline
- `TranslationRequest` (SourceText, SourceLang, TargetLang, Model, Options).
- `TranslationResult` (Text, Warnings, Diagnostics).
- `ITranslationService`: принимает запрос и возвращает результат.
- `PromptBuilder`: формирует промпт согласно `translategemma.MD`.

### 2.2 Ollama Integration
- `IOllamaClient` (list, show, pull, generate).
- `OllamaClient` использует `HttpClient` с таймаутами и отменой.
- Проверка доступности сервиса и моделей.

### 2.3 File Translation
- `IFileHandler` с методами `CanHandle`, `ExtractText`, `BuildOutput`.
- `FileTranslationService`: выбирает обработчик по расширению.
- Стратегия позволяет легко добавлять форматы.

### 2.4 Settings & Localization
- `ISettingsService`: загрузка/сохранение JSON.
- `ILocalizationService`: загрузка ресурсов RU/EN и горячее переключение.

## 3. Поток перевода
1. Пользователь вводит/загружает текст.
2. Выбирает языки и модель.
3. Нажимает “Перевод”.
4. `TranslationService` создает промпт и вызывает `OllamaClient.Generate`.
5. Результат отображается, предлагается сохранить.

## 4. DI и модульность
- `Microsoft.Extensions.DependencyInjection`.
- Регистрировать все сервисы в `App` или `Program`.
- ViewModels получают зависимости через конструктор.

## 5. Ошибки и устойчивость
- Ошибки сети показывать как уведомления, не падать.
- Разделять “нет модели”, “нет сервиса”, “ошибка формата”.
- Логировать ошибки в файл (опционально).

