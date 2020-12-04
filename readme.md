# Oliver

Deploy management app

---

[Wikipedia. Remote control history](https://en.wikipedia.org/wiki/Remote_control#History)

```
In 1894, the first example of wirelessly controlling at a distance was during a demonstration by the British physicist Oliver Lodge, in which he made use of a Branly's coherer to make a mirror galvanometer move a beam of light when an electromagnetic wave was artificially generated.
```

---

## ToDo:

### Global features

- Unpack archive with binaries on target machine [*]
- Store and substitute configurations [+]
- Run custom scripts (.ps, .cmd, .sh) on target machine [* (.sh)]

### Server features

- Template Api
  - What to do [*]
  - How to do [*]
- Logging Api
  - View deploing log [+]
- Configuration Api
  - Common variables
  - Template variables [+]
  - Tenant-Environment variables [+]

### Client features

- Listen mode (ToDo)
- Pooling mode [+]
- Unpack archive with binaries [*]
- Substitute configurations [+]
- Run scripts [+]
- Log deployment to server Logging Api [+]

## Questions/Issues

- For now, if more than one client, execution will be executed only on one client

---

#### Legend:

- [*] - WIP
- [+] - Done
- [-] - Removed
