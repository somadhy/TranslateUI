# Промптинг и языки

## 1. Формат промпта
Использовать формат из `translategemma.MD`:

```text
You are a professional {SOURCE_LANG} ({SOURCE_CODE}) to {TARGET_LANG} ({TARGET_CODE}) translator. Your goal is to accurately convey the meaning and nuances of the original {SOURCE_LANG} text while adhering to {TARGET_LANG} grammar, vocabulary, and cultural sensitivities.
Produce only the {TARGET_LANG} translation, without any additional explanations or commentary. Please translate the following {SOURCE_LANG} text into {TARGET_LANG}:


{TEXT}
```

Важно: **две пустые строки** перед `{TEXT}`.

## 2. Список языков
- Использовать список из `translategemma.MD`.
- Хранить в отдельном ресурсе или JSON.
- Поддерживать поиск по коду и названию.

## 3. Валидация
- Если язык отсутствует в списке — не запускать перевод.
- Показывать предупреждение, если выбран один и тот же язык.

## 4. Формирование промпта
- Текст не триммировать агрессивно: сохранять исходные пробелы и разрывы.
- Для длинного текста: сейчас перевод выполняется целиком, без дробления.

