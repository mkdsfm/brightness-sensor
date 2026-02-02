# brightness-sensor

Комплект из прошивки (ESP32-C3) и простого Windows-консольного приложения для автоматической регулировки яркости встроенного дисплея по датчику освещённости.

## Состав

- `firmware/` — PlatformIO-проект для ESP32-C3.
- `pc-app/` — .NET-приложение только для Windows: читает JSON из COM-порта и управляет яркостью через WMI.
- `docs/` — подключение, протокол и инструкции запуска.
- `appsettings.example.json` — пример конфигурации для ПК-приложения.

## Быстрый старт

1. Соберите и прошейте контроллер: см. `docs/build-and-run.md`.
2. Подключите датчик: см. `docs/wiring.md`.
3. Скопируйте `appsettings.example.json` в `pc-app/appsettings.json` и поправьте `serial.portName`.
4. Запустите ПК-приложение из `pc-app/`.

Важно: текущая версия ПК-приложения поддерживает только Windows. Для Linux/macOS потребуется отдельное приложение, которое сохраняет тот же контракт общения с устройством (тот же JSON-протокол из `docs/protocol.md`).

### Структура `pc-app/`

- `pc-app/Program.cs` — точка входа.
- `pc-app/Application/` — основной сценарий работы приложения.
- `pc-app/Configuration/` — загрузка и валидация `appsettings.json`.
- `pc-app/Protocol/` — парсер JSON-сообщений от устройства.
- `pc-app/Platform/Windows/` — Windows-специфичная работа с яркостью (WMI).

## Что ожидает конфиг

ПК-приложение ожидает файл `pc-app/appsettings.json` со следующими секциями:

- `serial`
  - `portName` — COM-порт ESP32 (например, `COM5`), обязательный.
  - `baudRate` — скорость порта, обычно `115200`.
- `processing`
  - `adcMin` и `adcMax` — рабочий диапазон ADC (обязательно `adcMax > adcMin`).
  - `invert` — инверсия шкалы (`true/false`).
  - `emaAlpha` — коэффициент EMA в диапазоне `(0; 1]`.
  - `hysteresisPercent` — минимальный шаг изменения яркости в процентах (`0..100`).
- `brightness`
  - `minPercent` и `maxPercent` — границы итоговой яркости (`0..100`, `min <= max`).

Пример см. в `appsettings.example.json`.

## Формат телеметрии

Каждое измерение отправляется одной JSON-строкой, например:

`{"deviceId":"esp32c3-01","sensorId":"light0","ts":1234567,"value":1872}`

Подробности: `docs/protocol.md`.
