# USB safety console

## Wiring (Arduino Uno)
| Component | Connection |
|---|---|
| 10 kilohm potentiometer outer pins | Uno 5 V and GND |
| Potentiometer centre/wiper | A0 |
| Push button | D2 to GND; firmware uses INPUT_PULLUP |
| SG90 signal (usually orange/yellow) | D9 |
| SG90 supply (usually red) | Regulated external 5 V supply sized for servo stall current |
| SG90 ground (usually brown/black) | External supply GND and Uno GND |
| Optional indicator | Built-in D13 LED, or external LED with suitable series resistor |
| USB | Uno to Windows PC |

Use a common ground. Do not power the servo from a digital I/O pin. Avoid drawing servo stall current through the Uno USB/5 V rail; use a suitable separate regulated supply. Start with the horn disconnected, confirm the safe closed/open travel, then attach the miniature barrier so it cannot jam or pinch. Pins and angles are centralized at the top of the firmware (closed 10 degrees, open 90 degrees).

## Firmware and connection
Upload `Arduino/ForgottenVillageController/ForgottenVillageController.ino` using the Arduino Uno target and Servo library. Close the Arduino Serial Monitor before Unity connects. In the in-game Settings choose USB mode, select the actual COM port and press Connect. Inspector fields expose any other port name and baud rate; both sides default to 115200 baud. The transport uses Windows Mono/.NET Framework and reflective SerialPort access, with a clear disconnected status if unavailable.

## Protocol
ASCII, one message per line, LF terminator (CRLF accepted). Sensor messages are bounded and validated:

- `POT:0` through `POT:1023`: normalized to 0..1; mapped to 1..15 kilohms in the filter station.
- `BTN:1`: debounced pressed edge; `BTN:0`: released. Unity suppresses repeated pressed messages until release.
- `GATE:OPEN` / `GATE:CLOSE`: accepted firmware commands. Unity's gameplay path sends OPEN only after all final evidence conditions pass.
- `ACK:GATE:OPEN` / `ACK:GATE:CLOSE`: firmware command acknowledgment.
- `ERR:INVALID_COMMAND` / `ERR:TOO_LONG`: firmware rejects invalid/oversized commands.

Firmware samples the potentiometer every 40 ms and sends changes of at least three ADC counts. Button debounce is 35 ms. The servo starts closed. Duplicate gate commands are acknowledged without reissuing the same servo movement.

Unity reads and writes on a background worker with timeouts; incoming messages are consumed on the main thread. The input queue is bounded. Disconnect and application exit stop the worker and dispose the port. Failed connections never block the game. Gate validation and gate acknowledgment are separate states. A missing ACK produces a timeout message; the ending offers an explicit verified-command retry. It never repeatedly sends actuator commands every frame.

## Hardware-free test
Keep Simulation enabled. The Alchemist's +/- controls feed the same normalized potentiometer input path. The Technician's Simulate button invokes BTN:1 then BTN:0. Completing the final evidence sequence sends the simulated GATE:OPEN command and reports a clearly labelled simulated acknowledgment. No hardware is required to reach the ending.

New Game resets game progress and the virtual gate. It does not silently command a real servo to close. Reset the physical demonstration gate deliberately before replay (firmware restarts closed). Hardware ACKs are not physical position feedback.

## Verification boundary
Firmware has been written, but no Uno upload, serial electrical test, servo movement measurement or Arduino toolchain compilation has been claimed. Validate polarity, power, travel limits and COM-port operation on the actual demonstration setup.
