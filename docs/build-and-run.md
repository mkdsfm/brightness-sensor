# Сборка и запуск

## Firmware (ESP32-C3, Arduino `.ino`)

Требования:

- Arduino IDE 2.x
- Установленный пакет плат `esp32` (Espressif Systems)
- Подключённый ESP32-C3 по USB

Шаги:

1. Откройте файл `firmware/firmware-esp32c3.ino` в Arduino IDE.
2. Выберите плату ESP32-C3 в меню **Tools -> Board**.
3. Выберите COM-порт устройства в меню **Tools -> Port**.
4. Установите USB CDC on Boot  в меню **Tools -> Port**.
5. Нажмите **Upload** для прошивки.
6. Откройте **Serial Monitor** и выставьте скорость `115200`.

Ожидаемый вывод монитора: JSON-строки с полями `deviceId`, `sensorId`, `ts`, `value`.

## PC application (.NET)

Требования:

- Windows 10/11
- .NET SDK 10.0+

Подготовка:

1. Откройте `pc-app/appsettings.json`.
2. Укажите корректный `serial.portName` (например, `COM5`).
3. При необходимости настройте диапазон ADC, инверсию, EMA и гистерезис.

Запуск (из папки `pc-app/`):

```powershell
dotnet restore
dotnet run
```

Приложение читает COM-порт, вычисляет целевую яркость и устанавливает её через WMI для встроенного дисплея.

Важно: реализация в `pc-app/` предназначена только для Windows. Для других ОС нужно отдельное приложение, которое поддерживает тот же контракт обмена с устройством (JSON-строки по протоколу из `docs/protocol.md`).

