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
  - `Load()`
  - `Save()`
  - `Current` для доступа к текущим значениям
- Изменения применяются сразу при изменении полей.

## 3. Локализация
- Все строки UI в ресурсах Avalonia:
  - `Resources/Strings.en.axaml`
  - `Resources/Strings.ru.axaml`
- Переключение языка в рантайме.
- Fallback на English при отсутствии строки.

## 4. Принципы UX
- Настройки доступны в отдельном окне.
- Изменения применяются сразу.
- Уровень логирования выбирается в настройках и применяется без перезапуска.
- Версия приложения отображается в окне настроек.

