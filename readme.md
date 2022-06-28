# Oliver

_Oliver_ is multi-tenant deploy management app.

Check [ToDo](#todo) for currently available features.

---

Named in memory of [Oliver Lodge](https://en.wikipedia.org/wiki/Oliver_Lodge#Accomplishments)

---

## ToDo:

### Global features

- [x] Unpack archive with binaries on target machine
- [x] Store and substitute configurations
- [x] Run custom scripts (`.ps1`, `.cmd`) on target machine
- [ ] Linux scripts (`.sh`)
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
- [x] Maybe `.sh` support? (added to future features)
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
- [ ] Fix logging formatters
