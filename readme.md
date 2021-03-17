# Oliver

Deploy management app

---

[Wikipedia. Remote control history](https://en.wikipedia.org/wiki/Remote_control#History)

> In 1894, the first example of wirelessly controlling at a distance was during a demonstration by the British physicist Oliver Lodge, in which he made use of a Branly's coherer to make a mirror galvanometer move a beam of light when an electromagnetic wave was artificially generated.

---

## ToDo:

### Global features

- [x] Unpack archive with binaries on target machine
- [x] Store and substitute configurations
- [x] Run custom scripts (`.ps1`, `.cmd`) on target machine
- [ ] Customizable packages lifecycle
- [ ] Assign **Instance** (tenant/environment) to client from API
- [ ] Administrator UI

### Server features
- Scaling
  - [ ] External database
  - [ ] External queue
- Logging deployment process
  - [x] View deploing log through API request
- Configuration/Variables
  - [ ] Common variables
  - [x] Template variables
  - [x] Tenant-Environment variables

### Client features

- [ ] Listen mode (maybe `grpc`?)
- [x] Pooling mode
- [x] Unpack archive with binaries
- [x] Substitute configurations
- [x] Run scripts
- [x] Send deployment process log to server Logging Api
- [ ] Self update

## Questions/Issues

- [ ] For now, if more than one client, execution will be executed only on one client
- [ ] Maybe `.sh` support?
- [ ] Different transport (brocker) support
  - [x] File system
  - [ ] Rabbit
  - [ ] Kaffka
  - [ ] ServiceBus
  - [ ] else?
- [ ] Different database providers
  - [x] LiteDb
  - [ ] MSSql
  - [ ] Postgres
  - [ ] MongoDb
  - [ ] Mysql?
  - [ ] else?
