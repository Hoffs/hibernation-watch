# HibernationWatch

`Client`/`Server` application combination, designed to execute google assistant commands for Windows power cycle events.

## Concept

`Client` sends a signal command to `Server` once it receives a suspend or resume event from Windows. `Server` is needed, because Windows gives very limited amount to act (less than 2 seconds). This is usually not enough to get a full google assistant command execution using gRPC, as it takes at least 1.5s. To also fit into Windows allowed time window, UDP is used to avoid doing TCP handshake as that has proven to be too slow as well. Limited "security" is achieved with a hardcoded key. Actions are also configured, to not allow arbitrary google assistant queries to be executed.

## Configuration

To configure, `Server` has to have google assistant client setup and device added. `Server` acts as a virtual device that "accepts" user input. Only one user can authenticate on behalf of the device. Authentication happens using provided url and copy-pasting query string back to the server application. 

- https://developers.google.com/assistant/sdk/guides/service/integrate


Requires:
- `client_secrets.json` with Google OAuth experted secrets file. Only for `Server`.
- `config.json` with application configuration.


### Available `config.json` options

Configuration options can be seen in `Config.cs` file.

- `Ip` - (string) IP address of server. Used by client only.
- `Port` - (int) port of server. Server binds to this port and client sends to this port.
- `Mode` - (string) mode of application, allowed values: `server`/`client`. `server` starts a `Server` part, `client` starts `Client` part.
- `DeviceModelId` - (string) Google registered device ID. Used by server only.
- `SecretKey` - (string) secret key used to "authenticate" UDP packet.
- `ActionResume` - (string) action that will be sent as a query to google assistant on resume. Used by server only.
- `ActionSuspend` - (string) action that will be sent as a query to google assistant on suspend. Used by server only.
- `Debug` - (bool) debug flag. If enabled, google assistant responses will be saved.