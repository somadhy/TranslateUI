# Настройки и локализация

## 1. Хранение настроек
- Формат: JSON в `%AppData%/TranslateUI/settings.json`.
- Схема:
  - `UiLanguage` (en/ru)
  - `DefaultSourceLang`
  - `DefaultTargetLang`
  - `OllamaUrl`
  - `DefaultModel`
  - `LogLevel` (Off/Debug/Info/Warn/Error)

## 2. Сервис настроек
- `ISettingsService`:
  - `LoadAsync()`
  - `SaveAsync(settings)`
  - `Get/Set` текущих значений
- Валидация перед сохранением (пустые поля, неверный URL).

## 3. Локализация
- Все строки UI в ресурсах Avalonia:
  - `Resources/Strings.en.axaml`
  - `Resources/Strings.ru.axaml`
- Переключение языка в рантайме.
- Fallback на English при отсутствии строки.

## 4. Принципы UX
- Настройки доступны в отдельном окне или боковой панели.
- Изменения применяются сразу, либо по кнопке “Сохранить”.
 - Уровень логирования выбирается в настройках и применяется без перезапуска.

